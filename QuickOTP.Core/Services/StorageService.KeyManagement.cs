using System.Globalization;
using System.Security.Cryptography;
using QuickOTP.Core.Configuration;

namespace QuickOTP.Core.Services;

public partial class StorageService
{
    private byte[] LoadOrCreateKey( )
    {
        var masterPassword = _options.MasterPassword;
        var keyFile = ReadKeyFile( );

        if ( !string.IsNullOrEmpty( masterPassword ) )
        {
            return LoadOrCreatePasswordProtectedKey( masterPassword, keyFile );
        }

        return LoadOrCreatePlatformKey( keyFile );
    }

    private byte[] LoadOrCreatePasswordProtectedKey( string masterPassword, KeyFileContents? keyFile )
    {
        if ( keyFile != null )
        {
            if ( keyFile.Mode == KeyFileModePbkdf2 )
            {
                return DecryptKeyWithPassword( keyFile, masterPassword );
            }

            var migratedKey = UnwrapKeyFromFile( keyFile );
            WriteKeyFilePassword( migratedKey, masterPassword );
            _keychain?.TryDeleteKey( );
            return migratedKey;
        }

        if ( _keychain != null && _keychain.TryReadKey( out var keyFromKeychain ) )
        {
            WriteKeyFilePassword( keyFromKeychain, masterPassword );
            _keychain.TryDeleteKey( );
            return keyFromKeychain;
        }

        var generatedKey = RandomNumberGenerator.GetBytes( KeySizeBytes );
        WriteKeyFilePassword( generatedKey, masterPassword );
        _keychain?.TryDeleteKey( );

        return generatedKey;
    }

    private byte[] LoadOrCreatePlatformKey( KeyFileContents? keyFile )
    {
        if ( keyFile != null && keyFile.Mode == KeyFileModePbkdf2 )
        {
            throw new Exception( $"Master password required to unlock storage. Set {AppConstants.Env.MasterPassword} or {AppConstants.Env.MasterPasswordFile}." );
        }

        if ( keyFile != null )
        {
            var fileKey = UnwrapKeyFromFile( keyFile );
            _keychain?.TryWriteKey( fileKey );
            return fileKey;
        }

        if ( _keychain != null && _keychain.TryReadKey( out var keyFromKeychain ) )
        {
            return keyFromKeychain;
        }

        var generatedKey = RandomNumberGenerator.GetBytes( KeySizeBytes );
        if ( _keychain != null && _keychain.TryWriteKey( generatedKey ) )
        {
            return generatedKey;
        }

        WriteKeyFileFallback( generatedKey );
        return generatedKey;
    }

    private KeyFileContents? ReadKeyFile( )
    {
        if ( !File.Exists( _keyFile ) )
        {
            return null;
        }

        var content = File.ReadAllText( _keyFile ).Trim( );
        if ( content.StartsWith( KeyFilePrefixV2, StringComparison.Ordinal ) )
        {
            return ParseKeyFileV2( content.Substring( KeyFilePrefixV2.Length ) );
        }

        if ( content.StartsWith( KeyFilePrefixV1, StringComparison.Ordinal ) )
        {
            return ParseKeyFileV1( content.Substring( KeyFilePrefixV1.Length ) );
        }

        throw new Exception( "Key file format is not recognized." );
    }

    private KeyFileContents ParseKeyFileV1( string payload )
    {
        var parts = payload.Split( ':', 2, StringSplitOptions.RemoveEmptyEntries );
        if ( parts.Length != 2 )
        {
            throw new Exception( "Key file format is invalid." );
        }

        return new KeyFileContents
        {
            Mode = parts[0],
            Payload = Convert.FromBase64String( parts[1] )
        };
    }

    private KeyFileContents ParseKeyFileV2( string payload )
    {
        var parts = payload.Split( ':', StringSplitOptions.RemoveEmptyEntries );
        if ( parts.Length < 2 )
        {
            throw new Exception( "Key file format is invalid." );
        }

        var mode = parts[0];
        if ( mode == KeyFileModePbkdf2 )
        {
            return ParsePbkdf2KeyFile( parts, mode );
        }

        if ( mode == KeyFileModeRaw || mode == KeyFileModeDpapi )
        {
            return ParseRawOrDpapiKeyFile( parts, mode );
        }

        throw new Exception( $"Unsupported key file mode: {mode}" );
    }

    private static KeyFileContents ParsePbkdf2KeyFile( string[] parts, string mode )
    {
        if ( parts.Length != 6 )
        {
            throw new Exception( "Key file format is invalid." );
        }

        if ( !int.TryParse( parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var iterations ) )
        {
            throw new Exception( "Key file format is invalid." );
        }

        return new KeyFileContents
        {
            Mode = mode,
            Iterations = iterations,
            Salt = Convert.FromBase64String( parts[2] ),
            Nonce = Convert.FromBase64String( parts[3] ),
            Tag = Convert.FromBase64String( parts[4] ),
            Ciphertext = Convert.FromBase64String( parts[5] )
        };
    }

    private static KeyFileContents ParseRawOrDpapiKeyFile( string[] parts, string mode )
    {
        if ( parts.Length != 2 )
        {
            throw new Exception( "Key file format is invalid." );
        }

        return new KeyFileContents
        {
            Mode = mode,
            Payload = Convert.FromBase64String( parts[1] )
        };
    }

    private byte[] UnwrapKeyFromFile( KeyFileContents keyFile ) =>
        keyFile.Mode switch
        {
            KeyFileModeRaw => keyFile.Payload,
            KeyFileModeDpapi when OperatingSystem.IsWindows( ) =>
                ProtectedData.Unprotect( keyFile.Payload, _entropy, DataProtectionScope.CurrentUser ),
            KeyFileModeDpapi =>
                throw new Exception( "Key file is protected for Windows and cannot be used on this OS." ),
            KeyFileModePbkdf2 =>
                throw new Exception( $"Master password required to unlock storage. Set {AppConstants.Env.MasterPassword} or {AppConstants.Env.MasterPasswordFile}." ),
            _ => throw new Exception( $"Unsupported key file mode: {keyFile.Mode}" )
        };

    private void WriteKeyFilePassword( byte[] key, string password )
    {
        var iterations = _options.Pbkdf2Iterations;
        var content = EncryptKeyWithPassword( key, password, iterations );

        File.WriteAllText( _keyFile, content );
        TryRestrictFilePermissions( _keyFile );
    }

    private void WriteKeyFileFallback( byte[] key )
    {
        var mode = KeyFileModeRaw;
        var storedKey = key;

        if ( OperatingSystem.IsWindows( ) )
        {
            try
            {
                storedKey = ProtectedData.Protect( key, _entropy, DataProtectionScope.CurrentUser );
                mode = KeyFileModeDpapi;
            }
            catch
            {
                storedKey = key;
                mode = KeyFileModeRaw;
            }
        }

        var content = $"{KeyFilePrefixV2}{mode}:{Convert.ToBase64String( storedKey )}";
        File.WriteAllText( _keyFile, content );
        TryRestrictFilePermissions( _keyFile );
    }

    private string EncryptKeyWithPassword( byte[] key, string password, int iterations )
    {
        var salt = RandomNumberGenerator.GetBytes( SaltSizeBytes );
        var derivedKey = DeriveKeyFromPassword( password, salt, iterations );
        var nonce = RandomNumberGenerator.GetBytes( NonceSizeBytes );
        var ciphertext = new byte[key.Length];
        var tag = new byte[TagSizeBytes];

        using ( var aes = new AesGcm( derivedKey, TagSizeBytes ) )
        {
            aes.Encrypt( nonce, key, ciphertext, tag, _entropy );
        }

        return $"{KeyFilePrefixV2}{KeyFileModePbkdf2}:{iterations.ToString( CultureInfo.InvariantCulture )}:"
               + $"{Convert.ToBase64String( salt )}:{Convert.ToBase64String( nonce )}:"
               + $"{Convert.ToBase64String( tag )}:{Convert.ToBase64String( ciphertext )}";
    }

    private byte[] DecryptKeyWithPassword( KeyFileContents keyFile, string password )
    {
        try
        {
            var derivedKey = DeriveKeyFromPassword( password, keyFile.Salt, keyFile.Iterations );
            var plaintext = new byte[keyFile.Ciphertext.Length];

            using ( var aes = new AesGcm( derivedKey, TagSizeBytes ) )
            {
                aes.Decrypt( keyFile.Nonce, keyFile.Ciphertext, keyFile.Tag, plaintext, _entropy );
            }

            return plaintext;
        }
        catch ( CryptographicException ex )
        {
            throw new Exception( "Master password did not unlock the storage key.", ex );
        }
    }

    private static byte[] DeriveKeyFromPassword( string password, byte[] salt, int iterations )
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            KeySizeBytes );
    }

    private static void TryRestrictFilePermissions( string path )
    {
        if ( OperatingSystem.IsWindows( ) )
        {
            return;
        }

        try
        {
            File.SetUnixFileMode( path, UnixFileMode.UserRead | UnixFileMode.UserWrite );
        }
        catch
        {
            // Best-effort; permissions may not be supported on all filesystems.
        }
    }

    private sealed class KeyFileContents
    {
        public string Mode { get; init; } = string.Empty;
        public byte[] Payload { get; init; } = [];
        public int Iterations { get; init; }
        public byte[] Salt { get; init; } = [];
        public byte[] Nonce { get; init; } = [];
        public byte[] Tag { get; init; } = [];
        public byte[] Ciphertext { get; init; } = [];
    }
}

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace QuickOTP.Core.Services;

public partial class TwoFasImportService
{
    private const int Pbkdf2Iterations = 10_000;
    private const int AesGcmTagSizeBytes = 16;

    private bool IsEncrypted( string content )
    {
        try
        {
            if ( content.StartsWith( "{" ) )
            {
                return false;
            }

            var bytes = Convert.FromBase64String( content.Trim( ) );
            return bytes.Length > 0 && !Encoding.UTF8.GetString( bytes ).StartsWith( "{" );
        }
        catch
        {
            return false;
        }
    }

    private string Decrypt2FasBackup( string encryptedContent, string password )
    {
        try
        {
            var encryptedBytes = Convert.FromBase64String( encryptedContent.Trim( ) );

            using ( var aes = Aes.Create( ) )
            {
                var salt = new byte[16];
                Array.Copy( encryptedBytes, 0, salt, 0, 16 );

                aes.Key = DeriveBackupKey( password, salt );

                aes.IV = new byte[16];
                Array.Copy( encryptedBytes, 16, aes.IV, 0, 16 );

                using ( var decryptor = aes.CreateDecryptor( ) )
                {
                    var cipherText = new byte[encryptedBytes.Length - 32];
                    Array.Copy( encryptedBytes, 32, cipherText, 0, cipherText.Length );

                    var decryptedBytes = decryptor.TransformFinalBlock( cipherText, 0, cipherText.Length );
                    return Encoding.UTF8.GetString( decryptedBytes );
                }
            }
        }
        catch ( Exception ex )
        {
            throw new Exception( $"Failed to decrypt backup. Wrong password or corrupted file: {ex.Message}", ex );
        }
    }

    private bool IsOfficialEncryptedBackupEnvelope( string content )
    {
        try
        {
            using var document = JsonDocument.Parse( content );
            return TryReadServicesEncrypted( document.RootElement, out var servicesEncrypted )
                   && !string.IsNullOrWhiteSpace( servicesEncrypted );
        }
        catch ( JsonException )
        {
            return false;
        }
    }

    private string DecryptOfficial2FasBackupEnvelope( string fileContent, string password )
    {
        try
        {
            using var document = JsonDocument.Parse( fileContent );
            if ( !TryReadServicesEncrypted( document.RootElement, out var servicesEncrypted )
                 || string.IsNullOrWhiteSpace( servicesEncrypted ) )
            {
                throw new Exception( "Missing servicesEncrypted field." );
            }

            var parts = servicesEncrypted.Split( ':', 3 );
            if ( parts.Length != 3 )
            {
                throw new Exception( "Invalid servicesEncrypted field format." );
            }

            var cipherTextWithTag = Convert.FromBase64String( parts[0] );
            var salt = Convert.FromBase64String( parts[1] );
            var iv = Convert.FromBase64String( parts[2] );

            if ( cipherTextWithTag.Length <= AesGcmTagSizeBytes )
            {
                throw new Exception( "Encrypted payload is too short." );
            }

            var cipherText = cipherTextWithTag[..^AesGcmTagSizeBytes];
            var tag = cipherTextWithTag[^AesGcmTagSizeBytes..];
            var key = DeriveOfficialBackupKey( password, salt );
            var plainBytes = new byte[cipherText.Length];

            using var aes = new AesGcm( key, AesGcmTagSizeBytes );
            aes.Decrypt( iv, cipherText, tag, plainBytes );

            return Encoding.UTF8.GetString( plainBytes );
        }
        catch ( Exception ex ) when ( ex is CryptographicException or FormatException or JsonException )
        {
            throw new Exception( $"Failed to decrypt backup. Wrong password or corrupted file: {ex.Message}", ex );
        }
    }

    private static bool TryReadServicesEncrypted( JsonElement root, out string? value )
    {
        value = null;
        if ( root.ValueKind != JsonValueKind.Object )
        {
            return false;
        }

        foreach ( var property in root.EnumerateObject( ) )
        {
            if ( !string.Equals( property.Name, "servicesEncrypted", StringComparison.OrdinalIgnoreCase ) )
            {
                continue;
            }

            if ( property.Value.ValueKind != JsonValueKind.String )
            {
                return false;
            }

            value = property.Value.GetString( );
            return true;
        }

        return false;
    }

    private string Encrypt2FasBackup( string content, string password )
    {
        using ( var aes = Aes.Create( ) )
        {
            var salt = GenerateSalt( );

            aes.Key = DeriveBackupKey( password, salt );

            aes.GenerateIV( );

            using ( var encryptor = aes.CreateEncryptor( ) )
            {
                var plainBytes = Encoding.UTF8.GetBytes( content );
                var cipherBytes = encryptor.TransformFinalBlock( plainBytes, 0, plainBytes.Length );

                var result = new byte[salt.Length + aes.IV.Length + cipherBytes.Length];
                Array.Copy( salt, 0, result, 0, salt.Length );
                Array.Copy( aes.IV, 0, result, salt.Length, aes.IV.Length );
                Array.Copy( cipherBytes, 0, result, salt.Length + aes.IV.Length, cipherBytes.Length );

                return Convert.ToBase64String( result );
            }
        }
    }

    private static byte[] GenerateSalt( )
    {
        var salt = new byte[16];
        using ( var rng = RandomNumberGenerator.Create( ) )
        {
            rng.GetBytes( salt );
        }

        return salt;
    }

    private static byte[] DeriveBackupKey( string password, byte[] salt )
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Pbkdf2Iterations,
            HashAlgorithmName.SHA256,
            32 );
    }

    private static byte[] DeriveOfficialBackupKey( string password, byte[] salt )
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Pbkdf2Iterations,
            HashAlgorithmName.SHA256,
            32 );
    }
}

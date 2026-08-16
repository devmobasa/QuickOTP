using System.Security.Cryptography;
using System.Text;

namespace QuickOTP.Core.Services;

public partial class StorageService
{
    private string EncryptData( string plainText )
    {
        try
        {
            var plaintextBytes = Encoding.UTF8.GetBytes( plainText );
            var nonce = RandomNumberGenerator.GetBytes( NonceSizeBytes );
            var ciphertext = new byte[plaintextBytes.Length];
            var tag = new byte[TagSizeBytes];

            using ( var aes = new AesGcm( _key, TagSizeBytes ) )
            {
                aes.Encrypt( nonce, plaintextBytes, ciphertext, tag, _entropy );
            }

            var payload = CombinePayload( nonce, tag, ciphertext );
            return EncryptionPrefix + Convert.ToBase64String( payload );
        }
        catch ( Exception ex )
        {
            throw new Exception( $"Encryption failed: {ex.Message}", ex );
        }
    }

    private string DecryptData( string encryptedText )
    {
        try
        {
            if ( encryptedText.StartsWith( EncryptionPrefix, StringComparison.Ordinal ) )
            {
                return DecryptModernPayload( encryptedText );
            }

            return TryDecodeLegacyContent( encryptedText );
        }
        catch ( Exception ex )
        {
            throw new Exception( $"Decryption failed: {ex.Message}", ex );
        }
    }

    private byte[] CombinePayload( byte[] nonce, byte[] tag, byte[] ciphertext )
    {
        var payload = new byte[nonce.Length + tag.Length + ciphertext.Length];

        Buffer.BlockCopy( nonce, 0, payload, 0, nonce.Length );
        Buffer.BlockCopy( tag, 0, payload, nonce.Length, tag.Length );
        Buffer.BlockCopy( ciphertext, 0, payload, nonce.Length + tag.Length, ciphertext.Length );

        return payload;
    }

    private string DecryptModernPayload( string encryptedText )
    {
        var payload = Convert.FromBase64String( encryptedText.Substring( EncryptionPrefix.Length ) );
        if ( payload.Length < NonceSizeBytes + TagSizeBytes )
        {
            throw new Exception( "Encrypted payload is too short." );
        }

        var nonce = payload.Take( NonceSizeBytes ).ToArray( );
        var tag = payload.Skip( NonceSizeBytes ).Take( TagSizeBytes ).ToArray( );
        var ciphertext = payload.Skip( NonceSizeBytes + TagSizeBytes ).ToArray( );

        var plaintext = new byte[ciphertext.Length];
        using ( var aes = new AesGcm( _key, TagSizeBytes ) )
        {
            aes.Decrypt( nonce, ciphertext, tag, plaintext, _entropy );
        }

        return Encoding.UTF8.GetString( plaintext );
    }

    private string TryDecodeLegacyContent( string encryptedText )
    {
        try
        {
            var decoded = Encoding.UTF8.GetString( Convert.FromBase64String( encryptedText ) );
            if ( LooksLikeJson( decoded ) )
            {
                return decoded;
            }
        }
        catch
        {
            // Ignore base64 decoding errors; fall back to raw content.
        }

        return encryptedText;
    }

    private static bool LooksLikeJson( string value )
    {
        var trimmed = value.TrimStart( );
        return trimmed.StartsWith( "{", StringComparison.Ordinal )
               || trimmed.StartsWith( "[", StringComparison.Ordinal );
    }
}

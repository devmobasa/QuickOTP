using OtpNet;
using QuickOTP.Core.Configuration;
using QuickOTP.Core.Models;

namespace QuickOTP.Core.Services;

public partial class TotpService
{
    public string GenerateTotp( TotpAccount account )
    {
        return GenerateTotpAt( account, DateTimeOffset.UtcNow );
    }

    public string GenerateTotpAt( TotpAccount account, DateTimeOffset timestamp )
    {
        try
        {
            var secretBytes = ParseSecretBytes( account.Secret );
            var totp = CreateTotpGenerator( account, secretBytes );

            account.LastUsed = timestamp.UtcDateTime;
            return totp.ComputeTotp( timestamp.UtcDateTime );
        }
        catch ( Exception ex )
        {
            throw new Exception( $"Failed to generate TOTP for {account.Name}: {ex.Message}", ex );
        }
    }

    public int GetRemainingSeconds( int period = AppConstants.Otp.DefaultPeriod )
    {
        var epoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds( );
        return period - (int)(epoch % period);
    }

    public string ParseOtpAuthUri( string uri )
    {
        try
        {
            ValidateOtpAuthUri( uri );

            var uriObj = new Uri( uri );
            var query = System.Web.HttpUtility.ParseQueryString( uriObj.Query );
            return GetRequiredSecretFromQuery( query );
        }
        catch ( Exception ex )
        {
            throw new Exception( $"Failed to parse OTP auth URI: {ex.Message}", ex );
        }
    }

    private static byte[] ParseSecretBytes( string secret )
    {
        return Base32Encoding.ToBytes( secret.Replace( " ", "" ) );
    }

    private Totp CreateTotpGenerator( TotpAccount account, byte[] secretBytes )
    {
        return new Totp(
            secretBytes,
            account.Period,
            GetOtpHashMode( account.Algorithm ),
            account.Digits
        );
    }

    private static void ValidateOtpAuthUri( string uri )
    {
        if ( uri.StartsWith( AppConstants.OtpAuth.TotpPrefix, StringComparison.OrdinalIgnoreCase )
             || uri.StartsWith( AppConstants.OtpAuth.HotpPrefix, StringComparison.OrdinalIgnoreCase ) )
        {
            return;
        }

        throw new ArgumentException( "Invalid OTP auth URI" );
    }

    private static string GetRequiredSecretFromQuery( System.Collections.Specialized.NameValueCollection query )
    {
        var secret = query[AppConstants.OtpAuth.SecretParameter];
        if ( string.IsNullOrEmpty( secret ) )
        {
            throw new ArgumentException( "Secret not found in URI" );
        }

        return secret;
    }

    private OtpHashMode GetOtpHashMode( string algorithm ) =>
        algorithm?.ToUpper( ) switch
        {
            AppConstants.Otp.Sha1 => OtpHashMode.Sha1,
            AppConstants.Otp.Sha256 => OtpHashMode.Sha256,
            AppConstants.Otp.Sha512 => OtpHashMode.Sha512,
            _ => OtpHashMode.Sha1
        };
}

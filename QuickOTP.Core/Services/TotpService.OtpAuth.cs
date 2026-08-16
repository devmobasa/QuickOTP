using System.Globalization;
using QuickOTP.Core.Configuration;
using QuickOTP.Core.Models;

namespace QuickOTP.Core.Services;

public partial class TotpService
{
    public TotpAccount ParseAccountFromOtpAuthUri( string uri )
    {
        try
        {
            ValidateTotpAuthUri( uri );

            var uriObj = new Uri( uri );
            var (issuerFromLabel, accountName) = ParseUriLabel( uriObj );
            var query = System.Web.HttpUtility.ParseQueryString( uriObj.Query );

            return new TotpAccount
            {
                Name = accountName,
                Issuer = ReadQueryOrDefault( query, AppConstants.OtpAuth.IssuerParameter, issuerFromLabel ),
                Secret = NormalizeSecret( ReadRequiredQuery( query, AppConstants.OtpAuth.SecretParameter ) ),
                Algorithm = ReadQueryOrDefault( query, AppConstants.OtpAuth.AlgorithmParameter, AppConstants.Otp.Sha1 ),
                Digits = ReadIntOrDefault( query, AppConstants.OtpAuth.DigitsParameter, AppConstants.Otp.DefaultDigits ),
                Period = ReadIntOrDefault( query, AppConstants.OtpAuth.PeriodParameter, AppConstants.Otp.DefaultPeriod )
            };
        }
        catch ( Exception ex )
        {
            throw new Exception( $"Failed to parse OTP auth URI: {ex.Message}", ex );
        }
    }

    private static (string Issuer, string AccountName) ParseUriLabel( Uri uri )
    {
        var label = Uri.UnescapeDataString( uri.AbsolutePath.TrimStart( '/' ) );
        var separatorIndex = label.IndexOf( ':' );
        if ( separatorIndex < 0 )
        {
            return (AppConstants.Display.Unknown, label);
        }

        var issuer = label[..separatorIndex];
        var accountName = label[(separatorIndex + 1)..];
        return (
            string.IsNullOrWhiteSpace( issuer ) ? AppConstants.Display.Unknown : issuer,
            accountName );
    }

    private static void ValidateTotpAuthUri( string uri )
    {
        if ( uri.StartsWith( AppConstants.OtpAuth.HotpPrefix, StringComparison.OrdinalIgnoreCase ) )
        {
            throw new ArgumentException( "HOTP URIs are not supported. Use a TOTP (otpauth://totp/) link." );
        }

        if ( !uri.StartsWith( AppConstants.OtpAuth.TotpPrefix, StringComparison.OrdinalIgnoreCase ) )
        {
            throw new ArgumentException( "Invalid OTP auth URI" );
        }
    }

    private static string ReadRequiredQuery( System.Collections.Specialized.NameValueCollection query, string key )
    {
        var value = query[key];
        if ( string.IsNullOrWhiteSpace( value ) )
        {
            throw new ArgumentException( $"{key} not found in URI" );
        }

        return value;
    }

    private static string ReadQueryOrDefault(
        System.Collections.Specialized.NameValueCollection query,
        string key,
        string fallback )
    {
        var value = query[key];
        return string.IsNullOrWhiteSpace( value ) ? fallback : value;
    }

    private static int ReadIntOrDefault(
        System.Collections.Specialized.NameValueCollection query,
        string key,
        int fallback )
    {
        var value = query[key];
        return int.TryParse( value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed )
            ? parsed
            : fallback;
    }

    private static string NormalizeSecret( string secret ) =>
        secret.Replace( " ", string.Empty ).ToUpperInvariant( );
}

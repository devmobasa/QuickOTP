using System.Globalization;
using QuickOTP.Core.Configuration;
using QuickOTP.Core.Models;

namespace QuickOTP.Editor;

internal static class CodeFormatter
{
    public static string Format( string code )
    {
        if ( code.Length == 6 )
        {
            return $"{code[..3]} {code[3..]}";
        }

        if ( code.Length == 8 )
        {
            return $"{code[..4]} {code[4..]}";
        }

        return code;
    }

    public static string Initials( string issuer )
    {
        var trimmed = string.IsNullOrWhiteSpace( issuer ) ? AppConstants.Display.Unknown : issuer.Trim( );
        return trimmed[..1].ToUpperInvariant( );
    }

    public static string DisplayName( TotpAccount account )
    {
        var issuer = string.IsNullOrWhiteSpace( account.Issuer ) ? AppConstants.Display.Unknown : account.Issuer.Trim( );
        var name = string.IsNullOrWhiteSpace( account.Name ) ? AppConstants.Display.DefaultAccount : account.Name.Trim( );
        return $"{issuer} · {name}";
    }

    public static string RemainingLabel( int seconds ) =>
        seconds.ToString( CultureInfo.InvariantCulture ) + "s";
}

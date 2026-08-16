using System.Text.Json;
using QuickOTP.Core.Models;

namespace QuickOTP.Core.Serialization;

internal static class AppJson
{
    public static string SerializeAccounts( List<TotpAccount> accounts ) =>
        JsonSerializer.Serialize( accounts, QuickOTPJsonContext.Default.ListTotpAccount );

    public static List<TotpAccount>? DeserializeAccounts( string json ) =>
        JsonSerializer.Deserialize( json, QuickOTPJsonContext.Default.ListTotpAccount );

    public static string SerializeBackup( TwoFasBackup backup ) =>
        JsonSerializer.Serialize( backup, QuickOTPJsonContext.Default.TwoFasBackup );

    public static TwoFasBackup? DeserializeBackup( string json ) =>
        JsonSerializer.Deserialize( json, QuickOTPJsonContext.Default.TwoFasBackup );

    public static List<TwoFasService>? DeserializeServices( string json ) =>
        JsonSerializer.Deserialize( json, QuickOTPJsonContext.Default.ListTwoFasService );
}

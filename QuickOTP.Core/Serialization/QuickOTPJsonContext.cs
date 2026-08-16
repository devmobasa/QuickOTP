using System.Text.Json.Serialization;
using QuickOTP.Core.Models;

namespace QuickOTP.Core.Serialization;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    WriteIndented = true )]
[JsonSerializable( typeof( TotpAccount ) )]
[JsonSerializable( typeof( List<TotpAccount> ) )]
[JsonSerializable( typeof( TwoFasBackup ) )]
[JsonSerializable( typeof( TwoFasService ) )]
[JsonSerializable( typeof( List<TwoFasService> ) )]
public partial class QuickOTPJsonContext : JsonSerializerContext;

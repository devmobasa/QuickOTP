using QuickOTP.Core.Configuration;

namespace QuickOTP.Core.Models;

public class TwoFasOtp
{
    public string? Account { get; set; }
    public string? Issuer { get; set; }
    public int Digits { get; set; } = AppConstants.Otp.DefaultDigits;
    public int Period { get; set; } = AppConstants.Otp.DefaultPeriod;
    public string Algorithm { get; set; } = AppConstants.Otp.Sha1;
    public string? TokenType { get; set; }
    public string? Source { get; set; }
    public string? Link { get; set; }
}

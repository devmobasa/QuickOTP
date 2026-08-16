using QuickOTP.Core.Configuration;

namespace QuickOTP.Core.Models;

public class TotpAccount
{
    public string Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public string Algorithm { get; set; } = AppConstants.Otp.Sha1;
    public int Digits { get; set; } = AppConstants.Otp.DefaultDigits;
    public int Period { get; set; } = AppConstants.Otp.DefaultPeriod;
    public string? Icon { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastUsed { get; set; }

    public TotpAccount( )
    {
        InitializeTimestamps( DateTime.UtcNow );
        Id = Guid.NewGuid( ).ToString( );
    }

    private void InitializeTimestamps( DateTime now )
    {
        CreatedAt = now;
        LastUsed = now;
    }
}

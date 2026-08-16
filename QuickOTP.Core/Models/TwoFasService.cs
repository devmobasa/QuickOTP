namespace QuickOTP.Core.Models;

public class TwoFasService
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Secret { get; set; }
    public long? UpdatedAt { get; set; }
    public TwoFasOrder? Order { get; set; }
    public TwoFasOtp? Otp { get; set; }
    public TwoFasIcon? Icon { get; set; }
    public string? GroupId { get; set; }
}

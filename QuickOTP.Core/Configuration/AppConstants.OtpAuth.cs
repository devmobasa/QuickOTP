namespace QuickOTP.Core.Configuration;

public static partial class AppConstants
{
    public static class OtpAuth
    {
        public const string TotpPrefix = "otpauth://totp/";
        public const string HotpPrefix = "otpauth://hotp/";
        public const string SecretParameter = "secret";
        public const string IssuerParameter = "issuer";
        public const string AlgorithmParameter = "algorithm";
        public const string DigitsParameter = "digits";
        public const string PeriodParameter = "period";
    }
}

namespace QuickOTP.Core.Configuration;

public static partial class AppConstants
{
    public static class Otp
    {
        public const string Sha1 = "SHA1";
        public const string Sha256 = "SHA256";
        public const string Sha512 = "SHA512";
        public const int DefaultDigits = 6;
        public const int DefaultPeriod = 30;
    }
}

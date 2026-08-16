namespace QuickOTP.Core.Configuration;

public static partial class AppConstants
{
    public static class TwoFas
    {
        public const int SchemaVersion = 4;
        public const string TokenTypeTotp = "TOTP";
        public const string SourceManual = "manual";
        public const string BackupAppVersionName = "QuickOTP.Console";
        public const string BackupAppOrigin = "QuickOTP.Console";
    }
}

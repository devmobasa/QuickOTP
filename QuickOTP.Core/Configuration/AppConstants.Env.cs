namespace QuickOTP.Core.Configuration;

public static partial class AppConstants
{
    public static class Env
    {
        public const string MasterPassword = "QUICKOTP_MASTER_PASSWORD";
        public const string MasterPasswordFile = "QUICKOTP_MASTER_PASSWORD_FILE";
        public const string Pbkdf2Iterations = "QUICKOTP_PBKDF2_ITERATIONS";
        public const string DisableKeychain = "QUICKOTP_DISABLE_KEYCHAIN";
        public const string KeychainService = "QUICKOTP_KEYCHAIN_SERVICE";
        public const string KeychainAccount = "QUICKOTP_KEYCHAIN_ACCOUNT";
    }
}

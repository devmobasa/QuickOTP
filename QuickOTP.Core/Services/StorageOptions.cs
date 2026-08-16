using System.Globalization;
using QuickOTP.Core.Configuration;

namespace QuickOTP.Core.Services;

public sealed class StorageOptions
{
    private const int DefaultPbkdf2Iterations = 200000;
    private const int MinimumPbkdf2Iterations = 10000;

    public string? MasterPassword { get; }
    public bool UseKeychain { get; }
    public string KeychainService { get; }
    public string KeychainAccount { get; }
    public int Pbkdf2Iterations { get; }

    public StorageOptions(
        string? masterPassword,
        bool useKeychain,
        string keychainService,
        string keychainAccount,
        int pbkdf2Iterations )
    {
        MasterPassword = masterPassword;
        UseKeychain = useKeychain;
        KeychainService = keychainService;
        KeychainAccount = keychainAccount;
        Pbkdf2Iterations = pbkdf2Iterations;
    }

    public static StorageOptions FromEnvironment( )
    {
        var masterPassword = ReadMasterPassword( );
        var useKeychain = !EnvIsTrue( AppConstants.Env.DisableKeychain );
        var (keychainService, keychainAccount) = ReadKeychainSettings( );
        var pbkdf2Iterations = ReadIterations( );

        return new StorageOptions(
            masterPassword,
            useKeychain,
            keychainService,
            keychainAccount,
            pbkdf2Iterations
        );
    }

    private static (string Service, string Account) ReadKeychainSettings( )
    {
        var service = ReadEnvOrDefault( AppConstants.Env.KeychainService, AppConstants.AppName );
        var account = ReadEnvOrDefault( AppConstants.Env.KeychainAccount, AppConstants.KeychainDefaultAccount );
        return (service, account);
    }

    private static string? ReadMasterPassword( )
    {
        var filePath = Environment.GetEnvironmentVariable( AppConstants.Env.MasterPasswordFile );
        if ( !string.IsNullOrWhiteSpace( filePath ) )
        {
            try
            {
                if ( File.Exists( filePath ) )
                {
                    return File.ReadAllText( filePath ).TrimEnd( '\r', '\n' );
                }
            }
            catch
            {
                return null;
            }
        }

        var password = Environment.GetEnvironmentVariable( AppConstants.Env.MasterPassword );
        return string.IsNullOrEmpty( password ) ? null : password;
    }

    private static int ReadIterations( )
    {
        var value = Environment.GetEnvironmentVariable( AppConstants.Env.Pbkdf2Iterations );
        if ( int.TryParse( value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed )
             && parsed >= MinimumPbkdf2Iterations )
        {
            return parsed;
        }

        return DefaultPbkdf2Iterations;
    }

    private static bool EnvIsTrue( string name )
    {
        var value = Environment.GetEnvironmentVariable( name );
        if ( string.IsNullOrWhiteSpace( value ) )
        {
            return false;
        }

        return value.Equals( "1", StringComparison.OrdinalIgnoreCase )
               || value.Equals( "true", StringComparison.OrdinalIgnoreCase )
               || value.Equals( "yes", StringComparison.OrdinalIgnoreCase )
               || value.Equals( "on", StringComparison.OrdinalIgnoreCase );
    }

    private static string ReadEnvOrDefault( string name, string defaultValue )
    {
        var value = Environment.GetEnvironmentVariable( name );
        return string.IsNullOrWhiteSpace( value ) ? defaultValue : value.Trim( );
    }
}

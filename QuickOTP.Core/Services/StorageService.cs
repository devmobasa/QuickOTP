using QuickOTP.Core.Configuration;
using QuickOTP.Core.Models;
using QuickOTP.Core.Serialization;

namespace QuickOTP.Core.Services;

public partial class StorageService
{
    private const string EncryptionPrefix = "2FAC1:";
    private const string KeyFilePrefixV1 = "2FACK1:";
    private const string KeyFilePrefixV2 = "2FACK2:";
    private const string KeyFileModeRaw = "raw";
    private const string KeyFileModeDpapi = "dpapi";
    private const string KeyFileModePbkdf2 = "pbkdf2";
    private const int KeySizeBytes = 32;
    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;
    private const int SaltSizeBytes = 16;

    private readonly StorageOptions _options;
    private readonly KeychainService? _keychain;
    private readonly string _dataPath;
    private readonly string _accountsFile;
    private readonly string _keyFile;
    private readonly byte[] _key;
    private readonly byte[] _entropy = [0x2F, 0x41, 0x53]; // "2FAS" in hex

    public StorageService( StorageOptions? options = null )
    {
        _options = options ?? StorageOptions.FromEnvironment( );
        _keychain = _options.UseKeychain
            ? new KeychainService( _options.KeychainService, _options.KeychainAccount )
            : null;

        _dataPath = Path.Combine(
            Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData ),
            AppConstants.AppName
        );

        EnsureDataDirectory( _dataPath );

        _accountsFile = Path.Combine( _dataPath, AppConstants.AccountsFileName );
        _keyFile = Path.Combine( _dataPath, AppConstants.KeyFileName );
        _key = LoadOrCreateKey( );
    }

    public List<TotpAccount> LoadAccounts( )
    {
        if ( !File.Exists( _accountsFile ) )
        {
            return [];
        }

        try
        {
            var encryptedData = File.ReadAllText( _accountsFile );
            var decryptedJson = DecryptData( encryptedData );
            return DeserializeAccounts( decryptedJson );
        }
        catch ( Exception ex )
        {
            throw new Exception( $"Failed to load accounts: {ex.Message}", ex );
        }
    }

    public void SaveAccounts( List<TotpAccount> accounts )
    {
        try
        {
            var json = AppJson.SerializeAccounts( accounts );
            var encryptedData = EncryptData( json );
            File.WriteAllText( _accountsFile, encryptedData );
        }
        catch ( Exception ex )
        {
            throw new Exception( $"Failed to save accounts: {ex.Message}", ex );
        }
    }

    public void AddAccount( TotpAccount account )
    {
        var accounts = LoadAccounts( );
        accounts.Add( account );
        SaveAccounts( accounts );
    }

    public void AddAccounts( IReadOnlyList<TotpAccount> importedAccounts )
    {
        if ( importedAccounts.Count == 0 )
        {
            return;
        }

        var accounts = LoadAccounts( );
        accounts.AddRange( importedAccounts );
        SaveAccounts( accounts );
    }

    public void RemoveAccount( string accountId )
    {
        var accounts = LoadAccounts( );
        accounts.RemoveAll( account => account.Id == accountId );
        SaveAccounts( accounts );
    }

    public bool UpdateAccount( TotpAccount updatedAccount )
    {
        var accounts = LoadAccounts( );
        var index = accounts.FindIndex( account => account.Id == updatedAccount.Id );
        if ( index < 0 )
        {
            return false;
        }

        accounts[index] = updatedAccount;
        SaveAccounts( accounts );
        return true;
    }

    public TotpAccount? GetAccount( string accountId )
    {
        var accounts = LoadAccounts( );
        return accounts.FirstOrDefault( account => account.Id == accountId );
    }

    private static List<TotpAccount> DeserializeAccounts( string json )
    {
        if ( string.IsNullOrWhiteSpace( json ) )
        {
            throw new Exception( "Vault file is empty." );
        }

        var accounts = AppJson.DeserializeAccounts( json );
        if ( accounts is null )
        {
            throw new Exception( "Vault file does not contain an account list." );
        }

        if ( accounts.Exists( account => account is null ) )
        {
            throw new Exception( "Vault file contains a null account." );
        }

        return accounts;
    }

    private static void EnsureDataDirectory( string path )
    {
        if ( !Directory.Exists( path ) )
        {
            Directory.CreateDirectory( path );
        }
    }
}

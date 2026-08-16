using QuickOTP.Core.Models;
using QuickOTP.Core.Serialization;

namespace QuickOTP.Core.Services;

public partial class StorageService
{
    public bool FileLooksEncrypted( string filePath )
    {
        var content = File.ReadAllText( filePath );
        return content.StartsWith( EncryptionPrefix, StringComparison.Ordinal );
    }

    public void ExportAccounts( string filePath, bool encrypted = false )
    {
        var accounts = LoadAccounts( );
        var json = AppJson.SerializeAccounts( accounts );

        if ( encrypted )
        {
            WriteEncryptedExport( filePath, json );
            return;
        }

        File.WriteAllText( filePath, json );
    }

    public void ImportAccounts( string filePath, bool encrypted = false )
    {
        var fileContent = File.ReadAllText( filePath );
        var json = encrypted ? DecryptData( fileContent ) : fileContent;

        var importedAccounts = DeserializeAccounts( json );
        if ( importedAccounts.Count == 0 )
        {
            return;
        }

        MergeImportedAccounts( importedAccounts );
    }

    private void WriteEncryptedExport( string filePath, string json )
    {
        var encryptedJson = EncryptData( json );
        File.WriteAllText( filePath, encryptedJson );
    }

    private void MergeImportedAccounts( List<TotpAccount> importedAccounts )
    {
        var existingAccounts = LoadAccounts( );
        existingAccounts.AddRange( importedAccounts );
        SaveAccounts( existingAccounts );
    }
}

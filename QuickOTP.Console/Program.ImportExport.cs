using QuickOTP.Core.Models;

namespace QuickOTP.Console;

internal static partial class Program
{
    private static void ImportFromUri( )
    {
        if ( !TryPromptForRequiredText( "Import URI", "otpauth://", "URI is required.", out var uri ) )
        {
            return;
        }

        try
        {
            var account = _totpService.ParseAccountFromOtpAuthUri( uri );
            _storageService.AddAccount( account );
            RefreshAccounts( reloadFromStorage: true );
        }
        catch ( Exception ex )
        {
            ShowError( "Import URI", ex.Message );
        }
    }

    private static void ExportJsonBackup( )
    {
        if ( !TryPromptForRequiredText( "Export JSON", "File path:", "File path is required.", out var path ) )
        {
            return;
        }

        var encrypt = Confirm( "Export JSON", "Encrypt export with local key?" );

        try
        {
            _storageService.ExportAccounts( path, encrypt );
            ShowInfo( "Export JSON", "Export completed." );
        }
        catch ( Exception ex )
        {
            ShowError( "Export JSON", ex.Message );
        }
    }

    private static void ImportJsonBackup( )
    {
        if ( !TryPromptForRequiredText( "Import JSON", "File path:", "File path is required.", out var path ) )
        {
            return;
        }

        var encrypted = Confirm( "Import JSON", "Is the file encrypted?" );

        try
        {
            _storageService.ImportAccounts( path, encrypted );
            RefreshAccounts( reloadFromStorage: true );
            ShowInfo( "Import JSON", "Import completed." );
        }
        catch ( Exception ex )
        {
            ShowError( "Import JSON", ex.Message );
        }
    }

    private static void Import2FasBackup( )
    {
        if ( !TryPromptForRequiredText( "Import .2fas", "File path:", "File path is required.", out var path ) )
        {
            return;
        }

        var encrypted = Confirm( "Import .2fas", "Is the file encrypted?" );
        if ( !TryGetImportPassword( encrypted, out var password ) )
        {
            return;
        }

        try
        {
            var importedAccounts = _twoFasImportService.ImportFrom2FasFile( path, password );
            if ( importedAccounts.Count == 0 )
            {
                ShowInfo( "Import .2fas", "No accounts found." );
                return;
            }

            if ( !Confirm( "Import .2fas", $"Found {importedAccounts.Count} accounts. Import?" ) )
            {
                return;
            }

            importedAccounts = SkipDuplicatesIfRequested( importedAccounts );
            PersistImportedAccounts( importedAccounts );

            RefreshAccounts( reloadFromStorage: true );
            ShowInfo( "Import .2fas", "Import completed." );
        }
        catch ( Exception ex )
        {
            ShowError( "Import .2fas", ex.Message );
        }
    }

    private static bool TryGetImportPassword( bool encrypted, out string? password )
    {
        password = null;

        if ( !encrypted )
        {
            return true;
        }

        return TryPromptForText( "Import .2fas", "Password:", string.Empty, true, out password );
    }

    private static List<TotpAccount> SkipDuplicatesIfRequested( List<TotpAccount> importedAccounts )
    {
        var existing = _storageService.LoadAccounts( );
        var duplicates = FindDuplicates( importedAccounts, existing );

        if ( duplicates.Count == 0 )
        {
            return importedAccounts;
        }

        if ( !Confirm( "Import .2fas", $"{duplicates.Count} duplicates found. Skip them?" ) )
        {
            return importedAccounts;
        }

        return importedAccounts.Except( duplicates ).ToList( );
    }

    private static List<TotpAccount> FindDuplicates( List<TotpAccount> importedAccounts, List<TotpAccount> existingAccounts )
    {
        return importedAccounts
            .Where( imported => existingAccounts.Any( current =>
                current.Secret == imported.Secret
                && current.Issuer == imported.Issuer ) )
            .ToList( );
    }

    private static void PersistImportedAccounts( List<TotpAccount> importedAccounts )
    {
        foreach ( var account in importedAccounts )
        {
            _storageService.AddAccount( account );
        }
    }

    private static void Export2FasBackup( )
    {
        if ( !TryPromptForRequiredText( "Export .2fas", "File path:", "File path is required.", out var path ) )
        {
            return;
        }

        path = Ensure2FasExtension( path );

        var encrypt = Confirm( "Export .2fas", "Encrypt backup with a password?" );
        if ( !TryGetExportPassword( encrypt, out var password ) )
        {
            return;
        }

        try
        {
            var accounts = _storageService.LoadAccounts( );
            _twoFasImportService.Export2FasFormat( accounts, path, encrypt, password );
            ShowInfo( "Export .2fas", "Export completed." );
        }
        catch ( Exception ex )
        {
            ShowError( "Export .2fas", ex.Message );
        }
    }

    private static string Ensure2FasExtension( string path )
    {
        return path.EndsWith( ".2fas", StringComparison.OrdinalIgnoreCase )
            ? path
            : path + ".2fas";
    }

    private static bool TryGetExportPassword( bool encrypt, out string? password )
    {
        password = null;

        if ( !encrypt )
        {
            return true;
        }

        if ( !TryPromptForText( "Export .2fas", "Password:", string.Empty, true, out var passwordValue ) )
        {
            return false;
        }

        if ( !TryPromptForText( "Export .2fas", "Confirm:", string.Empty, true, out var confirmValue ) )
        {
            return false;
        }

        if ( passwordValue != confirmValue )
        {
            ShowError( "Export .2fas", "Passwords do not match." );
            return false;
        }

        password = passwordValue;
        return true;
    }
}

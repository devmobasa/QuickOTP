using QuickOTP.Core.Models;

namespace QuickOTP.Editor.ViewModels;

public sealed partial class MainWindowViewModel
{
    public void ImportUri( )
    {
        var uri = string.IsNullOrWhiteSpace( UriInput ) ? SearchText.Trim( ) : UriInput.Trim( );
        if ( string.IsNullOrWhiteSpace( uri ) )
        {
            ShowToast( "Paste an otpauth:// link first." );
            return;
        }

        TotpAccount account;
        try
        {
            account = _totpService.ParseAccountFromOtpAuthUri( uri );
        }
        catch ( Exception ex )
        {
            ShowToast( ex.Message );
            return;
        }

        if ( !ConfirmIfDirty( ( ) => ApplyImportedUri( account ) ) )
        {
            return;
        }

        ApplyImportedUri( account );
    }

    private void ApplyImportedUri( TotpAccount account )
    {
        LoadAccountIntoEditor( account );
        IsNewAccount = true;
        _editingOriginal = null;
        SelectedAccount = null;
        IsSecretVisible = true;
        SearchText = string.Empty;
        UriInput = string.Empty;
        ShowToast( "Review the imported account, then save." );
    }

    public async Task BeginImport2FasAsync( string filePath )
    {
        if ( !ConfirmIfDirty( ( ) => _ = ContinueImport2FasAsync( filePath ) ) )
        {
            return;
        }

        await ContinueImport2FasAsync( filePath );
    }

    private async Task ContinueImport2FasAsync( string filePath )
    {
        try
        {
            if ( _twoFasImportService.BackupRequiresPassword( filePath ) )
            {
                PromptPassword(
                    "This backup is encrypted",
                    "Enter the password used when the .2fas file was exported.",
                    async password => await Import2FasWithPasswordAsync( filePath, password ) );
                return;
            }

            await Import2FasWithPasswordAsync( filePath, null );
        }
        catch ( Exception ex )
        {
            ShowToast( ex.Message );
        }
    }

    public async Task BeginImportJsonAsync( string filePath )
    {
        if ( !ConfirmIfDirty( ( ) => _ = ImportJsonCoreAsync( filePath ) ) )
        {
            return;
        }

        await ImportJsonCoreAsync( filePath );
    }

    private async Task ImportJsonCoreAsync( string filePath )
    {
        try
        {
            var encrypted = _storageService.FileLooksEncrypted( filePath );
            _storageService.ImportAccounts( filePath, encrypted );
            ReloadAccounts( );
            ShowToast( "JSON backup imported." );
            await Task.CompletedTask;
        }
        catch ( Exception ex )
        {
            ShowToast( ex.Message );
        }
    }

    public void ExportJson( string filePath, bool encrypted )
    {
        try
        {
            _storageService.ExportAccounts( filePath, encrypted );
            ShowToast( encrypted ? "Encrypted JSON backup saved." : "JSON backup saved." );
        }
        catch ( Exception ex )
        {
            ShowToast( ex.Message );
        }
    }

    public void BeginExport2Fas( string filePath, bool encrypt )
    {
        if ( !encrypt )
        {
            Export2Fas( filePath, false, null );
            return;
        }

        PromptPassword(
            "Encrypt this backup",
            "Choose a password. You will need it to import this file later.",
            password =>
            {
                Export2Fas( filePath, true, password );
                return Task.CompletedTask;
            },
            requireConfirm: true );
    }

    private async Task Import2FasWithPasswordAsync( string filePath, string? password )
    {
        try
        {
            var imported = _twoFasImportService.ImportFrom2FasFile( filePath, password );
            if ( imported.Count == 0 )
            {
                ShowToast( "No accounts found in that backup." );
                return;
            }

            _importCandidates = imported;
            var duplicates = CountDuplicates( imported );
            SkipDuplicates = duplicates > 0;
            ImportSummary = duplicates == 0
                ? $"Found {imported.Count} accounts. Import them into this vault?"
                : $"Found {imported.Count} accounts, {duplicates} already in this vault.";
            OverlayTitle = "Import 2FAS backup";
            OverlayMessage = ImportSummary;
            OverlayPrimaryLabel = "Import";
            Overlay = OverlayKind.ImportPreview;
            await Task.CompletedTask;
        }
        catch ( Exception ex )
        {
            ShowToast( ex.Message );
        }
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void ConfirmImport( )
    {
        var toImport = SkipDuplicates
            ? _importCandidates.Where( candidate => !IsDuplicate( candidate ) ).ToList( )
            : _importCandidates;

        CloseOverlay( );

        if ( toImport.Count == 0 )
        {
            ShowToast( "Nothing new to import." );
            return;
        }

        try
        {
            _storageService.AddAccounts( toImport );
            ReloadAccounts( );
            ShowToast( $"Imported {toImport.Count} accounts." );
        }
        catch ( Exception ex )
        {
            ShowToast( ex.Message );
        }
    }

    private void Export2Fas( string filePath, bool encrypt, string? password )
    {
        try
        {
            var path = filePath.EndsWith( ".2fas", StringComparison.OrdinalIgnoreCase )
                ? filePath
                : filePath + ".2fas";
            var accounts = _storageService.LoadAccounts( );
            _twoFasImportService.Export2FasFormat( accounts, path, encrypt, password );
            ShowToast( encrypt ? "Encrypted .2fas backup saved." : ".2fas backup saved." );
        }
        catch ( Exception ex )
        {
            ShowToast( ex.Message );
        }
    }

    private int CountDuplicates( List<TotpAccount> imported ) => imported.Count( IsDuplicate );

    private bool IsDuplicate( TotpAccount imported )
    {
        return _allAccounts.Any( current =>
            current.Account.Secret == imported.Secret
            && current.Account.Issuer == imported.Issuer );
    }
}

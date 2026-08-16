using QuickOTP.Core.Configuration;
using QuickOTP.Core.Models;

namespace QuickOTP.Editor.ViewModels;

public sealed partial class MainWindowViewModel
{
    private bool _isLoadingEditor;

    private void ReloadAccounts( string? selectId = null )
    {
        List<TotpAccount> loaded;
        try
        {
            loaded = _storageService.LoadAccounts( );
        }
        catch ( Exception ex )
        {
            ShowToast( ex.Message );
            return;
        }

        var previouslySelectedId = selectId ?? SelectedAccount?.Account.Id;
        _allAccounts.Clear( );
        FilteredAccounts.Clear( );

        foreach ( var account in loaded
                     .OrderBy( account => account.Issuer )
                     .ThenBy( account => account.Name ) )
        {
            var item = new AccountItemViewModel( account );
            UpdateItemCode( item );
            _allAccounts.Add( item );
        }

        AccountCount = _allAccounts.Count;
        ApplyFilter( );
        OnPropertyChanged( nameof( ShowNoMatches ) );
        SelectById( previouslySelectedId );
    }

    private void ApplyFilter( )
    {
        var query = SearchLooksLikeUri ? string.Empty : SearchText;

        FilteredAccounts.Clear( );
        foreach ( var item in _allAccounts )
        {
            if ( item.Matches( query ) )
            {
                FilteredAccounts.Add( item );
            }
        }

        OnPropertyChanged( nameof( ShowNoMatches ) );
    }

    private void SelectById( string? accountId )
    {
        if ( string.IsNullOrWhiteSpace( accountId ) )
        {
            if ( !IsNewAccount )
            {
                SelectedAccount = FilteredAccounts.FirstOrDefault( );
            }

            return;
        }

        SelectedAccount = FilteredAccounts.FirstOrDefault( item => item.Account.Id == accountId )
                          ?? _allAccounts.FirstOrDefault( item => item.Account.Id == accountId );
    }

    private void StartNewAccount( )
    {
        _isLoadingEditor = true;
        IsNewAccount = true;
        SelectedAccount = null;
        _editingOriginal = null;
        EditName = string.Empty;
        EditIssuer = string.Empty;
        EditSecret = string.Empty;
        EditAlgorithm = AppConstants.Otp.Sha1;
        EditDigits = AppConstants.Otp.DefaultDigits;
        EditPeriod = AppConstants.Otp.DefaultPeriod;
        IsSecretVisible = true;
        _isLoadingEditor = false;
        RefreshPreview( );
        NotifyFormState( );
    }

    private void LoadAccountIntoEditor( TotpAccount account )
    {
        _isLoadingEditor = true;
        IsNewAccount = false;
        _editingOriginal = CopyAccount( account );
        EditName = account.Name;
        EditIssuer = account.Issuer;
        EditSecret = account.Secret;
        EditAlgorithm = string.IsNullOrWhiteSpace( account.Algorithm ) ? AppConstants.Otp.Sha1 : account.Algorithm;
        EditDigits = account.Digits;
        EditPeriod = account.Period;
        IsSecretVisible = false;
        _isLoadingEditor = false;
        RefreshPreview( );
        NotifyFormState( );
    }

    private void ClearEditor( )
    {
        _isLoadingEditor = true;
        _editingOriginal = null;
        EditName = string.Empty;
        EditIssuer = string.Empty;
        EditSecret = string.Empty;
        EditAlgorithm = AppConstants.Otp.Sha1;
        EditDigits = AppConstants.Otp.DefaultDigits;
        EditPeriod = AppConstants.Otp.DefaultPeriod;
        LiveCode = string.Empty;
        CodeHint = null;
        QrImage = null;
        _isLoadingEditor = false;
        NotifyFormState( );
    }

    private void RefreshPreview( bool regenerateQr = true )
    {
        if ( _isLoadingEditor )
        {
            return;
        }

        NotifyFormState( );

        if ( !TryBuildAccountFromForm( out var account, out var error ) )
        {
            LiveCode = string.Empty;
            LiveRemaining = 0;
            CodeHint = error;
            if ( regenerateQr )
            {
                QrImage = null;
            }

            return;
        }

        try
        {
            LiveCode = _totpService.GenerateTotp( account );
            LiveRemaining = _totpService.GetRemainingSeconds( account.Period );
            CodeHint = null;

            if ( regenerateQr )
            {
                RefreshQr( account );
            }
        }
        catch
        {
            LiveCode = string.Empty;
            LiveRemaining = 0;
            CodeHint = "Enter a valid Base32 secret to preview the code.";
            if ( regenerateQr )
            {
                QrImage = null;
            }
        }
    }

    private void RefreshQr( TotpAccount account )
    {
        try
        {
            var uri = _qrCodeService.GenerateOtpAuthUri(
                account.Secret,
                account.Name,
                account.Issuer,
                account.Algorithm,
                account.Digits,
                account.Period );
            var png = _qrCodeService.GeneratePngBytes( uri, 6 );
            using var stream = new MemoryStream( png );
            QrImage = new Avalonia.Media.Imaging.Bitmap( stream );
        }
        catch
        {
            QrImage = null;
        }
    }

    private void UpdateItemCode( AccountItemViewModel item )
    {
        try
        {
            item.Code = _totpService.GenerateTotp( item.Account );
            item.RemainingSeconds = _totpService.GetRemainingSeconds( item.Account.Period );
        }
        catch
        {
            item.Code = "Error";
            item.RemainingSeconds = 0;
        }
    }

    private bool TryBuildAccountFromForm( out TotpAccount account, out string error )
    {
        var secret = ( EditSecret ?? string.Empty ).Replace( " ", string.Empty ).ToUpperInvariant( );
        if ( string.IsNullOrWhiteSpace( secret ) )
        {
            account = null!;
            error = "A Base32 secret is required.";
            return false;
        }

        account = new TotpAccount
        {
            Name = string.IsNullOrWhiteSpace( EditName ) ? AppConstants.Display.DefaultAccount : EditName.Trim( ),
            Issuer = string.IsNullOrWhiteSpace( EditIssuer ) ? AppConstants.Display.Unknown : EditIssuer.Trim( ),
            Secret = secret,
            Algorithm = string.IsNullOrWhiteSpace( EditAlgorithm ) ? AppConstants.Otp.Sha1 : EditAlgorithm.Trim( ),
            Digits = EditDigits,
            Period = EditPeriod <= 0 ? AppConstants.Otp.DefaultPeriod : EditPeriod
        };

        if ( !IsNewAccount && _editingOriginal != null )
        {
            account.Id = _editingOriginal.Id;
            account.CreatedAt = _editingOriginal.CreatedAt;
            account.LastUsed = _editingOriginal.LastUsed;
            account.Icon = _editingOriginal.Icon;
        }

        error = string.Empty;
        return true;
    }

    private bool ComputeHasUnsavedChanges( )
    {
        if ( IsNewAccount )
        {
            return !string.IsNullOrWhiteSpace( EditName )
                   || !string.IsNullOrWhiteSpace( EditIssuer )
                   || !string.IsNullOrWhiteSpace( EditSecret )
                   || !string.Equals( EditAlgorithm, AppConstants.Otp.Sha1, StringComparison.OrdinalIgnoreCase )
                   || EditDigits != AppConstants.Otp.DefaultDigits
                   || EditPeriod != AppConstants.Otp.DefaultPeriod;
        }

        if ( _editingOriginal == null )
        {
            return false;
        }

        return !string.Equals( EditName.Trim( ), _editingOriginal.Name.Trim( ), StringComparison.Ordinal )
               || !string.Equals( EditIssuer.Trim( ), _editingOriginal.Issuer.Trim( ), StringComparison.Ordinal )
               || !string.Equals(
                   ( EditSecret ?? string.Empty ).Replace( " ", string.Empty ),
                   _editingOriginal.Secret.Replace( " ", string.Empty ),
                   StringComparison.OrdinalIgnoreCase )
               || !string.Equals( EditAlgorithm, _editingOriginal.Algorithm, StringComparison.OrdinalIgnoreCase )
               || EditDigits != _editingOriginal.Digits
               || EditPeriod != _editingOriginal.Period;
    }

    private void NotifyFormState( )
    {
        OnPropertyChanged( nameof( HasUnsavedChanges ) );
        OnPropertyChanged( nameof( DigitSelectedIndex ) );
        OnPropertyChanged( nameof( PeriodSelectedIndex ) );
        OnPropertyChanged( nameof( LiveProgress ) );
        OnPropertyChanged( nameof( IsLiveUrgent ) );
        OnPropertyChanged( nameof( LiveRemainingLabel ) );
        OnPropertyChanged( nameof( FormattedLiveCode ) );
        OnPropertyChanged( nameof( ShowEditor ) );
        OnPropertyChanged( nameof( ShowEmptyEditor ) );
        OnPropertyChanged( nameof( ShowEmptyVault ) );
        SaveCommand.NotifyCanExecuteChanged( );
        DeleteCommand.NotifyCanExecuteChanged( );
    }

    private static TotpAccount CopyAccount( TotpAccount account ) =>
        new( )
        {
            Id = account.Id,
            Name = account.Name,
            Issuer = account.Issuer,
            Secret = account.Secret,
            Algorithm = account.Algorithm,
            Digits = account.Digits,
            Period = account.Period,
            Icon = account.Icon,
            CreatedAt = account.CreatedAt,
            LastUsed = account.LastUsed
        };
}

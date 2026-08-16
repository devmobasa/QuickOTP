using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickOTP.Core.Configuration;
using QuickOTP.Core.Models;
using QuickOTP.Core.Services;

namespace QuickOTP.Editor.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject, IDisposable
{
    private readonly StorageService _storageService;
    private readonly TotpService _totpService = new( );
    private readonly TwoFasImportService _twoFasImportService = new( );
    private readonly QrCodeService _qrCodeService = new( );
    private readonly List<AccountItemViewModel> _allAccounts = [];
    private TotpAccount? _editingOriginal;
    private Bitmap? _qrImage;
    private Action? _confirmAction;
    private Func<string, Task>? _passwordConsumer;
    private List<TotpAccount> _importCandidates = [];

    public MainWindowViewModel( StorageService storageService )
    {
        _storageService = storageService;
        AlgorithmOptions = [AppConstants.Otp.Sha1, AppConstants.Otp.Sha256, AppConstants.Otp.Sha512];
        DigitOptions = [6, 8];
        ReloadAccounts( );
    }

    public ObservableCollection<AccountItemViewModel> FilteredAccounts { get; } = [];

    public IReadOnlyList<string> AlgorithmOptions { get; }

    public IReadOnlyList<int> DigitOptions { get; }

    public ObservableCollection<int> PeriodOptions { get; } = [15, 30, 60, 90];

    [ObservableProperty]
    [NotifyPropertyChangedFor( nameof( HasAccounts ) )]
    [NotifyPropertyChangedFor( nameof( ShowEmptyVault ) )]
    [NotifyPropertyChangedFor( nameof( ShowNoMatches ) )]
    private int _accountCount;

    [ObservableProperty]
    private AccountItemViewModel? _selectedAccount;

    [ObservableProperty]
    [NotifyPropertyChangedFor( nameof( SearchLooksLikeUri ) )]
    [NotifyPropertyChangedFor( nameof( ShowNoMatches ) )]
    private string _searchText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor( nameof( SaveCommand ) )]
    [NotifyPropertyChangedFor( nameof( HasUnsavedChanges ) )]
    [NotifyPropertyChangedFor( nameof( EditorTitle ) )]
    private string _editName = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor( nameof( SaveCommand ) )]
    [NotifyPropertyChangedFor( nameof( HasUnsavedChanges ) )]
    private string _editIssuer = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor( nameof( SaveCommand ) )]
    [NotifyPropertyChangedFor( nameof( HasUnsavedChanges ) )]
    [NotifyPropertyChangedFor( nameof( SecretMaskChar ) )]
    private string _editSecret = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor( nameof( SaveCommand ) )]
    [NotifyPropertyChangedFor( nameof( HasUnsavedChanges ) )]
    private string _editAlgorithm = AppConstants.Otp.Sha1;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor( nameof( SaveCommand ) )]
    [NotifyPropertyChangedFor( nameof( HasUnsavedChanges ) )]
    [NotifyPropertyChangedFor( nameof( DigitSelectedIndex ) )]
    private int _editDigits = AppConstants.Otp.DefaultDigits;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor( nameof( SaveCommand ) )]
    [NotifyPropertyChangedFor( nameof( HasUnsavedChanges ) )]
    [NotifyPropertyChangedFor( nameof( PeriodSelectedIndex ) )]
    private int _editPeriod = AppConstants.Otp.DefaultPeriod;

    [ObservableProperty]
    [NotifyPropertyChangedFor( nameof( SecretMaskChar ) )]
    [NotifyPropertyChangedFor( nameof( SecretVisibilityLabel ) )]
    private bool _isSecretVisible;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor( nameof( SaveCommand ) )]
    [NotifyCanExecuteChangedFor( nameof( DeleteCommand ) )]
    [NotifyPropertyChangedFor( nameof( HasUnsavedChanges ) )]
    [NotifyPropertyChangedFor( nameof( EditorTitle ) )]
    [NotifyPropertyChangedFor( nameof( ShowEditor ) )]
    [NotifyPropertyChangedFor( nameof( ShowEmptyEditor ) )]
    [NotifyPropertyChangedFor( nameof( ShowEmptyVault ) )]
    [NotifyPropertyChangedFor( nameof( CanDelete ) )]
    private bool _isNewAccount;

    [ObservableProperty]
    [NotifyPropertyChangedFor( nameof( FormattedLiveCode ) )]
    private string _liveCode = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor( nameof( LiveRemainingLabel ) )]
    [NotifyPropertyChangedFor( nameof( LiveProgress ) )]
    [NotifyPropertyChangedFor( nameof( IsLiveUrgent ) )]
    private int _liveRemaining;

    [ObservableProperty]
    [NotifyPropertyChangedFor( nameof( HasCodeHint ) )]
    private string? _codeHint;

    [ObservableProperty]
    [NotifyPropertyChangedFor( nameof( IsOverlayOpen ) )]
    [NotifyPropertyChangedFor( nameof( IsConfirmOverlay ) )]
    [NotifyPropertyChangedFor( nameof( IsPasswordOverlay ) )]
    [NotifyPropertyChangedFor( nameof( IsImportPreviewOverlay ) )]
    [NotifyPropertyChangedFor( nameof( IsUriImportOverlay ) )]
    private OverlayKind _overlay = OverlayKind.None;

    [ObservableProperty]
    private string _overlayTitle = string.Empty;

    [ObservableProperty]
    private string _overlayMessage = string.Empty;

    [ObservableProperty]
    private string _overlayPrimaryLabel = "Continue";

    [ObservableProperty]
    private string _passwordInput = string.Empty;

    [ObservableProperty]
    private string _confirmPasswordInput = string.Empty;

    [ObservableProperty]
    private bool _overlayRequiresPasswordConfirm;

    [ObservableProperty]
    private bool _skipDuplicates = true;

    [ObservableProperty]
    private string _importSummary = string.Empty;

    [ObservableProperty]
    private string _uriInput = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor( nameof( HasToast ) )]
    private string _toastMessage = string.Empty;

    public bool HasAccounts => AccountCount > 0;

    public bool ShowEditor => IsNewAccount || SelectedAccount != null;

    public bool ShowEmptyEditor => HasAccounts && !ShowEditor;

    public bool ShowEmptyVault => !HasAccounts && !IsNewAccount;

    public bool IsOverlayOpen => Overlay != OverlayKind.None;

    public bool IsConfirmOverlay => Overlay == OverlayKind.Confirm;

    public bool IsPasswordOverlay => Overlay == OverlayKind.Password;

    public bool IsImportPreviewOverlay => Overlay == OverlayKind.ImportPreview;

    public bool IsUriImportOverlay => Overlay == OverlayKind.UriImport;

    public bool HasToast => !string.IsNullOrWhiteSpace( ToastMessage );

    public bool HasCodeHint => !string.IsNullOrWhiteSpace( CodeHint );

    public bool ShowNoMatches => HasAccounts && FilteredAccounts.Count == 0 && !SearchLooksLikeUri;

    public bool SearchLooksLikeUri => SearchText.TrimStart( ).StartsWith( "otpauth://", StringComparison.OrdinalIgnoreCase );

    public bool HasUnsavedChanges => ComputeHasUnsavedChanges( );

    public bool CanDelete => !IsNewAccount && SelectedAccount != null;

    public string EditorTitle => IsNewAccount ? "New account" : "Edit account";

    public char SecretMaskChar => IsSecretVisible ? '\0' : '•';

    public string SecretVisibilityLabel => IsSecretVisible ? "Hide" : "Show";

    public string FormattedLiveCode => string.IsNullOrWhiteSpace( LiveCode ) ? "••• •••" : CodeFormatter.Format( LiveCode );

    public string LiveRemainingLabel => string.IsNullOrWhiteSpace( LiveCode )
        ? "Waiting for a valid secret"
        : $"{LiveRemaining}s left in this window";

    public double LiveProgress => EditPeriod <= 0 || string.IsNullOrWhiteSpace( LiveCode )
        ? 0
        : LiveRemaining / (double)EditPeriod;

    public bool IsLiveUrgent => !string.IsNullOrWhiteSpace( LiveCode ) && LiveRemaining <= 5;

    public int DigitSelectedIndex
    {
        get
        {
            var index = IndexOf( DigitOptions, EditDigits );
            return index < 0 ? 0 : index;
        }
        set
        {
            if ( value >= 0 && value < DigitOptions.Count && EditDigits != DigitOptions[value] )
            {
                EditDigits = DigitOptions[value];
            }
        }
    }

    public int PeriodSelectedIndex
    {
        get
        {
            var index = IndexOf( PeriodOptions, EditPeriod );
            return index;
        }
        set
        {
            if ( value >= 0 && value < PeriodOptions.Count && EditPeriod != PeriodOptions[value] )
            {
                EditPeriod = PeriodOptions[value];
            }
        }
    }

    private void EnsurePeriodOption( int period )
    {
        if ( period <= 0 || PeriodOptions.Contains( period ) )
        {
            return;
        }

        var insertAt = PeriodOptions.Count;
        for ( var i = 0; i < PeriodOptions.Count; i++ )
        {
            if ( PeriodOptions[i] > period )
            {
                insertAt = i;
                break;
            }
        }

        PeriodOptions.Insert( insertAt, period );
        OnPropertyChanged( nameof( PeriodSelectedIndex ) );
    }

    private static int IndexOf( IReadOnlyList<int> values, int target )
    {
        for ( var i = 0; i < values.Count; i++ )
        {
            if ( values[i] == target )
            {
                return i;
            }
        }

        return -1;
    }

    public Bitmap? QrImage
    {
        get => _qrImage;
        private set
        {
            if ( ReferenceEquals( _qrImage, value ) )
            {
                return;
            }

            _qrImage?.Dispose( );
            _qrImage = value;
            OnPropertyChanged( );
        }
    }

    public void Dispose( )
    {
        QrImage = null;
    }

    partial void OnSearchTextChanged( string value ) => ApplyFilter( );

    partial void OnSelectedAccountChanged( AccountItemViewModel? value )
    {
        if ( value == null )
        {
            if ( !IsNewAccount )
            {
                ClearEditor( );
            }

            return;
        }

        LoadAccountIntoEditor( value.Account );
    }

    partial void OnEditNameChanged( string value ) => RefreshPreview( );

    partial void OnEditIssuerChanged( string value ) => RefreshPreview( );

    partial void OnEditSecretChanged( string value ) => RefreshPreview( );

    partial void OnEditAlgorithmChanged( string value ) => RefreshPreview( );

    partial void OnEditDigitsChanged( int value ) => RefreshPreview( );

    partial void OnEditPeriodChanged( int value )
    {
        EnsurePeriodOption( value );
        RefreshPreview( );
    }

    public void RefreshCodes( )
    {
        foreach ( var item in _allAccounts )
        {
            UpdateItemCode( item );
        }

        RefreshPreview( regenerateQr: false );
    }

    [RelayCommand]
    private void NewAccount( )
    {
        if ( !ConfirmIfDirty( StartNewAccount ) )
        {
            return;
        }

        StartNewAccount( );
    }

    [RelayCommand( CanExecute = nameof( HasUnsavedChanges ) )]
    private void Save( )
    {
        if ( !TryBuildAccountFromForm( out var account, out var error ) )
        {
            ShowToast( error );
            return;
        }

        try
        {
            _totpService.GenerateTotp( account );

            if ( IsNewAccount )
            {
                _storageService.AddAccount( account );
                ShowToast( "Account saved." );
            }
            else
            {
                if ( !_storageService.UpdateAccount( account ) )
                {
                    ShowToast( "That account is no longer in the vault." );
                    ReloadAccounts( );
                    return;
                }

                ShowToast( "Changes saved." );
            }

            ReloadAccounts( account.Id );
        }
        catch ( Exception ex )
        {
            ShowToast( ex.Message );
        }
    }

    [RelayCommand( CanExecute = nameof( CanDelete ) )]
    private void Delete( )
    {
        if ( SelectedAccount == null )
        {
            return;
        }

        var account = SelectedAccount.Account;
        PromptConfirm(
            "Remove this account?",
            $"{account.Issuer} · {account.Name} will be removed from this vault.",
            "Remove",
            ( ) =>
            {
                try
                {
                    _storageService.RemoveAccount( account.Id );
                    ShowToast( "Account removed." );
                    ReloadAccounts( );
                }
                catch ( Exception ex )
                {
                    ShowToast( ex.Message );
                }
            } );
    }

    [RelayCommand]
    private void ToggleSecretVisibility( ) => IsSecretVisible = !IsSecretVisible;

    [RelayCommand]
    private async Task CopyCodeAsync( )
    {
        if ( string.IsNullOrWhiteSpace( LiveCode ) )
        {
            ShowToast( "No code to copy yet." );
            return;
        }

        var copied = await ClipboardRequested( LiveCode );
        ShowToast( copied ? "Code copied." : "Could not copy the code." );
    }

    [RelayCommand]
    private void ImportSearchUri( )
    {
        UriInput = SearchText.Trim( );
        ImportUri( );
    }

    [RelayCommand]
    private void OpenUriImport( )
    {
        UriInput = SearchLooksLikeUri ? SearchText.Trim( ) : string.Empty;
        OverlayTitle = "Import authenticator link";
        OverlayMessage = "Paste an otpauth:// URI from your authenticator or QR export.";
        OverlayPrimaryLabel = "Import";
        Overlay = OverlayKind.UriImport;
    }

    [RelayCommand]
    private void SubmitUriImport( )
    {
        CloseOverlay( );
        ImportUri( );
    }

    public Func<string, Task<bool>> ClipboardRequested { get; set; } =
        static _ => Task.FromResult( false );

    public void RequestSelectAccount( AccountItemViewModel? next )
    {
        if ( ReferenceEquals( SelectedAccount, next ) )
        {
            return;
        }

        if ( !ConfirmIfDirty( ( ) => SelectedAccount = next ) )
        {
            return;
        }

        SelectedAccount = next;
    }
}

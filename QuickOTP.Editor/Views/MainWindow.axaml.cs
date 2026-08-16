using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using QuickOTP.Editor.ViewModels;

namespace QuickOTP.Editor.Views;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _refreshTimer;
    private bool _suppressSelection;
    private bool _allowClose;

    public MainWindow( )
    {
        InitializeComponent( );
        _refreshTimer = new DispatcherTimer( TimeSpan.FromSeconds( 1 ), DispatcherPriority.Background, ( _, _ ) => ViewModel?.RefreshCodes( ) );
        Opened += OnOpened;
        Closing += OnClosing;
        Closed += OnClosed;
    }

    private MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;

    private void OnOpened( object? sender, EventArgs e )
    {
        if ( ViewModel != null )
        {
            ViewModel.ClipboardRequested = text => ClipboardHelper.TryCopyAsync( this, text );
        }

        _refreshTimer.Start( );
        SearchBox.Focus( );
    }

    private void OnClosing( object? sender, WindowClosingEventArgs e )
    {
        if ( _allowClose || ViewModel is not { HasUnsavedChanges: true } )
        {
            return;
        }

        e.Cancel = true;
        ViewModel.PromptConfirm(
            "Discard unsaved changes?",
            "You have edits that have not been saved.",
            "Discard",
            ( ) =>
            {
                _allowClose = true;
                Close( );
            } );
    }

    private void OnClosed( object? sender, EventArgs e )
    {
        _refreshTimer.Stop( );
        ViewModel?.Dispose( );
    }

    private void OnAccountSelectionChanged( object? sender, SelectionChangedEventArgs e )
    {
        if ( _suppressSelection || ViewModel == null )
        {
            return;
        }

        var next = AccountsList.SelectedItem as AccountItemViewModel;
        if ( ReferenceEquals( ViewModel.SelectedAccount, next ) )
        {
            return;
        }

        if ( next == null && ViewModel.SelectedAccount != null )
        {
            return;
        }

        if ( ViewModel.HasUnsavedChanges )
        {
            _suppressSelection = true;
            AccountsList.SelectedItem = ViewModel.SelectedAccount;
            _suppressSelection = false;
            ViewModel.PromptConfirm(
                "Discard unsaved changes?",
                "You have edits that have not been saved.",
                "Discard",
                ( ) => ViewModel.SelectedAccount = next );
            return;
        }

        ViewModel.SelectedAccount = next;
    }

    protected override void OnKeyDown( KeyEventArgs e )
    {
        if ( ViewModel?.IsOverlayOpen == true )
        {
            if ( TryHandleOverlayShortcut( e ) )
            {
                return;
            }

            base.OnKeyDown( e );
            return;
        }

        if ( e.KeyModifiers.HasFlag( KeyModifiers.Control ) )
        {
            switch ( e.Key )
            {
                case Key.N:
                    ViewModel?.NewAccountCommand.Execute( null );
                    e.Handled = true;
                    return;
                case Key.S:
                    ViewModel?.SaveCommand.Execute( null );
                    e.Handled = true;
                    return;
                case Key.F:
                    SearchBox.Focus( );
                    SearchBox.SelectAll( );
                    e.Handled = true;
                    return;
            }
        }

        if ( e.Key is Key.Enter or Key.Return && !SearchBox.IsFocused )
        {
            ViewModel?.CopyCodeCommand.Execute( null );
            e.Handled = true;
            return;
        }

        base.OnKeyDown( e );
    }

    private bool TryHandleOverlayShortcut( KeyEventArgs e )
    {
        if ( e.KeyModifiers.HasFlag( KeyModifiers.Control ) && e.Key is Key.N or Key.S or Key.F )
        {
            e.Handled = true;
            return true;
        }

        if ( e.Key is Key.Enter or Key.Return )
        {
            ViewModel?.SubmitOpenOverlay( );
            e.Handled = true;
            return true;
        }

        if ( e.Key == Key.Escape )
        {
            ViewModel?.CancelOverlayCommand.Execute( null );
            e.Handled = true;
            return true;
        }

        return false;
    }

    private async void OnImport2FasClick( object? sender, RoutedEventArgs e )
    {
        var path = await PickOpenPathAsync( "Import 2FAS backup", CreateFileType( "2FAS backup", "*.2fas", "*.json" ) );
        if ( path == null || ViewModel == null )
        {
            return;
        }

        await ViewModel.BeginImport2FasAsync( path );
    }

    private async void OnImportJsonClick( object? sender, RoutedEventArgs e )
    {
        var path = await PickOpenPathAsync( "Import JSON backup", CreateFileType( "JSON backup", "*.json" ) );
        if ( path == null || ViewModel == null )
        {
            return;
        }

        await ViewModel.BeginImportJsonAsync( path );
    }

    private async void OnExport2FasClick( object? sender, RoutedEventArgs e )
    {
        var path = await PickSavePathAsync( "Export 2FAS backup", "vault.2fas", CreateFileType( "2FAS backup", "*.2fas" ) );
        if ( path == null || ViewModel == null )
        {
            return;
        }

        ViewModel.BeginExport2Fas( path, encrypt: false );
    }

    private async void OnExport2FasEncryptedClick( object? sender, RoutedEventArgs e )
    {
        var path = await PickSavePathAsync( "Export encrypted 2FAS backup", "vault.2fas", CreateFileType( "2FAS backup", "*.2fas" ) );
        if ( path == null || ViewModel == null )
        {
            return;
        }

        ViewModel.BeginExport2Fas( path, encrypt: true );
    }

    private async void OnExportJsonClick( object? sender, RoutedEventArgs e )
    {
        var path = await PickSavePathAsync( "Export JSON backup", "accounts.json", CreateFileType( "JSON backup", "*.json" ) );
        if ( path == null || ViewModel == null )
        {
            return;
        }

        ViewModel.ExportJson( path, encrypted: false );
    }

    private async void OnExportJsonEncryptedClick( object? sender, RoutedEventArgs e )
    {
        var path = await PickSavePathAsync( "Export encrypted JSON backup", "accounts.json", CreateFileType( "JSON backup", "*.json" ) );
        if ( path == null || ViewModel == null )
        {
            return;
        }

        ViewModel.ExportJson( path, encrypted: true );
    }

    private async Task<string?> PickOpenPathAsync( string title, FilePickerFileType fileType )
    {
        var files = await StorageProvider.OpenFilePickerAsync( new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = [fileType]
        } );

        return files.Count == 0 ? null : files[0].TryGetLocalPath( );
    }

    private async Task<string?> PickSavePathAsync( string title, string suggestedName, FilePickerFileType fileType )
    {
        var file = await StorageProvider.SaveFilePickerAsync( new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = suggestedName,
            FileTypeChoices = [fileType]
        } );

        return file?.TryGetLocalPath( );
    }

    private static FilePickerFileType CreateFileType( string name, params string[] patterns ) =>
        new( name ) { Patterns = patterns };
}

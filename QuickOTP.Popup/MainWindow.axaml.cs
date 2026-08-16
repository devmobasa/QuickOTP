using System;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using QuickOTP.Core.Services;

namespace QuickOTP.Popup;

public partial class MainWindow : Window
{
    private static readonly Key? EnterKey = TryParseKey( "Enter" );
    private static readonly Key? ReturnKey = TryParseKey( "Return" );
    private static readonly Key? NumpadEnterKey = TryParseKey( "NumPadEnter" );

    private readonly StorageService? _storageService;
    private readonly TotpService _totpService = new( );
    private readonly ObservableCollection<AccountListItem> _allAccounts = [];
    private readonly DispatcherTimer _refreshTimer;
    private readonly DispatcherTimer _exitTimer;
    private bool _isClosing;
    private bool _vaultLoadFailed;

    public ObservableCollection<AccountListItem> FilteredAccounts { get; } = [];

    public MainWindow( ) : this( null )
    {
    }

    public MainWindow( StorageService? storageService )
    {
        _storageService = storageService;
        InitializeComponent( );
        InitializeViewState( );

        _refreshTimer = CreateRefreshTimer( );
        _refreshTimer.Start( );

        _exitTimer = CreateExitTimer( );
        RegisterWindowEvents( );
    }

    private void InitializeViewState( )
    {
        DataContext = this;
        AddHandler( InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, true );

        LoadAccounts( );
        ApplyFilter( SearchBox.Text );
    }

    private DispatcherTimer CreateRefreshTimer( )
    {
        return new DispatcherTimer( TimeSpan.FromSeconds( 1 ), DispatcherPriority.Normal, ( _, _ ) => UpdateCodes( ) );
    }

    private DispatcherTimer CreateExitTimer( )
    {
        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds( 750 )
        };

        timer.Tick += ( _, _ ) => CloseAndShutdown( );
        return timer;
    }

    private void RegisterWindowEvents( )
    {
        Opened += ( _, _ ) =>
        {
            SearchBox.Focus( );
            SearchBox.SelectAll( );
        };
    }

    private void CloseAndShutdown( )
    {
        _exitTimer.Stop( );
        Close( );

        if ( Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop )
        {
            desktop.Shutdown( );
        }
    }

    protected override void OnClosed( EventArgs e )
    {
        _refreshTimer.Stop( );
        _exitTimer.Stop( );

        base.OnClosed( e );
    }
}

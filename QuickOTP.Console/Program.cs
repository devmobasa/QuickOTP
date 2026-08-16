using Terminal.Gui;
using QuickOTP.Core.Models;
using QuickOTP.Core.Services;

namespace QuickOTP.Console;

internal static partial class Program
{
    private const int AccountLabelWidth = 36;

    private enum SortMode
    {
        IssuerThenName,
        NameThenIssuer
    }

    private static StorageService _storageService = null!;
    private static TotpService _totpService = null!;
    private static QrCodeService _qrCodeService = null!;
    private static TwoFasImportService _twoFasImportService = null!;
    private static TextCopy.Clipboard? _clipboard;

    private static List<TotpAccount> _accounts = new( );
    private static List<TotpAccount> _viewAccounts = new( );
    private static ListView _accountsList = null!;
    private static TextField _searchField = null!;
    private static SortMode _sortMode = SortMode.IssuerThenName;
    private static string _filterText = string.Empty;
    private static Label _issuerValue = null!;
    private static Label _nameValue = null!;
    private static Label _codeValue = null!;
    private static Label _remainingValue = null!;
    private static Label _algorithmValue = null!;
    private static Label _digitsValue = null!;
    private static Label _periodValue = null!;
    private static object? _refreshToken;

    private static void Main( )
    {
        if ( !TryInitializeServices( out var error ) )
        {
            System.Console.Error.WriteLine( error );
            return;
        }

        Application.Init( );

        try
        {
            BuildUi( );
            RefreshAccounts( reloadFromStorage: true );
            StartRefreshTimer( );
            Application.Run( );
        }
        finally
        {
            StopRefreshTimer( );
            Application.Shutdown( );
        }
    }

    private static bool TryInitializeServices( out string error )
    {
        error = string.Empty;

        try
        {
            _storageService = new StorageService( );
            _totpService = new TotpService( );
            _qrCodeService = new QrCodeService( );
            _twoFasImportService = new TwoFasImportService( );
            _clipboard = new TextCopy.Clipboard( );
            return true;
        }
        catch ( Exception ex )
        {
            error = ex.Message;
            return false;
        }
    }
}

using Terminal.Gui;

namespace QuickOTP.Console;

internal static partial class Program
{
    private static void BuildUi( )
    {
        var top = Application.Top;

        top.Add( BuildMenuBar( ) );
        top.Add( BuildStatusBar( ) );

        var window = BuildMainWindow( );
        top.Add( window );

        _searchField.SetFocus( );
    }

    private static Window BuildMainWindow( )
    {
        var window = new Window( "QuickOTP" )
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill( ),
            Height = Dim.Fill( ) - 1
        };

        _searchField = BuildSearchField( );
        _accountsList = BuildAccountsList( );

        window.Add( _searchField );
        window.Add( _accountsList );
        window.Add( BuildDetailsPane( ) );

        return window;
    }

    private static TextField BuildSearchField( )
    {
        var searchField = new TextField( string.Empty )
        {
            X = 0,
            Y = 0,
            Width = Dim.Percent( 55 ),
            Height = 1
        };

        searchField.TextChanged += _ =>
        {
            _filterText = searchField.Text.ToString( )?.Trim( ) ?? string.Empty;
            RefreshAccounts( reloadFromStorage: false );
        };

        return searchField;
    }

    private static ListView BuildAccountsList( )
    {
        var accountsList = new ListView
        {
            X = 0,
            Y = Pos.Bottom( _searchField ) + 1,
            Width = Dim.Percent( 55 ),
            Height = Dim.Fill( ),
            AllowsMarking = true,
            AllowsMultipleSelection = true
        };

        accountsList.SelectedItemChanged += delegate { UpdateDetails( ); };
        accountsList.OpenSelectedItem += _ => CopySelectedCode( );

        return accountsList;
    }

    private static MenuBar BuildMenuBar( )
    {
        return new MenuBar( new[]
        {
            new MenuBarItem( "_File", new[]
            {
                new MenuItem( "_Quit", string.Empty, () => Application.RequestStop( ) )
            } ),
            new MenuBarItem( "_Accounts", new[]
            {
                new MenuItem( "_Add", string.Empty, AddAccount ),
                new MenuItem( "_Edit", string.Empty, EditSelectedAccount ),
                new MenuItem( "_Remove", string.Empty, RemoveSelectedAccount ),
                new MenuItem( "Remove _Selected", string.Empty, RemoveSelectedAccounts ),
                new MenuItem( "Sort by _Issuer", string.Empty, () => ChangeSort( SortMode.IssuerThenName ) ),
                new MenuItem( "Sort by _Name", string.Empty, () => ChangeSort( SortMode.NameThenIssuer ) ),
                new MenuItem( "_Copy Code", string.Empty, CopySelectedCode ),
                new MenuItem( "Show _QR", string.Empty, ShowQrForSelected )
            } ),
            new MenuBarItem( "_Import", new[]
            {
                new MenuItem( "From _URI", string.Empty, ImportFromUri ),
                new MenuItem( "From _JSON", string.Empty, ImportJsonBackup ),
                new MenuItem( "From ._2fas", string.Empty, Import2FasBackup )
            } ),
            new MenuBarItem( "_Export", new[]
            {
                new MenuItem( "To _JSON", string.Empty, ExportJsonBackup ),
                new MenuItem( "To ._2fas", string.Empty, Export2FasBackup )
            } ),
            new MenuBarItem( "_Help", new[]
            {
                new MenuItem( "_About", string.Empty, ShowAbout )
            } )
        } );
    }

    private static StatusBar BuildStatusBar( )
    {
        return new StatusBar( new[]
        {
            new StatusItem( Key.F2, "~F2~ Add", AddAccount ),
            new StatusItem( Key.F3, "~F3~ Edit", EditSelectedAccount ),
            new StatusItem( Key.F4, "~F4~ Remove", RemoveSelectedAccount ),
            new StatusItem( Key.F5, "~F5~ Sort Issuer", () => ChangeSort( SortMode.IssuerThenName ) ),
            new StatusItem( Key.F6, "~F6~ Sort Name", () => ChangeSort( SortMode.NameThenIssuer ) ),
            new StatusItem( Key.F7, "~F7~ Copy", CopySelectedCode ),
            new StatusItem( Key.F10, "~F10~ Quit", () => Application.RequestStop( ) )
        } );
    }

    private static FrameView BuildDetailsPane( )
    {
        var details = new FrameView( "Details" )
        {
            X = Pos.Right( _accountsList ),
            Y = 0,
            Width = Dim.Fill( ),
            Height = Dim.Fill( )
        };

        var labelWidth = 12;

        _issuerValue = AddDetailsRow( details, "Issuer:", 1, labelWidth );
        _nameValue = AddDetailsRow( details, "Account:", 3, labelWidth );
        _codeValue = AddDetailsRow( details, "Code:", 5, labelWidth );
        _remainingValue = AddDetailsRow( details, "Expires:", 7, labelWidth );
        _algorithmValue = AddDetailsRow( details, "Algorithm:", 9, labelWidth );
        _digitsValue = AddDetailsRow( details, "Digits:", 11, labelWidth );
        _periodValue = AddDetailsRow( details, "Period:", 13, labelWidth );

        return details;
    }

    private static Label AddDetailsRow( FrameView details, string title, int y, int labelWidth )
    {
        details.Add( new Label( title )
        {
            X = 1,
            Y = y,
            Width = labelWidth
        } );

        var value = new Label( string.Empty )
        {
            X = labelWidth + 2,
            Y = y,
            Width = Dim.Fill( )
        };

        details.Add( value );
        return value;
    }

    private static void StartRefreshTimer( )
    {
        _refreshToken = Application.MainLoop.AddTimeout( TimeSpan.FromSeconds( 1 ), _ =>
        {
            RefreshAccounts( reloadFromStorage: false );
            return true;
        } );
    }

    private static void StopRefreshTimer( )
    {
        if ( _refreshToken == null )
        {
            return;
        }

        Application.MainLoop.RemoveTimeout( _refreshToken );
        _refreshToken = null;
    }
}

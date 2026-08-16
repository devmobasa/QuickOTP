using System.Globalization;
using Terminal.Gui;
using QuickOTP.Core.Configuration;
using QuickOTP.Core.Models;

namespace QuickOTP.Console;

internal static partial class Program
{
    private sealed class AddAccountDialog
    {
        public required Dialog Dialog { get; init; }
        public required TextField NameField { get; init; }
        public required TextField IssuerField { get; init; }
        public required TextField SecretField { get; init; }
        public required TextField AlgorithmField { get; init; }
        public required TextField DigitsField { get; init; }
        public required TextField PeriodField { get; init; }
        public bool Confirmed { get; set; }
    }

    private static void RefreshAccounts( bool reloadFromStorage )
    {
        if ( reloadFromStorage )
        {
            try
            {
                _accounts = _storageService.LoadAccounts( );
            }
            catch ( Exception ex )
            {
                ShowError( "Load accounts", ex.Message );
                return;
            }
        }

        _viewAccounts = ApplyFilterAndSort( _accounts );
        SetAccountListSource( _viewAccounts );
        UpdateDetails( );
    }

    private static void SetAccountListSource( List<TotpAccount> accounts )
    {
        var selectedIndex = _accountsList.SelectedItem;
        var rows = BuildAccountRows( accounts );

        _accountsList.SetSource( rows );
        _accountsList.SelectedItem = rows.Count == 0
            ? -1
            : Math.Clamp( selectedIndex, 0, rows.Count - 1 );
    }

    private static List<string> BuildAccountRows( List<TotpAccount> accounts )
    {
        var rows = new List<string>( accounts.Count );
        foreach ( var account in accounts )
        {
            rows.Add( FormatAccountRow( account ) );
        }

        return rows;
    }

    private static List<TotpAccount> ApplyFilterAndSort( List<TotpAccount> accounts )
    {
        IEnumerable<TotpAccount> query = accounts;

        if ( !string.IsNullOrWhiteSpace( _filterText ) )
        {
            query = query.Where( account => MatchesFilter( account, _filterText ) );
        }

        query = _sortMode == SortMode.IssuerThenName
            ? query.OrderBy( account => account.Issuer ?? string.Empty ).ThenBy( account => account.Name ?? string.Empty )
            : query.OrderBy( account => account.Name ?? string.Empty ).ThenBy( account => account.Issuer ?? string.Empty );

        return query.ToList( );
    }

    private static bool MatchesFilter( TotpAccount account, string filterText )
    {
        var filter = filterText.ToLowerInvariant( );

        return MatchesFilterValue( account.Issuer, filter ) || MatchesFilterValue( account.Name, filter );
    }

    private static bool MatchesFilterValue( string? value, string filter )
    {
        return !string.IsNullOrWhiteSpace( value ) && value.ToLowerInvariant( ).Contains( filter );
    }

    private static string FormatAccountRow( TotpAccount account )
    {
        var label = BuildAccountLabel( account );
        var (code, remaining) = GenerateCodeAndRemaining( account );

        return $"{label} {code} {remaining,2}s";
    }

    private static string BuildAccountLabel( TotpAccount account )
    {
        var issuer = string.IsNullOrWhiteSpace( account.Issuer )
            ? AppConstants.Display.Unknown
            : account.Issuer.Trim( );

        var name = string.IsNullOrWhiteSpace( account.Name )
            ? AppConstants.Display.DefaultAccount
            : account.Name.Trim( );

        var label = $"{issuer} - {name}";
        if ( label.Length > AccountLabelWidth )
        {
            return label.Substring( 0, AccountLabelWidth );
        }

        return label.PadRight( AccountLabelWidth );
    }

    private static (string Code, int Remaining) GenerateCodeAndRemaining( TotpAccount account )
    {
        try
        {
            var code = _totpService.GenerateTotp( account );
            var remaining = _totpService.GetRemainingSeconds( account.Period );
            return (code, remaining);
        }
        catch
        {
            return ("Error", 0);
        }
    }

    private static void UpdateDetails( )
    {
        var account = GetSelectedAccount( );
        if ( account == null )
        {
            ClearDetails( );
            return;
        }

        PopulateStaticDetails( account );
        PopulateCodeDetails( account );
    }

    private static void ClearDetails( )
    {
        _issuerValue.Text = string.Empty;
        _nameValue.Text = string.Empty;
        _codeValue.Text = string.Empty;
        _remainingValue.Text = string.Empty;
        _algorithmValue.Text = string.Empty;
        _digitsValue.Text = string.Empty;
        _periodValue.Text = string.Empty;
    }

    private static void PopulateStaticDetails( TotpAccount account )
    {
        _issuerValue.Text = account.Issuer;
        _nameValue.Text = account.Name;
        _algorithmValue.Text = account.Algorithm;
        _digitsValue.Text = account.Digits.ToString( CultureInfo.InvariantCulture );
        _periodValue.Text = account.Period.ToString( CultureInfo.InvariantCulture );
    }

    private static void PopulateCodeDetails( TotpAccount account )
    {
        try
        {
            _codeValue.Text = _totpService.GenerateTotp( account );
            _remainingValue.Text = $"{_totpService.GetRemainingSeconds( account.Period )}s";
        }
        catch ( Exception ex )
        {
            _codeValue.Text = "Error";
            _remainingValue.Text = ex.Message;
        }
    }

    private static TotpAccount? GetSelectedAccount( )
    {
        return _accountsList.SelectedItem < 0 || _accountsList.SelectedItem >= _viewAccounts.Count
            ? null
            : _viewAccounts[_accountsList.SelectedItem];
    }

    private static List<TotpAccount> GetSelectedAccounts( )
    {
        var indices = GetMarkedIndices( );

        if ( indices.Count == 0 && _accountsList.SelectedItem >= 0 && _accountsList.SelectedItem < _viewAccounts.Count )
        {
            indices.Add( _accountsList.SelectedItem );
        }

        return indices
            .Where( index => index >= 0 && index < _viewAccounts.Count )
            .Select( index => _viewAccounts[index] )
            .ToList( );
    }

    private static List<int> GetMarkedIndices( )
    {
        if ( !_accountsList.AllowsMarking )
        {
            return [];
        }

        var indices = new List<int>( );
        for ( var i = 0; i < _viewAccounts.Count; i++ )
        {
            if ( _accountsList.Source.IsMarked( i ) )
            {
                indices.Add( i );
            }
        }

        return indices;
    }

    private static void AddAccount( )
    {
        var form = CreateAddAccountDialog( );

        try
        {
            Application.Run( form.Dialog );

            if ( !form.Confirmed )
            {
                return;
            }

            if ( !TryBuildAccountFromDialog( form, out var account ) )
            {
                return;
            }

            SaveNewAccount( account );
        }
        finally
        {
            form.Dialog.Dispose( );
        }
    }

    private static void EditSelectedAccount( )
    {
        var account = GetSelectedAccount( );
        if ( account == null )
        {
            ShowInfo( "Edit Account", "No account selected." );
            return;
        }

        var form = CreateEditAccountDialog( account );

        try
        {
            Application.Run( form.Dialog );

            if ( !form.Confirmed )
            {
                return;
            }

            if ( !TryBuildAccountFromDialog( form, out var editedAccount ) )
            {
                return;
            }

            editedAccount.Id = account.Id;
            editedAccount.CreatedAt = account.CreatedAt;
            editedAccount.LastUsed = account.LastUsed;
            editedAccount.Icon = account.Icon;

            SaveEditedAccount( editedAccount );
        }
        finally
        {
            form.Dialog.Dispose( );
        }
    }

    private static AddAccountDialog CreateAddAccountDialog( )
    {
        var dialog = new Dialog( "Add Account", 64, 18 );

        var form = new AddAccountDialog
        {
            Dialog = dialog,
            NameField = new TextField( string.Empty ) { X = 16, Y = 1, Width = 42 },
            IssuerField = new TextField( string.Empty ) { X = 16, Y = 3, Width = 42 },
            SecretField = new TextField( string.Empty ) { X = 16, Y = 5, Width = 42 },
            AlgorithmField = new TextField( AppConstants.Otp.Sha1 ) { X = 16, Y = 7, Width = 42 },
            DigitsField = new TextField( AppConstants.Otp.DefaultDigits.ToString( CultureInfo.InvariantCulture ) ) { X = 16, Y = 9, Width = 42 },
            PeriodField = new TextField( AppConstants.Otp.DefaultPeriod.ToString( CultureInfo.InvariantCulture ) ) { X = 16, Y = 11, Width = 42 }
        };

        AddAddAccountFields( form );
        AddAddAccountButtons( form );
        return form;
    }

    private static AddAccountDialog CreateEditAccountDialog( TotpAccount account )
    {
        var form = CreateAccountDialog(
            "Edit Account",
            account.Name,
            account.Issuer,
            account.Secret,
            account.Algorithm,
            account.Digits,
            account.Period );

        AddAccountFields( form );
        AddAccountButtons( form, "Save" );
        return form;
    }

    private static AddAccountDialog CreateAccountDialog(
        string title,
        string name,
        string issuer,
        string secret,
        string algorithm,
        int digits,
        int period )
    {
        var dialog = new Dialog( title, 64, 18 );

        return new AddAccountDialog
        {
            Dialog = dialog,
            NameField = new TextField( name ) { X = 16, Y = 1, Width = 42 },
            IssuerField = new TextField( issuer ) { X = 16, Y = 3, Width = 42 },
            SecretField = new TextField( secret ) { X = 16, Y = 5, Width = 42 },
            AlgorithmField = new TextField( algorithm ) { X = 16, Y = 7, Width = 42 },
            DigitsField = new TextField( digits.ToString( CultureInfo.InvariantCulture ) ) { X = 16, Y = 9, Width = 42 },
            PeriodField = new TextField( period.ToString( CultureInfo.InvariantCulture ) ) { X = 16, Y = 11, Width = 42 }
        };
    }

    private static void AddAddAccountFields( AddAccountDialog form )
    {
        AddAccountFields( form );
    }

    private static void AddAccountFields( AddAccountDialog form )
    {
        form.Dialog.Add( new Label( "Account Name:" ) { X = 1, Y = 1 } );
        form.Dialog.Add( form.NameField );

        form.Dialog.Add( new Label( "Issuer:" ) { X = 1, Y = 3 } );
        form.Dialog.Add( form.IssuerField );

        form.Dialog.Add( new Label( "Secret (Base32):" ) { X = 1, Y = 5 } );
        form.Dialog.Add( form.SecretField );

        form.Dialog.Add( new Label( "Algorithm:" ) { X = 1, Y = 7 } );
        form.Dialog.Add( form.AlgorithmField );

        form.Dialog.Add( new Label( "Digits (6/8):" ) { X = 1, Y = 9 } );
        form.Dialog.Add( form.DigitsField );

        form.Dialog.Add( new Label( "Period (s):" ) { X = 1, Y = 11 } );
        form.Dialog.Add( form.PeriodField );
    }

    private static void AddAddAccountButtons( AddAccountDialog form )
    {
        AddAccountButtons( form, "Add" );
    }

    private static void AddAccountButtons( AddAccountDialog form, string okText )
    {
        var ok = new Button( okText );
        ok.Clicked += () =>
        {
            form.Confirmed = true;
            Application.RequestStop( );
        };

        var cancel = new Button( "Cancel" );
        cancel.Clicked += () => Application.RequestStop( );

        form.Dialog.AddButton( ok );
        form.Dialog.AddButton( cancel );
    }

    private static bool TryBuildAccountFromDialog( AddAccountDialog form, out TotpAccount account )
    {
        var secret = NormalizeSecret( form.SecretField.Text.ToString( ) );
        if ( string.IsNullOrWhiteSpace( secret ) )
        {
            ShowError( "Add Account", "Secret key is required." );
            account = null!;
            return false;
        }

        var name = NormalizeText( form.NameField.Text.ToString( ) );
        var issuer = NormalizeText( form.IssuerField.Text.ToString( ) );
        var algorithm = NormalizeText( form.AlgorithmField.Text.ToString( ), AppConstants.Otp.Sha1 );
        var digits = ParseIntOrDefault( form.DigitsField.Text.ToString( ), AppConstants.Otp.DefaultDigits );
        var period = ParseIntOrDefault( form.PeriodField.Text.ToString( ), AppConstants.Otp.DefaultPeriod );

        account = new TotpAccount
        {
            Name = name,
            Issuer = issuer,
            Secret = secret,
            Algorithm = algorithm,
            Digits = digits,
            Period = period
        };

        return true;
    }

    private static void SaveNewAccount( TotpAccount account )
    {
        try
        {
            _totpService.GenerateTotp( account );
            _storageService.AddAccount( account );
            RefreshAccounts( reloadFromStorage: true );
        }
        catch ( Exception ex )
        {
            ShowError( "Add Account", ex.Message );
        }
    }

    private static void SaveEditedAccount( TotpAccount account )
    {
        try
        {
            _totpService.GenerateTotp( account );
            if ( !_storageService.UpdateAccount( account ) )
            {
                ShowError( "Edit Account", "Account was not found." );
                return;
            }

            RefreshAccounts( reloadFromStorage: true );
        }
        catch ( Exception ex )
        {
            ShowError( "Edit Account", ex.Message );
        }
    }

    private static string NormalizeText( string? value, string fallback = "" )
    {
        var trimmed = value?.Trim( );
        return string.IsNullOrEmpty( trimmed ) ? fallback : trimmed;
    }

    private static string NormalizeSecret( string? value )
    {
        return (value ?? string.Empty)
            .Replace( " ", string.Empty )
            .ToUpperInvariant( );
    }

    private static int ParseIntOrDefault( string? value, int fallback )
    {
        return int.TryParse( value, out var parsed ) ? parsed : fallback;
    }

    private static void RemoveSelectedAccount( )
    {
        var account = GetSelectedAccount( );
        if ( account == null )
        {
            ShowInfo( "Remove Account", "No account selected." );
            return;
        }

        if ( !Confirm( "Remove Account", $"Remove {account.Issuer} - {account.Name}?" ) )
        {
            return;
        }

        try
        {
            _storageService.RemoveAccount( account.Id );
            RefreshAccounts( reloadFromStorage: true );
        }
        catch ( Exception ex )
        {
            ShowError( "Remove Account", ex.Message );
        }
    }

    private static void RemoveSelectedAccounts( )
    {
        var accounts = GetSelectedAccounts( );
        if ( accounts.Count == 0 )
        {
            ShowInfo( "Remove Accounts", "No accounts selected." );
            return;
        }

        if ( !Confirm( "Remove Accounts", $"Remove {accounts.Count} selected account(s)?" ) )
        {
            return;
        }

        var deleted = 0;
        Exception? error = null;

        foreach ( var account in accounts )
        {
            try
            {
                _storageService.RemoveAccount( account.Id );
                deleted++;
            }
            catch ( Exception ex )
            {
                error = ex;
            }
        }

        if ( deleted > 0 )
        {
            RefreshAccounts( reloadFromStorage: true );
        }

        if ( error != null )
        {
            var message = deleted == 0
                ? error.Message
                : $"Removed {deleted} account(s), then failed: {error.Message}";
            ShowError( "Remove Accounts", message );
        }
    }

    private static void ChangeSort( SortMode mode )
    {
        _sortMode = mode;
        RefreshAccounts( reloadFromStorage: false );
    }

    private static void ShowQrForSelected( )
    {
        var account = GetSelectedAccount( );
        if ( account == null )
        {
            ShowInfo( "QR Code", "No account selected." );
            return;
        }

        var uri = _qrCodeService.GenerateOtpAuthUri(
            account.Secret,
            account.Name,
            account.Issuer,
            account.Algorithm,
            account.Digits,
            account.Period
        );

        var ascii = _qrCodeService.GenerateQrCodeAscii( uri );
        var dialog = BuildQrDialog( ascii, uri );

        Application.Run( dialog );
        dialog.Dispose( );
    }

    private static Dialog BuildQrDialog( string ascii, string uri )
    {
        var dialog = new Dialog( "QR Code", 70, 24 );

        var textView = new TextView
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill( ) - 2,
            Height = Dim.Fill( ) - 3,
            ReadOnly = true,
            Text = ascii + Environment.NewLine + uri
        };

        dialog.Add( textView );

        var close = new Button( "Close" );
        close.Clicked += () => Application.RequestStop( );
        dialog.AddButton( close );

        return dialog;
    }

    private static void CopySelectedCode( )
    {
        var account = GetSelectedAccount( );
        if ( account == null )
        {
            ShowInfo( "Copy Code", "No account selected." );
            return;
        }

        try
        {
            var code = _totpService.GenerateTotp( account );

            if ( TrySetClipboardText( code, out var clipboardError ) )
            {
                ShowInfo( "Copy Code", "Code copied to clipboard." );
                return;
            }

            ShowError( "Copy Code", BuildClipboardErrorMessage( clipboardError ) );
        }
        catch ( Exception ex )
        {
            ShowError( "Copy Code", ex.Message );
        }
    }

    private static string BuildClipboardErrorMessage( string clipboardError )
    {
        var hint = "Clipboard helper not found. Install wl-clipboard (Wayland) or xclip/xsel (X11).";
        return string.IsNullOrWhiteSpace( clipboardError )
            ? hint
            : $"{clipboardError}\n\n{hint}";
    }

    private static void ShowAbout( )
    {
        ShowInfo( "About", "QuickOTP TUI using Terminal.Gui" );
    }
}

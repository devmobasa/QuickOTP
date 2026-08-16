using System;
using System.Linq;
using QuickOTP.Core.Models;

namespace QuickOTP.Popup;

public partial class MainWindow
{
    private void LoadAccounts( )
    {
        _allAccounts.Clear( );
        _vaultLoadFailed = _storageService is null;

        if ( _vaultLoadFailed )
        {
            UpdateCodes( );
            return;
        }

        try
        {
            foreach ( var account in LoadSortedAccounts( ) )
            {
                _allAccounts.Add( new AccountListItem( account ) );
            }
        }
        catch
        {
            _vaultLoadFailed = true;
        }

        UpdateCodes( );
    }

    private IOrderedEnumerable<TotpAccount> LoadSortedAccounts( )
    {
        return _storageService!.LoadAccounts( )
            .OrderBy( account => account.Issuer )
            .ThenBy( account => account.Name );
    }

    private void UpdateCodes( )
    {
        foreach ( var item in _allAccounts )
        {
            item.Code = GenerateCodeSafely( item );
            item.RemainingSeconds = _totpService.GetRemainingSeconds( item.Account.Period );
        }
    }

    private string GenerateCodeSafely( AccountListItem item )
    {
        try
        {
            return _totpService.GenerateTotp( item.Account );
        }
        catch
        {
            return "Error";
        }
    }

    private void ApplyFilter( string? query )
    {
        var trimmedQuery = (query ?? string.Empty).Trim( );

        FilteredAccounts.Clear( );
        foreach ( var item in _allAccounts )
        {
            if ( IsMatch( trimmedQuery, item.SearchText ) )
            {
                FilteredAccounts.Add( item );
            }
        }

        UpdateListSelectionState( );
    }

    private void UpdateListSelectionState( )
    {
        SearchBox.IsEnabled = !_vaultLoadFailed;
        AccountsList.IsVisible = !_vaultLoadFailed;
        EmptyState.IsVisible = !_vaultLoadFailed && FilteredAccounts.Count == 0;
        ErrorState.IsVisible = _vaultLoadFailed;

        if ( !_vaultLoadFailed && FilteredAccounts.Count > 0 )
        {
            AccountsList.SelectedIndex = 0;
        }
    }

    private static bool IsMatch( string query, string text )
    {
        if ( string.IsNullOrWhiteSpace( query ) )
        {
            return true;
        }

        return FuzzyMatch( query, text );
    }

    private static bool FuzzyMatch( string query, string text )
    {
        var normalizedQuery = query.ToLowerInvariant( );
        var normalizedText = text.ToLowerInvariant( );

        var queryIndex = 0;
        for ( var i = 0; i < normalizedText.Length && queryIndex < normalizedQuery.Length; i++ )
        {
            if ( normalizedText[i] == normalizedQuery[queryIndex] )
            {
                queryIndex++;
            }
        }

        return queryIndex == normalizedQuery.Length;
    }
}

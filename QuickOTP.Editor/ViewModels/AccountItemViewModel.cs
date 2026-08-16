using CommunityToolkit.Mvvm.ComponentModel;
using QuickOTP.Core.Configuration;
using QuickOTP.Core.Models;

namespace QuickOTP.Editor.ViewModels;

public sealed partial class AccountItemViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor( nameof( FormattedCode ) )]
    private string _code = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor( nameof( RemainingLabel ) )]
    [NotifyPropertyChangedFor( nameof( Progress ) )]
    [NotifyPropertyChangedFor( nameof( IsUrgent ) )]
    private int _remainingSeconds;

    public AccountItemViewModel( TotpAccount account )
    {
        Account = account;
        Issuer = string.IsNullOrWhiteSpace( account.Issuer ) ? AppConstants.Display.Unknown : account.Issuer.Trim( );
        AccountName = string.IsNullOrWhiteSpace( account.Name ) ? AppConstants.Display.DefaultAccount : account.Name.Trim( );
        DisplayName = $"{Issuer} · {AccountName}";
        Initials = CodeFormatter.Initials( Issuer );
        SearchText = $"{Issuer} {AccountName}";
    }

    public TotpAccount Account { get; }

    public string Issuer { get; }

    public string AccountName { get; }

    public string DisplayName { get; }

    public string Initials { get; }

    public string SearchText { get; }

    public string FormattedCode => CodeFormatter.Format( Code );

    public string RemainingLabel => CodeFormatter.RemainingLabel( RemainingSeconds );

    public double Progress => Account.Period <= 0 ? 0 : RemainingSeconds / (double)Account.Period;

    public bool IsUrgent => RemainingSeconds <= 5;

    public bool Matches( string query )
    {
        if ( string.IsNullOrWhiteSpace( query ) )
        {
            return true;
        }

        var normalizedQuery = query.Trim( ).ToLowerInvariant( );
        var haystack = SearchText.ToLowerInvariant( );
        if ( haystack.Contains( normalizedQuery ) )
        {
            return true;
        }

        var queryIndex = 0;
        foreach ( var character in haystack )
        {
            if ( character == normalizedQuery[queryIndex] )
            {
                queryIndex++;
                if ( queryIndex == normalizedQuery.Length )
                {
                    return true;
                }
            }
        }

        return false;
    }
}

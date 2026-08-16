using System.ComponentModel;
using System.Runtime.CompilerServices;
using QuickOTP.Core.Configuration;
using QuickOTP.Core.Models;

namespace QuickOTP.Popup;

public sealed class AccountListItem : INotifyPropertyChanged
{
    private string _code = string.Empty;
    private int _remainingSeconds;

    public AccountListItem( TotpAccount account )
    {
        Account = account;

        var issuer = NormalizeDisplayValue( account.Issuer, AppConstants.Display.Unknown );
        var name = NormalizeDisplayValue( account.Name, AppConstants.Display.DefaultAccount );

        DisplayName = BuildDisplayName( issuer, name );
        SearchText = BuildSearchText( issuer, name );
    }

    public TotpAccount Account { get; }

    public string DisplayName { get; }

    public string SearchText { get; }

    public string Code
    {
        get => _code;
        set => SetField( ref _code, value );
    }

    public int RemainingSeconds
    {
        get => _remainingSeconds;
        set
        {
            if ( SetField( ref _remainingSeconds, value ) )
            {
                OnPropertyChanged( nameof( RemainingLabel ) );
            }
        }
    }

    public string RemainingLabel => $"{RemainingSeconds}s";

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetField<T>( ref T field, T value, [CallerMemberName] string? propertyName = null )
    {
        if ( Equals( field, value ) )
        {
            return false;
        }

        field = value;
        OnPropertyChanged( propertyName );
        return true;
    }

    private void OnPropertyChanged( [CallerMemberName] string? propertyName = null ) => PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) );

    private static string NormalizeDisplayValue( string? value, string fallback )
    {
        return string.IsNullOrWhiteSpace( value ) ? fallback : value.Trim( );
    }

    private static string BuildDisplayName( string issuer, string name )
    {
        return $"{issuer} - {name}";
    }

    private static string BuildSearchText( string issuer, string name )
    {
        return $"{issuer} {name}";
    }
}

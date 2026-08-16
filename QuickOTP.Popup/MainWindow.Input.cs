using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace QuickOTP.Popup;

public partial class MainWindow
{
    private void OnSearchChanged( object? sender, TextChangedEventArgs e )
    {
        ApplyFilter( SearchBox.Text );
    }

    private async void OnItemDoubleTapped( object? sender, TappedEventArgs e )
    {
        await CopySelectedAndCloseAsync( );
    }

    private async void OnItemTapped( object? sender, TappedEventArgs e )
    {
        if ( FindListBoxItem( e.Source ) is null )
        {
            return;
        }

        await CopySelectedAndCloseAsync( );
    }

    private async void OnListPointerReleased( object? sender, PointerReleasedEventArgs e )
    {
        if ( e.InitialPressMouseButton != MouseButton.Left )
        {
            return;
        }

        var listBoxItem = FindListBoxItem( e.Source );
        if ( listBoxItem is null )
        {
            return;
        }

        if ( listBoxItem.DataContext is AccountListItem item )
        {
            AccountsList.SelectedItem = item;
        }

        await CopySelectedAndCloseAsync( );
    }

    private async void OnDefaultCopyClick( object? sender, RoutedEventArgs e )
    {
        await CopySelectedAndCloseAsync( );
    }

    private async void OnKeyDown( object? sender, KeyEventArgs e )
    {
        if ( IsEnterKey( e.Key ) )
        {
            await CopySelectedAndCloseAsync( );
            e.Handled = true;
            return;
        }

        if ( e.Key == Key.Escape )
        {
            RequestClose( );
            e.Handled = true;
            return;
        }

        if ( e.Key != Key.Down && e.Key != Key.Up )
        {
            return;
        }

        if ( MoveSelection( e.Key == Key.Down ? 1 : -1 ) )
        {
            e.Handled = true;
        }
    }

    private bool MoveSelection( int delta )
    {
        if ( FilteredAccounts.Count == 0 )
        {
            return false;
        }

        var selectedIndex = AccountsList.SelectedIndex;
        var nextIndex = selectedIndex < 0
            ? 0
            : Math.Clamp( selectedIndex + delta, 0, FilteredAccounts.Count - 1 );

        AccountsList.SelectedIndex = nextIndex;

        var selectedItem = AccountsList.SelectedItem;
        if ( selectedItem != null )
        {
            AccountsList.ScrollIntoView( selectedItem );
        }

        return true;
    }

    private static ListBoxItem? FindListBoxItem( object? source )
    {
        var visual = source as Visual;
        while ( visual != null )
        {
            if ( visual is ListBoxItem item )
            {
                return item;
            }

            visual = visual.GetVisualParent( );
        }

        return null;
    }

    private static bool IsEnterKey( Key key )
    {
        return (EnterKey.HasValue && key == EnterKey.Value)
               || (ReturnKey.HasValue && key == ReturnKey.Value)
               || (NumpadEnterKey.HasValue && key == NumpadEnterKey.Value);
    }

    private static Key? TryParseKey( string name )
    {
        return Enum.TryParse<Key>( name, out var key ) ? key : null;
    }
}

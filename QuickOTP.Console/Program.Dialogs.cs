using Terminal.Gui;

namespace QuickOTP.Console;

internal static partial class Program
{
    private static bool TryPromptForRequiredText( string title, string label, string requiredError, out string value )
    {
        value = string.Empty;

        if ( !TryPromptForText( title, label, string.Empty, false, out var promptedValue ) )
        {
            return false;
        }

        var trimmed = promptedValue.Trim( );
        if ( string.IsNullOrWhiteSpace( trimmed ) )
        {
            ShowError( title, requiredError );
            return false;
        }

        value = trimmed;
        return true;
    }

    private static void ShowInfo( string title, string message )
    {
        MessageBox.Query( title, message, "Ok" );
    }

    private static void ShowError( string title, string message )
    {
        MessageBox.ErrorQuery( title, message, "Ok" );
    }

    private static bool Confirm( string title, string message )
    {
        return MessageBox.Query( title, message, "Yes", "No" ) == 0;
    }

    private static bool TryPromptForText( string title, string label, string initial, bool secret, out string value )
    {
        value = string.Empty;

        var dialog = new Dialog( title, 60, 8 );
        var textField = BuildPromptTextField( initial, secret );
        var confirmed = false;

        dialog.Add( new Label( label ) { X = 1, Y = 1 } );
        dialog.Add( textField );
        AddPromptButtons( dialog, () => confirmed = true );

        Application.Run( dialog );
        var textValue = textField.Text.ToString( ) ?? string.Empty;
        dialog.Dispose( );

        if ( !confirmed )
        {
            return false;
        }

        value = textValue;
        return true;
    }

    private static TextField BuildPromptTextField( string initial, bool secret )
    {
        var textField = new TextField( initial ) { X = 1, Y = 2, Width = Dim.Fill( ) - 2 };
        if ( secret )
        {
            textField.Secret = true;
        }

        return textField;
    }

    private static void AddPromptButtons( Dialog dialog, Action onConfirm )
    {
        var ok = new Button( "Ok" );
        ok.Clicked += () =>
        {
            onConfirm( );
            Application.RequestStop( );
        };

        var cancel = new Button( "Cancel" );
        cancel.Clicked += () => Application.RequestStop( );

        dialog.AddButton( ok );
        dialog.AddButton( cancel );
    }
}

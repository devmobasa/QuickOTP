using SystemConsole = System.Console;

namespace QuickOTP.Console.Services;

public class ClipboardService
{
    private const string SetupHint = "(To enable clipboard, install TextCopy package: dotnet add package TextCopy)";

    public void SetText( string text )
    {
        WriteCodeToConsole( text );
        WriteSetupHint( );
    }

    private static void WriteCodeToConsole( string text ) => SystemConsole.WriteLine( $"Code: {text}" );

    private static void WriteSetupHint( ) => SystemConsole.WriteLine( SetupHint );
}

using Avalonia;
using Avalonia.Fonts.Inter;

namespace QuickOTP.Editor;

internal static class Program
{
    [STAThread]
    public static void Main( string[] args ) => StartDesktopLifetime( args );

    public static AppBuilder BuildAvaloniaApp( ) => ConfigureBuilder( AppBuilder.Configure<App>( ) );

    private static void StartDesktopLifetime( string[] args )
    {
        BuildAvaloniaApp( ).StartWithClassicDesktopLifetime( args );
    }

    private static AppBuilder ConfigureBuilder( AppBuilder builder )
    {
        return builder
            .UsePlatformDetect( )
            .WithInterFont( )
            .LogToTrace( );
    }
}

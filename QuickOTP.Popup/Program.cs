using System;
using Avalonia;

namespace QuickOTP.Popup;

internal static class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main( string[] args ) => StartDesktopLifetime( args );

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp( ) => ConfigureBuilder( AppBuilder.Configure<App>( ) );

    private static void StartDesktopLifetime( string[] args )
    {
        BuildAvaloniaApp( ).StartWithClassicDesktopLifetime( args );
    }

    private static AppBuilder ConfigureBuilder( AppBuilder builder )
    {
        return builder
            .UsePlatformDetect( )
            .LogToTrace( );
    }
}

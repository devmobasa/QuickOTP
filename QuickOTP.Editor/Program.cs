using Avalonia;
using Avalonia.Dialogs;
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
        builder = builder
            .UsePlatformDetect( )
            .WithInterFont( )
            .LogToTrace( );

        if ( OperatingSystem.IsLinux( ) )
        {
            builder = builder.UseManagedSystemDialogs( );
        }

        return builder;
    }
}

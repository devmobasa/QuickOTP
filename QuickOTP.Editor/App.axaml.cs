using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using QuickOTP.Core.Services;
using QuickOTP.Editor.ViewModels;
using QuickOTP.Editor.Views;

namespace QuickOTP.Editor;

public partial class App : Application
{
    public override void Initialize( ) => AvaloniaXamlLoader.Load( this );

    public override void OnFrameworkInitializationCompleted( )
    {
        if ( ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop )
        {
            desktop.MainWindow = CreateMainWindow( );
        }

        base.OnFrameworkInitializationCompleted( );
    }

    private static Window CreateMainWindow( )
    {
        if ( !TryCreateStorage( out var storage, out var error ) )
        {
            return CreateStorageErrorWindow( error );
        }

        return new MainWindow
        {
            DataContext = new MainWindowViewModel( storage )
        };
    }

    private static bool TryCreateStorage( out StorageService storage, out string error )
    {
        try
        {
            storage = new StorageService( );
            error = string.Empty;
            return true;
        }
        catch ( Exception ex )
        {
            storage = null!;
            error = ex.Message;
            return false;
        }
    }

    private static Window CreateStorageErrorWindow( string details )
    {
        var closeButton = new Button
        {
            Content = "Close",
            Classes = { "tide" },
            HorizontalAlignment = HorizontalAlignment.Right,
            MinWidth = 96,
            IsDefault = true,
            IsCancel = true
        };

        var window = new Window
        {
            Title = "QuickOTP Vault",
            Width = 520,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            CanResize = false,
            Content = new Border
            {
                Padding = new Thickness( 28 ),
                Child = new StackPanel
                {
                    Spacing = 16,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "Could not open the vault",
                            FontSize = 22,
                            FontWeight = FontWeight.SemiBold
                        },
                        new TextBlock
                        {
                            Text = "The local key file could not be read. If this vault is password-protected, set the master password environment variable and try again.",
                            TextWrapping = TextWrapping.Wrap,
                            Opacity = 0.8
                        },
                        new TextBlock
                        {
                            Text = details,
                            TextWrapping = TextWrapping.Wrap,
                            FontSize = 12,
                            Opacity = 0.7
                        },
                        closeButton
                    }
                }
            }
        };

        closeButton.Click += ( _, _ ) => window.Close( );
        return window;
    }
}

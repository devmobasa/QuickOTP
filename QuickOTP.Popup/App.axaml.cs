using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using QuickOTP.Core.Services;

namespace QuickOTP.Popup;

public partial class App : Application
{
    public override void Initialize( ) => AvaloniaXamlLoader.Load( this );

    public override void OnFrameworkInitializationCompleted( )
    {
        var desktop = GetDesktopLifetime( );
        if ( desktop != null )
        {
            desktop.MainWindow = CreateMainWindow( );
        }

        base.OnFrameworkInitializationCompleted( );
    }

    private IClassicDesktopStyleApplicationLifetime? GetDesktopLifetime( ) =>
        ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;

    private static Window CreateMainWindow( )
    {
        if ( !TryCreateStorage( out var storage, out var error ) )
        {
            return CreateStorageErrorWindow( error );
        }

        return new MainWindow( storage );
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
            HorizontalAlignment = HorizontalAlignment.Right,
            MinWidth = 96,
            Padding = new Thickness( 16, 8 ),
            IsDefault = true,
            IsCancel = true
        };

        var window = new Window
        {
            Title = "2FA Codes",
            Width = 480,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            Topmost = true,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Content = new Border
            {
                Background = new SolidColorBrush( Color.Parse( "#F5F1E9" ) ),
                Padding = new Thickness( 24 ),
                Child = new StackPanel
                {
                    Spacing = 14,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "Could not open the vault",
                            FontSize = 20,
                            FontWeight = FontWeight.SemiBold,
                            Foreground = new SolidColorBrush( Color.Parse( "#8B3A2F" ) )
                        },
                        new TextBlock
                        {
                            Text = "The local key file could not be read. If this vault is password-protected, set the master password environment variable and try again.",
                            TextWrapping = TextWrapping.Wrap,
                            Foreground = new SolidColorBrush( Color.Parse( "#6F6A63" ) )
                        },
                        new TextBlock
                        {
                            Text = details,
                            TextWrapping = TextWrapping.Wrap,
                            FontSize = 12,
                            Foreground = new SolidColorBrush( Color.Parse( "#6F6A63" ) )
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

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using QuickOTP.Core.Configuration;

namespace QuickOTP.Popup;

public partial class MainWindow
{
    private async Task CopySelectedAndCloseAsync( )
    {
        if ( _isClosing )
        {
            return;
        }

        var selected = GetSelectedItem( );
        if ( selected == null )
        {
            return;
        }

        _isClosing = true;

        try
        {
            if ( !await TryCopyWithWlCopyAsync( selected.Code ) )
            {
                await TryCopyWithAvaloniaClipboardAsync( selected.Code );
            }
        }
        catch
        {
            // Ignore clipboard failures; still close the window.
        }

        RequestClose( );
    }

    private AccountListItem? GetSelectedItem( )
    {
        var selected = AccountsList.SelectedItem as AccountListItem;
        if ( selected != null )
        {
            return selected;
        }

        if ( FilteredAccounts.Count == 0 )
        {
            return null;
        }

        selected = FilteredAccounts[0];
        AccountsList.SelectedIndex = 0;
        return selected;
    }

    private async Task TryCopyWithAvaloniaClipboardAsync( string text )
    {
        var clipboard = TopLevel.GetTopLevel( this )?.Clipboard;
        if ( clipboard == null )
        {
            return;
        }

        var setTask = clipboard.SetTextAsync( text );
        await Task.WhenAny( setTask, Task.Delay( 750 ) );
    }

    private void RequestClose( )
    {
        Hide( );
        _exitTimer.Stop( );
        _exitTimer.Start( );
    }

    private static async Task<bool> TryCopyWithWlCopyAsync( string text )
    {
        if ( !OperatingSystem.IsLinux( ) )
        {
            return false;
        }

        if ( string.IsNullOrWhiteSpace( Environment.GetEnvironmentVariable( AppConstants.Clipboard.WaylandDisplayEnv ) ) )
        {
            return false;
        }

        var wlCopyPath = FindExecutableOnPath( AppConstants.Clipboard.WlCopyCommand );
        if ( wlCopyPath is null )
        {
            return false;
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = wlCopyPath,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            startInfo.ArgumentList.Add( "--type" );
            startInfo.ArgumentList.Add( AppConstants.Clipboard.WlCopyMimeText );

            using var process = Process.Start( startInfo );
            if ( process == null )
            {
                return false;
            }

            await process.StandardInput.WriteAsync( text );
            process.StandardInput.Close( );

            var waitForExit = process.WaitForExitAsync( );
            var completed = await Task.WhenAny( waitForExit, Task.Delay( 1500 ) );
            if ( completed != waitForExit || !process.HasExited )
            {
                TryKillProcess( process );
                return false;
            }

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static string? FindExecutableOnPath( string name )
    {
        var path = Environment.GetEnvironmentVariable( "PATH" );
        if ( string.IsNullOrWhiteSpace( path ) )
        {
            return null;
        }

        foreach ( var segment in path.Split( Path.PathSeparator ) )
        {
            if ( string.IsNullOrWhiteSpace( segment ) )
            {
                continue;
            }

            var candidate = Path.Combine( segment.Trim( ), name );
            if ( File.Exists( candidate ) )
            {
                return candidate;
            }
        }

        return null;
    }

    private static void TryKillProcess( Process process )
    {
        try
        {
            if ( !process.HasExited )
            {
                process.Kill( entireProcessTree: true );
            }
        }
        catch
        {
            // Best-effort cleanup when wl-copy hangs.
        }
    }
}

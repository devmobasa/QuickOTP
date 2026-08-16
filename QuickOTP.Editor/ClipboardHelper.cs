using System.Diagnostics;
using QuickOTP.Core.Configuration;

namespace QuickOTP.Editor;

internal static class ClipboardHelper
{
    public static async Task<bool> TryCopyAsync( Avalonia.Controls.TopLevel? topLevel, string text )
    {
        if ( await TryCopyWithWlCopyAsync( text ) )
        {
            return true;
        }

        var clipboard = topLevel?.Clipboard;
        if ( clipboard == null )
        {
            return false;
        }

        try
        {
            await clipboard.SetTextAsync( text );
            return true;
        }
        catch
        {
            return false;
        }
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

            startInfo.ArgumentList.Add( AppConstants.Clipboard.WlCopyTypeFlag );
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

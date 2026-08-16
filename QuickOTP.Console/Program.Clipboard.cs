using System.Diagnostics;
using QuickOTP.Core.Configuration;

namespace QuickOTP.Console;

internal static partial class Program
{
    private static bool TrySetClipboardText( string text, out string error )
    {
        error = string.Empty;

        if ( _clipboard != null )
        {
            try
            {
                _clipboard.SetText( text );
                return true;
            }
            catch ( Exception ex )
            {
                error = ex.Message;
            }
        }

        if ( !OperatingSystem.IsLinux( ) )
        {
            return false;
        }

        if ( IsWayland( ) && TryRunClipboardCommand( AppConstants.Clipboard.WlCopyCommand, new[]
             {
                 AppConstants.Clipboard.WlCopyTypeFlag,
                 AppConstants.Clipboard.WlCopyMimeText
             }, text ) )
        {
            return true;
        }

        if ( IsX11( ) && TryRunClipboardCommand( AppConstants.Clipboard.XClipCommand, new[]
             {
                 AppConstants.Clipboard.XClipSelectionFlag,
                 AppConstants.Clipboard.XClipSelectionClipboard
             }, text ) )
        {
            return true;
        }

        return TryRunClipboardCommand( AppConstants.Clipboard.XSelCommand, new[]
        {
            AppConstants.Clipboard.XSelClipboardFlag,
            AppConstants.Clipboard.XSelInputFlag
        }, text );
    }

    private static bool IsWayland( )
    {
        return !string.IsNullOrWhiteSpace( Environment.GetEnvironmentVariable( AppConstants.Clipboard.WaylandDisplayEnv ) );
    }

    private static bool IsX11( )
    {
        return !string.IsNullOrWhiteSpace( Environment.GetEnvironmentVariable( AppConstants.Clipboard.X11DisplayEnv ) );
    }

    private static bool TryRunClipboardCommand( string fileName, string[] args, string text )
    {
        try
        {
            var startInfo = BuildClipboardCommandStartInfo( fileName, args );
            using var process = Process.Start( startInfo );
            if ( process == null )
            {
                return false;
            }

            process.StandardInput.Write( text );
            process.StandardInput.Close( );

            if ( !process.WaitForExit( 1500 ) )
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

    private static ProcessStartInfo BuildClipboardCommandStartInfo( string fileName, string[] args )
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach ( var arg in args )
        {
            startInfo.ArgumentList.Add( arg );
        }

        return startInfo;
    }

    private static void TryKillProcess( Process process )
    {
        try
        {
            process.Kill( );
        }
        catch
        {
            // Ignore kill failures.
        }
    }
}

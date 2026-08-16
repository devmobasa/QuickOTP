using System.Diagnostics;

namespace QuickOTP.Core.Services;

internal sealed partial class KeychainService
{
    private static bool TryDecodeKey( string value, out byte[] key )
    {
        try
        {
            key = Convert.FromBase64String( value.Trim( ) );
            return key.Length == ExpectedKeySizeBytes;
        }
        catch
        {
            key = [];
            return false;
        }
    }

    private static ProcessResult RunProcess( string fileName, string[] args, string? stdin = null )
    {
        try
        {
            var startInfo = CreateProcessStartInfo( fileName, args, stdin != null );
            using var process = Process.Start( startInfo );
            if ( process == null )
            {
                return new ProcessResult( false, string.Empty, "Failed to start process." );
            }

            var stdoutTask = process.StandardOutput.ReadToEndAsync( );
            var stderrTask = process.StandardError.ReadToEndAsync( );

            if ( stdin != null )
            {
                process.StandardInput.Write( stdin );
                process.StandardInput.Close( );
            }

            if ( !process.WaitForExit( ProcessTimeoutMs ) )
            {
                TryKillProcess( process );
                return new ProcessResult( false, string.Empty, "Process timeout." );
            }

            var stdout = stdoutTask.GetAwaiter( ).GetResult( );
            var stderr = stderrTask.GetAwaiter( ).GetResult( );
            return new ProcessResult( process.ExitCode == 0, stdout, stderr );
        }
        catch ( Exception ex )
        {
            return new ProcessResult( false, string.Empty, $"Process failed: {ex.Message}" );
        }
    }

    private static ProcessStartInfo CreateProcessStartInfo( string fileName, string[] args, bool redirectInput )
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = redirectInput,
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
            // Ignore kill errors.
        }
    }

    private readonly struct ProcessResult
    {
        public bool Success { get; }
        public string Stdout { get; }
        public string Stderr { get; }

        public ProcessResult( bool success, string stdout, string stderr )
        {
            Success = success;
            Stdout = stdout;
            Stderr = stderr;
        }
    }
}

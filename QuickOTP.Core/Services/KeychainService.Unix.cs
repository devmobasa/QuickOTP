namespace QuickOTP.Core.Services;

internal sealed partial class KeychainService
{
    private bool TryReadMac( out byte[] key )
    {
        key = [];

        var result = RunProcess( "security", [
            "find-generic-password",
            "-a",
            _account,
            "-s",
            _service,
            "-w"
        ] );

        if ( !result.Success )
        {
            return false;
        }

        var secret = result.Stdout.Trim( );
        return TryDecodeKey( secret, out key );
    }

    private bool TryWriteMac( byte[] key )
    {
        var secret = Convert.ToBase64String( key );

        var result = RunProcess( "security", [
            "add-generic-password",
            "-a",
            _account,
            "-s",
            _service,
            "-U"
        ], secret + Environment.NewLine );

        return result.Success;
    }

    private bool TryDeleteMac( )
    {
        var result = RunProcess( "security", [
            "delete-generic-password",
            "-a",
            _account,
            "-s",
            _service
        ] );

        return result.Success;
    }

    private bool TryReadLinux( out byte[] key )
    {
        key = [];

        var result = RunProcess( "secret-tool", [
            "lookup",
            "service",
            _service,
            "account",
            _account
        ] );

        if ( !result.Success )
        {
            return false;
        }

        var secret = result.Stdout.Trim( );
        return TryDecodeKey( secret, out key );
    }

    private bool TryWriteLinux( byte[] key )
    {
        var secret = Convert.ToBase64String( key );

        var result = RunProcess( "secret-tool", [
            "store",
            "--label",
            _service,
            "service",
            _service,
            "account",
            _account
        ], secret );

        return result.Success;
    }

    private bool TryDeleteLinux( )
    {
        var result = RunProcess( "secret-tool", [
            "clear",
            "service",
            _service,
            "account",
            _account
        ] );

        return result.Success;
    }
}

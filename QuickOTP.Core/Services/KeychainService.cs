namespace QuickOTP.Core.Services;

internal sealed partial class KeychainService
{
    private const int ExpectedKeySizeBytes = 32;
    private const int ProcessTimeoutMs = 10000;
    private const uint CredentialTypeGeneric = 1;
    private const uint CredentialPersistLocalMachine = 2;

    private readonly string _service;
    private readonly string _account;

    public KeychainService( string service, string account )
    {
        _service = service;
        _account = account;
    }

    public bool TryReadKey( out byte[] key )
    {
        if ( OperatingSystem.IsWindows( ) )
        {
            return TryReadWindows( out key );
        }

        if ( OperatingSystem.IsMacOS( ) )
        {
            return TryReadMac( out key );
        }

        if ( OperatingSystem.IsLinux( ) )
        {
            return TryReadLinux( out key );
        }

        key = [];
        return false;
    }

    public bool TryWriteKey( byte[] key )
    {
        if ( OperatingSystem.IsWindows( ) )
        {
            return TryWriteWindows( key );
        }

        if ( OperatingSystem.IsMacOS( ) )
        {
            return TryWriteMac( key );
        }

        if ( OperatingSystem.IsLinux( ) )
        {
            return TryWriteLinux( key );
        }

        return false;
    }

    public bool TryDeleteKey( )
    {
        if ( OperatingSystem.IsWindows( ) )
        {
            return TryDeleteWindows( );
        }

        if ( OperatingSystem.IsMacOS( ) )
        {
            return TryDeleteMac( );
        }

        if ( OperatingSystem.IsLinux( ) )
        {
            return TryDeleteLinux( );
        }

        return false;
    }

    private string TargetName => $"{_service}:{_account}";
}

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace QuickOTP.Core.Services;

internal sealed partial class KeychainService
{
    private bool TryReadWindows( out byte[] key )
    {
        key = [];

        if ( !CredRead( TargetName, CredentialTypeGeneric, 0, out var credentialPtr ) )
        {
            return false;
        }

        try
        {
            var credential = Marshal.PtrToStructure<CREDENTIAL>( credentialPtr );
            if ( credential.CredentialBlobSize == 0 )
            {
                return false;
            }

            var blob = new byte[credential.CredentialBlobSize];
            Marshal.Copy( credential.CredentialBlob, blob, 0, (int)credential.CredentialBlobSize );

            var secret = Encoding.UTF8.GetString( blob );
            return TryDecodeKey( secret, out key );
        }
        finally
        {
            CredFree( credentialPtr );
        }
    }

    private bool TryWriteWindows( byte[] key )
    {
        var secret = Convert.ToBase64String( key );
        var secretBytes = Encoding.UTF8.GetBytes( secret );
        var blobPtr = Marshal.AllocCoTaskMem( secretBytes.Length );

        try
        {
            Marshal.Copy( secretBytes, 0, blobPtr, secretBytes.Length );

            var credential = new CREDENTIAL
            {
                Flags = 0,
                Type = CredentialTypeGeneric,
                TargetName = TargetName,
                Comment = null,
                CredentialBlobSize = (uint)secretBytes.Length,
                CredentialBlob = blobPtr,
                Persist = CredentialPersistLocalMachine,
                AttributeCount = 0,
                Attributes = IntPtr.Zero,
                TargetAlias = null,
                UserName = _account
            };

            return CredWrite( ref credential, 0 );
        }
        finally
        {
            Marshal.FreeCoTaskMem( blobPtr );
        }
    }

    private bool TryDeleteWindows( )
    {
        return CredDelete( TargetName, CredentialTypeGeneric, 0 );
    }

    [StructLayout( LayoutKind.Sequential, CharSet = CharSet.Unicode )]
    private struct CREDENTIAL
    {
        public uint Flags;
        public uint Type;
        public string TargetName;
        public string? Comment;
        public FILETIME LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public string? TargetAlias;
        public string? UserName;
    }

    [DllImport( "advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true )]
    private static extern bool CredRead(
        string target,
        uint type,
        uint reservedFlag,
        out IntPtr credentialPtr );

    [DllImport( "advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true )]
    private static extern bool CredWrite( ref CREDENTIAL userCredential, uint flags );

    [DllImport( "advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true )]
    private static extern bool CredDelete( string target, uint type, uint flags );

    [DllImport( "advapi32.dll", SetLastError = true )]
    private static extern void CredFree( IntPtr buffer );
}

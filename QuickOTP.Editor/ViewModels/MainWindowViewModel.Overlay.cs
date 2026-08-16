namespace QuickOTP.Editor.ViewModels;

public sealed partial class MainWindowViewModel
{
    private CancellationTokenSource? _toastCancellation;

    public bool ConfirmIfDirty( Action continueWith )
    {
        if ( !HasUnsavedChanges )
        {
            return true;
        }

        PromptConfirm(
            "Discard unsaved changes?",
            "You have edits that have not been saved.",
            "Discard",
            continueWith );
        return false;
    }

    public void PromptConfirm( string title, string message, string primaryLabel, Action onConfirm )
    {
        _confirmAction = onConfirm;
        OverlayTitle = title;
        OverlayMessage = message;
        OverlayPrimaryLabel = primaryLabel;
        OverlayRequiresPasswordConfirm = false;
        Overlay = OverlayKind.Confirm;
    }

    public void PromptPassword(
        string title,
        string message,
        Func<string, Task> onPassword,
        bool requireConfirm = false )
    {
        _passwordConsumer = onPassword;
        PasswordInput = string.Empty;
        ConfirmPasswordInput = string.Empty;
        OverlayTitle = title;
        OverlayMessage = message;
        OverlayPrimaryLabel = requireConfirm ? "Save backup" : "Unlock";
        OverlayRequiresPasswordConfirm = requireConfirm;
        Overlay = OverlayKind.Password;
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void CancelOverlay( ) => CloseOverlay( );

    public void SubmitOpenOverlay( )
    {
        switch ( Overlay )
        {
            case OverlayKind.Confirm:
                ConfirmOverlayCommand.Execute( null );
                break;
            case OverlayKind.Password:
                SubmitPasswordCommand.Execute( null );
                break;
            case OverlayKind.ImportPreview:
                ConfirmImportCommand.Execute( null );
                break;
            case OverlayKind.UriImport:
                SubmitUriImportCommand.Execute( null );
                break;
        }
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private void ConfirmOverlay( )
    {
        var action = _confirmAction;
        CloseOverlay( );
        action?.Invoke( );
    }

    [CommunityToolkit.Mvvm.Input.RelayCommand]
    private async Task SubmitPasswordAsync( )
    {
        if ( OverlayRequiresPasswordConfirm && PasswordInput != ConfirmPasswordInput )
        {
            ShowToast( "Those passwords do not match." );
            return;
        }

        if ( string.IsNullOrWhiteSpace( PasswordInput ) )
        {
            ShowToast( "A password is required." );
            return;
        }

        var consumer = _passwordConsumer;
        var password = PasswordInput;
        CloseOverlay( );
        if ( consumer != null )
        {
            await consumer( password );
        }
    }

    public void CloseOverlay( )
    {
        Overlay = OverlayKind.None;
        OverlayTitle = string.Empty;
        OverlayMessage = string.Empty;
        PasswordInput = string.Empty;
        ConfirmPasswordInput = string.Empty;
        OverlayRequiresPasswordConfirm = false;
        _confirmAction = null;
        _passwordConsumer = null;
    }

    public void ShowToast( string message )
    {
        ToastMessage = message;
        _toastCancellation?.Cancel( );
        _toastCancellation?.Dispose( );
        var cancellation = new CancellationTokenSource( );
        _toastCancellation = cancellation;
        _ = HideToastAfterDelayAsync( cancellation.Token );
    }

    private async Task HideToastAfterDelayAsync( CancellationToken cancellationToken )
    {
        try
        {
            await Task.Delay( 3200, cancellationToken );
            ToastMessage = string.Empty;
        }
        catch ( TaskCanceledException )
        {
            // A newer toast replaced this one.
        }
    }
}

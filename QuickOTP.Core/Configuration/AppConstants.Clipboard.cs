namespace QuickOTP.Core.Configuration;

public static partial class AppConstants
{
    public static class Clipboard
    {
        public const string WaylandDisplayEnv = "WAYLAND_DISPLAY";
        public const string WlCopyCommand = "wl-copy";
        public const string WlCopyTypeFlag = "--type";
        public const string WlCopyMimeText = "text/plain;charset=utf-8";
        public const string X11DisplayEnv = "DISPLAY";
        public const string XClipCommand = "xclip";
        public const string XClipSelectionFlag = "-selection";
        public const string XClipSelectionClipboard = "clipboard";
        public const string XSelCommand = "xsel";
        public const string XSelClipboardFlag = "--clipboard";
        public const string XSelInputFlag = "--input";
    }
}

using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace FFPerformanceEngine.App;

public static partial class OverlayInterop
{
    private const int GwlExStyle = -20;
    private const long WsExTransparent = 0x20L;
    private const long WsExToolWindow = 0x80L;

    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static partial nint GetWindowLongPtr(nint hWnd, int nIndex);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static partial nint SetWindowLongPtr(nint hWnd, int nIndex, nint dwNewLong);

    public static void SetClickThrough(Window window, bool enabled)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == 0) return;
        var current = GetWindowLongPtr(hwnd, GwlExStyle).ToInt64();
        var next = enabled ? current | WsExTransparent | WsExToolWindow : (current & ~WsExTransparent) | WsExToolWindow;
        SetWindowLongPtr(hwnd, GwlExStyle, new nint(next));
    }
}

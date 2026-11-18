using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace HS175D
{
    /// <summary>
    /// Grabs AirSimNH's own rendered window pixels directly (PrintWindow) instead of asking AirSim's
    /// RPC API for a screenshot (simGetImage/simGetImages) - full native resolution/quality, no PNG
    /// encode/decode, no RPC round-trip at all. Requires the window to actually be presenting a real
    /// frame (so AirSim must run WITHOUT -RenderOffscreen - see ToggleAirSimProcess), but doesn't
    /// need to be visible on screen: AirSimProcessWatchLoop moves it off-screen once found, since
    /// DWM keeps compositing off-screen/occluded windows normally (only actually-minimized windows
    /// stop) - so this stays out of the way without needing true window embedding (SetParent), which
    /// risks focus/input/resize complications for a generic (non-embedding-aware) Unreal window.
    /// </summary>
    public static class WindowCapture
    {
        // System.Drawing (GDI+) is explicitly documented as not thread-safe - PrintWindow below
        // holds a GDI+ Graphics/HDC open for however long that call takes (bounded by the caller's
        // own timeout, but that only bounds how long the CALLER waits, not how long the HDC itself
        // stays held if the call is genuinely stuck against an unresponsive window). AirSimPanel's
        // own GDI+ work (RecomposeVideo, compositing the video+sticks+detections every frame) uses
        // Monitor.TryEnter on this SAME lock with a short timeout rather than a plain lock/await, so
        // a stuck PrintWindow call can only ever make it skip a redraw, never block the UI thread -
        // this is what was actually causing the freeze reported once XInput polling (GamepadLoop)
        // started calling RecomposeVideo at 60Hz, which made the two threads' GDI+ usage overlap far
        // more often than VideoLoop's own (slower) redraw cadence ever did on its own.
        public static readonly object GdiLock = new();

        [DllImport("user32.dll")] static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);
        [DllImport("user32.dll")] static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll")] static extern bool IsWindow(IntPtr hWnd);
        [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        [StructLayout(LayoutKind.Sequential)]
        struct RECT { public int Left, Top, Right, Bottom; }

        const uint PW_CLIENTONLY = 0x1;
        const uint PW_RENDERFULLCONTENT = 0x2;   // required for DWM-composited/DirectX content - plain PrintWindow just gives black for these
        static readonly IntPtr HWND_TOP = IntPtr.Zero;
        const uint SWP_NOZORDER = 0x0004;
        const uint SWP_SHOWWINDOW = 0x0040;

        /// <summary>First non-zero MainWindowHandle among all processes with this name - AirSimNH
        /// launches as a stub that spawns the real engine process (same as the kill/priority code
        /// elsewhere in this app), and the real game window can take a few seconds to appear, so
        /// callers should keep polling this rather than expect it immediately after launch.</summary>
        public static IntPtr FindMainWindow(string processName)
        {
            foreach (var p in Process.GetProcessesByName(processName))
            {
                p.Refresh();   // MainWindowHandle is cached at query time - stale otherwise
                IntPtr h = p.MainWindowHandle;
                p.Dispose();
                if (h != IntPtr.Zero) return h;
            }
            return IntPtr.Zero;
        }

        /// <summary>Moves the window far outside any monitor's bounds at a fixed, deliberately
        /// generous size - confirmed safe by manually dragging the window off-screen while it was
        /// running (capture kept working fine), which ruled out an earlier theory that off-screen
        /// positioning itself was the cause of a freeze seen after Connect. The actual freeze was
        /// most likely the caller (AirSimProcessWatchLoop) re-issuing this AND FindMainWindow every
        /// 1.5 seconds indefinitely - repeatedly calling into a foreign process's window is exactly
        /// the kind of thing that has no guaranteed bound on how long it can take if that process is
        /// ever slow to service it. Callers should invoke this ONCE, right after the window is first
        /// found, not on a recurring timer.</summary>
        public static void MoveOffscreen(IntPtr hwnd, int width = 1280, int height = 720)
        {
            if (hwnd != IntPtr.Zero && IsWindow(hwnd))
                SetWindowPos(hwnd, HWND_TOP, -3000, 0, width, height, SWP_NOZORDER | SWP_SHOWWINDOW);
        }

        /// <summary>One capture of the window's client area. Returns null if the window's gone or
        /// has no usable size (e.g. still loading).</summary>
        public static Bitmap? Capture(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero || !IsWindow(hwnd)) return null;
            if (!GetClientRect(hwnd, out var rect)) return null;
            int w = rect.Right - rect.Left, h = rect.Bottom - rect.Top;
            if (w <= 0 || h <= 0) return null;

            var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            lock (GdiLock)
            {
                using var g = Graphics.FromImage(bmp);
                IntPtr hdc = g.GetHdc();
                try { PrintWindow(hwnd, hdc, PW_CLIENTONLY | PW_RENDERFULLCONTENT); }
                finally { g.ReleaseHdc(hdc); }
            }
            return bmp;
        }
    }
}

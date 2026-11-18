using System.Runtime.InteropServices;

namespace HS175D
{
    /// <summary>
    /// PictureBox that also reports raw Windows Pointer (WM_POINTER) touch input, so two fingers can
    /// drag two different joysticks at once. Plain WinForms MouseDown/Move/Up can't do this: Windows
    /// only ever *promotes* ONE contact (the primary touch point) to legacy WM_LBUTTONDOWN/MOVE/UP
    /// messages, and AirSimPanel's existing handlers additionally rely on a single, exclusive
    /// _video.Capture - so a second simultaneous finger never generated a mouse event at all, and
    /// fighting over Capture could stall even the first one. WM_POINTER carries a distinct pointer ID
    /// per contact and isn't capture-exclusive, so each finger can be tracked independently.
    ///
    /// The promoted legacy mouse messages for the primary contact still arrive alongside these -
    /// AirSimPanel's Mouse handlers must skip them (via IsSyntheticTouchMessage) so that contact isn't
    /// driven twice (once here, once there) with two different coordinate deltas.
    /// </summary>
    public sealed class TouchPictureBox : PictureBox
    {
        const int WM_POINTERDOWN = 0x0246;
        const int WM_POINTERUPDATE = 0x0245;
        const int WM_POINTERUP = 0x0247;
        const int WM_POINTERCAPTURECHANGED = 0x0249;   // OS stole the pointer mid-drag (e.g. a system gesture) - treat like an up so the stick doesn't stick.
        const int PT_TOUCH = 2;                        // pen (3) and mouse (4) already work fine via the normal Mouse events, so only touch is handled here.

        [StructLayout(LayoutKind.Sequential)]
        struct POINT { public int X, Y; }

        [StructLayout(LayoutKind.Sequential)]
        struct POINTER_INFO
        {
            public int pointerType;
            public uint pointerId;
            public uint frameId;
            public int pointerFlags;
            public IntPtr sourceDevice;
            public IntPtr hwndTarget;
            public POINT ptPixelLocation;
            public POINT ptPixelLocationRaw;
            public POINT ptHimetricLocation;
            public POINT ptHimetricLocationRaw;
            public uint dwTime;
            public uint historyCount;
            public int InputData;
            public uint dwKeyStates;
            public ulong PerformanceCount;
            public int ButtonChangeType;
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetPointerInfo(uint pointerId, out POINTER_INFO pointerInfo);

        [DllImport("user32.dll")]
        static extern IntPtr GetMessageExtraInfo();

        const long MI_WP_SIGNATURE = 0xFF515700;
        const long SIGNATURE_MASK = 0xFFFFFF00;

        /// <summary>True if the Mouse event currently being handled was promoted from touch/pen input
        /// rather than a real mouse - callers use this to ignore the primary contact's promoted
        /// events, since TouchDown/Move/Up above already handles that same contact. Only valid when
        /// called synchronously from inside a Mouse event handler (reads the current message's extra
        /// info off this thread).</summary>
        public static bool IsSyntheticTouchMessage()
        {
            long extra = GetMessageExtraInfo().ToInt64();
            return (extra & SIGNATURE_MASK) == MI_WP_SIGNATURE;
        }

        /// <summary>Fired for a new touch contact; Point is in this control's client coordinates, same
        /// space as MouseEventArgs.Location.</summary>
        public event Action<int, Point>? TouchDown;
        public event Action<int, Point>? TouchMove;
        public event Action<int>? TouchUp;

        protected override void WndProc(ref Message m)
        {
            if (m.Msg is WM_POINTERDOWN or WM_POINTERUPDATE or WM_POINTERUP or WM_POINTERCAPTURECHANGED)
            {
                int pointerId = (int)((long)m.WParam & 0xFFFF);   // GET_POINTERID_WPARAM: low word of wParam
                if (m.Msg == WM_POINTERCAPTURECHANGED)
                {
                    TouchUp?.Invoke(pointerId);
                }
                else if (GetPointerInfo((uint)pointerId, out var info) && info.pointerType == PT_TOUCH)
                {
                    var client = PointToClient(new Point(info.ptPixelLocation.X, info.ptPixelLocation.Y));
                    switch (m.Msg)
                    {
                        case WM_POINTERDOWN: TouchDown?.Invoke(pointerId, client); break;
                        case WM_POINTERUPDATE: TouchMove?.Invoke(pointerId, client); break;
                        case WM_POINTERUP: TouchUp?.Invoke(pointerId); break;
                    }
                }
            }
            base.WndProc(ref m);
        }
    }
}

namespace HS175D
{
    /// <summary>
    /// Self-centering virtual stick: drag inside the ring, release to snap back to dead-centre.
    /// Reports both axes as 0-255 bytes (128 = centre) - same convention as the real drone's stick
    /// bytes, so callers convert exactly like DronePage.xaml.cs does.
    ///
    /// Deliberately NOT a WinForms Control - an earlier version (JoystickControl) was, using the
    /// standard "see-through overlay" trick (WS_EX_TRANSPARENT + BackColor=Transparent +
    /// SupportsTransparentBackColor) to render translucently over a sibling PictureBox showing the
    /// video. Confirmed NOT working here: that trick relies on the OS compositing this control
    /// against whatever's already on screen beneath it, but .NET's own double-buffering
    /// (ControlStyles.OptimizedDoubleBuffer, needed to avoid flicker while dragging) paints to an
    /// off-screen bitmap first that is NOT seeded with the actual screen content behind it - so the
    /// "transparent" area just showed whatever that buffer defaulted to, not the video. Drawing the
    /// stick directly onto the SAME bitmap as the video frame (see AirSimPanel.RecomposeVideo)
    /// sidesteps window compositing entirely: it's just GDI+ drawing pixels onto pixels, so the
    /// alpha blending is unconditionally correct regardless of what WinForms/the OS do with windows.
    /// AirSimPanel owns mouse hit-testing (via _video's own Mouse events) and calls MouseDown/
    /// MouseMove/MouseUp here, then repaints by calling Draw again - there's no Handle/Invalidate of
    /// its own since this isn't a Control.
    /// </summary>
    public sealed class JoystickState
    {
        /// <summary>Ring radius in pixels, operator-adjustable (Sticks panel). Was a const; a fixed
        /// size can't suit both a desktop monitor and a tablet being flown with thumbs. KnobRadius is
        /// derived rather than separately settable so the stick always scales as one piece - 0.4 is the
        /// ratio the original fixed 55/22 pair used.</summary>
        public int Radius = 55;
        public int KnobRadius => (int)(Radius * 0.4f);
        public int Diameter => 2 * Radius + 16;   // matches the old Control's Size (padding included)
        int MaxTravel => Radius - KnobRadius;

        int _knobX, _knobY;   // pixel offset from centre
        Point _dragAnchorCursor;      // cursor position at MouseDown
        int _dragAnchorKnobX, _dragAnchorKnobY;   // knob position at MouseDown - drag moves relative to THIS, not to the click point

        /// <summary>Centre of the stick, in the same pixel space Draw()/HitTest() are called with
        /// (AirSimPanel keeps this as _video's own local coordinates).</summary>
        public Point Center;

        /// <summary>0.0 (invisible) - 1.0 (fully opaque). Ring and knob are separate so the outer ring
        /// can be faded almost away while the knob stays readable, or the reverse.</summary>
        public float RingOpacity = 1f;
        public float KnobOpacity = 1f;

        /// <summary>Ring (fill + outline) and knob colours. The ring uses one colour for both its fill
        /// and its outline so a single control can drive it.</summary>
        public Color RingColor = Theme.Text;
        public Color KnobColor = Theme.Accent;

        /// <summary>Hides the stick entirely. Also switches off hit-testing - an invisible stick that
        /// still swallowed drags would be a trap, particularly when flying on the gamepad with the
        /// on-screen sticks turned off deliberately.</summary>
        public bool Visible = true;

        public bool Dragging { get; private set; }

        /// <summary>Fires continuously while dragging and once more (128,128) on release.</summary>
        public event Action<byte, byte>? ValueChanged;

        public bool HitTest(Point p)
        {
            if (!Visible) return false;
            double dx = p.X - Center.X, dy = p.Y - Center.Y;
            return dx * dx + dy * dy <= (double)Radius * Radius;
        }

        // Relative dragging, not click-to-position: MouseDown just "grabs" the knob wherever it
        // currently sits (0,0 in the common case, since MouseUp always snaps back there) - it does
        // NOT jump the knob to the click point. Only actual cursor MOVEMENT after that moves the
        // knob, by the same delta the cursor moved. Clicking anywhere within the ring/knob (even
        // right at its edge) therefore does nothing on its own and can't cause a surprise value
        // change/drone movement the instant you click, which absolute click-to-position did.
        public void MouseDown(Point p)
        {
            Dragging = true;
            _dragAnchorCursor = p;
            _dragAnchorKnobX = _knobX;
            _dragAnchorKnobY = _knobY;
        }

        /// <summary>sensitivity scales how far the knob moves per pixel of cursor movement - 1.0 is
        /// the normal 1:1 drag, a smaller value (AirSimPanel's right-mouse-button drag) gives finer
        /// control for precise aiming (e.g. lining up a survey Mark Point) at the cost of needing a
        /// bigger physical drag to reach full deflection.</summary>
        public void MouseMove(Point p, float sensitivity = 1f)
        {
            if (!Dragging) return;
            int dx = _dragAnchorKnobX + (int)((p.X - _dragAnchorCursor.X) * sensitivity);
            int dy = _dragAnchorKnobY + (int)((p.Y - _dragAnchorCursor.Y) * sensitivity);
            double dist = Math.Sqrt((double)dx * dx + (double)dy * dy);
            if (dist > MaxTravel)
            {
                double scale = MaxTravel / dist;
                dx = (int)(dx * scale); dy = (int)(dy * scale);
            }
            _knobX = dx; _knobY = dy;
            Report();
        }

        public void MouseUp()
        {
            Dragging = false;
            _knobX = 0; _knobY = 0;
            Report();
        }

        /// <summary>Purely visual - moves the knob to reflect an external input source (a game
        /// controller) WITHOUT firing ValueChanged, since that source (AirSimPanel.GamepadLoop)
        /// already drives the panel's roll/pitch/yaw/throttle fields directly and independently;
        /// this only keeps the on-screen stick showing where the physical stick actually is. nx/ny
        /// are normalized -1..1, ny positive = pushed up (matches XInputGamepad's convention).</summary>
        public void SetVisualPosition(float nx, float ny)
        {
            if (Dragging) return;   // an active mouse drag wins visually over a stale controller poll
            _knobX = (int)(Math.Clamp(nx, -1f, 1f) * MaxTravel);
            _knobY = (int)(-Math.Clamp(ny, -1f, 1f) * MaxTravel);   // internal _knobY is screen-space (down-positive), hence the negation
        }

        void Report()
        {
            byte bx = (byte)Math.Clamp(128 + _knobX / (double)MaxTravel * 127, 0, 255);
            // screen-down is positive y, but "up" should mean a higher stick value, hence the minus.
            byte by = (byte)Math.Clamp(128 - _knobY / (double)MaxTravel * 127, 0, 255);
            ValueChanged?.Invoke(bx, by);
        }

        /// <summary>Draws the ring+knob centred at Center directly onto the given Graphics surface -
        /// caller is responsible for that surface eventually landing somewhere on screen (see
        /// AirSimPanel.RecomposeVideo, which draws this onto a copy of the latest video frame).</summary>
        public void Draw(Graphics g)
        {
            if (!Visible) return;
            int cx = Center.X, cy = Center.Y;
            using (var ringBrush = new SolidBrush(Color.FromArgb(Alpha(70, RingOpacity), RingColor)))
                g.FillEllipse(ringBrush, cx - Radius, cy - Radius, Radius * 2, Radius * 2);
            using (var ringPen = new Pen(Color.FromArgb(Alpha(120, RingOpacity), RingColor)))
                g.DrawEllipse(ringPen, cx - Radius, cy - Radius, Radius * 2, Radius * 2);
            using (var knobBrush = new SolidBrush(Color.FromArgb(Alpha(190, KnobOpacity), KnobColor)))
                g.FillEllipse(knobBrush, cx + _knobX - KnobRadius, cy + _knobY - KnobRadius, KnobRadius * 2, KnobRadius * 2);
        }

        // The base alphas above are what the original fixed styling used; opacity scales them, and the
        // clamp keeps a rounding overshoot from throwing out of Color.FromArgb's 0-255 range.
        static int Alpha(int baseAlpha, float opacity) => Math.Clamp((int)(baseAlpha * opacity), 0, 255);
    }
}

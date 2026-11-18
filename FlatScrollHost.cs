namespace HS175D
{
    /// <summary>
    /// Vertical-only scrolling container with a flat, theme-drawn scrollbar.
    ///
    /// Built rather than using Panel.AutoScroll because AutoScroll gives you the OS scrollbars: a wide
    /// chrome-coloured bar that ignores the dark theme entirely, plus a horizontal bar that appears the
    /// moment any child is a pixel too wide. Here the bar is painted by us - a few pixels of flat
    /// colour - and there is no horizontal axis at all, so it can't appear.
    ///
    /// Add children to <see cref="Content"/>, not to the host, and set Content.Height to the full
    /// laid-out height; the host clips and slides it. Content is kept exactly as wide as the viewport
    /// minus the bar, so a child sized to Content.Width can never provoke horizontal overflow.
    ///
    /// Wheel handling goes through IMessageFilter rather than OnMouseWheel because WinForms delivers
    /// wheel messages to the FOCUSED control, not the hovered one - without this, scrolling would only
    /// work after clicking the sidebar. Filtering also means the wheel scrolls the column even when the
    /// pointer is over a TrackBar, which incidentally prevents the classic accident of nudging a
    /// setting while trying to scroll past it.
    /// </summary>
    public sealed class FlatScrollHost : Panel, IMessageFilter
    {
        const int BarWidth = 8;
        const int WheelStep = 54;
        const int WM_MOUSEWHEEL = 0x020A;

        // Content-visibility (not the default) tells the WinForms Designer to serialize what's put
        // INSIDE this sub-panel (its Controls collection) as part of the host's own InitializeComponent,
        // the same technique SplitContainer's own Panel1/Panel2 use. Without this the Designer has no
        // way to persist "add this control to Content" - a plain get-only property whose value isn't
        // itself a field on the form gets silently dropped on the next design-time round-trip, which is
        // exactly what happened when AirSimForm.Designer.cs tried it without this attribute.
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public Panel Content { get; } = new() { Location = Point.Empty, BackColor = Theme.Bg1 };

        int _offset;
        bool _dragging;
        int _dragStartY, _dragStartOffset;

        public FlatScrollHost()
        {
            DoubleBuffered = true;
            BackColor = Theme.Bg1;
            Controls.Add(Content);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Application.AddMessageFilter(this);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            Application.RemoveMessageFilter(this);
            base.OnHandleDestroyed(e);
        }

        int MaxOffset => Math.Max(0, Content.Height - ClientSize.Height);

        /// <summary>Current scroll position, for callers that need to map a child's position into the
        /// host's own coordinate space.</summary>
        public int Offset => _offset;

        public void ScrollBy(int dy) => SetOffset(_offset + dy);

        void SetOffset(int value)
        {
            int clamped = Math.Clamp(value, 0, MaxOffset);
            if (clamped == _offset && Content.Top == -clamped) return;
            _offset = clamped;
            Content.Top = -clamped;
            Invalidate();
        }

        /// <summary>Call after changing Content.Height so the bar and clamp catch up.</summary>
        public void ContentChanged()
        {
            Content.Width = Math.Max(0, ClientSize.Width - BarWidth);
            SetOffset(_offset);
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ContentChanged();
        }

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg != WM_MOUSEWHEEL || !IsHandleCreated || !Visible || MaxOffset <= 0) return false;
            if (!RectangleToScreen(ClientRectangle).Contains(Cursor.Position)) return false;
            int delta = (short)((m.WParam.ToInt64() >> 16) & 0xFFFF);
            ScrollBy(-delta / 120 * WheelStep);
            return true;   // swallowed, so it never also reaches a TrackBar underneath
        }

        (int Top, int Height) Thumb()
        {
            int view = ClientSize.Height;
            int total = Math.Max(1, Content.Height);
            int height = Math.Max(28, (int)((long)view * view / total));
            int travel = Math.Max(1, view - height);
            int top = (int)((long)_offset * travel / Math.Max(1, MaxOffset));
            return (top, height);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (MaxOffset <= 0) return;   // nothing to scroll: no bar at all, rather than a dead one
            int x = ClientSize.Width - BarWidth;
            using (var track = new SolidBrush(Theme.Bg2))
                e.Graphics.FillRectangle(track, x, 0, BarWidth, ClientSize.Height);
            var (top, height) = Thumb();
            using (var thumb = new SolidBrush(_dragging ? Theme.Accent : Theme.Muted))
                e.Graphics.FillRectangle(thumb, x + 2, top, BarWidth - 4, height);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (MaxOffset > 0 && e.X >= ClientSize.Width - BarWidth)
            {
                var (top, height) = Thumb();
                if (e.Y < top || e.Y > top + height)
                {
                    ScrollBy(e.Y < top ? -ClientSize.Height : ClientSize.Height);   // page up/down
                    return;
                }
                _dragging = true;
                _dragStartY = e.Y;
                _dragStartOffset = _offset;
                Invalidate();
                return;
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_dragging)
            {
                var (_, height) = Thumb();
                int travel = Math.Max(1, ClientSize.Height - height);
                SetOffset(_dragStartOffset + (int)((long)(e.Y - _dragStartY) * MaxOffset / travel));
                return;
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (_dragging) { _dragging = false; Invalidate(); }
            base.OnMouseUp(e);
        }
    }
}

namespace HS175D
{
    /// <summary>
    /// A borderless, top-most, click-through window that paints a thick red frame around the entire
    /// screen. Shown whenever flight commands are armed, so the warning is impossible to miss even if
    /// the main app is minimized or hidden behind other windows. It never takes focus or blocks clicks.
    /// </summary>
    public sealed class ArmOverlay : Form
    {
        const int Thickness = 9;

        public ArmOverlay()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.Black;          // the TransparencyKey — interior is fully see-through
            TransparencyKey = Color.Black;
            DoubleBuffered = true;
            Enabled = false;                  // belt-and-suspenders: never interactive
        }

        // Show without stealing focus from whatever the user is doing.
        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_EX_LAYERED = 0x80000, WS_EX_TRANSPARENT = 0x20,
                          WS_EX_NOACTIVATE = 0x08000000, WS_EX_TOOLWINDOW = 0x80;
                var cp = base.CreateParams;
                cp.ExStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW;
                return cp;
            }
        }

        /// <summary>Frame the screen the owner currently sits on, then show on top.</summary>
        public void ShowOn(Form owner)
        {
            var screen = Screen.FromControl(owner) ?? Screen.PrimaryScreen!;
            Bounds = screen.Bounds;
            if (!Visible) Show();
            TopMost = true;
            BringToFront();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            using var pen = new Pen(Color.Red, Thickness);
            int h = Thickness / 2;
            e.Graphics.DrawRectangle(pen, h, h, ClientSize.Width - Thickness, ClientSize.Height - Thickness);
        }
    }
}

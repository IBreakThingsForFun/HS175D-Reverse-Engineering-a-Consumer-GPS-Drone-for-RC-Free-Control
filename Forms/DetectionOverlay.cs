namespace HS175D
{
    /// <summary>
    /// A borderless, top-most, click-through window that draws NPU detection bounding boxes on top
    /// of the live mpv video feed in <see cref="VideoPanel"/> — same transparency trick as
    /// <see cref="ArmOverlay"/> (TransparencyKey + WS_EX_LAYERED/TRANSPARENT), but positioned over
    /// the video host control instead of the whole screen, and painting per-frame detection boxes
    /// instead of a fixed warning frame.
    ///
    /// Boxes come in in the NPU's fixed source resolution (1280x720 — the drone's RTSP feed
    /// resolution) and are mapped into whatever the video host's current size/scale-mode is via the
    /// same Fit (letterbox) / Fill (crop) math VideoPanel already uses for mpv's panscan property.
    /// </summary>
    public sealed class DetectionOverlay : Form
    {
        const int SourceWidth = 1280;
        const int SourceHeight = 720;

        List<(string Label, Rectangle Box, float Confidence)> _detections = new();
        bool _fillMode;

        public DetectionOverlay()
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

        /// <summary>Position/size to exactly cover <paramref name="videoHost"/>, then show on top.</summary>
        public void ShowOver(Control videoHost)
        {
            Bounds = videoHost.RectangleToScreen(videoHost.ClientRectangle);
            if (!Visible) Show();
            TopMost = true;
            BringToFront();
            Invalidate();
        }

        /// <summary>Latest detections (in the NPU's fixed 1280x720 source space) plus the video
        /// panel's current scale mode (false = Fit/letterbox, true = Fill/crop), then repaint.</summary>
        public void SetDetections(List<(string Label, Rectangle Box, float Confidence)> detections, bool fillMode)
        {
            _detections = detections;
            _fillMode = fillMode;
            if (IsHandleCreated) BeginInvoke(Invalidate);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (_detections.Count == 0) return;

            int w = ClientSize.Width, h = ClientSize.Height;
            if (w <= 0 || h <= 0) return;

            // Same Fit/Fill choice VideoPanel makes via mpv's "panscan" property, computed here
            // against the source video's known fixed resolution so boxes land in the right spot
            // whether the video is letterboxed (Fit) or cropped-to-fill (Fill).
            float scale = _fillMode
                ? Math.Max(w / (float)SourceWidth, h / (float)SourceHeight)
                : Math.Min(w / (float)SourceWidth, h / (float)SourceHeight);
            float offsetX = (w - SourceWidth * scale) / 2f;
            float offsetY = (h - SourceHeight * scale) / 2f;

            using var pen = new Pen(Color.Lime, 2);
            using var font = new Font("Consolas", 10f, FontStyle.Bold);
            using var textBrush = new SolidBrush(Color.Lime);
            using var shadowBrush = new SolidBrush(Color.Black);

            foreach (var (label, box, confidence) in _detections)
            {
                var rect = new RectangleF(
                    offsetX + box.Left * scale, offsetY + box.Top * scale,
                    box.Width * scale, box.Height * scale);
                e.Graphics.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);

                string text = $"{label} {confidence * 100:F0}%";
                var textPos = new PointF(rect.X, rect.Y - 18);
                e.Graphics.DrawString(text, font, shadowBrush, textPos.X + 1, textPos.Y + 1);
                e.Graphics.DrawString(text, font, textBrush, textPos);
            }
        }
    }
}

using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Media;

namespace HS175D
{
    public enum IconCaptionPosition { None, Right, Bottom }

    /// <summary>How the button reacts to the mouse hovering over it: Fill changes the background
    /// (standard FlatAppearance.MouseOverBackColor behaviour), BorderOnly keeps the background fixed
    /// and instead draws an accent-coloured outline, None does nothing extra.</summary>
    public enum HoverStyle { Fill, BorderOnly, None }

    /// <summary>Reusable flat toolbar button: a hand-drawn colour pictogram (IconGlyphs) with an
    /// optional caption to its right or underneath. Icons are real Unicode-emoji stand-ins, not text -
    /// see IconGlyphs.cs for why (WinForms/GDI+, WPF's text stack, and RichEdit all fail to render
    /// colour emoji glyphs on this platform, confirmed by testing all three).
    ///
    /// Built-in mouse/click/hover/press feedback all comes free from the base Button + FlatAppearance
    /// (MouseEnter/Leave/Down/Up/Click already fire normally - just subscribe to them like any other
    /// Button). This class adds the extras that aren't: an outline-only hover style, a flashing
    /// border to call out "this is active", an optional click sound, and muted/desaturated icon
    /// colour while Enabled = false instead of the plain grey WinForms would otherwise show.</summary>
    public class IconButton : Button
    {
        // Browsable/serializable (not Hidden, as these used to be) so the WinForms Designer can both
        // construct an IconButton (needs a public parameterless ctor - see below) and persist which
        // glyph/caption/size it's showing as ordinary property assignments in InitializeComponent,
        // instead of silently dropping every IconButton field as never-constructed on its next
        // regeneration pass - which is exactly what happened before this fix.
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string Glyph { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string Caption { get => _caption; set { _caption = value; Invalidate(); } }
        string _caption;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public IconCaptionPosition CaptionPosition { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int IconSize { get; set; } = 22;

        // All Visible now too (see the comment above the first block) - HoverBorderColor specifically
        // used to be marked Hidden while AirSimForm.Designer.cs's InitializeComponent assigned it
        // anyway; that direct contradiction (code touches a property flagged "never serialize this")
        // is what was making the Designer discard the ENTIRE IconButton's construction on regenerate,
        // not just the one property - so every one of these needs to agree with what actually gets set.
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public HoverStyle HoverStyle { get => _hoverStyle; set { _hoverStyle = value; ApplyHoverFill(); } }
        HoverStyle _hoverStyle = HoverStyle.Fill;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color? HoverFillColor { get => _hoverFillColor; set { _hoverFillColor = value; ApplyHoverFill(); } }
        Color? _hoverFillColor;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color HoverBorderColor { get; set; } = Color.DeepSkyBlue;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Color FlashColor { get; set; } = Color.Gold;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int FlashIntervalMs { get; set; } = 450;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool FlashBorder
        {
            get => _flashing;
            set
            {
                if (_flashing == value) return;
                _flashing = value;
                if (value)
                {
                    _flashTimer ??= new System.Windows.Forms.Timer();
                    _flashTimer.Interval = FlashIntervalMs;
                    _flashTimer.Tick -= FlashTick;
                    _flashTimer.Tick += FlashTick;
                    _flashOn = true;
                    _flashTimer.Start();
                }
                else
                {
                    _flashTimer?.Stop();
                    _flashOn = false;
                }
                Invalidate();
            }
        }
        bool _flashing, _flashOn;
        System.Windows.Forms.Timer? _flashTimer;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool PlayClickSound { get; set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string? ClickSoundPath { get; set; }

        bool _hovering;

        // glyph now defaults to "" (rather than being mandatory) purely so the Designer has a public
        // parameterless constructor to call when regenerating a field's construction - existing call
        // sites that already pass an explicit glyph are unaffected.
        public IconButton(string glyph = "", string caption = "", IconCaptionPosition captionPosition = IconCaptionPosition.None)
        {
            Glyph = glyph;
            _caption = caption;
            CaptionPosition = captionPosition;
            Text = "";
        }

        void FlashTick(object? sender, EventArgs e) { _flashOn = !_flashOn; Invalidate(); }

        void ApplyHoverFill()
        {
            FlatAppearance.MouseOverBackColor = _hoverStyle == HoverStyle.Fill
                ? _hoverFillColor ?? ControlPaint.Light(BackColor, 0.25f)
                : BackColor;
        }

        protected override void OnBackColorChanged(EventArgs e) { base.OnBackColorChanged(e); ApplyHoverFill(); }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hovering = true;
            if (_hoverStyle == HoverStyle.BorderOnly) Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hovering = false;
            if (_hoverStyle == HoverStyle.BorderOnly) Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            if (!PlayClickSound) return;
            try
            {
                if (!string.IsNullOrEmpty(ClickSoundPath) && File.Exists(ClickSoundPath))
                    new SoundPlayer(ClickSoundPath).Play();
                else
                    SystemSounds.Beep.Play();
            }
            catch { /* sound is best-effort, never block the click */ }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (_hoverStyle == HoverStyle.BorderOnly && _hovering) DrawOverlayBorder(g, HoverBorderColor);
            if (_flashOn) DrawOverlayBorder(g, FlashColor);

            DrawContent(g);
        }

        void DrawOverlayBorder(Graphics g, Color c)
        {
            using var pen = new Pen(c, 2f);
            var r = ClientRectangle;
            g.DrawRectangle(pen, 1, 1, r.Width - 3, r.Height - 3);
        }

        void DrawContent(Graphics g)
        {
            var rect = ClientRectangle;
            var icon = Enabled ? IconCache.Get(Glyph, IconSize) : IconCache.GetMuted(Glyph, IconSize);
            Color textColor = Enabled ? ForeColor : ControlPaint.Light(ForeColor, 0.4f);

            switch (CaptionPosition)
            {
                case IconCaptionPosition.Right:
                {
                    var iconRect = new Rectangle(6, (rect.Height - IconSize) / 2, IconSize, IconSize);
                    g.DrawImage(icon, iconRect);
                    var textRect = new Rectangle(iconRect.Right + 6, 0, Math.Max(0, rect.Width - iconRect.Right - 10), rect.Height);
                    TextRenderer.DrawText(g, Caption, Font, textRect, textColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                    break;
                }
                case IconCaptionPosition.Bottom:
                {
                    var iconRect = new Rectangle((rect.Width - IconSize) / 2, 4, IconSize, IconSize);
                    g.DrawImage(icon, iconRect);
                    var textRect = new Rectangle(2, iconRect.Bottom + 2, rect.Width - 4, Math.Max(0, rect.Height - iconRect.Bottom - 4));
                    TextRenderer.DrawText(g, Caption, Font, textRect, textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.EndEllipsis);
                    break;
                }
                default:
                {
                    var iconRect = new Rectangle((rect.Width - IconSize) / 2, (rect.Height - IconSize) / 2, IconSize, IconSize);
                    g.DrawImage(icon, iconRect);
                    break;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _flashTimer?.Dispose();
            base.Dispose(disposing);
        }
    }

    /// <summary>Renders each IconGlyphs pictogram to a small bitmap once and reuses it - OnPaint fires
    /// on every hover/press/flash tick, so redoing the vector drawing from scratch each time would be
    /// wasteful. The muted (disabled) variant is the same bitmap desaturated + dimmed via a
    /// ColorMatrix rather than a second hand-drawn version.</summary>
    static class IconCache
    {
        static readonly Dictionary<(string glyph, int size), Bitmap> _normal = new();
        static readonly Dictionary<(string glyph, int size), Bitmap> _muted = new();

        public static Bitmap Get(string glyph, int size)
        {
            var key = (glyph, size);
            if (_normal.TryGetValue(key, out var b)) return b;
            var bmp = new Bitmap(Math.Max(1, size), Math.Max(1, size));
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                IconGlyphs.Draw(glyph, g, new RectangleF(0, 0, bmp.Width, bmp.Height));
            }
            _normal[key] = bmp;
            return bmp;
        }

        public static Bitmap GetMuted(string glyph, int size)
        {
            var key = (glyph, size);
            if (_muted.TryGetValue(key, out var b)) return b;
            var src = Get(glyph, size);
            var bmp = new Bitmap(src.Width, src.Height);
            using (var g = Graphics.FromImage(bmp))
            using (var attr = new ImageAttributes())
            {
                attr.SetColorMatrix(MutedMatrix);
                g.DrawImage(src, new Rectangle(0, 0, src.Width, src.Height), 0, 0, src.Width, src.Height, GraphicsUnit.Pixel, attr);
            }
            _muted[key] = bmp;
            return bmp;
        }

        // Blends each channel 55% toward its own greyscale luminance (desaturate) and cuts alpha to
        // 70% (so it blends into the button's own background and reads as dimmer, not just greyer).
        static readonly ColorMatrix MutedMatrix = BuildMutedMatrix();
        static ColorMatrix BuildMutedMatrix()
        {
            const float sat = 0.45f;
            const float lr = 0.3086f, lg = 0.6094f, lb = 0.0820f;
            float sr = lr * (1 - sat), sg = lg * (1 - sat), sb = lb * (1 - sat);
            return new ColorMatrix(new float[][]
            {
                new float[] { sr + sat, sr, sr, 0, 0 },
                new float[] { sg, sg + sat, sg, 0, 0 },
                new float[] { sb, sb, sb + sat, 0, 0 },
                new float[] { 0, 0, 0, 0.7f, 0 },
                new float[] { 0, 0, 0, 0, 1 },
            });
        }
    }
}

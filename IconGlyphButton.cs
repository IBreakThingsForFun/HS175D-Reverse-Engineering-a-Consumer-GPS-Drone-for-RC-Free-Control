using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace HS175D
{
    /// <summary>What ContentMode.Both actually lays out relative to the icon.</summary>
    public enum IconTextPosition { Right, Left, Top, Bottom }

    /// <summary>Whether to draw the icon, the caption, or both.</summary>
    public enum IconContentMode { IconOnly, TextOnly, IconAndText }

    /// <summary>The two icon sizes actually used across this app (44x44 icon-only toolbar buttons use
    /// Large; the icon+caption ones use Small) - a fixed two-choice property grid dropdown rather than
    /// an arbitrary pixel count, since that's genuinely all this app ever needs.</summary>
    public enum IconSizeOption { Small, Large }

    /// <summary>
    /// Reusable, Designer-first icon button: a hand-drawn colour pictogram (IconGlyphs) optionally
    /// paired with a caption on any of the four sides, or either one alone. Deliberately minimal
    /// construction surface (parameterless ctor, plain settable properties, no exotic Hidden/Visible
    /// mismatches) so it survives the WinForms Designer's regenerate pass - see AirSimForm.Designer.cs
    /// for the history of what happens when a control doesn't.
    ///
    /// The enabled/disabled colour change is deliberately NOT instant - it steps through a few frames
    /// on a System.Windows.Forms.Timer (never a blocking sleep/wait) so it reads as a quick transition
    /// rather than a jarring snap. Actual interactivity (mouse/keyboard/Click) still follows the real
    /// Enabled property immediately, exactly like every other control - only the paint is eased.
    /// </summary>
    public class IconGlyphButton : Button
    {
        [Category("Appearance")]
        [Description("Which pictogram to draw - see IconGlyphs.cs for the full list. Free text is fine; the dropdown just lists the known names.")]
        [DefaultValue("")]
        [TypeConverter(typeof(GlyphNameConverter))]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string Glyph { get; set; } = "";

        [Category("Appearance")]
        [Description("What this button actually draws: just the icon, just the caption text, or both.")]
        [DefaultValue(IconContentMode.IconOnly)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public IconContentMode ContentMode { get => _contentMode; set { _contentMode = value; Invalidate(); } }
        IconContentMode _contentMode = IconContentMode.IconOnly;

        [Category("Appearance")]
        [Description("Caption text. Kept separate from the inherited Text property, which this control always leaves empty, so the base Button painting never double-draws it.")]
        [DefaultValue("")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string Caption { get => _caption; set { _caption = value; Invalidate(); } }
        string _caption = "";

        [Category("Layout")]
        [Description("Where the caption sits relative to the icon, when ContentMode is IconAndText. Ignored for IconOnly/TextOnly.")]
        [DefaultValue(IconTextPosition.Right)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public IconTextPosition TextPosition { get => _textPosition; set { _textPosition = value; Invalidate(); } }
        IconTextPosition _textPosition = IconTextPosition.Right;

        [Category("Layout")]
        [Description("Small (16px) or Large (24px) icon - the two sizes already used across the app. When ContentMode is IconAndText the icon is also drawn a little smaller than this so the caption has room without the button feeling crowded.")]
        [DefaultValue(IconSizeOption.Large)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public IconSizeOption IconSize { get => _iconSizeOption; set { _iconSizeOption = value; Invalidate(); } }
        IconSizeOption _iconSizeOption = IconSizeOption.Large;

        int IconPixelSize => _iconSizeOption == IconSizeOption.Small ? 16 : 24;

        // Applied only in IconAndText mode - shrinks + shifts the icon so both it and the caption fit
        // inside the SAME footprint as an icon-only button, rather than growing the button.
        const float BothModeIconScale = 0.78f;

        [Category("Behavior")]
        [Description("Total time (ms) for the enabled<->disabled colour transition, split into a handful of discrete steps rather than one smooth continuous fade or an instant snap.")]
        [DefaultValue(320)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int TransitionMs { get; set; } = 320;

        const int TransitionSteps = 4;
        System.Windows.Forms.Timer? _transitionTimer;
        int _transitionStep;
        bool _transitionTargetEnabled = true;
        // 0 = fully "disabled" look, 1 = fully "enabled" look. Read only at paint time - the Timer
        // never touches pixels itself, it only nudges this and calls Invalidate().
        float Blend => TransitionSteps == 0 ? (_transitionTargetEnabled ? 1f : 0f) : (float)_transitionStep / TransitionSteps;

        public IconGlyphButton()
        {
            Text = "";
            _transitionStep = TransitionSteps;
        }

        // Real Enabled changes immediately (input, focus, everything the platform already does) -
        // Click, key handling, async Click handlers (Click += async (s, e) => await ..., which
        // WinForms already supports natively - nothing extra needed here) all behave exactly as any
        // other Button the instant this fires. Only the PAINT is deferred, via a non-blocking Timer.
        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            _transitionTargetEnabled = Enabled;
            _transitionTimer ??= new System.Windows.Forms.Timer();
            _transitionTimer.Interval = Math.Max(1, TransitionMs / TransitionSteps);
            _transitionTimer.Tick -= TransitionTick;
            _transitionTimer.Tick += TransitionTick;
            _transitionTimer.Start();
        }

        void TransitionTick(object? sender, EventArgs e)
        {
            _transitionStep += _transitionTargetEnabled ? 1 : -1;
            int target = _transitionTargetEnabled ? TransitionSteps : 0;
            _transitionStep = Math.Clamp(_transitionStep, 0, TransitionSteps);
            Invalidate();
            if (_transitionStep == target) _transitionTimer!.Stop();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _transitionTimer?.Dispose();
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            bool both = _contentMode == IconContentMode.IconAndText;
            bool showIcon = _contentMode != IconContentMode.TextOnly;
            bool showText = _contentMode != IconContentMode.IconOnly;
            int iconSize = both ? Math.Max(8, (int)(IconPixelSize * BothModeIconScale)) : IconPixelSize;
            float blend = Blend;
            Color textColor = LerpColor(ControlPaint.Light(ForeColor, 0.4f), ForeColor, blend);
            var rect = ClientRectangle;

            Rectangle iconRect, textRect;
            if (!both)
            {
                iconRect = new Rectangle((rect.Width - iconSize) / 2, (rect.Height - iconSize) / 2, iconSize, iconSize);
                textRect = rect;
            }
            else
            {
                switch (_textPosition)
                {
                    case IconTextPosition.Left:
                        iconRect = new Rectangle(rect.Width - iconSize - 6, (rect.Height - iconSize) / 2, iconSize, iconSize);
                        textRect = new Rectangle(4, 0, Math.Max(0, iconRect.Left - 8), rect.Height);
                        break;
                    case IconTextPosition.Top:
                        iconRect = new Rectangle((rect.Width - iconSize) / 2, rect.Height - iconSize - 4, iconSize, iconSize);
                        textRect = new Rectangle(2, 2, Math.Max(0, rect.Width - 4), Math.Max(0, iconRect.Top - 4));
                        break;
                    case IconTextPosition.Bottom:
                        iconRect = new Rectangle((rect.Width - iconSize) / 2, 4, iconSize, iconSize);
                        textRect = new Rectangle(2, iconRect.Bottom + 2, Math.Max(0, rect.Width - 4), Math.Max(0, rect.Height - iconRect.Bottom - 4));
                        break;
                    default: // Right
                        iconRect = new Rectangle(6, (rect.Height - iconSize) / 2, iconSize, iconSize);
                        textRect = new Rectangle(iconRect.Right + 6, 0, Math.Max(0, rect.Width - iconRect.Right - 10), rect.Height);
                        break;
                }
            }

            if (showIcon && !string.IsNullOrEmpty(Glyph))
                DrawIconFaded(g, IconCache.Get(Glyph, iconSize), iconRect, blend);

            if (showText && !string.IsNullOrEmpty(Caption))
            {
                var flags = _textPosition switch
                {
                    IconTextPosition.Top => TextFormatFlags.HorizontalCenter | TextFormatFlags.Top,
                    IconTextPosition.Bottom => TextFormatFlags.HorizontalCenter | TextFormatFlags.Top,
                    IconTextPosition.Left => TextFormatFlags.Right | TextFormatFlags.VerticalCenter,
                    _ => TextFormatFlags.Left | TextFormatFlags.VerticalCenter,
                };
                if (!both) flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter;
                TextRenderer.DrawText(g, Caption, Font, textRect, textColor, flags | TextFormatFlags.EndEllipsis);
            }
        }

        // Fades the icon toward the disabled look over a few Timer ticks instead of the previous
        // hard Enabled ? normal : muted switch - scales alpha only (via the ColorMatrix's Matrix33
        // cell), which is enough to read clearly as "becoming disabled" without needing a second,
        // separately-cached desaturated bitmap per blend step.
        static void DrawIconFaded(Graphics g, Bitmap bmp, Rectangle dest, float blend)
        {
            float opacity = 0.35f + 0.65f * Math.Clamp(blend, 0f, 1f);
            if (opacity >= 0.999f) { g.DrawImage(bmp, dest); return; }
            using var attr = new ImageAttributes();
            var m = new ColorMatrix { Matrix33 = opacity };
            attr.SetColorMatrix(m, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
            g.DrawImage(bmp, dest, 0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel, attr);
        }

        static Color LerpColor(Color a, Color b, float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            return Color.FromArgb(
                a.A + (int)((b.A - a.A) * t),
                a.R + (int)((b.R - a.R) * t),
                a.G + (int)((b.G - a.G) * t),
                a.B + (int)((b.B - a.B) * t));
        }
    }

    /// <summary>Offers the known IconGlyphs names as property-grid dropdown suggestions for
    /// IconGlyphButton.Glyph, without preventing free-text entry (GetStandardValuesExclusive is
    /// false) - new glyphs added to IconGlyphs.cs later just aren't suggested, they still work fine.</summary>
    public sealed class GlyphNameConverter : StringConverter
    {
        static readonly string[] Names =
        {
            "axes3d", "bolt", "cloud", "crosshair", "cube", "dish", "flag", "grid", "hover", "joystick",
            "land", "map", "orbit", "pin", "plug", "rocket", "route", "takeoff", "target", "tree",
            "wand", "wireframe", "world"
        };

        public override bool GetStandardValuesSupported(ITypeDescriptorContext? context) => true;
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context) => false;
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context) => new(Names);
    }
}

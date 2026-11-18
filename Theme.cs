using System.Drawing.Drawing2D;
using System.Text.Json;

namespace HS175D
{
    /// <summary>Direction of a tapped packet, for the live RE monitor.</summary>
    public enum PacketDir { Tx, Rx }

    /// <summary>
    /// Dark "mission-control" theme. Flat controls, thin borders, cyan accent, mono data fonts.
    /// Modern + slightly futuristic, deliberately not fanciful (no gradients/animation).
    /// Apply(root) walks a control tree and restyles it; existing custom button/label colors are
    /// preserved (so the red Stop / green confirmed-working cues survive) while default-colored
    /// controls get the dark treatment.
    /// </summary>
    public static class Theme
    {
        // Mutable palette so the Settings panel can re-skin the whole app live. Defaults = "Mission Control".
        public static Color Bg0    = Color.FromArgb(34, 39, 46);    // window
        public static Color Bg1    = Color.FromArgb(45, 51, 59);    // card / panel
        public static Color Bg2    = Color.FromArgb(55, 62, 71);    // control surface
        public static Color Border = Color.FromArgb(68, 76, 86);
        public static Color Text   = Color.FromArgb(201, 209, 217);
        public static Color Muted  = Color.FromArgb(139, 148, 158);
        public static Color Accent = Color.FromArgb(34, 211, 238);  // cyan-400
        public static Color AccentDim = Color.FromArgb(14, 116, 144);
        public static Color Good   = Color.FromArgb(63, 185, 80);
        public static Color Warn   = Color.FromArgb(245, 158, 11);
        public static Color Danger = Color.FromArgb(248, 81, 73);

        public static string FontName = "Segoe UI";
        public static float  FontSize = 9F;
        public static string PresetName = "Mission Control";

        public static readonly Font Mono = new("Consolas", 9F);
        public static readonly Font MonoSmall = new("Consolas", 8.25F);

        static readonly Color GreenCue = Color.FromArgb(206, 240, 206);   // "confirmed working" button tint

        // Original (designer) colors, captured the first time each control is themed, so a live
        // re-skin always re-derives from the SOURCE color rather than an already-themed one.
        static readonly Dictionary<Control, (Color f, Color b)> _orig = new();

        static Color FixFore(Color c)
        {
            // Compare by ARGB value — designer-assigned Color.Black is a *known* color and won't
            // == Color.FromArgb black, so identity comparison silently misses it (left flight text dark).
            int argb = c.ToArgb();
            if (argb == Color.Black.ToArgb() || c == SystemColors.ControlText) return Text;
            if (argb == Color.Gray.ToArgb() || argb == Color.DimGray.ToArgb()) return Muted;
            if (argb == Color.Firebrick.ToArgb())                              return Danger;
            if (argb == Color.Green.ToArgb())                                  return Good;
            if (argb == Color.White.ToArgb())                                  return Color.White;
            return c;   // leave intentional custom colors
        }

        static Color FixBack(Color c)
        {
            int argb = c.ToArgb();
            if (argb == Color.Firebrick.ToArgb()) return Danger;
            return c;                              // keep custom button tints (e.g. the green "working" cue)
        }

        public static void Apply(Control root) { Record(root); Walk(root); }

        // Snapshot designer colors BEFORE any theming, so an unset child doesn't inherit its
        // parent's freshly-darkened background and get mis-flagged as custom-colored.
        static void Record(Control c)
        {
            if (!_orig.ContainsKey(c)) _orig[c] = (c.ForeColor, c.BackColor);
            foreach (Control ch in c.Controls) Record(ch);
        }

        /// <summary>Re-skin an already-themed tree with the current palette + font (live settings change).</summary>
        public static void Recolor(Control root)
        {
            root.Font = new Font(FontName, FontSize);
            Walk(root);
            root.Invalidate(true);
        }

        static void Walk(Control c)
        {
            if (!_orig.TryGetValue(c, out var o)) { o = (c.ForeColor, c.BackColor); _orig[c] = o; }
            Color of = o.f, ob = o.b;     // original designer colors

            switch (c)
            {
                case Form f:
                    f.BackColor = Bg0; f.ForeColor = Text;
                    break;

                case GroupBox g:
                    g.BackColor = Bg1; g.ForeColor = FixFore(of);
                    break;

                case Button b:
                    bool customBg = ob.A != 0 && ob.ToArgb() != SystemColors.Control.ToArgb();
                    b.FlatStyle = FlatStyle.Flat;
                    b.FlatAppearance.BorderColor = Border;
                    b.UseVisualStyleBackColor = false;
                    if (customBg) { b.BackColor = FixBack(ob); b.ForeColor = of;          b.FlatAppearance.MouseOverBackColor = b.BackColor; }
                    else          { b.BackColor = Bg2;         b.ForeColor = FixFore(of);  b.FlatAppearance.MouseOverBackColor = AccentDim; }
                    break;

                case CheckBox cb:
                    cb.ForeColor = FixFore(of);
                    cb.BackColor = cb.Parent?.BackColor ?? Bg1;
                    break;

                case Label lb:
                    lb.ForeColor = FixFore(of);
                    lb.BackColor = Color.Transparent;
                    break;

                case TextBox tx:
                    tx.BackColor = Bg2; tx.ForeColor = Text;   // keep each box's designer BorderStyle
                    break;

                case ComboBox co:
                    co.FlatStyle = FlatStyle.Flat; co.BackColor = Bg2; co.ForeColor = Text;
                    break;

                case NumericUpDown nu:
                    nu.BackColor = Bg2; nu.ForeColor = Text; nu.BorderStyle = BorderStyle.FixedSingle;
                    break;

                case ListBox ls:
                    ls.BackColor = Bg2; ls.ForeColor = Text; ls.BorderStyle = BorderStyle.FixedSingle;
                    break;

                case ListView lv:
                    // row area only - column headers are native Win32 comctl32 chrome and only
                    // themeable via OwnerDraw, which isn't worth it for the handful of places this
                    // is used
                    lv.BackColor = Bg2; lv.ForeColor = Text; lv.BorderStyle = BorderStyle.FixedSingle;
                    break;

                case TrackBar tb:
                    tb.BackColor = tb.Parent?.BackColor ?? Bg1;
                    tb.TickStyle = TickStyle.None;        // flat slider, no VB6 tick marks
                    break;

                case TabControl _:
                    break; // owner-drawn separately (StyleTabs)

                case Panel pn:
                    pn.BackColor = (ob.A == 0 || ob.ToArgb() == SystemColors.Control.ToArgb()) ? Bg1 : FixBack(ob);
                    pn.ForeColor = Text;
                    break;
            }

            foreach (Control child in c.Controls) Walk(child);
        }

        // ---- preset themes + persistence (driven by the Settings panel) ----
        public static readonly string[] Presets =
            { "Mission Control", "Midnight Amber", "Slate Blue", "Daylight", "High Contrast" };

        static Color Hex(int rgb) => Color.FromArgb(255, (rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);

        public static void UsePreset(string name)
        {
            switch (name)
            {
                case "Midnight Amber":
                    Set(0x1A1714, 0x241F1A, 0x2E2820, 0x4A3F2E, 0xE8D9C0, 0xA89878, 0xF59E0B, 0x8A5A12, 0x84CC16, 0xF59E0B, 0xEF4444); break;
                case "Slate Blue":
                    Set(0x2B2F36, 0x363B44, 0x424852, 0x565D68, 0xD7DCE2, 0x9AA3AE, 0x60A5FA, 0x2A4A78, 0x4ADE80, 0xFBBF24, 0xF87171); break;
                case "Daylight":
                    Set(0xF3F4F6, 0xFFFFFF, 0xE9ECEF, 0xCBD2D9, 0x1F2937, 0x6B7280, 0x2563EB, 0x93C5FD, 0x16A34A, 0xD97706, 0xDC2626); break;
                case "High Contrast":
                    Set(0x000000, 0x0A0A0A, 0x161616, 0x3A3A3A, 0xFFFFFF, 0xC0C0C0, 0x00E5FF, 0x005F6B, 0x00E676, 0xFFC400, 0xFF3D3D); break;
                default:                       // Mission Control
                    Set(0x22272E, 0x2D333B, 0x373E47, 0x444C56, 0xC9D1D9, 0x8B949E, 0x22D3EE, 0x0E7488, 0x3FB950, 0xF59E0B, 0xF85149); break;
            }
            PresetName = name;
        }

        static void Set(int bg0, int bg1, int bg2, int bd, int tx, int mu, int ac, int acd, int gd, int wn, int dg)
        {
            Bg0 = Hex(bg0); Bg1 = Hex(bg1); Bg2 = Hex(bg2); Border = Hex(bd); Text = Hex(tx);
            Muted = Hex(mu); Accent = Hex(ac); AccentDim = Hex(acd); Good = Hex(gd); Warn = Hex(wn); Danger = Hex(dg);
        }

        static string SettingsPath => Path.Combine(AppContext.BaseDirectory, "theme.json");

        public static void Save()
        {
            try
            {
                var s = new ThemeSettings
                {
                    Preset = PresetName, FontName = FontName, FontSize = FontSize,
                    Bg0 = Bg0.ToArgb(), Bg1 = Bg1.ToArgb(), Bg2 = Bg2.ToArgb(), Border = Border.ToArgb(),
                    Text = Text.ToArgb(), Muted = Muted.ToArgb(), Accent = Accent.ToArgb(),
                    AccentDim = AccentDim.ToArgb(), Good = Good.ToArgb(), Warn = Warn.ToArgb(), Danger = Danger.ToArgb()
                };
                File.WriteAllText(SettingsPath, JsonSerializer.Serialize(s, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { /* settings are best-effort */ }
        }

        public static void Load()
        {
            try
            {
                if (!File.Exists(SettingsPath)) return;
                var s = JsonSerializer.Deserialize<ThemeSettings>(File.ReadAllText(SettingsPath));
                if (s is null) return;
                PresetName = s.Preset; FontName = s.FontName; FontSize = s.FontSize;
                Bg0 = Color.FromArgb(s.Bg0); Bg1 = Color.FromArgb(s.Bg1); Bg2 = Color.FromArgb(s.Bg2);
                Border = Color.FromArgb(s.Border); Text = Color.FromArgb(s.Text); Muted = Color.FromArgb(s.Muted);
                Accent = Color.FromArgb(s.Accent); AccentDim = Color.FromArgb(s.AccentDim);
                Good = Color.FromArgb(s.Good); Warn = Color.FromArgb(s.Warn); Danger = Color.FromArgb(s.Danger);
            }
            catch { /* fall back to defaults */ }
        }

        // ---- factory helpers for the new windows (already-themed controls) ----
        public static Button Btn(string text, int x, int y, int w = 96, int h = 26)
        {
            var b = new Button
            {
                Text = text, Location = new Point(x, y), Size = new Size(w, h),
                FlatStyle = FlatStyle.Flat, BackColor = Bg2, ForeColor = Text,
                UseVisualStyleBackColor = false, Font = new Font("Segoe UI", 9F)
            };
            b.FlatAppearance.BorderColor = Border;
            b.FlatAppearance.MouseOverBackColor = AccentDim;
            return b;
        }

        public static Button AccentBtn(string text, int x, int y, int w = 96, int h = 26)
        {
            var b = Btn(text, x, y, w, h);
            b.BackColor = AccentDim; b.ForeColor = Color.White;
            b.FlatAppearance.BorderColor = Accent;
            b.FlatAppearance.MouseOverBackColor = Accent;
            return b;
        }

        /// <summary>Square, icon-only toolbar button (label text belongs in a tooltip instead) drawing
        /// a hand-coloured IconGlyphs pictogram - see IconButton/IconGlyphs.cs for why (real colour
        /// emoji rendering has no working path in WinForms/GDI+, WPF's text stack, or RichEdit).</summary>
        public static IconButton IconBtn(string glyph, int x, int y, int size = 44)
        {
            var b = new IconButton(glyph) { Location = new Point(x, y), Size = new Size(size, size), IconSize = (int)(size * 0.55) };
            StyleFlatButton(b);
            return b;
        }

        /// <summary>Icon + caption toolbar button (text to the right or underneath the icon), same
        /// reusable IconButton control with a caption position set instead of None.</summary>
        public static IconButton IconBtn(string glyph, string caption, IconCaptionPosition captionPosition, int x, int y, int w, int h, int iconSize = 22)
        {
            var b = new IconButton(glyph, caption, captionPosition) { Location = new Point(x, y), Size = new Size(w, h), IconSize = iconSize, Font = new Font("Segoe UI", 9F) };
            StyleFlatButton(b);
            return b;
        }

        static void StyleFlatButton(Button b)
        {
            b.FlatStyle = FlatStyle.Flat; b.BackColor = Bg2; b.ForeColor = Text; b.UseVisualStyleBackColor = false;
            b.FlatAppearance.BorderColor = Border;
            b.FlatAppearance.MouseOverBackColor = AccentDim;
            if (b is IconButton ib) ib.HoverBorderColor = Accent;
        }

        public static Label Lbl(string text, int x, int y, Color? fore = null) => new()
        {
            Text = text, Location = new Point(x, y), AutoSize = true,
            ForeColor = fore ?? Text, BackColor = Color.Transparent
        };

        public static TextBox Mono_(bool readOnly, bool multiline = true) => new()
        {
            Multiline = multiline, ReadOnly = readOnly, WordWrap = false,
            ScrollBars = multiline ? ScrollBars.Both : ScrollBars.None,
            BackColor = Bg2, ForeColor = Text, BorderStyle = BorderStyle.FixedSingle, Font = Mono
        };

        // ---- collapsible "card" (flat panel + header bar) — replaces dated GroupBox chrome ----
        public const int CardHeaderH = 30;

        /// <summary>A flat card: accent title bar with a collapse chevron over a body panel.</summary>
        public static Panel Card(string title, out Panel body, int width, int bodyHeight, bool startCollapsed = false)
        {
            int w = width <= 0 ? 460 : width;
            var card = new Panel { BackColor = Bg1, Size = new Size(w, CardHeaderH + bodyHeight), Margin = new Padding(7) };

            var header = new Panel { Dock = DockStyle.Top, Height = CardHeaderH, BackColor = Bg2, Cursor = Cursors.Hand };
            var lbl = new Label
            {
                Text = title ?? "", ForeColor = Accent, AutoSize = true,
                Font = new Font("Segoe UI Semibold", 9.75F), Location = new Point(11, 7),
                BackColor = Color.Transparent, Cursor = Cursors.Hand
            };
            var chev = new Label
            {
                Text = "▾", ForeColor = Muted, AutoSize = true, Location = new Point(w - 26, 7),
                Anchor = AnchorStyles.Top | AnchorStyles.Right, BackColor = Color.Transparent, Cursor = Cursors.Hand
            };

            body = new Panel
            {
                BackColor = Bg1, Location = new Point(0, CardHeaderH), Size = new Size(w, bodyHeight),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            var b = body;

            header.Controls.Add(lbl);
            header.Controls.Add(chev);
            card.Controls.Add(header);
            card.Controls.Add(b);

            void Toggle()
            {
                b.Visible = !b.Visible;
                chev.Text = b.Visible ? "▾" : "▸";
                card.Height = CardHeaderH + (b.Visible ? bodyHeight : 0);
            }
            header.Click += (s, e) => Toggle();
            lbl.Click += (s, e) => Toggle();
            chev.Click += (s, e) => Toggle();

            card.Paint += (s, e) =>
            {
                using var p = new Pen(Border);
                e.Graphics.DrawRectangle(p, 0, 0, card.Width - 1, card.Height - 1);
            };
            if (startCollapsed) Toggle();
            return card;
        }

        /// <summary>Re-chrome a GroupBox as a card: its children move into the card body, positions kept,
        /// so every wired-up event handler survives — only the container changes.</summary>
        public static Panel CardFromGroup(GroupBox g, bool startCollapsed = false)
        {
            var card = Card(g.Text, out var body, g.Width, g.Height, startCollapsed);
            var kids = new List<Control>();
            foreach (Control k in g.Controls) kids.Add(k);
            foreach (var k in kids) { var loc = k.Location; g.Controls.Remove(k); body.Controls.Add(k); k.Location = loc; }
            return card;
        }

        /// <summary>Collapsible Dock=Top toolbar strip for docked, width-elastic layouts (Card()/
        /// CardFromGroup are for fixed-width dialogs instead - this stretches with its parent and its
        /// button row wraps like a normal toolbar FlowLayoutPanel already does). Returns the button
        /// row to add controls to; the header (click anywhere on it) and collapse are wired already.</summary>
        public static FlowLayoutPanel ToolbarSection(string title, out FlowLayoutPanel body, bool startCollapsed = false)
        {
            var outer = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, FlowDirection = FlowDirection.TopDown, WrapContents = false,
                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, BackColor = Bg1, Margin = new Padding(0)
            };

            var header = new Panel { Dock = DockStyle.Top, Height = CardHeaderH, BackColor = Bg2, Cursor = Cursors.Hand, Margin = new Padding(0) };
            var lbl = new Label
            {
                Text = title, ForeColor = Accent, AutoSize = false, Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(11, 0, 0, 0),
                Font = new Font("Segoe UI Semibold", 9.75F), BackColor = Color.Transparent, Cursor = Cursors.Hand
            };
            var chev = new Label
            {
                Text = "▾", ForeColor = Muted, AutoSize = false, Dock = DockStyle.Right, Width = 26,
                TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent, Cursor = Cursors.Hand
            };
            header.Controls.Add(lbl);
            header.Controls.Add(chev);

            body = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, FlowDirection = FlowDirection.LeftToRight, WrapContents = true,
                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Bg1, Padding = new Padding(8, 6, 8, 6), Margin = new Padding(0)
            };
            var b = body;

            void Toggle()
            {
                b.Visible = !b.Visible;
                chev.Text = b.Visible ? "▾" : "▸";
            }
            header.Click += (s, e) => Toggle();
            lbl.Click += (s, e) => Toggle();
            chev.Click += (s, e) => Toggle();

            outer.Controls.Add(header);
            outer.Controls.Add(b);
            if (startCollapsed) Toggle();
            return outer;
        }

        // ---- static card chrome (drawn over a flat Panel; no controls created/reparented) ----
        public const int CardHeadH = 22;

        /// <summary>Paint a flat Panel as a modern card: accent header bar with its Text as the title,
        /// plus a thin border. Children keep their positions (header occupies the top 22px the old
        /// GroupBox title used). Pure drawing — nothing is instantiated or moved at runtime.</summary>
        public static void CardChrome(Panel p)
        {
            p.BackColor = Bg1;
            bool collapsed = false;     // body hidden, card shrunk to its header
            int savedH = p.Height;      // full height to restore on expand
            bool dragging = false;
            bool resizing = false;
            int rz = 0;                 // resize mode: 1=bottom (vertical), 2=right, 3=corner
            Point grab = Point.Empty;
            const int RzEdge = 7;

            // Which resize edge (if any) the point is over. Disabled while collapsed.
            int EdgeAt(Point pt)
            {
                if (collapsed) return 0;
                bool b = pt.Y >= p.Height - RzEdge, r = pt.X >= p.Width - RzEdge;
                return (b && r) ? 3 : b ? 1 : r ? 2 : 0;
            }

            p.Paint += (s, e) =>
            {
                var g = e.Graphics;
                using (var hb = new SolidBrush(Bg2)) g.FillRectangle(hb, 0, 0, p.Width, CardHeadH);
                using (var ab = new SolidBrush(Accent)) g.FillRectangle(ab, 0, 0, 3, CardHeadH);
                TextRenderer.DrawText(g, p.Text ?? "", new Font("Segoe UI Semibold", 9F),
                    new Rectangle(10, 0, p.Width - 36, CardHeadH), Accent,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                TextRenderer.DrawText(g, collapsed ? "▸" : "▾", new Font("Segoe UI", 9F),
                    new Rectangle(p.Width - 24, 0, 20, CardHeadH), Muted,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                using var bp = new Pen(Border);
                g.DrawRectangle(bp, 0, 0, p.Width - 1, p.Height - 1);
            };

            // Header is the grab/collapse strip. Body controls start below CardHeadH, so a hit there
            // is always on the header itself — no child intercepts it.
            p.MouseDown += (s, e) =>
            {
                int edge = EdgeAt(e.Location);
                if (edge != 0) { resizing = true; rz = edge; p.BringToFront(); return; }   // drag an edge → resize
                if (e.Y > CardHeadH) return;
                p.BringToFront();
                if (e.X >= p.Width - 26)                       // chevron → collapse / expand
                {
                    collapsed = !collapsed;
                    if (collapsed) { savedH = p.Height; foreach (Control c in p.Controls) c.Visible = false; p.Height = CardHeadH + 2; }
                    else           { foreach (Control c in p.Controls) c.Visible = true;  p.Height = savedH; }
                    p.Invalidate();
                }
                else { dragging = true; grab = e.Location; p.Cursor = Cursors.SizeAll; }
            };
            p.MouseMove += (s, e) =>
            {
                if (resizing)
                {
                    int w = p.Width, h = p.Height;
                    if (rz is 2 or 3) w = Math.Max(140, e.X);                 // right edge follows cursor
                    if (rz is 1 or 3) h = Math.Max(CardHeadH + 40, e.Y);      // bottom edge follows cursor
                    p.Size = new Size((int)Math.Round(w / 8.0) * 8, (int)Math.Round(h / 8.0) * 8);
                }
                else if (dragging) p.Location = SnapDrag(p, p.Left + e.X - grab.X, p.Top + e.Y - grab.Y);
                else
                {
                    int edge = EdgeAt(e.Location);
                    p.Cursor = edge == 3 ? Cursors.SizeNWSE : edge == 1 ? Cursors.SizeNS : edge == 2 ? Cursors.SizeWE
                             : e.Y <= CardHeadH ? Cursors.SizeAll : Cursors.Default;
                }
            };
            p.MouseUp += (s, e) => { dragging = false; resizing = false; p.Cursor = Cursors.Default; };

            p.Invalidate();
        }

        // Snap a dragged card to an 8px grid, then magnetically to sibling edges so panels line up.
        const int Grid = 8, SnapTol = 11, Gap = 6;
        static Point SnapDrag(Panel p, int x, int y)
        {
            x = (int)Math.Round(x / (double)Grid) * Grid;
            y = (int)Math.Round(y / (double)Grid) * Grid;
            if (p.Parent is { } parent)
            {
                foreach (Control sib in parent.Controls)
                {
                    if (sib == p || sib is not Panel) continue;
                    if      (Math.Abs(x - sib.Left)        <= SnapTol) x = sib.Left;
                    else if (Math.Abs(x - (sib.Right + Gap)) <= SnapTol) x = sib.Right + Gap;
                    if      (Math.Abs(y - sib.Top)         <= SnapTol) y = sib.Top;
                    else if (Math.Abs(y - (sib.Bottom + Gap)) <= SnapTol) y = sib.Bottom + Gap;
                }
            }
            return new Point(Math.Max(Gap, x), Math.Max(Gap, y));
        }

        /// <summary>Flat toolbar strip: just a bottom hairline, no title header (for the top bar).</summary>
        public static void ToolbarChrome(Panel p)
        {
            p.BackColor = Bg1;
            p.Paint += (s, e) =>
            {
                using var bp = new Pen(Border);
                e.Graphics.DrawLine(bp, 0, p.Height - 1, p.Width, p.Height - 1);
            };
            p.Invalidate();
        }

        /// <summary>Owner-draw a TabControl with the dark theme (default tab headers are un-themable).</summary>
        public static void StyleTabs(TabControl tabs)
        {
            tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabs.SizeMode = TabSizeMode.Fixed;
            tabs.ItemSize = new Size(118, 30);
            tabs.Padding = new Point(16, 4);
            tabs.BackColor = Bg0;
            tabs.DrawItem += (s, e) =>
            {
                var tc = (TabControl)s!;
                var page = tc.TabPages[e.Index];
                bool sel = e.Index == tc.SelectedIndex;
                using var bg = new SolidBrush(sel ? Bg1 : Bg0);
                e.Graphics.FillRectangle(bg, e.Bounds);
                if (sel)
                {
                    using var bar = new SolidBrush(Accent);
                    e.Graphics.FillRectangle(bar, e.Bounds.X, e.Bounds.Bottom - 3, e.Bounds.Width, 3);
                }
                TextRenderer.DrawText(e.Graphics, page.Text, new Font("Segoe UI", 9.5F, sel ? FontStyle.Bold : FontStyle.Regular),
                    e.Bounds, sel ? Accent : Muted,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            foreach (TabPage p in tabs.TabPages) { p.BackColor = Bg1; p.ForeColor = Text; }
        }

        /// <summary>Dark-theme a menu / context menu (the default renderer is light gray = VB6).</summary>
        public static void StyleMenu(ToolStrip menu)
        {
            menu.Renderer = new ToolStripProfessionalRenderer(new DarkMenuColors()) { RoundedEdges = false };
            menu.BackColor = Bg1; menu.ForeColor = Text;
            foreach (ToolStripItem it in menu.Items) StyleMenuItem(it);
        }

        static void StyleMenuItem(ToolStripItem it)
        {
            it.BackColor = Bg1; it.ForeColor = Text;
            if (it is ToolStripMenuItem mi)
                foreach (ToolStripItem sub in mi.DropDownItems) StyleMenuItem(sub);
        }
    }

    /// <summary>Color table that makes ToolStrip menus follow the dark palette live.</summary>
    public sealed class DarkMenuColors : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground   => Theme.Bg1;
        public override Color ImageMarginGradientBegin      => Theme.Bg1;
        public override Color ImageMarginGradientMiddle     => Theme.Bg1;
        public override Color ImageMarginGradientEnd        => Theme.Bg1;
        public override Color MenuBorder                    => Theme.Border;
        public override Color MenuItemBorder                => Theme.Accent;
        public override Color MenuItemSelected              => Theme.AccentDim;
        public override Color MenuItemSelectedGradientBegin => Theme.AccentDim;
        public override Color MenuItemSelectedGradientEnd   => Theme.AccentDim;
        public override Color MenuItemPressedGradientBegin  => Theme.Bg2;
        public override Color MenuItemPressedGradientEnd    => Theme.Bg2;
        public override Color SeparatorDark                 => Theme.Border;
        public override Color SeparatorLight                => Theme.Border;
    }

    /// <summary>Serializable snapshot of the palette + font, persisted to theme.json next to the exe.</summary>
    public sealed class ThemeSettings
    {
        public string Preset { get; set; } = "Mission Control";
        public string FontName { get; set; } = "Segoe UI";
        public float FontSize { get; set; } = 9F;
        public int Bg0 { get; set; }
        public int Bg1 { get; set; }
        public int Bg2 { get; set; }
        public int Border { get; set; }
        public int Text { get; set; }
        public int Muted { get; set; }
        public int Accent { get; set; }
        public int AccentDim { get; set; }
        public int Good { get; set; }
        public int Warn { get; set; }
        public int Danger { get; set; }
    }
}

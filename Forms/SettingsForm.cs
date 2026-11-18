using System.Drawing.Text;

namespace HS175D
{
    /// <summary>
    /// Appearance settings: pick a preset theme or hand-tune background / panel / control / text /
    /// accent / border colors plus the UI font + size. Every change is applied live to the main
    /// window through the <c>onChanged</c> callback, and persisted to theme.json on close.
    /// </summary>
    public sealed class SettingsForm : Form
    {
        readonly Action _onChanged;
        readonly List<(Button swatch, Func<Color> get)> _swatches = new();
        readonly ToolTip _tip = new() { AutoPopDelay = 30000, InitialDelay = 350, ReshowDelay = 80, ShowAlways = true };
        ComboBox _preset = null!;

        public SettingsForm(Action onChanged)
        {
            _onChanged = onChanged;

            Text = "Appearance settings";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(360, 430);

            int y = 16;

            Controls.Add(Theme.Lbl("Preset theme", 18, y + 3));
            _preset = new ComboBox
            {
                Location = new Point(150, y), Size = new Size(192, 24),
                DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat
            };
            _preset.Items.AddRange(Theme.Presets);
            _preset.SelectedItem = Theme.Presets.Contains(Theme.PresetName) ? Theme.PresetName : Theme.Presets[0];
            _preset.SelectedIndexChanged += (s, e) =>
            {
                if (_preset.SelectedItem is string name) { Theme.UsePreset(name); RefreshSwatches(); Changed(); }
            };
            Controls.Add(_preset);
            y += 40;

            // ---- individual colors ----
            var rows = new (string label, Func<Color> get, Action<Color> set)[]
            {
                ("Background",  () => Theme.Bg0,    c => Theme.Bg0 = c),
                ("Panels",      () => Theme.Bg1,    c => Theme.Bg1 = c),
                ("Controls",    () => Theme.Bg2,    c => Theme.Bg2 = c),
                ("Font color",  () => Theme.Text,   c => Theme.Text = c),
                ("Accent",      () => Theme.Accent, c => Theme.Accent = c),
                ("Borders",     () => Theme.Border, c => Theme.Border = c),
            };
            foreach (var (label, get, set) in rows)
            {
                Controls.Add(Theme.Lbl(label, 18, y + 5));
                var sw = new Button
                {
                    Location = new Point(150, y), Size = new Size(192, 26),
                    FlatStyle = FlatStyle.Flat, BackColor = get(), Text = ""
                };
                sw.FlatAppearance.BorderColor = Color.FromArgb(120, 120, 120);
                _tip.SetToolTip(sw, $"Click to pick a color for {label}.");
                sw.Click += (s, e) =>
                {
                    using var dlg = new ColorDialog { Color = get(), FullOpen = true };
                    if (dlg.ShowDialog(this) == DialogResult.OK) { set(dlg.Color); sw.BackColor = dlg.Color; Changed(); }
                };
                _swatches.Add((sw, get));
                Controls.Add(sw);
                y += 34;
            }

            y += 6;
            // ---- font family + size ----
            Controls.Add(Theme.Lbl("Font", 18, y + 5));
            var fonts = new ComboBox
            {
                Location = new Point(150, y), Size = new Size(130, 24),
                DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat
            };
            foreach (FontFamily f in FontFamily.Families) fonts.Items.Add(f.Name);
            if (fonts.Items.Contains(Theme.FontName)) fonts.SelectedItem = Theme.FontName;
            fonts.SelectedIndexChanged += (s, e) => { if (fonts.SelectedItem is string n) { Theme.FontName = n; Changed(); } };
            Controls.Add(fonts);

            var size = new NumericUpDown
            {
                Location = new Point(286, y), Size = new Size(56, 24),
                Minimum = 7, Maximum = 16, DecimalPlaces = 1, Increment = 0.5M,
                Value = (decimal)Math.Clamp(Theme.FontSize, 7f, 16f), BorderStyle = BorderStyle.FixedSingle
            };
            size.ValueChanged += (s, e) => { Theme.FontSize = (float)size.Value; Changed(); };
            Controls.Add(size);
            y += 44;

            var reset = Theme.Btn("Reset preset", 150, y, 110, 28);
            reset.Click += (s, e) => { if (_preset.SelectedItem is string n) { Theme.UsePreset(n); RefreshSwatches(); Changed(); } };
            _tip.SetToolTip(reset, "Discard hand-tuned colors and reload the selected preset.");
            Controls.Add(reset);

            var done = Theme.AccentBtn("Done", 266, y, 76, 28);
            done.Click += (s, e) => { Theme.Save(); Close(); };
            _tip.SetToolTip(done, "Save these settings to theme.json and close.");
            Controls.Add(done);

            _tip.SetToolTip(_preset, "Pick a built-in color preset.");
            _tip.SetToolTip(fonts, "UI font family.");
            _tip.SetToolTip(size, "UI font size.");

            Theme.Apply(this);
            RefreshSwatches();   // re-show true colors (Apply leaves custom button bg, but be explicit)
        }

        void Changed() { _onChanged(); }

        void RefreshSwatches()
        {
            foreach (var (sw, get) in _swatches) sw.BackColor = get();
        }
    }
}

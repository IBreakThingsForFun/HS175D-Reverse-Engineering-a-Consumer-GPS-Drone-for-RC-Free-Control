using System.Text;

namespace HS175D
{
    /// <summary>
    /// Two independent serial monitors side by side (each its own port + running log + snapshots),
    /// plus shared tools across both: Mark-Both / Before-After bracketing (label an RC action on both
    /// logs at the same instant), Compare A|B (line diff), and Triggers (notify when a pattern arrives).
    /// </summary>
    public sealed class SerialMonitorForm : Form
    {
        readonly SerialMonitorPane _a = new("A");
        readonly SerialMonitorPane _b = new("B");
        readonly TextBox _txtMarkBoth = new();
        readonly TextBox _txtTrig = new();
        readonly ToolTip _tip = new() { AutoPopDelay = 30000, InitialDelay = 350, ReshowDelay = 80, ShowAlways = true };

        public SerialMonitorForm()
        {
            Text = "Serial Monitor — dual";
            BackColor = Theme.Bg0;
            ClientSize = new Size(1180, 560);
            MinimumSize = new Size(780, 320);
            StartPosition = FormStartPosition.CenterParent;

            var bar = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Theme.Bg1 };
            var lblM = Theme.Lbl("Both:", 8, 12, Theme.Muted);
            _txtMarkBoth.SetBounds(46, 8, 176, 22); _txtMarkBoth.BackColor = Theme.Bg2; _txtMarkBoth.ForeColor = Theme.Text; _txtMarkBoth.BorderStyle = BorderStyle.FixedSingle;
            _txtMarkBoth.PlaceholderText = "note → BOTH logs…";
            var btnMarkBoth = Theme.AccentBtn("Mark Both", 228, 7, 84);
            var btnBefore = Theme.Btn("▼ Before", 318, 7, 74);
            var btnAfter = Theme.Btn("▲ After", 396, 7, 74);
            var btnDiff = Theme.Btn("Compare A | B…", 484, 7, 116);
            var lblT = Theme.Lbl("Triggers:", 616, 12, Theme.Muted);
            _txtTrig.SetBounds(682, 8, 210, 22); _txtTrig.BackColor = Theme.Bg2; _txtTrig.ForeColor = Theme.Text; _txtTrig.BorderStyle = BorderStyle.FixedSingle;
            _txtTrig.PlaceholderText = "comma-sep, e.g. 40 C3, STARTED";
            var btnTrig = Theme.Btn("Set", 898, 7, 44);
            _tip.SetToolTip(btnMarkBoth, "Insert the note (or 'MARK' if blank) into BOTH logs at the same instant.");
            _tip.SetToolTip(btnBefore, "Mark 'BEFORE' in both logs — do this right before you act on the RC.");
            _tip.SetToolTip(btnAfter, "Mark 'AFTER' in both logs — do this right after the RC action, bracketing it.");
            _tip.SetToolTip(btnDiff, "Show lines unique to A, unique to B, and the count in common.");
            _tip.SetToolTip(btnTrig, "Apply the comma-separated trigger patterns to both panes — flashes the title bar on a match.");
            bar.Controls.AddRange(new Control[] { lblM, _txtMarkBoth, btnMarkBoth, btnBefore, btnAfter, btnDiff, lblT, _txtTrig, btnTrig });

            // SerialMonitorForm constructor
            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, BackColor = Theme.Bg0, SplitterWidth = 6 };
            _a.Dock = DockStyle.Fill; _b.Dock = DockStyle.Fill;
            split.Panel1.Controls.Add(_a);
            split.Panel2.Controls.Add(_b);
            Controls.Add(bar);
            Controls.Add(split);

            // set mins and initial splitter position after layout
            Load += (s, e) =>
            {
                split.Panel1MinSize = 320;
                split.Panel2MinSize = 320;
                split.SplitterDistance = Math.Max(split.Panel1MinSize, split.Width / 2);
            };

            void MarkBoth(string s) { _a.InsertMarker(s); _b.InsertMarker(s); }
            string Note() => _txtMarkBoth.Text.Trim() is { Length: > 0 } n ? n : "MARK";
            btnMarkBoth.Click += (s, e) => { MarkBoth(Note()); _txtMarkBoth.Clear(); };
            _txtMarkBoth.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { MarkBoth(Note()); _txtMarkBoth.Clear(); e.SuppressKeyPress = true; } };
            // Bracket an RC action: type what you're about to do, hit Before, do it on the RC, hit After.
            btnBefore.Click += (s, e) => MarkBoth("vvv BEFORE: " + _txtMarkBoth.Text.Trim());
            btnAfter.Click += (s, e) => MarkBoth("^^^ AFTER: " + _txtMarkBoth.Text.Trim());
            btnDiff.Click += (s, e) => ShowDiff();

            void ApplyTrig() { var pats = _txtTrig.Text.Split(',').Select(p => p.Trim()).Where(p => p.Length > 0).ToArray(); _a.SetTriggers(pats); _b.SetTriggers(pats); }
            btnTrig.Click += (s, e) => ApplyTrig();
            _txtTrig.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { ApplyTrig(); e.SuppressKeyPress = true; } };

            _a.TriggerHit += (p, pat) => FlashTitle(p.Title, pat);
            _b.TriggerHit += (p, pat) => FlashTitle(p.Title, pat);
        }

        void FlashTitle(string pane, string pat)
        {
            Text = $"Serial Monitor — ⚡ {pane} hit '{pat}'  ({DateTime.Now:HH:mm:ss})";
        }

        private void InitializeComponent()
        {

        }

        void ShowDiff()
        {
            var la = _a.ViewText.Replace("\r", "").Split('\n').Where(x => x.Trim().Length > 0).ToList();
            var lb = _b.ViewText.Replace("\r", "").Split('\n').Where(x => x.Trim().Length > 0).ToList();
            var sa = new HashSet<string>(la);
            var sb = new HashSet<string>(lb);
            var onlyA = la.Where(x => !sb.Contains(x)).Distinct().ToList();
            var onlyB = lb.Where(x => !sa.Contains(x)).Distinct().ToList();
            int common = sa.Count(x => sb.Contains(x));

            var o = new StringBuilder();
            o.AppendLine($"# distinct lines — A:{sa.Count}  B:{sb.Count}  common:{common}");
            o.AppendLine();
            o.AppendLine($"# ONLY in A ({onlyA.Count}):");
            foreach (var x in onlyA) o.AppendLine("  " + x);
            o.AppendLine();
            o.AppendLine($"# ONLY in B ({onlyB.Count}):");
            foreach (var x in onlyB) o.AppendLine("  " + x);

            var dlg = new Form { Text = "Compare A | B", BackColor = Theme.Bg0, ClientSize = new Size(760, 560), StartPosition = FormStartPosition.CenterParent };
            var tb = Theme.Mono_(readOnly: true); tb.Dock = DockStyle.Fill; tb.Text = o.ToString();
            dlg.Controls.Add(tb);
            dlg.Show(this);
        }
    }
}

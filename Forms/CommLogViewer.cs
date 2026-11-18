namespace HS175D
{
    /// <summary>
    /// Reads the persistent <see cref="CommLog"/> SQLite database. Two tabs:
    ///   • Log          — every transport exchange, filterable by protocol / direction / text, with a
    ///                     detail pane (hex + decoded text) and JSON export.
    ///   • Packet Types — the <see cref="PacketCatalog"/>: every distinct frame shape seen per protocol,
    ///                     with first/last-seen + counts; types new this session are highlighted so an
    ///                     unfamiliar packet is obvious. Notes are editable and persist.
    /// </summary>
    public sealed class CommLogViewer : Form
    {
        // ---- Log tab ----
        readonly ComboBox _cboTransport = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        readonly ComboBox _cboDir = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        readonly TextBox _txtSearch = new();
        readonly NumericUpDown _limit = new() { Minimum = 10, Maximum = 5000, Value = 300, Increment = 50, Width = 70 };
        readonly CheckBox _auto = new() { Text = "Auto", AutoSize = true };
        readonly ListView _log = new() { View = View.Details, FullRowSelect = true, GridLines = true, MultiSelect = false, HideSelection = false, Dock = DockStyle.Fill };
        readonly TextBox _detail = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, WordWrap = false, Dock = DockStyle.Fill, Font = new Font("Consolas", 9F) };

        // ---- Packet Types tab ----
        readonly ComboBox _cboPtTransport = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        readonly CheckBox _ptAuto = new() { Text = "Auto", AutoSize = true, Checked = true };
        readonly ListView _types = new() { View = View.Details, FullRowSelect = true, GridLines = true, MultiSelect = false, HideSelection = false, Dock = DockStyle.Fill };
        readonly TextBox _ptNote = new() { Dock = DockStyle.Fill };

        readonly System.Windows.Forms.Timer _timer = new() { Interval = 1500 };
        readonly TabControl _tabs = new() { Dock = DockStyle.Fill };
        readonly ToolTip _tip = new() { AutoPopDelay = 30000, InitialDelay = 350, ReshowDelay = 80, ShowAlways = true };

        public CommLogViewer()
        {
            Text = "Comm Log — Drone RE Studio";
            ClientSize = new Size(940, 640);
            MinimumSize = new Size(680, 460);
            Font = new Font("Segoe UI", 9F);

            _tabs.TabPages.Add(BuildLogTab());
            _tabs.TabPages.Add(BuildTypesTab());
            Controls.Add(_tabs);

            _timer.Tick += (s, e) => AutoTick();
            _timer.Start();
            PacketCatalog.OnNew += OnNewType;
            FormClosed += (s, e) => { _timer.Stop(); PacketCatalog.OnNew -= OnNewType; };

            Theme.Apply(this);
            _log.BackColor = _types.BackColor = Theme.Bg1; _log.ForeColor = _types.ForeColor = Theme.Text;
            _detail.BackColor = Theme.Bg0; _detail.ForeColor = Theme.Good;

            ReloadLog();
            ReloadTypes();
        }

        // ================= Log tab =================
        TabPage BuildLogTab()
        {
            var page = new TabPage("Log") { BackColor = Theme.Bg0 };

            var bar = new Panel { Dock = DockStyle.Top, Height = 38 };
            var lblT = new Label { Text = "Protocol:", AutoSize = true, Location = new Point(6, 11) };
            _cboTransport.Location = new Point(66, 7); _cboTransport.Size = new Size(120, 23);
            _cboTransport.Items.AddRange(new object[] { "All", "ADB", "FTP", "UDP", "CATALOG", "EXT" });
            _cboTransport.SelectedIndex = 0;
            var lblD = new Label { Text = "Dir:", AutoSize = true, Location = new Point(196, 11) };
            _cboDir.Location = new Point(228, 7); _cboDir.Size = new Size(80, 23);
            _cboDir.Items.AddRange(new object[] { "All", "tx", "rx", "event" });
            _cboDir.SelectedIndex = 0;
            var lblS = new Label { Text = "Search:", AutoSize = true, Location = new Point(318, 11) };
            _txtSearch.Location = new Point(370, 7); _txtSearch.Size = new Size(220, 23);
            _txtSearch.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            var lblL = new Label { Text = "Limit:", AutoSize = true, Location = new Point(600, 11), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            _limit.Location = new Point(642, 7); _limit.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _auto.Location = new Point(720, 10); _auto.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            var btnRefresh = new Button { Text = "Refresh", Location = new Point(772, 6), Size = new Size(74, 26), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            var btnExport = new Button { Text = "Export…", Location = new Point(850, 6), Size = new Size(78, 26), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            _tip.SetToolTip(btnRefresh, "Reload the log from the CommLog SQLite database with the current filters.");
            _tip.SetToolTip(btnExport, "Export the currently-filtered rows to a JSON file.");
            _tip.SetToolTip(_auto, "Auto-refresh the log every 1.5s.");
            _tip.SetToolTip(_txtSearch, "Filter rows by text (hex or decoded).");
            _tip.SetToolTip(_limit, "Max rows to load.");
            bar.Controls.AddRange(new Control[] { lblT, _cboTransport, lblD, _cboDir, lblS, _txtSearch, lblL, _limit, _auto, btnRefresh, btnExport });

            _log.Columns.Add("Time", 92);
            _log.Columns.Add("Protocol", 78);
            _log.Columns.Add("Dir", 46);
            _log.Columns.Add("Name", 300);
            _log.Columns.Add("Len", 54);
            _log.Columns.Add("OK", 36);
            _log.Columns.Add("Info", 240);

            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterWidth = 5 };
            split.Panel1.Controls.Add(_log);
            split.Panel2.Controls.Add(_detail);
            page.Controls.Add(split);   // fill first
            page.Controls.Add(bar);     // then top
            split.SplitterDistance = 380;

            _cboTransport.SelectedIndexChanged += (s, e) => ReloadLog();
            _cboDir.SelectedIndexChanged += (s, e) => ReloadLog();
            _txtSearch.TextChanged += (s, e) => ReloadLog();
            btnRefresh.Click += (s, e) => ReloadLog();
            btnExport.Click += (s, e) => ExportLog();
            _log.SelectedIndexChanged += (s, e) => ShowDetail();
            return page;
        }

        void ReloadLog()
        {
            string? tr = _cboTransport.SelectedItem as string; if (tr == "All") tr = null;
            string? dir = _cboDir.SelectedItem as string; if (dir == "All") dir = null;
            var rows = CommLog.Query(tr, (int)_limit.Value, _txtSearch.Text.Trim());

            _log.BeginUpdate();
            _log.Items.Clear();
            foreach (var r in rows)
            {
                string rdir = Get(r, "direction");
                if (dir != null && rdir != dir) continue;
                bool? ok = r.TryGetValue("ok", out var ov) && ov is bool b ? b : (bool?)null;
                string err = Get(r, "error");
                string hex = Get(r, "hex");
                string text = Get(r, "text");
                var it = new ListViewItem(new[]
                {
                    LocalTime(Get(r, "ts")),
                    Get(r, "transport"),
                    rdir,
                    Get(r, "name"),
                    r.TryGetValue("len", out var lv) && lv != null ? lv.ToString() ?? "" : "",
                    ok == null ? "" : (ok.Value ? "✓" : "✗"),
                    err.Length > 0 ? err : (text.Length > 0 ? Trunc(text) : Trunc(hex))
                })
                { Tag = r };
                it.ForeColor = ok == false ? Theme.Warn
                    : rdir == "event" ? Theme.Accent
                    : rdir == "rx" ? Theme.Good : Theme.Text;
                _log.Items.Add(it);
            }
            _log.EndUpdate();
            EnsureTransportChoices(rows);
        }

        void ShowDetail()
        {
            if (_log.SelectedItems.Count == 0) { _detail.Text = ""; return; }
            if (_log.SelectedItems[0].Tag is not Dictionary<string, object?> r) return;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"id        {Get(r, "id")}");
            sb.AppendLine($"time      {Get(r, "ts")}  (local {LocalTime(Get(r, "ts"))})");
            sb.AppendLine($"protocol  {Get(r, "transport")}    direction {Get(r, "direction")}    corr {Get(r, "corr")}");
            sb.AppendLine($"name      {Get(r, "name")}");
            sb.AppendLine($"target    {Get(r, "target")}");
            if (r.TryGetValue("len", out var lv) && lv != null) sb.AppendLine($"length    {lv}");
            if (r.TryGetValue("ok", out var ov) && ov is bool b) sb.AppendLine($"ok        {b}");
            string err = Get(r, "error"); if (err.Length > 0) sb.AppendLine($"error     {err}");
            string hex = Get(r, "hex"); if (hex.Length > 0) sb.AppendLine($"\nhex:\n{Spaced(hex)}");
            string text = Get(r, "text"); if (text.Length > 0) sb.AppendLine($"\ntext:\n{text}");
            _detail.Text = sb.ToString().Replace("\n", "\r\n");
        }

        void ExportLog()
        {
            string? tr = _cboTransport.SelectedItem as string; if (tr == "All") tr = null;
            using var dlg = new SaveFileDialog { Filter = "JSON (*.json)|*.json", FileName = $"comm_log_{DateTime.Now:yyyyMMdd_HHmmss}.json" };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            var res = CommLog.Export(dlg.FileName, tr, (int)_limit.Value);
            MessageBox.Show(this, res.ToString(), "Exported", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ================= Packet Types tab =================
        TabPage BuildTypesTab()
        {
            var page = new TabPage("Packet Types") { BackColor = Theme.Bg0 };

            var bar = new Panel { Dock = DockStyle.Top, Height = 38 };
            var lblT = new Label { Text = "Protocol:", AutoSize = true, Location = new Point(6, 11) };
            _cboPtTransport.Location = new Point(66, 7); _cboPtTransport.Size = new Size(140, 23);
            _cboPtTransport.Items.Add("All"); _cboPtTransport.SelectedIndex = 0;
            _ptAuto.Location = new Point(220, 10);
            var btnRefresh = new Button { Text = "Refresh", Location = new Point(276, 6), Size = new Size(74, 26) };
            var lblHint = new Label { Text = "New-this-session types are highlighted.", AutoSize = true, Location = new Point(366, 11), ForeColor = Theme.Accent };
            _tip.SetToolTip(btnRefresh, "Reload the packet-type catalog for the selected transport.");
            _tip.SetToolTip(_ptAuto, "Auto-refresh the packet-type list every 1.5s.");
            bar.Controls.AddRange(new Control[] { lblT, _cboPtTransport, _ptAuto, btnRefresh, lblHint });

            _types.Columns.Add("Protocol", 80);
            _types.Columns.Add("Dir", 46);
            _types.Columns.Add("Signature", 90);
            _types.Columns.Add("Count", 70);
            _types.Columns.Add("First seen", 150);
            _types.Columns.Add("Last seen", 150);
            _types.Columns.Add("Len", 50);
            _types.Columns.Add("Sample", 200);
            _types.Columns.Add("Note", 160);

            var notePanel = new Panel { Dock = DockStyle.Bottom, Height = 32 };
            var lblN = new Label { Text = "Note:", AutoSize = true, Location = new Point(6, 7) };
            _ptNote.Location = new Point(48, 4); _ptNote.Size = new Size(760, 23);
            _ptNote.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            var btnNote = new Button { Text = "Save note", Location = new Point(814, 3), Size = new Size(86, 25), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            _tip.SetToolTip(btnNote, "Save this note against the selected packet type — persists in the catalog.");
            notePanel.Controls.AddRange(new Control[] { lblN, _ptNote, btnNote });

            page.Controls.Add(_types);     // fill first
            page.Controls.Add(notePanel);  // bottom
            page.Controls.Add(bar);        // top

            _cboPtTransport.SelectedIndexChanged += (s, e) => ReloadTypes();
            btnRefresh.Click += (s, e) => ReloadTypes();
            _types.SelectedIndexChanged += (s, e) => { var pt = SelectedType(); if (pt != null) _ptNote.Text = pt.Note ?? ""; };
            btnNote.Click += (s, e) => { var pt = SelectedType(); if (pt != null) { PacketCatalog.SetNote(pt, _ptNote.Text.Trim()); ReloadTypes(); } };
            return page;
        }

        void ReloadTypes()
        {
            string? tr = _cboPtTransport.SelectedItem as string; if (tr == "All") tr = null;
            var list = PacketCatalog.Snapshot(tr);
            _types.BeginUpdate();
            _types.Items.Clear();
            foreach (var p in list)
            {
                var it = new ListViewItem(new[]
                {
                    p.Transport, p.Direction, p.Sig, p.Count.ToString(),
                    LocalTime(p.FirstSeen, true), LocalTime(p.LastSeen, true),
                    p.SampleLen.ToString(), Trunc(Spaced(p.SampleHex ?? "")), p.Note ?? ""
                })
                { Tag = p };
                if (p.NewThisSession) { it.BackColor = Theme.AccentDim; it.ForeColor = Color.White; }
                _types.Items.Add(it);
            }
            _types.EndUpdate();

            foreach (var t in list.Select(p => p.Transport).Distinct())
                if (!_cboPtTransport.Items.Contains(t)) _cboPtTransport.Items.Add(t);
        }

        PacketCatalog.PacketType? SelectedType()
            => _types.SelectedItems.Count > 0 ? _types.SelectedItems[0].Tag as PacketCatalog.PacketType : null;

        void OnNewType(PacketCatalog.PacketType pt)
        {
            if (IsDisposed) return;
            try { BeginInvoke(() => ReloadTypes()); } catch { }
        }

        // ================= shared =================
        void AutoTick()
        {
            if (_tabs.SelectedIndex == 0) { if (_auto.Checked) ReloadLog(); }
            else { if (_ptAuto.Checked) ReloadTypes(); }
        }

        void EnsureTransportChoices(List<Dictionary<string, object?>> rows)
        {
            foreach (var t in rows.Select(r => Get(r, "transport")).Where(t => t.Length > 0).Distinct())
                if (!_cboTransport.Items.Contains(t)) _cboTransport.Items.Add(t);
        }

        static string Get(Dictionary<string, object?> r, string k) => r.TryGetValue(k, out var v) && v != null ? v.ToString() ?? "" : "";
        static string Trunc(string s) => s.Length > 120 ? s[..120] + "…" : s;
        static string Spaced(string hex)
        {
            if (hex.Length <= 2) return hex;
            var sb = new System.Text.StringBuilder(hex.Length + hex.Length / 2);
            for (int i = 0; i < hex.Length; i += 2) { if (i > 0) sb.Append(' '); sb.Append(hex, i, Math.Min(2, hex.Length - i)); }
            return sb.ToString();
        }
        static string LocalTime(string iso, bool withDate = false)
        {
            return DateTime.TryParse(iso, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt)
                ? dt.ToLocalTime().ToString(withDate ? "MM-dd HH:mm:ss" : "HH:mm:ss")
                : iso;
        }
    }
}

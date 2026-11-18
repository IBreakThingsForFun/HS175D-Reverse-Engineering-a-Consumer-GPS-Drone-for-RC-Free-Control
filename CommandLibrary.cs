using System.Text.Json;

namespace HS175D
{
    /// <summary>One reusable command / path / frame snippet in the library.</summary>
    public sealed class CommandEntry
    {
        public string Category { get; set; } = "ADB";   // ADB | FTP | Inject | Shim | …
        public string Label { get; set; } = "";
        public string Command { get; set; } = "";
        public string Note { get; set; } = "";
        public int UsedCount { get; set; }
        public string? LastUsed { get; set; }
        public bool Builtin { get; set; }
    }

    /// <summary>
    /// Persistent, categorized library of the commands/paths/frames we actually use — so the common
    /// operations are click-to-run instead of retyped (or re-asked of the AI). Backed by
    /// <c>commands.json</c> next to the exe, seeded with sensible defaults on first run, and
    /// self-populating: every command actually run is folded back in via <see cref="RecordUsed"/>.
    /// </summary>
    public static class CommandLibrary
    {
        static readonly object _lock = new();
        static readonly string _path = Path.Combine(AppContext.BaseDirectory, "commands.json");
        static List<CommandEntry> _items = new();
        static bool _loaded;

        public static event Action? Changed;

        public static void Load()
        {
            lock (_lock)
            {
                if (_loaded) return;
                _loaded = true;
                try
                {
                    if (File.Exists(_path))
                        _items = JsonSerializer.Deserialize<List<CommandEntry>>(File.ReadAllText(_path)) ?? new();
                    else { _items = Defaults(); SaveLocked(); }
                }
                catch { _items = Defaults(); }
            }
        }

        public static List<CommandEntry> Items(string? category = null, string? contains = null)
        {
            Load();
            lock (_lock)
            {
                IEnumerable<CommandEntry> q = _items;
                if (!string.IsNullOrWhiteSpace(category) && category != "All")
                    q = q.Where(e => e.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(contains))
                    q = q.Where(e => (e.Label + " " + e.Command + " " + e.Note).Contains(contains, StringComparison.OrdinalIgnoreCase));
                return q.OrderByDescending(e => e.UsedCount).ThenBy(e => e.Category).ThenBy(e => e.Label).ToList();
            }
        }

        public static string[] Categories()
        {
            Load();
            lock (_lock)
                return _items.Select(e => e.Category).Distinct(StringComparer.OrdinalIgnoreCase)
                             .OrderBy(c => c).ToArray();
        }

        public static void AddOrUpdate(CommandEntry e)
        {
            Load();
            lock (_lock)
            {
                var match = _items.FirstOrDefault(x =>
                    x.Category.Equals(e.Category, StringComparison.OrdinalIgnoreCase) &&
                    x.Command.Trim() == e.Command.Trim());
                if (match == null) _items.Add(e);
                else { match.Label = e.Label; match.Note = e.Note; }
                SaveLocked();
            }
            Changed?.Invoke();
        }

        public static void Remove(CommandEntry e)
        {
            lock (_lock) { _items.Remove(e); SaveLocked(); }
            Changed?.Invoke();
        }

        /// <summary>Fold a command that was actually executed back into the library (count + timestamp).</summary>
        public static void RecordUsed(string category, string command)
        {
            if (string.IsNullOrWhiteSpace(command)) return;
            Load();
            lock (_lock)
            {
                var match = _items.FirstOrDefault(x =>
                    x.Category.Equals(category, StringComparison.OrdinalIgnoreCase) &&
                    x.Command.Trim() == command.Trim());
                if (match == null)
                {
                    match = new CommandEntry { Category = category, Label = Shorten(command), Command = command.Trim() };
                    _items.Add(match);
                }
                match.UsedCount++;
                match.LastUsed = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                SaveLocked();
            }
            Changed?.Invoke();
        }

        static void SaveLocked()
        {
            try { File.WriteAllText(_path, JsonSerializer.Serialize(_items, new JsonSerializerOptions { WriteIndented = true })); }
            catch { }
        }

        static string Shorten(string s) => s.Length <= 32 ? s : s[..32] + "…";

        static List<CommandEntry> Defaults() => new()
        {
            // --- ADB shell (the recon set we've been running) ---
            new() { Category="ADB", Label="logcat (tail 200)", Command="logcat -d -t 200", Builtin=true },
            new() { Category="ADB", Label="MCU frame dumps", Command="logcat -d -t 400 | grep -i \"app ot yt\"", Note="needs loglevel 5", Builtin=true },
            new() { Category="ADB", Label="dmesg (tail 200)", Command="dmesg | tail -n 200", Builtin=true },
            new() { Category="ADB", Label="flash map", Command="cat /proc/mtd", Builtin=true },
            new() { Category="ADB", Label="MCU message buffer", Command="cat /tmp/messages", Builtin=true },
            new() { Category="ADB", Label="current loglevel", Command="cat /tmp/loglevel", Builtin=true },
            new() { Category="ADB", Label="set loglevel 5", Command="echo 5 > /tmp/loglevel", Note="verbose MCU logging", Builtin=true },
            new() { Category="ADB", Label="key processes", Command="ps | grep -E \"hostapd|img_trans|app\"", Builtin=true },
            new() { Category="ADB", Label="live hostapd.conf", Command="cat /tmp/hostapd.conf", Builtin=true },
            // --- FTP paths ---
            new() { Category="FTP", Label="/tmp/messages", Command="/tmp/messages", Builtin=true },
            new() { Category="FTP", Label="/tmp/loglevel", Command="/tmp/loglevel", Builtin=true },
            new() { Category="FTP", Label="/tmp/hostapd.conf (live)", Command="/tmp/hostapd.conf", Builtin=true },
            new() { Category="FTP", Label="/etc/hostapd.conf (5GHz)", Command="/etc/hostapd.conf", Builtin=true },
            new() { Category="FTP", Label="/netPrivate/hostapd.conf", Command="/netPrivate/hostapd.conf", Note="persistent, NOT power-safe", Builtin=true },
            new() { Category="FTP", Label="/netPrivate/DroneID.json", Command="/netPrivate/DroneID.json", Builtin=true },
            new() { Category="FTP", Label="/netPrivate/A17Startup.sh", Command="/netPrivate/A17Startup.sh", Builtin=true },
            new() { Category="FTP", Label="/etc/wifi_switch.sh", Command="/etc/wifi_switch.sh", Builtin=true },
            // --- Inject (raw UDP frames, port 19798 unless noted) ---
            new() { Category="Inject", Label="2nd video probe (:1563)", Command="D8 C0 D9", Note="bind :1563 first; proves 2nd video path", Builtin=true },
            // --- Shim / setup notes ---
            new() { Category="Shim", Label="WiFi → 2.4GHz (runtime)", Command="cp /tmp/hostapd.conf /tmp/hostapd.bak && sed -i 's/^channel=.*/channel=6/' /tmp/hostapd.conf", Note="RAM only; reverts on reboot — see notes", Builtin=true },
            new() { Category="Shim", Label="flight UART", Command="ttyS1", Note="MCU flight protocol UART", Builtin=true },
        };
    }

    /// <summary>
    /// Browse / search / edit the <see cref="CommandLibrary"/>. Opened standalone (copies to clipboard)
    /// or from a console with load-back callbacks so a pick drops straight into the ADB / FTP box.
    /// </summary>
    public sealed class CommandLibraryForm : Form
    {
        readonly Action<string>? _useAdb;
        readonly Action<string>? _useFtp;

        readonly ComboBox _cboCat = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        readonly TextBox _txtSearch = new();
        readonly ListView _list = new() { View = View.Details, FullRowSelect = true, GridLines = true, MultiSelect = false, HideSelection = false };
        readonly Button _btnUse = new() { Text = "Use ▶" };

        readonly ComboBox _edCat = new() { DropDownStyle = ComboBoxStyle.DropDown };
        readonly TextBox _edLabel = new();
        readonly TextBox _edCommand = new();
        readonly TextBox _edNote = new();
        readonly Button _btnSave = new() { Text = "Save / Update" };
        readonly Button _btnNew = new() { Text = "Clear" };
        readonly Button _btnDelete = new() { Text = "Delete" };
        readonly Label _status = new() { AutoSize = true, ForeColor = Color.DimGray };
        readonly ToolTip _tip = new() { AutoPopDelay = 30000, InitialDelay = 350, ReshowDelay = 80, ShowAlways = true };

        public CommandLibraryForm(Action<string>? useAdb = null, Action<string>? useFtp = null)
        {
            _useAdb = useAdb; _useFtp = useFtp;
            Text = "Command Library — Drone RE Studio";
            ClientSize = new Size(820, 560);
            MinimumSize = new Size(640, 420);
            Font = new Font("Segoe UI", 9F);

            var lblCat = new Label { Text = "Category:", AutoSize = true, Location = new Point(8, 12) };
            _cboCat.Location = new Point(70, 8); _cboCat.Size = new Size(140, 23);
            var lblFind = new Label { Text = "Search:", AutoSize = true, Location = new Point(222, 12) };
            _txtSearch.Location = new Point(278, 8); _txtSearch.Size = new Size(240, 23);
            _txtSearch.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _btnUse.Location = new Point(728, 7); _btnUse.Size = new Size(84, 25);
            _btnUse.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _tip.SetToolTip(_cboCat, "Filter the list by category.");
            _tip.SetToolTip(_txtSearch, "Filter by label/command/note text.");
            _tip.SetToolTip(_btnUse, "Load the selected entry back into the adb/FTP box it came from (or copy to clipboard if opened standalone).");

            _list.Location = new Point(8, 40);
            _list.Size = new Size(804, 330);
            _list.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _list.Columns.Add("Category", 90);
            _list.Columns.Add("Label", 170);
            _list.Columns.Add("Command", 300);
            _list.Columns.Add("Used", 46);
            _list.Columns.Add("Last used", 110);
            _list.Columns.Add("Note", 160);

            // editor strip
            int ey = 384;
            var l1 = new Label { Text = "Category:", AutoSize = true, Location = new Point(8, ey + 4) };
            _edCat.Location = new Point(70, ey); _edCat.Size = new Size(140, 23);
            _edCat.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            var l2 = new Label { Text = "Label:", AutoSize = true, Location = new Point(222, ey + 4) };
            _edLabel.Location = new Point(266, ey); _edLabel.Size = new Size(250, 23);
            _edLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            var l3 = new Label { Text = "Note:", AutoSize = true, Location = new Point(528, ey + 4) };
            _edNote.Location = new Point(566, ey); _edNote.Size = new Size(246, 23);
            _edNote.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            var l4 = new Label { Text = "Command:", AutoSize = true, Location = new Point(8, ey + 34) };
            _edCommand.Location = new Point(70, ey + 30); _edCommand.Size = new Size(446, 23);
            _edCommand.Font = new Font("Consolas", 9F);
            _edCommand.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _btnSave.Location = new Point(524, ey + 29); _btnSave.Size = new Size(108, 25);
            _btnSave.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _btnNew.Location = new Point(636, ey + 29); _btnNew.Size = new Size(76, 25);
            _btnNew.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _btnDelete.Location = new Point(716, ey + 29); _btnDelete.Size = new Size(96, 25);
            _btnDelete.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _tip.SetToolTip(_edCat, "Category for the entry being edited (type a new one to create a category).");
            _tip.SetToolTip(_edLabel, "Display label for this entry.");
            _tip.SetToolTip(_edNote, "Optional note.");
            _tip.SetToolTip(_edCommand, "The adb shell command or FTP path to save.");
            _tip.SetToolTip(_btnSave, "Save this entry (updates it if it already exists) into the library.");
            _tip.SetToolTip(_btnNew, "Clear the editor fields to start a new entry.");
            _tip.SetToolTip(_btnDelete, "Delete the selected entry from the library.");

            _status.Location = new Point(8, ey + 62);
            _status.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

            Controls.AddRange(new Control[]
            {
                lblCat, _cboCat, lblFind, _txtSearch, _btnUse, _list,
                l1, _edCat, l2, _edLabel, l3, _edNote, l4, _edCommand,
                _btnSave, _btnNew, _btnDelete, _status
            });

            _cboCat.SelectedIndexChanged += (s, e) => Reload();
            _txtSearch.TextChanged += (s, e) => Reload();
            _list.SelectedIndexChanged += (s, e) => LoadEditorFromSelection();
            _list.DoubleClick += (s, e) => UseSelected();
            _btnUse.Click += (s, e) => UseSelected();
            _btnSave.Click += (s, e) => SaveEditor();
            _btnNew.Click += (s, e) => ClearEditor();
            _btnDelete.Click += (s, e) => DeleteSelected();

            Theme.Apply(this);
            _list.BackColor = Theme.Bg1; _list.ForeColor = Theme.Text;

            RefreshCategories();
            Reload();
        }

        void RefreshCategories()
        {
            string cur = _cboCat.SelectedItem as string ?? "All";
            _cboCat.Items.Clear();
            _cboCat.Items.Add("All");
            foreach (var c in CommandLibrary.Categories()) _cboCat.Items.Add(c);
            _cboCat.SelectedItem = _cboCat.Items.Contains(cur) ? cur : "All";

            if (_edCat.Items.Count == 0)
                foreach (var c in new[] { "ADB", "FTP", "Inject", "Shim" }) _edCat.Items.Add(c);
        }

        void Reload()
        {
            string cat = _cboCat.SelectedItem as string ?? "All";
            _list.BeginUpdate();
            _list.Items.Clear();
            foreach (var e in CommandLibrary.Items(cat, _txtSearch.Text.Trim()))
            {
                var it = new ListViewItem(new[]
                {
                    e.Category, e.Label, e.Command, e.UsedCount.ToString(), e.LastUsed ?? "", e.Note
                })
                { Tag = e };
                _list.Items.Add(it);
            }
            _list.EndUpdate();
            SetStatus($"{_list.Items.Count} entr{(_list.Items.Count == 1 ? "y" : "ies")}");
        }

        CommandEntry? Selected() => _list.SelectedItems.Count > 0 ? _list.SelectedItems[0].Tag as CommandEntry : null;

        void LoadEditorFromSelection()
        {
            var e = Selected();
            if (e == null) return;
            _edCat.Text = e.Category; _edLabel.Text = e.Label; _edCommand.Text = e.Command; _edNote.Text = e.Note;
        }

        void ClearEditor()
        {
            _edLabel.Text = _edCommand.Text = _edNote.Text = "";
            if (_cboCat.SelectedItem is string c && c != "All") _edCat.Text = c;
        }

        void SaveEditor()
        {
            string cmd = _edCommand.Text.Trim();
            if (cmd.Length == 0) { SetStatus("command is empty", Theme.Warn); return; }
            CommandLibrary.AddOrUpdate(new CommandEntry
            {
                Category = string.IsNullOrWhiteSpace(_edCat.Text) ? "ADB" : _edCat.Text.Trim(),
                Label = _edLabel.Text.Trim().Length == 0 ? cmd : _edLabel.Text.Trim(),
                Command = cmd,
                Note = _edNote.Text.Trim()
            });
            RefreshCategories(); Reload();
            SetStatus("saved");
        }

        void DeleteSelected()
        {
            var e = Selected();
            if (e == null) return;
            CommandLibrary.Remove(e);
            RefreshCategories(); Reload(); ClearEditor();
            SetStatus("deleted");
        }

        void UseSelected()
        {
            var e = Selected();
            if (e == null) return;
            if (e.Category.Equals("FTP", StringComparison.OrdinalIgnoreCase) && _useFtp != null) _useFtp(e.Command);
            else if (_useAdb != null) _useAdb(e.Command);
            else { try { Clipboard.SetText(e.Command); } catch { } SetStatus("copied to clipboard: " + e.Command); }
            CommandLibrary.RecordUsed(e.Category, e.Command);
            Reload();
        }

        void SetStatus(string text, Color? c = null) { _status.ForeColor = c ?? Color.DimGray; _status.Text = text; }
    }
}

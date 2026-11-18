using System.IO.Ports;
using System.Text;

namespace HS175D
{
    /// <summary>
    /// One independent serial-port monitor. Connect + view (text/hex), a continuous running log
    /// (Record), an independent one-shot Snapshot, inline timestamped markers, and trigger matching.
    /// Hosted two-up by <see cref="SerialMonitorForm"/>; the two panes never touch each other's port
    /// or log — cross-pane actions (mark-both, diff) are driven by the form.
    /// </summary>
    public sealed class SerialMonitorPane : UserControl
    {
        readonly ToolTip _tip = new() { AutoPopDelay = 30000, InitialDelay = 350, ReshowDelay = 80, ShowAlways = true };
        SerialPort? _port;
        readonly ComboBox _cboPort = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        readonly ComboBox _cboBaud = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        readonly Button _btnRefresh = Theme.Btn("⟳", 0, 0, 30);
        readonly Button _btnConnect = Theme.AccentBtn("Connect", 0, 0, 84);
        readonly Button _btnClear   = Theme.Btn("Clear", 0, 0, 50);
        readonly CheckBox _chkHex   = new() { Text = "Hex", AutoSize = true };
        readonly NumericUpDown _numDur = new() { Minimum = 0, Maximum = 86400, Value = 0 };
        readonly Label _lblDur = Theme.Lbl("Stop(s):", 0, 0, Theme.Text);
        readonly Button _btnRec  = Theme.Btn("● Record", 0, 0, 74);
        readonly Button _btnSnap = Theme.Btn("Snapshot…", 0, 0, 80);
        readonly TextBox _txtMark = new();
        readonly Button _btnMark = Theme.Btn("Mark", 0, 0, 50);
        readonly Label _status = Theme.Lbl("Disconnected", 0, 0, Theme.Danger);
        readonly TextBox _rx = Theme.Mono_(readOnly: true);

        readonly List<byte> _raw = new();
        const int RawCap = 1_000_000;
        readonly StringBuilder _pending = new();
        readonly System.Windows.Forms.Timer _flush = new() { Interval = 100 };
        volatile bool _hex;
        long _count;
        StreamWriter? _log;
        DateTime _startUtc;
        string[] _triggers = Array.Empty<string>();

        public string Title { get; }
        public event Action<SerialMonitorPane, string>? TriggerHit;

        public SerialMonitorPane(string title)
        {
            Title = title;
            BackColor = Theme.Bg1;

            var top = new Panel { Dock = DockStyle.Top, Height = 76, BackColor = Theme.Bg1 };
            var lblTitle = Theme.Lbl(title, 8, 8, Theme.Accent); lblTitle.Font = new Font(lblTitle.Font, FontStyle.Bold);

            _cboPort.SetBounds(30, 6, 96, 24); _cboPort.BackColor = Theme.Bg2; _cboPort.ForeColor = Theme.Text; _cboPort.FlatStyle = FlatStyle.Flat;
            _btnRefresh.Location = new Point(130, 5);
            _cboBaud.SetBounds(166, 6, 80, 24); _cboBaud.BackColor = Theme.Bg2; _cboBaud.ForeColor = Theme.Text; _cboBaud.FlatStyle = FlatStyle.Flat;
            _cboBaud.Items.AddRange(new object[] { 4800, 9600, 19200, 38400, 57600, 74880, 115200, 230400, 460800, 921600 });
            _cboBaud.SelectedItem = 115200;
            _btnConnect.Location = new Point(252, 5);
            _chkHex.Location = new Point(344, 10); _chkHex.ForeColor = Theme.Text; _chkHex.BackColor = Color.Transparent;
            _btnClear.Location = new Point(392, 5);

            _lblDur.Location = new Point(8, 46);
            _numDur.SetBounds(58, 42, 54, 24); _numDur.BackColor = Theme.Bg2; _numDur.ForeColor = Theme.Text; _numDur.BorderStyle = BorderStyle.FixedSingle;
            _btnRec.Location  = new Point(120, 41);
            _btnSnap.Location = new Point(198, 41);
            _txtMark.SetBounds(286, 43, 120, 22); _txtMark.BackColor = Theme.Bg2; _txtMark.ForeColor = Theme.Text; _txtMark.BorderStyle = BorderStyle.FixedSingle;
            _txtMark.PlaceholderText = "marker note…";
            _btnMark.Location = new Point(412, 41);

            top.Controls.AddRange(new Control[] { lblTitle, _cboPort, _btnRefresh, _cboBaud, _btnConnect, _chkHex, _btnClear,
                                                  _lblDur, _numDur, _btnRec, _btnSnap, _txtMark, _btnMark });

            _status.Dock = DockStyle.Bottom; _status.Height = 20; _status.TextAlign = ContentAlignment.MiddleLeft;
            _rx.Dock = DockStyle.Fill; _rx.WordWrap = true; _rx.ScrollBars = ScrollBars.Vertical;

            Controls.Add(top);
            Controls.Add(_status);
            Controls.Add(_rx);

            _btnRefresh.Click += (s, e) => RefreshPorts();
            _btnConnect.Click += (s, e) => Toggle();
            _btnClear.Click   += (s, e) => { lock (_raw) _raw.Clear(); lock (_pending) _pending.Clear(); _count = 0; _rx.Clear(); UpdateStatus(); };
            _chkHex.CheckedChanged += (s, e) => { _hex = _chkHex.Checked; Rerender(); };
            _btnRec.Click  += (s, e) => ToggleRecord();
            _btnSnap.Click += (s, e) => SaveSnapshot();
            _btnMark.Click += (s, e) => MarkFromBox();

            _tip.SetToolTip(_btnRefresh, "Rescan available COM ports.");
            _tip.SetToolTip(_btnConnect, "Open/close this pane's serial connection.");
            _tip.SetToolTip(_btnClear, "Clear the log view and buffered data.");
            _tip.SetToolTip(_chkHex, "Render bytes as hex instead of text.");
            _tip.SetToolTip(_btnRec, "Start/stop a continuous timestamped recording to a log file.");
            _tip.SetToolTip(_btnSnap, "Save a one-shot snapshot of the current view to a file.");
            _tip.SetToolTip(_btnMark, "Insert the note in the box as a timestamped marker in this pane's log.");
            _tip.SetToolTip(_numDur, "Auto-stop recording after this many seconds (0 = no limit).");
            _tip.SetToolTip(_txtMark, "Note text for the Mark button — press Enter or click Mark to insert.");
            _tip.SetToolTip(_cboPort, "This pane's COM port.");
            _tip.SetToolTip(_cboBaud, "This pane's baud rate.");
            _txtMark.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { MarkFromBox(); e.SuppressKeyPress = true; } };

            RefreshPorts();
            _flush.Tick += (s, e) => OnTick();
            _flush.Start();
            Disposed += (s, e) => { _flush.Stop(); _flush.Dispose(); ClosePort(); };
        }

        // ---- public API used by the host form ----
        public void SetTriggers(string[] patterns) => _triggers = patterns ?? Array.Empty<string>();
        public string ViewText => _rx.Text;
        public bool IsConnected => _port is { IsOpen: true };

        /// <summary>Insert a timestamped marker into the view AND the running log (if recording).</summary>
        public void InsertMarker(string note)
        {
            var line = $"\r\n=== {DateTime.Now:HH:mm:ss}  {note} ===\r\n";
            lock (_pending) _pending.Append(line);
        }

        void MarkFromBox()
        {
            var n = _txtMark.Text.Trim();
            InsertMarker(n.Length == 0 ? "MARK" : n);
            _txtMark.Clear();
        }

        void RefreshPorts()
        {
            var sel = (_cboPort.SelectedItem as PortInfo.Entry)?.Port;
            _cboPort.Items.Clear();
            var ports = PortInfo.List();
            _cboPort.Items.AddRange(ports);
            var match = sel != null ? ports.FirstOrDefault(p => p.Port == sel) : null;
            if (match != null) _cboPort.SelectedItem = match;
            else if (ports.Length > 0) _cboPort.SelectedIndex = ports.Length - 1;
        }

        void Toggle()
        {
            if (_port is { IsOpen: true }) { ClosePort(); return; }
            if (_cboPort.SelectedItem is not PortInfo.Entry sel) { SetStatus("pick a COM port", Theme.Danger); return; }
            string name = sel.Port;
            try
            {
                _port = new SerialPort(name, (int)_cboBaud.SelectedItem!, Parity.None, 8, StopBits.One)
                { DtrEnable = false, RtsEnable = false, ReadTimeout = 500 };
                _port.DataReceived += OnData;
                _port.Open();
                try { _port.Write(new byte[] { (byte)'\n' }, 0, 1); } catch { }   // wake the ESP32 (start-on-connect)
                _startUtc = DateTime.UtcNow;
                _btnConnect.Text = "Disconnect";
                UpdateStatus();
            }
            catch (Exception ex) { SetStatus("open failed: " + ex.Message, Theme.Danger); ClosePort(); }
        }

        void ClosePort()
        {
            try { if (_port != null) { _port.DataReceived -= OnData; if (_port.IsOpen) _port.Close(); _port.Dispose(); } }
            catch { }
            _port = null;
            StopRecord();
            if (_btnConnect.IsHandleCreated) _btnConnect.Text = "Connect";
            SetStatus("Disconnected", Theme.Danger);
        }

        void OnData(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                var p = _port; if (p == null) return;
                int n = p.BytesToRead; if (n <= 0) return;
                var buf = new byte[n];
                int got = p.Read(buf, 0, n); if (got <= 0) return;
                if (got != n) Array.Resize(ref buf, got);

                _count += got;
                lock (_raw) { _raw.AddRange(buf); if (_raw.Count > RawCap) _raw.RemoveRange(0, _raw.Count - RawCap); }
                var text = Format(buf, _hex);
                if (text.Length > 0)
                    lock (_pending) { _pending.Append(text); if (_pending.Length > 2_000_000) _pending.Remove(0, _pending.Length - 2_000_000); }
            }
            catch { }
        }

        static string Format(byte[] b, bool hex)
        {
            var sb = new StringBuilder(b.Length * (hex ? 3 : 1));
            if (hex) foreach (var x in b) { sb.Append(x.ToString("x2")); sb.Append(' '); }
            else foreach (var x in b) sb.Append(x == 0x0a ? "\r\n" : x == 0x0d ? "" : (x >= 0x20 && x < 0x7f) ? (char)x : '.');
            return sb.ToString();
        }

        void Rerender()
        {
            byte[] snap; lock (_raw) snap = _raw.ToArray();
            lock (_pending) _pending.Clear();
            if (!IsHandleCreated) return;
            try { BeginInvoke(() => { _rx.Clear(); _rx.AppendText(Format(snap, _hex)); }); } catch { }
        }

        void OnTick()
        {
            FlushPending();
            if (_port is { IsOpen: true })
            {
                var elapsed = DateTime.UtcNow - _startUtc;
                UpdateStatus(elapsed);
                int limit = (int)_numDur.Value;
                if (limit > 0 && elapsed.TotalSeconds >= limit) ClosePort();   // auto-stop
            }
        }

        void FlushPending()
        {
            string s;
            lock (_pending) { if (_pending.Length == 0) return; s = _pending.ToString(); _pending.Clear(); }
            if (_log != null) { try { _log.Write(s); _log.Flush(); } catch { } }
            CheckTriggers(s);
            if (!IsHandleCreated) return;
            if (_rx.TextLength > 400000) _rx.Clear();
            const int MaxAppend = 200000;
            if (s.Length > MaxAppend) s = s[^MaxAppend..];
            _rx.AppendText(s);
        }

        void CheckTriggers(string s)
        {
            if (_triggers.Length == 0) return;
            foreach (var pat in _triggers)
            {
                if (pat.Length == 0) continue;
                if (s.IndexOf(pat, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    lock (_pending) _pending.Append($"\r\n>>> TRIGGER '{pat}' @ {DateTime.Now:HH:mm:ss} <<<\r\n");
                    try { System.Media.SystemSounds.Exclamation.Play(); } catch { }
                    if (IsHandleCreated) { try { BeginInvoke(() => TriggerHit?.Invoke(this, pat)); } catch { } }
                }
            }
        }

        void ToggleRecord()
        {
            if (_log != null) { StopRecord(); return; }
            using var dlg = new SaveFileDialog { Filter = "Log (*.txt)|*.txt|All files (*.*)|*.*", FileName = $"{Title}_rec_{DateTime.Now:yyyyMMdd_HHmmss}.txt" };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                _log = new StreamWriter(dlg.FileName, append: false) { AutoFlush = false };
                _log.WriteLine($"=== recording {Title} started {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");   // header => never 0 bytes
                _log.Flush();
                _btnRec.Text = "■ Stop"; UpdateStatus();
            }
            catch (Exception ex) { SetStatus("record failed: " + ex.Message, Theme.Danger); }
        }

        void StopRecord()
        {
            if (_log == null) return;
            try { _log.Flush(); _log.Dispose(); } catch { }
            _log = null;
            if (_btnRec.IsHandleCreated) _btnRec.Text = "● Record";
        }

        // Independent one-shot: writes the CURRENT view straight to a new file. Never touches the running log.
        void SaveSnapshot()
        {
            var content = _rx.Text;
            using var dlg = new SaveFileDialog { Filter = "Text (*.txt)|*.txt|All files (*.*)|*.*", FileName = $"{Title}_snap_{DateTime.Now:yyyyMMdd_HHmmss}.txt" };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            try { File.WriteAllText(dlg.FileName, content); SetStatus($"snapshot saved ({content.Length:N0} chars)", Theme.Good); }
            catch (Exception ex) { SetStatus("snapshot failed: " + ex.Message, Theme.Danger); }
        }

        void UpdateStatus(TimeSpan? elapsed = null)
        {
            if (_port is { IsOpen: true })
            {
                var t = elapsed ?? DateTime.UtcNow - _startUtc;
                var rec = _log != null ? "  ●REC" : "";
                SetStatus($"{_port.PortName} @ {_cboBaud.SelectedItem} — {_count:N0} B — {t:mm\\:ss}{rec}", Theme.Good);
            }
        }

        void SetStatus(string t, Color c)
        {
            if (!IsHandleCreated) { _status.Text = t; _status.ForeColor = c; return; }
            try { BeginInvoke(() => { _status.Text = t; _status.ForeColor = c; }); } catch { }
        }
    }
}

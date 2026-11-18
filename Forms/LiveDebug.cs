using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace HS175D
{
    /// <summary>
    /// Live multi-source debug console. Tails several drone-side streams over adb AT ONCE and merges
    /// them into one timestamped, source-tagged, filterable view, so the whole craft can be debugged
    /// on the bench (no flying required). Default sources:
    ///   • LOGCAT   — system log (follows)            : logcat -v time
    ///   • MCU      — loglevel-5 flight frame dumps    : logcat | grep -i 'app ot yt'   (PROTOCOL.md)
    ///   • KERNEL   — kernel ring buffer (streams)     : cat /proc/kmsg
    ///   • UART(FC) — raw SoC↔flight-controller link   : stty .../dev/ttyS1 raw; cat | hexdump -C
    ///   • WIFI     — phone/app↔drone packets          : tcpdump -i ap0 -l -n -s 0
    /// Every command is an editable adb-shell line — the drone's exact toolset (hexdump/tcpdump/baud)
    /// varies, so tweak per source and re-Start. Keep the main app connected (23333 keepalive) so the
    /// drone doesn't idle-off mid-capture (DEVICE_ACCESS.md §0).
    /// </summary>
    public sealed class LiveDebug : Form
    {
        readonly string _adb;
        readonly string _ip;

        sealed class Source
        {
            public string Name = "";
            public Color Color;
            public CheckBox Enable = null!;
            public TextBox Cmd = null!;
            public Process? Proc;
        }

        readonly record struct LogLine(DateTime Ts, string Tag, Color Color, string Text);

        readonly List<Source> _sources = new();
        readonly ConcurrentQueue<LogLine> _incoming = new();
        readonly List<LogLine> _buffer = new();      // capped ring: feeds filter re-render + Save
        const int MaxBuffer = 20000;
        const int MaxRender = 4000;                  // lines re-drawn on filter change / overflow trim

        readonly RichTextBox _view = new()
        {
            ReadOnly = true, WordWrap = false, ScrollBars = RichTextBoxScrollBars.Both,
            Font = new Font("Consolas", 9F), DetectUrls = false, HideSelection = false,
            BackColor = Color.FromArgb(18, 18, 18), ForeColor = Color.Gainsboro
        };
        readonly TextBox _filter = new();
        readonly CheckBox _pause = new() { Text = "Pause", AutoSize = true };
        readonly CheckBox _autoscroll = new() { Text = "Auto-scroll", AutoSize = true, Checked = true };
        readonly Button _btnStart = new() { Text = "Start All" };
        readonly Button _btnStop = new() { Text = "Stop All" };
        readonly Button _btnClear = new() { Text = "Clear" };
        readonly Button _btnSave = new() { Text = "Save…" };
        readonly Label _status = new() { AutoSize = true, ForeColor = Color.DimGray, Text = "idle" };
        readonly System.Windows.Forms.Timer _flush = new() { Interval = 120 };

        // ---- Flight-Link Lab: shell runner + loglevel setter + GPS/optical capture & auto-diff ----
        readonly Action<int>? _setLogLevel;                 // routes SET_HY_LOGLEVEL (CMD 137) over WiFi via Form1
        readonly TextBox _shell = new() { Font = new Font("Consolas", 9F), Text = "tail -n 40 /tmp/messages" };
        readonly Button _btnRun = new() { Text = "Run" };
        readonly Button _btnRecon = new() { Text = "Recon" };
        readonly Button _btnPull = new() { Text = "Pull" };
        readonly Button _btnGrabA = new() { Text = "Grab GPS" };
        readonly Button _btnGrabB = new() { Text = "Grab optical" };
        readonly Button _btnDiff = new() { Text = "Diff A/B" };
        readonly NumericUpDown _numLog = new() { Minimum = 0, Maximum = 5, Value = 5 };
        readonly Button _btnSetLog = new() { Text = "Set log" };
        readonly Label _capInfo = new() { AutoSize = true, ForeColor = Color.DimGray, Text = "GPS:—  OPT:—" };
        string? _capA, _capB;                               // raw ttyS1 hex captures for GPS / optical

        // live mode watch — continuously parses ttyS1 and shows the current 0x8b byte[13] (0e=GPS / 02=optical)
        Process? _watchProc;
        readonly StringBuilder _watchBuf = new();
        readonly Button _btnWatch = new() { Text = "Watch mode" };
        readonly Label _lblMode = new() { AutoSize = true, ForeColor = Color.DimGray, Text = "mode: —", Font = new Font("Segoe UI", 11F, FontStyle.Bold) };
        readonly ToolTip _tip = new() { AutoPopDelay = 30000, InitialDelay = 350, ReshowDelay = 80, ShowAlways = true };

        public LiveDebug(string adb, string ip, Action<int>? setLogLevel = null)
        {
            _adb = adb;
            _setLogLevel = setLogLevel;
            _ip = string.IsNullOrWhiteSpace(ip) ? "192.168.100.1" : ip.Trim();

            Text = $"Live debug — {_ip} (adb :5555, {_sources.Count}+ streams)";
            ClientSize = new Size(1000, 720);
            MinimumSize = new Size(680, 460);
            Font = new Font("Segoe UI", 9F);

            // ---- source rows: [enable+name]  [editable adb-shell command] -----
            AddSource("LOGCAT",   Color.FromArgb(120, 200, 255), "logcat -v time",                                                true);
            AddSource("MCU",      Color.Gold,                    "tail -n 0 -f /tmp/messages",                                    true);
            AddSource("KERNEL",   Color.FromArgb(170, 170, 170), "cat /proc/kmsg",                                                true);
            AddSource("UART(FC)", Color.FromArgb(120, 230, 120), "stty -F /dev/ttyS1 raw -echo 115200; cat /dev/ttyS1 | hexdump -C", false);
            AddSource("WIFI",     Color.Violet,                  "tcpdump -i ap0 -l -n -s 0",                                     false);

            int y = 10;
            foreach (var s in _sources)
            {
                s.Enable.SetBounds(8, y + 2, 96, 20);
                s.Cmd.SetBounds(108, y, ClientSize.Width - 116, 23);
                s.Cmd.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                _tip.SetToolTip(s.Enable, $"Include the {s.Name} stream in Start All.");
                _tip.SetToolTip(s.Cmd, "Editable adb-shell command for this source — tweak and re-Start if the drone's toolset differs.");
                Controls.Add(s.Enable);
                Controls.Add(s.Cmd);
                y += 28;
            }

            // ---- Flight-Link Lab: shell runner ------------------------------
            int ly = y + 6;
            var lblRun = new Label { Text = "Run:", AutoSize = true, Location = new Point(8, ly + 5) };
            _shell.SetBounds(46, ly + 2, ClientSize.Width - 46 - 254, 23);
            _shell.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _btnRecon.SetBounds(ClientSize.Width - 246, ly, 74, 26);
            _btnRecon.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _btnPull.SetBounds(ClientSize.Width - 164, ly, 74, 26);
            _btnPull.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _btnRun.SetBounds(ClientSize.Width - 82, ly, 74, 26);
            _btnRun.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            // ---- Flight-Link Lab: GPS/optical capture + diff + loglevel -----
            int ly2 = ly + 32;
            _btnGrabA.SetBounds(8, ly2, 92, 26);
            _btnGrabB.SetBounds(104, ly2, 104, 26);
            _btnDiff.SetBounds(212, ly2, 80, 26);
            _capInfo.Location = new Point(300, ly2 + 6);
            _btnWatch.SetBounds(404, ly2, 100, 26);
            _lblMode.Location = new Point(512, ly2 + 4);
            var lblLog = new Label { Text = "loglevel", AutoSize = true, Location = new Point(ClientSize.Width - 188, ly2 + 5), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            _numLog.SetBounds(ClientSize.Width - 130, ly2 + 2, 46, 23);
            _numLog.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _btnSetLog.SetBounds(ClientSize.Width - 80, ly2, 72, 26);
            _btnSetLog.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            // ---- control row -------------------------------------------------
            int cy = ly2 + 34;
            _btnStart.SetBounds(8, cy, 84, 26);
            _btnStop.SetBounds(96, cy, 84, 26);
            var lblFilter = new Label { Text = "Filter:", AutoSize = true, Location = new Point(192, cy + 5) };
            _filter.SetBounds(238, cy + 2, 250, 23);
            _pause.Location = new Point(500, cy + 5);
            _autoscroll.Location = new Point(562, cy + 5);
            _btnClear.SetBounds(672, cy, 72, 26);
            _btnSave.SetBounds(748, cy, 84, 26);

            _view.SetBounds(8, cy + 32, ClientSize.Width - 16, ClientSize.Height - (cy + 32) - 30);
            _view.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            _status.Location = new Point(8, ClientSize.Height - 22);
            _status.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

            Controls.AddRange(new Control[]
            {
                lblRun, _shell, _btnRecon, _btnPull, _btnRun,
                _btnGrabA, _btnGrabB, _btnDiff, _capInfo, _btnWatch, _lblMode, lblLog, _numLog, _btnSetLog,
                _btnStart, _btnStop, lblFilter, _filter, _pause, _autoscroll,
                _btnClear, _btnSave, _view, _status
            });

            _btnStart.Click += (_, _) => StartAll();
            _btnStop.Click  += (_, _) => _ = StopAllAsync();
            _btnClear.Click += (_, _) => { _buffer.Clear(); _view.Clear(); };
            _btnSave.Click  += (_, _) => Save();
            _btnRun.Click   += (_, _) => RunShell();
            _btnRecon.Click += (_, _) => Recon();
            _btnPull.Click  += (_, _) => PullConfigs();
            _btnSetLog.Click += (_, _) => SetLog();
            _btnGrabA.Click += (_, _) => Grab("GPS");
            _btnGrabB.Click += (_, _) => Grab("optical");
            _btnDiff.Click  += (_, _) => DiffAB();
            _btnWatch.Click += (_, _) => ToggleWatch();

            _tip.SetToolTip(_btnStart, "Start every enabled source stream over adb.");
            _tip.SetToolTip(_btnStop, "Stop all running source streams.");
            _tip.SetToolTip(_btnClear, "Clear the merged view and buffer.");
            _tip.SetToolTip(_btnSave, "Save the current buffer to a file.");
            _tip.SetToolTip(_filter, "Filter the merged view by text.");
            _tip.SetToolTip(_pause, "Pause the live view (streams keep buffering).");
            _tip.SetToolTip(_autoscroll, "Keep the view scrolled to the newest line.");
            _tip.SetToolTip(_shell, "One-off adb shell command to run.");
            _tip.SetToolTip(_btnRun, "Run the shell command above and show its output.");
            _tip.SetToolTip(_btnRecon, "Quick recon sweep — a handful of useful read-only commands in one click.");
            _tip.SetToolTip(_btnPull, "Pull key config files (hostapd.conf etc.) from the drone for inspection.");
            _tip.SetToolTip(_btnGrabA, "Capture a raw ttyS1 hex sample while the drone is in GPS mode (capture A).");
            _tip.SetToolTip(_btnGrabB, "Capture a raw ttyS1 hex sample while the drone is in Optical mode (capture B).");
            _tip.SetToolTip(_btnDiff, "Diff capture A vs capture B to isolate the GPS↔optical mode-state bytes.");
            _tip.SetToolTip(_numLog, "SET_HY_LOGLEVEL value to apply (5 = firmware frame-dump logging on).");
            _tip.SetToolTip(_btnSetLog, "Apply the loglevel above over the live WiFi control connection (CMD 137).");
            _tip.SetToolTip(_btnWatch, "Continuously parse ttyS1 and show the live GPS/Optical mode byte (0x8b[13]).");
            _filter.TextChanged += (_, _) => ReRender();
            _flush.Tick += FlushTick;

            Theme.Apply(this);
            _view.BackColor = Color.FromArgb(18, 18, 18);   // keep the console dark after theming
            _view.ForeColor = Color.Gainsboro;
        }

        void AddSource(string name, Color color, string defaultCmd, bool on)
        {
            _sources.Add(new Source
            {
                Name = name,
                Color = color,
                Enable = new CheckBox { Text = name, Checked = on, AutoSize = true },
                Cmd = new TextBox { Text = defaultCmd, Font = new Font("Consolas", 9F) }
            });
        }

        // ================= start / stop streaming ===========================
        async void StartAll()
        {
            _btnStart.Enabled = false; _btnStop.Enabled = false;
            Status("connecting…");
            await StopAllAsync();

            // Snapshot the enabled sources on the UI thread, then do ALL the blocking adb work
            // (connect + Process.Start) on a background thread so the UI never freezes.
            var jobs = new List<(Source s, string cmd)>();
            foreach (var s in _sources)
            {
                var cmd = s.Cmd.Text.Trim();
                if (s.Enable.Checked && cmd.Length > 0) jobs.Add((s, cmd));
            }
            int started = 0;
            try
            {
                started = await Task.Run(() =>
                {
                    RunOnce($"connect {_ip}:5555");
                    int n = 0;
                    foreach (var (s, cmd) in jobs)
                        if ((s.Proc = StartStream(s, cmd)) != null) n++;
                    return n;
                });
            }
            catch (Exception ex) { Status("start failed: " + ex.Message); }
            _flush.Start();
            _btnStart.Enabled = true; _btnStop.Enabled = true;
            Status(started > 0 ? $"streaming {started} source(s)…" : "nothing enabled.");
        }

        Process? StartStream(Source s, string cmd)
        {
            var psi = new ProcessStartInfo(_adb, $"-s {_ip}:5555 shell \"{cmd}\"")
            {
                RedirectStandardOutput = true, RedirectStandardError = true,
                UseShellExecute = false, CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8
            };
            Process p;
            try { p = Process.Start(psi)!; }
            catch (Exception ex) { Enqueue(s, "[start failed] " + ex.Message); return null; }

            p.OutputDataReceived += (_, e) => { if (e.Data != null) Enqueue(s, e.Data); };
            p.ErrorDataReceived  += (_, e) => { if (e.Data != null) Enqueue(s, "[err] " + e.Data); };
            try { p.BeginOutputReadLine(); p.BeginErrorReadLine(); }
            catch (Exception ex) { Enqueue(s, "[read failed] " + ex.Message); }
            return p;
        }

        async Task StopAllAsync()
        {
            _flush.Stop();
            var procs = new List<Process>();
            foreach (var s in _sources) { if (s.Proc != null) procs.Add(s.Proc); s.Proc = null; }
            if (procs.Count > 0)
                await Task.Run(() => { foreach (var p in procs) KillProc(p); });   // killing adb can stall — keep it off the UI thread
            FlushTick(null, EventArgs.Empty);   // drain whatever arrived before the kill
            Status("stopped.");
        }

        static void KillProc(Process p)
        {
            try { if (!p.HasExited) p.Kill(entireProcessTree: true); } catch { }
            try { p.Dispose(); } catch { }
        }

        void RunOnce(string args)
        {
            var psi = new ProcessStartInfo(_adb, args)
            {
                RedirectStandardOutput = true, RedirectStandardError = true,
                UseShellExecute = false, CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            p?.WaitForExit(8000);
        }

        void Enqueue(Source s, string line) => _incoming.Enqueue(new LogLine(DateTime.Now, s.Name, s.Color, line));

        // ================= tool probe =======================================
        // On open, ask the drone which tools exist so unusable sources are unticked up front
        // (rather than silently dying with an [err] when you hit Start).
        async Task CheckToolsAsync()
        {
            Status("probing drone tools…");
            string outp;
            try
            {
                await Task.Run(() => RunOnce($"connect {_ip}:5555"));
                outp = await Task.Run(() => AdbCapture(
                    "for t in logcat tcpdump hexdump od stty; do which $t >/dev/null 2>&1 && echo have:$t; done; [ -e /dev/ttyS1 ] && echo have:ttyS1"));
            }
            catch (Exception ex) { Status("tool probe failed: " + ex.Message); return; }

            var have = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var raw in outp.Replace("\r", "").Split('\n'))
            {
                var t = raw.Trim();
                if (t.StartsWith("have:")) have.Add(t[5..]);
            }
            Banner("tool check — present: " + (have.Count > 0 ? string.Join(" ", have) : "(none — is adb reachable / authorized?)"));

            // UART: needs the serial node + a hex dumper; fall back hexdump → od if only od exists.
            var uart = Find("UART(FC)");
            if (uart != null && have.Contains("ttyS1") && !have.Contains("hexdump") && have.Contains("od"))
            {
                uart.Cmd.Text = "stty -F /dev/ttyS1 raw -echo 115200; cat /dev/ttyS1 | od -An -tx1";
                Banner("UART: hexdump missing → using od -An -tx1");
            }
            Mark("UART(FC)", have.Contains("ttyS1") && (have.Contains("hexdump") || have.Contains("od")), "needs /dev/ttyS1 + hexdump/od");
            Mark("WIFI", have.Contains("tcpdump"), "tcpdump not found — push a static build to /tmp and edit the command");
            Status("tool check done — Start All when ready.");
        }

        void Mark(string name, bool ok, string whyNot)
        {
            if (ok) return;
            var s = Find(name);
            if (s == null) return;
            s.Enable.Checked = false;
            Banner($"{name} unavailable — {whyNot}");
        }

        Source? Find(string name)
        {
            foreach (var s in _sources) if (s.Name == name) return s;
            return null;
        }

        string AdbCapture(string remoteCmd)
        {
            var psi = new ProcessStartInfo(_adb, $"-s {_ip}:5555 shell \"{remoteCmd}\"")
            {
                RedirectStandardOutput = true, RedirectStandardError = true,
                UseShellExecute = false, CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p == null) return "";
            string o = p.StandardOutput.ReadToEnd();
            p.WaitForExit(8000);
            return o;
        }

        void Banner(string text)
        {
            var ln = new LogLine(DateTime.Now, "DEBUG", Color.Orange, text);
            _buffer.Add(ln);
            if (!_pause.Checked) AppendBatch(() => AppendLine(ln));
        }

        // ================= render ===========================================
        void FlushTick(object? sender, EventArgs e)
        {
            if (_incoming.IsEmpty) return;
            var batch = new List<LogLine>(256);
            while (_incoming.TryDequeue(out var ln)) batch.Add(ln);

            _buffer.AddRange(batch);
            if (_buffer.Count > MaxBuffer) _buffer.RemoveRange(0, _buffer.Count - MaxBuffer);

            if (_pause.Checked) return;
            if (_view.TextLength > 1_500_000) { ReRender(); return; }   // keep the control from bloating

            string flt = _filter.Text;
            AppendBatch(() => { foreach (var ln in batch) if (Passes(ln, flt)) AppendLine(ln); });
        }

        void ReRender()
        {
            string flt = _filter.Text;
            AppendBatch(() =>
            {
                _view.Clear();
                int start = Math.Max(0, _buffer.Count - MaxRender);
                for (int i = start; i < _buffer.Count; i++)
                    if (Passes(_buffer[i], flt)) AppendLine(_buffer[i]);
            });
        }

        // Append while honoring Auto-scroll: when it's OFF, RichTextBox.AppendText would still yank the
        // view to the caret, so snapshot the pixel scroll position and restore it after appending.
        void AppendBatch(Action append)
        {
            bool auto = _autoscroll.Checked;
            Point sp = default;
            if (!auto) SendMessage(_view.Handle, EM_GETSCROLLPOS, 0, ref sp);
            append();
            if (auto) ScrollEnd();
            else SendMessage(_view.Handle, EM_SETSCROLLPOS, 0, ref sp);
        }

        const int WM_USER = 0x400, EM_GETSCROLLPOS = WM_USER + 221, EM_SETSCROLLPOS = WM_USER + 222;
        [DllImport("user32.dll")]
        static extern int SendMessage(IntPtr hWnd, int msg, int wParam, ref Point lParam);

        void AppendLine(in LogLine ln)
        {
            _view.SelectionColor = Color.FromArgb(120, 120, 120);
            _view.AppendText(ln.Ts.ToString("HH:mm:ss.fff "));
            _view.SelectionColor = ln.Color;
            _view.AppendText((ln.Tag.Length > 8 ? ln.Tag[..8] : ln.Tag.PadRight(8)) + " ");
            _view.SelectionColor = Color.Gainsboro;
            _view.AppendText(ln.Text + "\n");
        }

        void ScrollEnd() { _view.SelectionStart = _view.TextLength; _view.SelectionLength = 0; _view.ScrollToCaret(); }

        static bool Passes(in LogLine ln, string flt)
            => flt.Length == 0
            || ln.Text.Contains(flt, StringComparison.OrdinalIgnoreCase)
            || ln.Tag.Contains(flt, StringComparison.OrdinalIgnoreCase);

        void Save()
        {
            using var dlg = new SaveFileDialog { Filter = "Log (*.log)|*.log|Text (*.txt)|*.txt|All files|*.*", FileName = $"dronedebug_{DateTime.Now:yyyyMMdd_HHmmss}.log" };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            var sb = new StringBuilder(_buffer.Count * 64);
            foreach (var ln in _buffer)
                sb.Append(ln.Ts.ToString("HH:mm:ss.fff ")).Append(ln.Tag.PadRight(8)).Append(' ').AppendLine(ln.Text);
            try { File.WriteAllText(dlg.FileName, sb.ToString()); Status($"saved {_buffer.Count} lines."); }
            catch (Exception ex) { Status("save failed: " + ex.Message); }
        }

        // ================= Flight-Link Lab ==================================
        // Push an arbitrary line into the merged view (used by the shell runner / diff).
        void Emit(string tag, Color c, string text)
        {
            var ln = new LogLine(DateTime.Now, tag, c, text);
            _buffer.Add(ln);
            if (!_pause.Checked) AppendBatch(() => AppendLine(ln));
        }

        // "do whatever we want" — run any adb-shell line and dump its output into the view.
        async void RunShell()
        {
            var cmd = _shell.Text.Trim();
            if (cmd.Length == 0) return;
            Emit("CMD", Color.Aqua, "$ " + cmd);
            await Task.Run(() => RunOnce($"connect {_ip}:5555"));
            string outp = await Task.Run(() => AdbCapture(cmd));
            foreach (var line in outp.Replace("\r", "").Split('\n'))
                if (line.Length > 0) Emit("CMD", Color.Aqua, line);
        }

        // One-click drone recon: which process owns the flight UART, its binary, the arm/lock/mode
        // strings baked into it, what's listening, and likely control nodes. Maps how the RC's arm
        // could be replicated from inside. (All single-quoted — no inner " — so it survives adb's wrap.)
        static readonly (string label, string cmd)[] ReconSteps =
        {
            ("system",     "uname -a; cat /proc/cmdline"),
            ("processes",  "ps -w 2>/dev/null || ps"),
            ("owns ttyS1", "fuser /dev/ttyS1 2>/dev/null; for f in /proc/[0-9]*/fd; do ls -l $f 2>/dev/null | grep -q ttyS1 && { p=${f%/fd}; echo OWNER $p $(tr '\\0' ' ' < $p/cmdline); }; done"),
            ("ttyS1 exe",  "for f in /proc/[0-9]*/fd; do ls -l $f 2>/dev/null | grep -q ttyS1 && readlink ${f%/fd}/exe; done | sort -u"),
            ("arm strings","for f in /proc/[0-9]*/fd; do ls -l $f 2>/dev/null | grep -q ttyS1 && { e=$(readlink ${f%/fd}/exe); echo --- $e ---; strings $e 2>/dev/null | grep -iE 'lock|unlock|arm|disarm|gps|optic|vision|posmode|flymode|takeoff|bind|calib' | sort -u | head -60; }; done"),
            ("listening",  "netstat -ln 2>/dev/null | head -40"),
            ("/tmp",       "ls -la /tmp 2>/dev/null"),
            ("nodes",      "ls -l /dev/ttyS* 2>/dev/null; ls -l /tmp/*.sock /tmp/*fifo* 2>/dev/null"),
        };

        async void Recon()
        {
            _btnRecon.Enabled = false;
            Banner("=== drone recon — how the flight bridge talks to the FC (paste this back) ===");
            try
            {
                await Task.Run(() => RunOnce($"connect {_ip}:5555"));
                foreach (var (label, cmd) in ReconSteps)
                {
                    Emit("RECON", Color.Orange, "# " + label);
                    string outp = await Task.Run(() => AdbCapture(cmd));
                    foreach (var line in outp.Replace("\r", "").Split('\n'))
                        if (line.Trim().Length > 0) Emit("RECON", Color.FromArgb(205, 205, 160), "  " + line);
                }
                Banner("=== recon done ===");
            }
            catch (Exception ex) { Banner("recon failed: " + ex.Message); }
            _btnRecon.Enabled = true;
        }

        // Copy the interesting drone-side config/protocol files to the PC so they can be read offline.
        static readonly string[] PullFiles =
        {
            "/tmp/ProtocolDesc.json", "/tmp/DroneID.json", "/tmp/A17Startup.sh",
            "/tmp/wifi_switch.sh", "/tmp/loglevel", "/tmp/messages",
        };

        async void PullConfigs()
        {
            _btnPull.Enabled = false;
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "HS175D_drone");
            try
            {
                Directory.CreateDirectory(dir);
                Banner($"pulling drone files → {dir}");
                await Task.Run(() => RunOnce($"connect {_ip}:5555"));
                foreach (var f in PullFiles)
                {
                    string local = Path.Combine(dir, Path.GetFileName(f));
                    await Task.Run(() => RunOnce($"-s {_ip}:5555 pull {f} \"{local}\""));
                    Emit("PULL", Color.Aqua, (File.Exists(local) ? "  ok    " : "  FAIL  ") + f + "  →  " + local);
                }
                Banner($"=== pull done — files are in {dir} ===");
            }
            catch (Exception ex) { Banner("pull failed: " + ex.Message); }
            _btnPull.Enabled = true;
        }

        // Set firmware log verbosity over WiFi (CMD 137). Level >=4 dumps the MCU frames to /tmp/messages.
        void SetLog()
        {
            int lv = (int)_numLog.Value;
            if (_setLogLevel == null) { Banner("loglevel set unavailable (open Live Debug from the main window so it can use the WiFi command channel)."); return; }
            _setLogLevel(lv);
            Banner($"requested SET_HY_LOGLEVEL={lv} over WiFi (CMD 137)" + (lv >= 4 ? " — MCU frames now dump to /tmp/messages." : "."));
        }

        // One-click capture of the SoC↔MCU link for a labelled mode (GPS / optical), parsed into frames.
        async void Grab(string which)
        {
            _btnGrabA.Enabled = _btnGrabB.Enabled = _btnDiff.Enabled = false;
            Banner($"capturing {which} from ttyS1 (~8 s) — hold {which} mode, sticks still…");
            try
            {
                await Task.Run(() => RunOnce($"connect {_ip}:5555"));
                string outp = await CaptureForAsync("stty -F /dev/ttyS1 raw -echo 115200; cat /dev/ttyS1 | od -An -tx1", 8000);
                if (which == "GPS") _capA = outp; else _capB = outp;
                int n = FcFrames.Parse(outp).Count;
                SetCapInfo();
                Banner($"{which}: {n} valid frames captured." + (n < 20 ? "  (low — capture is lossy; try again / hold longer)" : ""));
            }
            catch (Exception ex) { Banner($"{which} capture failed: " + ex.Message); }
            _btnGrabA.Enabled = _btnGrabB.Enabled = _btnDiff.Enabled = true;
        }

        // Start a streaming adb command, collect its stdout for ms, then kill it (robust whether or
        // not the drone has `timeout`).
        async Task<string> CaptureForAsync(string remoteCmd, int ms)
        {
            return await Task.Run(() =>
            {
                var psi = new ProcessStartInfo(_adb, $"-s {_ip}:5555 shell \"{remoteCmd}\"")
                {
                    RedirectStandardOutput = true, RedirectStandardError = true,
                    UseShellExecute = false, CreateNoWindow = true, StandardOutputEncoding = Encoding.UTF8
                };
                var sb = new StringBuilder();
                Process? p = null;
                try
                {
                    p = Process.Start(psi);
                    if (p == null) return "";
                    p.OutputDataReceived += (_, e) => { if (e.Data != null) lock (sb) sb.AppendLine(e.Data); };
                    p.BeginOutputReadLine();
                    p.WaitForExit(ms);
                }
                catch { }
                finally { if (p != null) KillProc(p); }
                lock (sb) return sb.ToString();
            });
        }

        void SetCapInfo()
        {
            string a = _capA != null ? FcFrames.Parse(_capA).Count.ToString() : "—";
            string b = _capB != null ? FcFrames.Parse(_capB).Count.ToString() : "—";
            _capInfo.Text = $"GPS:{a}  OPT:{b}";
        }

        // Compare the two captures and print the fields that are stable-but-different — the mode bit.
        void DiffAB()
        {
            if (_capA == null || _capB == null) { Banner("grab BOTH a GPS and an optical capture first."); return; }
            Banner("=== diff: GPS vs optical (stable fields that differ) ===");
            foreach (var line in FcFrames.Diff(_capA, _capB)) Emit("DIFF", Color.Yellow, line);
            Banner("=== a field above that's steady in each yet different between them is the mode flag ===");
        }

        // ---- live mode watch: stream ttyS1, show the latest 0x8b byte[13] as GPS / OPTICAL / 0xNN ----
        void ToggleWatch()
        {
            if (_watchProc != null) { StopWatch(); return; }
            _ = StartWatchAsync();
        }

        async Task StartWatchAsync()
        {
            _btnWatch.Text = "Stop watch";
            _lblMode.Text = "mode: …"; _lblMode.ForeColor = Color.DimGray;
            lock (_watchBuf) _watchBuf.Clear();
            await Task.Run(() => RunOnce($"connect {_ip}:5555"));
            try
            {
                var psi = new ProcessStartInfo(_adb, $"-s {_ip}:5555 shell \"stty -F /dev/ttyS1 raw -echo 115200; cat /dev/ttyS1 | od -An -tx1\"")
                {
                    RedirectStandardOutput = true, RedirectStandardError = true,
                    UseShellExecute = false, CreateNoWindow = true, StandardOutputEncoding = Encoding.UTF8
                };
                _watchProc = Process.Start(psi);
                if (_watchProc != null)
                {
                    _watchProc.OutputDataReceived += (_, e) => { if (e.Data != null) OnWatchData(e.Data); };
                    _watchProc.BeginOutputReadLine();
                    Banner("mode watch ON — inject a command and watch the readout. (Turn the UART(FC) source OFF to avoid fighting it for bytes.)");
                }
            }
            catch (Exception ex) { Banner("mode watch failed: " + ex.Message); StopWatch(); }
        }

        void StopWatch()
        {
            _btnWatch.Text = "Watch mode";
            var p = _watchProc; _watchProc = null;
            if (p != null) Task.Run(() => KillProc(p));
            _lblMode.Text = "mode: —"; _lblMode.ForeColor = Color.DimGray;
        }

        void OnWatchData(string chunk)
        {
            string snap;
            lock (_watchBuf)
            {
                _watchBuf.Append(chunk).Append(' ');
                if (_watchBuf.Length > 8192) _watchBuf.Remove(0, _watchBuf.Length - 4096);
                snap = _watchBuf.ToString();
            }
            var frames = FcFrames.Parse(snap);
            for (int i = frames.Count - 1; i >= 0; i--)
                if (frames[i].Type == 0x8b)
                {
                    byte b = frames[i].Payload[13];
                    if (!IsDisposed) try { BeginInvoke(() => ShowMode(b)); } catch { }
                    break;
                }
        }

        void ShowMode(byte b)
        {
            (string t, Color c) = b switch
            {
                0x0e => ("GPS",     Color.FromArgb(120, 200, 255)),
                0x02 => ("OPTICAL", Color.FromArgb(80, 230, 120)),
                _    => ($"0x{b:x2}", Color.Gold)
            };
            _lblMode.Text = "mode: " + t;
            _lblMode.ForeColor = c;
        }

        void Status(string s) { _status.ForeColor = Color.DimGray; _status.Text = s; }

        protected override void OnShown(EventArgs e) { base.OnShown(e); _ = CheckToolsAsync(); }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _flush.Stop();
            var procs = new List<Process>();
            foreach (var s in _sources) { if (s.Proc != null) procs.Add(s.Proc); s.Proc = null; }
            if (_watchProc != null) { procs.Add(_watchProc); _watchProc = null; }
            if (procs.Count > 0) Task.Run(() => { foreach (var p in procs) KillProc(p); });   // fire-and-forget so closing is instant
            base.OnFormClosing(e);
        }
    }
}

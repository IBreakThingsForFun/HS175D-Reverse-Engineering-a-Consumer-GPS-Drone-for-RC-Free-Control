using System.Diagnostics;
using System.Net;
using System.Text;

namespace HS175D
{
    /// <summary>
    /// Drone-side log/config viewer. The TCP-4646 JSON channel has no filesystem or log access
    /// (camera/system only — see docs/PROTOCOL.md §7), so this window uses the two documented
    /// back doors instead (docs/DEVICE_ACCESS.md §1):
    ///   - FTP  (HY819/1663819, port 21, whole-FS R/W) for reading/saving config &amp; log FILES.
    ///   - adb  (port 5555, root shell) for live commands: logcat, dmesg, /proc, the loglevel-5 dumps.
    ///
    /// Self-contained on purpose: opening it from Form1 keeps the main control loop / Designer layout
    /// untouched. The main form's 23333 keepalive (while connected) also staves off the drone's
    /// ~1–2 min idle power-off that would otherwise drop FTP/adb mid-browse (DEVICE_ACCESS.md §0).
    /// </summary>
    public sealed class DroneConsole : Form
    {
        const string FtpUser = "HY819";
        const string FtpPass = "1663819";

        readonly string _ip;
        readonly string _adb;   // resolved adb.exe path (PATH or the documented platform-tools dir)

        readonly ComboBox _cboPath = new() { DropDownStyle = ComboBoxStyle.DropDown };
        readonly Button _btnRead = new() { Text = "Read (FTP)" };
        readonly Button _btnSave = new() { Text = "Save (FTP)" };
        readonly TextBox _content = new()
        {
            Multiline = true, ScrollBars = ScrollBars.Both, WordWrap = false,
            Font = new Font("Consolas", 9F), AcceptsTab = true, AcceptsReturn = true
        };

        readonly TextBox _txtCmd = new() { Text = "logcat -d -t 200" };
        readonly Button _btnRun = new() { Text = "Run (adb)" };
        readonly TextBox _out = new()
        {
            Multiline = true, ScrollBars = ScrollBars.Both, WordWrap = false, ReadOnly = true,
            Font = new Font("Consolas", 9F), BackColor = Color.FromArgb(24, 24, 24), ForeColor = Color.Gainsboro
        };
        readonly Label _status = new() { AutoSize = true, ForeColor = Color.DimGray, Text = "" };
        readonly ToolTip _tip = new() { AutoPopDelay = 30000, InitialDelay = 350, ReshowDelay = 80, ShowAlways = true };

        public DroneConsole(string droneIp)
        {
            _ip = string.IsNullOrWhiteSpace(droneIp) ? "192.168.100.1" : droneIp.Trim();
            _adb = ResolveAdb();

            Text = $"Drone Console — {_ip}  (FTP {FtpUser}/****  ·  adb :5555)  ·  Drone RE Studio";
            ClientSize = new Size(980, 660);
            MinimumSize = new Size(560, 420);
            Font = new Font("Segoe UI", 9F);

            // ---- FTP file row ------------------------------------------------
            var lblFile = new Label { Text = "File:", AutoSize = true, Location = new Point(8, 13) };
            _cboPath.Location = new Point(48, 9);
            _cboPath.Size = new Size(560, 23);
            _cboPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _cboPath.Items.AddRange(new object[]
            {
                "/tmp/messages",                // loglevel-5 MCU frame dump (app ot yt) — for the mode-flip capture
                "/tmp/loglevel",                // current verbose-logging level
                "/tmp/hostapd.conf",            // live AP config (RAM)
                "/etc/hostapd.conf",            // stock 5 GHz fallback
                "/netPrivate/hostapd.conf",     // persistent override
                "/netPrivate/DroneID.json",
                "/netPrivate/A17Startup.sh",
                "/etc/wifi_switch.sh",
            });
            _cboPath.SelectedIndex = 0;
            foreach (var e in CommandLibrary.Items("FTP"))   // pull in any user-saved FTP paths
                if (!_cboPath.Items.Contains(e.Command)) _cboPath.Items.Add(e.Command);
            _btnRead.Location = new Point(616, 8); _btnRead.Size = new Size(86, 24);
            _btnRead.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _btnSave.Location = new Point(706, 8); _btnSave.Size = new Size(86, 24);
            _btnSave.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            _content.Location = new Point(8, 40);
            _content.Size = new Size(784, 260);
            _content.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            // ---- adb command row --------------------------------------------
            var lblCmd = new Label { Text = "adb shell:", AutoSize = true, Location = new Point(8, 312) };
            _txtCmd.Location = new Point(80, 309);
            _txtCmd.Size = new Size(622, 23);
            _txtCmd.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _btnRun.Location = new Point(706, 308); _btnRun.Size = new Size(86, 24);
            _btnRun.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            // quick-command buttons
            int qx = 8;
            foreach (var (label, cmd) in new[]
            {
                ("logcat",   "logcat -d -t 200"),
                ("MCU dumps","logcat -d -t 400 | grep -i \"app ot yt\""),  // loglevel-5 frame dumps
                ("dmesg",    "dmesg | tail -n 200"),
                ("/proc/mtd","cat /proc/mtd"),
                ("hostapd",  "cat /tmp/hostapd.conf"),
                ("pids",     "ps | grep -E \"hostapd|img_trans|app\""),
            })
            {
                var b = new Button { Text = label, Location = new Point(qx, 338), Size = new Size(108, 24), Tag = cmd };
                b.Click += (s, e) => { _txtCmd.Text = (string)((Button)s!).Tag!; _ = RunAdbAsync(); };
                _tip.SetToolTip(b, $"Run: {cmd}");
                Controls.Add(b);
                qx += 114;
            }

            // Live multi-source debug console (streams logcat/MCU/kernel/UART/tcpdump at once).
            var btnLive = new Button { Text = "Live Debug ▶", Location = new Point(qx, 338), Size = new Size(120, 24) };
            btnLive.Click += (s, e) => new LiveDebug(_adb, _ip).Show();
            _tip.SetToolTip(btnLive, "Open the live multi-source debug console (logcat/MCU/kernel/UART/tcpdump tailed at once).");
            Controls.Add(btnLive);

            // Command Library — click-to-load saved adb commands / FTP paths (self-populating).
            var btnLib = new Button { Text = "📚 Library", Location = new Point(qx + 126, 338), Size = new Size(120, 24) };
            btnLib.Click += (s, e) => new CommandLibraryForm(
                adb => _txtCmd.Text = adb,
                ftp => _cboPath.Text = ftp).Show(this);
            _tip.SetToolTip(btnLib, "Open the saved-command library — click an entry to load it into adb shell or FTP path.");
            Controls.Add(btnLib);

            _out.Location = new Point(8, 370);
            _out.Size = new Size(784, 252);
            _out.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            _status.Location = new Point(8, 630);
            _status.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

            Controls.AddRange(new Control[]
            {
                lblFile, _cboPath, _btnRead, _btnSave, _content,
                lblCmd, _txtCmd, _btnRun, _out, _status
            });

            _btnRead.Click += async (s, e) => await ReadFileAsync();
            _btnSave.Click += async (s, e) => await SaveFileAsync();
            _btnRun.Click  += async (s, e) => await RunAdbAsync();

            _tip.SetToolTip(_btnRead, "Read the file at this path over FTP into the editor below.");
            _tip.SetToolTip(_btnSave, "Write the editor's contents back to this path over FTP.");
            _tip.SetToolTip(_btnRun, "Run this command over adb shell and show the output below.");
            _tip.SetToolTip(_cboPath, "FTP file path on the drone (root FTP creds: HY819/1663819).");
            _tip.SetToolTip(_txtCmd, "adb shell command to run.");

            Theme.Apply(this);
            _content.BackColor = Theme.Bg2; _content.ForeColor = Theme.Text;
            _out.BackColor = Theme.Bg0; _out.ForeColor = Theme.Good;

            if (_adb == "adb")
                SetStatus("adb.exe not found in platform-tools — relying on PATH. FTP works regardless.", Theme.Warn);
        }

        // ================= FTP (config / log files) =========================
#pragma warning disable SYSLIB0014   // FtpWebRequest is obsolete but the only built-in FTP client; no external dep wanted
        async Task ReadFileAsync()
        {
            string path = _cboPath.Text.Trim();
            if (path.Length == 0) return;
            Busy(true); SetStatus($"FTP GET {path} …", Color.DimGray);
            string corr = CommLog.Tx("FTP", "GET " + path, _ip);
            try
            {
                string text = await Task.Run(() => FtpGet(path));
                _content.Text = text.Replace("\n", "\r\n").Replace("\r\r\n", "\r\n");
                SetStatus($"Read {path}  ({text.Length} bytes)", Color.Green);
                CommLog.Rx("FTP", "GET " + path, _ip, text: text, corr: corr);
                CommandLibrary.RecordUsed("FTP", path);
            }
            catch (Exception ex)
            {
                SetStatus("FTP read failed: " + ex.Message, Color.Firebrick);
                CommLog.Rx("FTP", "GET " + path, _ip, ok: false, error: ex.Message, corr: corr);
            }
            finally { Busy(false); }
        }

        async Task SaveFileAsync()
        {
            string path = _cboPath.Text.Trim();
            if (path.Length == 0) return;
            if (MessageBox.Show($"Overwrite {path} on the drone via FTP?\n\n" +
                "Note: /netPrivate is NOT power-safe and self-reverts on an unclean battery pull.",
                "Confirm write", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK) return;
            Busy(true); SetStatus($"FTP PUT {path} …", Color.DimGray);
            try
            {
                string body = _content.Text.Replace("\r\n", "\n");   // store unix newlines
                string corr = CommLog.Tx("FTP", "PUT " + path, _ip, text: body);
                try
                {
                    await Task.Run(() => FtpPut(path, body));
                    SetStatus($"Wrote {path}  ({body.Length} bytes)", Color.Green);
                    CommLog.Rx("FTP", "PUT " + path, _ip, text: $"OK {body.Length} bytes", corr: corr);
                    CommandLibrary.RecordUsed("FTP", path);
                }
                catch (Exception ex)
                {
                    SetStatus("FTP write failed: " + ex.Message, Color.Firebrick);
                    CommLog.Rx("FTP", "PUT " + path, _ip, ok: false, error: ex.Message, corr: corr);
                }
            }
            finally { Busy(false); }
        }

        string FtpGet(string absPath)
        {
            var req = (FtpWebRequest)WebRequest.Create($"ftp://{_ip}{absPath}");
            req.Method = WebRequestMethods.Ftp.DownloadFile;
            req.Credentials = new NetworkCredential(FtpUser, FtpPass);
            req.UseBinary = true; req.UsePassive = true; req.KeepAlive = false;
            using var resp = (FtpWebResponse)req.GetResponse();
            using var s = resp.GetResponseStream();
            using var ms = new MemoryStream();
            s!.CopyTo(ms);
            return Encoding.UTF8.GetString(ms.ToArray());
        }

        void FtpPut(string absPath, string body)
        {
            var req = (FtpWebRequest)WebRequest.Create($"ftp://{_ip}{absPath}");
            req.Method = WebRequestMethods.Ftp.UploadFile;
            req.Credentials = new NetworkCredential(FtpUser, FtpPass);
            req.UseBinary = true; req.UsePassive = true; req.KeepAlive = false;
            var data = Encoding.UTF8.GetBytes(body);
            req.ContentLength = data.Length;
            using (var rs = req.GetRequestStream()) rs.Write(data, 0, data.Length);
            using var resp = (FtpWebResponse)req.GetResponse();   // forces completion / throws on error
        }
#pragma warning restore SYSLIB0014

        // ================= adb (live shell) =================================
        async Task RunAdbAsync()
        {
            string cmd = _txtCmd.Text.Trim();
            if (cmd.Length == 0) return;
            Busy(true); SetStatus($"adb shell: {cmd}", Color.DimGray);
            try
            {
                await Task.Run(() => Adb($"connect {_ip}:5555"));   // idempotent
                string output = await Task.Run(() => Adb($"-s {_ip}:5555 shell \"{cmd}\""));
                CommLog.Exchange("ADB", cmd, _ip, requestText: cmd, responseText: output);
                CommandLibrary.RecordUsed("ADB", cmd);
                _out.Text = output.Replace("\n", "\r\n");
                _out.SelectionStart = _out.TextLength; _out.ScrollToCaret();
                SetStatus("adb done.", Color.Green);
            }
            catch (Exception ex) { SetStatus("adb failed: " + ex.Message, Color.Firebrick); }
            finally { Busy(false); }
        }

        string Adb(string args)
        {
            var psi = new ProcessStartInfo(_adb, args)
            {
                RedirectStandardOutput = true, RedirectStandardError = true,
                UseShellExecute = false, CreateNoWindow = true
            };
            using var p = Process.Start(psi) ?? throw new InvalidOperationException("could not start adb");
            string o = p.StandardOutput.ReadToEnd();
            string e = p.StandardError.ReadToEnd();
            p.WaitForExit(15000);
            return o + (e.Length > 0 ? "\n[stderr] " + e : "");
        }

        internal static string ResolveAdb()
        {
            // Prefer an in-repo copy (kept alongside the app so everything travels together),
            // then fall back to whatever `adb` resolves to on PATH.
            string inRepo = Path.Combine(AppContext.BaseDirectory, "adb-platform-tools", "adb.exe");
            return File.Exists(inRepo) ? inRepo : "adb";
        }

        /// <summary>Headless adb shell exec (connect + run), for the DebugBus `adb` command — no window needed.</summary>
        internal static string RunAdb(string ip, string shellCmd)
        {
            string adb = ResolveAdb();
            RunAdbRaw(adb, $"connect {ip}:5555");                       // idempotent
            string output = RunAdbRaw(adb, $"-s {ip}:5555 shell \"{shellCmd}\"");
            CommLog.Exchange("ADB", shellCmd, ip, requestText: shellCmd, responseText: output);
            return output;
        }

        static string RunAdbRaw(string adb, string args)
        {
            var psi = new ProcessStartInfo(adb, args)
            {
                RedirectStandardOutput = true, RedirectStandardError = true,
                UseShellExecute = false, CreateNoWindow = true
            };
            using var p = Process.Start(psi) ?? throw new InvalidOperationException("could not start adb");
            string o = p.StandardOutput.ReadToEnd();
            string e = p.StandardError.ReadToEnd();
            p.WaitForExit(20000);
            return o + (e.Length > 0 ? "\n[stderr] " + e : "");
        }

        // ================= helpers ==========================================
        void Busy(bool b)
        {
            _btnRead.Enabled = _btnSave.Enabled = _btnRun.Enabled = !b;
            Cursor = b ? Cursors.WaitCursor : Cursors.Default;
        }

        void SetStatus(string text, Color c) { _status.ForeColor = c; _status.Text = text; }
    }
}

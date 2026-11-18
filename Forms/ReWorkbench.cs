using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace HS175D
{
    /// <summary>
    /// Reverse-engineering workbench. A single resizable, dark "mission-control" surface with tabs:
    ///   Monitor   — live colour-coded hex of all app↔drone traffic (taps Form1, read-only).
    ///   Inject    — craft arbitrary frames with auto checksum/wrapper and fire them on any port.
    ///   Replay    — capture the live stream to a file and replay it back to the drone.
    ///   Telemetry — decoded 58xx readout (model / attitude / sats / GPS), flight-console style.
    ///   ESP32     — serial bridge to the ground-station firmware.
    /// Observation is non-invasive: Form1 raises a static PacketTap on its existing send/recv paths;
    /// nothing here changes the flight or telemetry logic.
    /// </summary>
    public sealed partial class ReWorkbench : Form
    {
        readonly string _ip;
        readonly ConcurrentQueue<(Color c, string text)> _monQ = new();
        readonly System.Windows.Forms.Timer _flush = new() { Interval = 120 };

        // capture / replay
        readonly List<PacketRecord> _store = new();
        volatile bool _capturing;
        List<PacketRecord> _replay = new();
        CancellationTokenSource? _replayCts;

        // telemetry
        readonly Dictionary<byte, (int count, int len, string hex)> _subSeen = new();
        long _lastSubRefresh;

        // injector socket (independent of Form1's bound :9961)
        UdpClient? _inj;
        System.Threading.CancellationTokenSource? _rampCts;

        readonly ToolTip _tip = new() { AutoPopDelay = 30000, InitialDelay = 350, ReshowDelay = 80, ShowAlways = true };

        // Parameterless constructor exists ONLY so the WinForms Designer can instantiate and render
        // the form at design time. The application always uses ReWorkbench(string). It must do
        // nothing but InitializeComponent — no Theme/socket/DebugBus work that would fail at design time.
        public ReWorkbench() { _ip = "192.168.100.1"; InitializeComponent(); }

        public ReWorkbench(string droneIp)
        {
            _ip = string.IsNullOrWhiteSpace(droneIp) ? "192.168.100.1" : droneIp.Trim();

            InitializeComponent();

            // Custom panels live OUTSIDE the Designer file (VideoPanel needs the mpv engine + the drone
            // IP, which don't exist at design time) — added here at runtime, the same way Form1 fills its
            // video panel in OnLoad. This is what lets the Designer open the form.
            var videoPanel = new VideoPanel();
            videoPanel.SetIpProvider(() => _ip);
            _tabVideo.Controls.Add(videoPanel);
            _tabEsp32.Controls.Add(new Esp32Panel());
            _tabNrf24.Controls.Add(new Nrf24Panel());

            Theme.Apply(this);
            Theme.StyleTabs(_tabs);

            // runtime values the Designer can't know statically
            _lblSub.Text = $"drone {_ip}";
            _injIp.Text = _ip;

            AddTooltips();
            _capRefreshTimer.Start();

            _flush.Tick += (s, e) => FlushMonitor();
            _flush.Start();

            Form1.PacketTap += OnTap;

            // headless throttle ramp — driveable from curl during bench tests (see DebugBus :8731)
            DebugBus.Register("inject.ramp",
                "throttle ramp over UDP19798 — args {start?=128,target?=200,step?=5,delayMs?=50,framesPerStep?=1,holdSec?=0}  (128=idle/center)",
                this, a =>
                {
                    if (_rampCts != null) return new { error = "ramp already running" };
                    int start = DebugBus.Int(a, "start") ?? 128, target = DebugBus.Int(a, "target") ?? 200;
                    int step = DebugBus.Int(a, "step") ?? 5, delay = DebugBus.Int(a, "delayMs") ?? 50;
                    int perStep = DebugBus.Int(a, "framesPerStep") ?? 1, holdMs = (DebugBus.Int(a, "holdSec") ?? 0) * 1000;
                    RunRamp(start, target, step, delay, perStep, holdMs);
                    return new { started = true, start, target, step, delayMs = delay, framesPerStep = perStep, holdMs };
                });
            DebugBus.Register("inject.ramp.stop", "cancel a running throttle ramp", this,
                _ => { _rampCts?.Cancel(); return new { stopped = true }; });

            FormClosed += (s, e) =>
            {
                Form1.PacketTap -= OnTap;
                DebugBus.Unregister("inject.ramp");
                DebugBus.Unregister("inject.ramp.stop");
                _rampCts?.Cancel();
                _flush.Stop();
                _replayCts?.Cancel();
                SwdHold.Stop();   // release the probe if an optical hold is still running
                try { _inj?.Dispose(); } catch { }
            };
        }

        void AddTooltips()
        {
            _tip.SetToolTip(_btnConsole, "Open the Drone Console — FTP file browse + adb root shell.");
            _tip.SetToolTip(_btnSwd, "One-click OpenOCD session that continuously rewrites the optical-mode SRAM flags — holds RC-free/GPS-free control open. Click again to release the probe.");
            _tip.SetToolTip(_btnWifi, "Open the drone's SoftAP control — band switch, multi-station, factory restore.");

            _tip.SetToolTip(_btnMonClear, "Clear the monitor pane.");
            _tip.SetToolTip(_fTx, "Show outgoing (TX) frames.");
            _tip.SetToolTip(_fRx, "Show incoming (RX) frames.");
            _tip.SetToolTip(_fHideStream, "Hide the repetitive stick/ping frames to reduce noise.");
            _tip.SetToolTip(_fHideSession, "Hide the 23333 keepalive session frames.");
            _tip.SetToolTip(_fPause, "Pause the live scroll (frames keep queuing, just not drawn).");

            _tip.SetToolTip(_btnInjXor, "Recompute and append the checksum byte (per the checksum dropdown) to the current hex.");
            _tip.SetToolTip(_btnInjWrap, "Wrap the current inner bytes in the 48 00 04 frame header.");
            _tip.SetToolTip(_btnInjStick, "Pack the four stick values above into a wrapped, checksummed stick frame.");
            _tip.SetToolTip(_btnInjSend, "Send the hex bytes above to Target:port, 'count' times, and show any reply.");

            _tip.SetToolTip(_btnRamp, "Ramp throttle start→target→back to 128 over the main window's live control loop. Requires Connect + arm + 23333 ON.");
            _tip.SetToolTip(_btnRampStop, "Cancel a running ramp and drop the control loop back to floor/neutral.");

            _tip.SetToolTip(_btnGpsBuild, "Pack the mode/coords/alt/speed/radius fields above into a wire-format hex frame.");
            _tip.SetToolTip(_btnGpsSend, "Send the hex frame (building it first if the box is empty) as a waypoint/orbit command — UNVERIFIED.");

            _tip.SetToolTip(_btnCapStart, "Clear the buffer and start capturing live TX/RX frames.");
            _tip.SetToolTip(_btnCapStop, "Stop capturing.");
            _tip.SetToolTip(_btnCapSave, "Save the captured buffer to a file.");
            _tip.SetToolTip(_btnCapLoad, "Load a previously-saved capture file for replay.");
            _tip.SetToolTip(_btnReplayGo, "Replay the loaded capture back to the drone at the chosen speed multiplier.");
            _tip.SetToolTip(_btnReplayStop, "Cancel a running replay.");
            _tip.SetToolTip(_numReplayRate, "Replay speed multiplier.");
            _tip.SetToolTip(_chkTxOnly, "Only replay TX (outgoing) frames, skipping recorded RX.");
        }

        // ============================================================ event handlers
        void BarToolbar_Resize(object? sender, EventArgs e)
        {
            _btnConsole.Left = _barToolbar.Width - 188;
            _btnSwd.Left = _barToolbar.Width - 380;
            _btnWifi.Left = _barToolbar.Width - 458;
        }

        void BtnConsole_Click(object? sender, EventArgs e) => new DroneConsole(_ip).Show(this);

        void BtnSwd_Click(object? sender, EventArgs e)
        {
            Action<string> log = m => _monQ.Enqueue((Theme.Accent, $"{DateTime.Now:HH:mm:ss.fff}  SWD  {m}\n"));
            if (SwdHold.Running) { SwdHold.Stop(log); _btnSwd.Text = "SWD: Hold Optical ▷"; _btnSwd.BackColor = Theme.Bg2; }
            else if (SwdHold.Start(log)) { _btnSwd.Text = "SWD: HOLDING ■ (release)"; _btnSwd.BackColor = Theme.Warn; }
        }

        void BtnWifi_Click(object? sender, EventArgs e) => new WifiSettingsForm(_ip).Show(this);

        void BtnMonClear_Click(object? sender, EventArgs e) { _mon.Clear(); while (_monQ.TryDequeue(out _)) { } }

        void BtnInjXor_Click(object? sender, EventArgs e)
        {
            var b = ParseHex(_injHex.Text);
            _injHex.Text = _injCksum.SelectedIndex switch
            {
                0 => Hex(WithXor(b)),       // XOR inner[1..n-1)
                1 => Hex(WithAdditive(b)),  // additive sum (MCU/optical-flow family)
                _ => Hex(b),                // None
            };
        }

        void BtnInjWrap_Click(object? sender, EventArgs e) { var b = ParseHex(_injHex.Text); _injHex.Text = Hex(Wrap48(b)); }

        void BtnInjStick_Click(object? sender, EventArgs e)
        {
            byte[] inner = { 0x68, 0x01, 0x0d, (byte)_injR.Value, (byte)_injP.Value, (byte)_injT.Value, (byte)_injY.Value,
                             0x20, 0x18, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            _injHex.Text = Hex(Wrap48(WithXor(inner)));
            _injCksum.SelectedIndex = 0;
        }

        void BtnInjSend_Click(object? sender, EventArgs e) => SendInject();

        void BtnRamp_Click(object? sender, EventArgs e) => StartRamp();

        void BtnRampStop_Click(object? sender, EventArgs e)
        {
            try { DebugBus.Dispatch("throttle.ramp.stop"); _rStatus.AppendText("  STOP sent (control loop → floor)\r\n"); }
            catch (Exception ex) { _rStatus.AppendText("  error: " + ex.Message + "\r\n"); }
        }

        void BtnGpsBuild_Click(object? sender, EventArgs e) => _gHex.Text = Hex(BuildGpsFrame());
        void BtnGpsSend_Click(object? sender, EventArgs e) => SendGps();

        void BtnCapStart_Click(object? sender, EventArgs e) { lock (_store) _store.Clear(); _capturing = true; UpdateCapLbl(); }
        void BtnCapStop_Click(object? sender, EventArgs e) { _capturing = false; UpdateCapLbl(); }
        void BtnCapSave_Click(object? sender, EventArgs e) => SaveBuffer();
        void BtnCapLoad_Click(object? sender, EventArgs e) => LoadReplay();
        void BtnReplayGo_Click(object? sender, EventArgs e) => StartReplay((int)_numReplayRate.Value, _chkTxOnly.Checked);
        void BtnReplayStop_Click(object? sender, EventArgs e) => _replayCts?.Cancel();
        void CapRefreshTimer_Tick(object? sender, EventArgs e) { if (_capturing) UpdateCapLbl(); }

        void OnTap(PacketDir dir, string proto, byte[] data)
        {
            var rec = new PacketRecord { Ms = Environment.TickCount64, Dir = dir, Proto = proto, Data = data };
            if (_capturing) lock (_store) _store.Add(rec);

            if (dir == PacketDir.Rx && proto == "UDP19798" && data.Length > 1 && data[0] == 0x58)
                DecodeTelemetry(data);

            if (_fPause is { Checked: true }) return;
            if (!PassesFilter(dir, proto, data)) return;

            string line = $"{DateTime.Now:HH:mm:ss.fff}  {(dir == PacketDir.Tx ? "TX" : "RX")} {proto,-9} [{data.Length,3}]  {Hex(data, 48)}\n";
            _monQ.Enqueue((FamColor(proto, data), line));
        }

        bool PassesFilter(PacketDir dir, string proto, byte[] d)
        {
            if (dir == PacketDir.Tx && !_fTx.Checked) return false;
            if (dir == PacketDir.Rx && !_fRx.Checked) return false;
            if (_fHideSession.Checked && proto == "UDP23333") return false;
            if (_fHideStream.Checked && d.Length > 0 && (d[0] == 0x48 || d[0] == 0x68)) return false;
            return true;
        }

        static Color FamColor(string proto, byte[] d)
        {
            if (proto.StartsWith("TCP")) return Color.FromArgb(180, 190, 200);
            byte h = d.Length > 0 ? d[0] : (byte)0;
            return h switch
            {
                0x48 or 0x68 => Theme.Accent,
                0x58 => Theme.Good,
                0x52 => Theme.Muted,
                0x88 => Theme.Warn,
                _ => Theme.Text,
            };
        }

        void FlushMonitor()
        {
            if (_monQ.IsEmpty) return;
            if (_mon.TextLength > 200_000) _mon.Clear();
            int budget = 250;   // cap per tick so a flood can't freeze the UI
            var sb = new StringBuilder();
            Color cur = Color.Empty;
            while (budget-- > 0 && _monQ.TryDequeue(out var item))
            {
                if (item.c != cur && sb.Length > 0) { AppendColored(cur, sb.ToString()); sb.Clear(); }
                cur = item.c; sb.Append(item.text);
            }
            if (sb.Length > 0) AppendColored(cur, sb.ToString());
            _mon.SelectionStart = _mon.TextLength;
            _mon.ScrollToCaret();
            if (_monQ.Count > 2000) while (_monQ.TryDequeue(out _)) { }   // drop backlog if we fell behind
        }

        void AppendColored(Color c, string text)
        {
            _mon.SelectionStart = _mon.TextLength;
            _mon.SelectionLength = 0;
            _mon.SelectionColor = c == Color.Empty ? Theme.Text : c;
            _mon.AppendText(text);
        }

        async void SendInject()
        {
            byte[] bytes;
            try { bytes = ParseHex(_injHex.Text); }
            catch (Exception ex) { _injReply.Text = "bad hex: " + ex.Message; return; }
            if (bytes.Length == 0) return;
            if (!int.TryParse(_injPort.Text.Trim(), out int port)) { _injReply.Text = "bad port"; return; }
            string ip = _injIp.Text.Trim();
            int count = (int)_injCount.Value;
            try
            {
                _inj ??= new UdpClient(0);   // ephemeral local port
                for (int i = 0; i < count; i++) await _inj.SendAsync(bytes, bytes.Length, ip, port);
                _monQ.Enqueue((Theme.Warn, $"{DateTime.Now:HH:mm:ss.fff}  TX INJECT    [{bytes.Length,3}]  {Hex(bytes, 48)}  x{count}\n"));

                _inj.Client.ReceiveTimeout = 500;
                try
                {
                    var from = new IPEndPoint(IPAddress.Any, 0);
                    byte[] r = _inj.Receive(ref from);
                    _injReply.Text = $"{from}: {Hex(r)}";
                }
                catch (SocketException) { _injReply.Text = "(no reply)"; }
            }
            catch (Exception ex) { _injReply.Text = "send failed: " + ex.Message; }
        }

        // throttle-only stick frame (roll/pitch/yaw centred at 128), reusing the 68-family XOR + 48-wrapper
        byte[] StickFrame(int thr) =>
            Wrap48(WithXor(new byte[] { 0x68, 0x01, 0x0d, 128, 128, (byte)thr, 128,
                                        0x20, 0x18, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }));

        // ▶ RAMP rides Form1's single 50 Hz control loop — one coherent stream (23333 keepalive + :9961
        // source port) — via the throttle.ramp command, instead of firing a parallel inject from an
        // ephemeral socket that fights the main stream. Requires Connect + "Enable flight commands" (arm)
        // + 23333 session ON in the main window. (frames/step has no effect here — the loop is fixed-rate.)
        void StartRamp()
        {
            var args = new Dictionary<string, string>
            {
                ["start"]   = ((int)_rStart.Value).ToString(),
                ["target"]  = ((int)_rTarget.Value).ToString(),
                ["step"]    = ((int)_rStep.Value).ToString(),
                ["delayMs"] = ((int)_rDelay.Value).ToString(),
                ["holdSec"] = ((int)_rHold.Value).ToString(),
                ["settle"]  = "128",   // sweep up→target→back down to 128 (stick center), then broadcast 128
            };
            try
            {
                var r = DebugBus.Dispatch("throttle.ramp", args);
                _rStatus.AppendText($"{DateTime.Now:HH:mm:ss.fff}  RAMP via control loop → {System.Text.Json.JsonSerializer.Serialize(r)}\r\n");
            }
            catch (Exception ex) { _rStatus.AppendText("  error: " + ex.Message + "\r\n"); }
        }

        // shared by the ▶ RAMP button and the DebugBus "inject.ramp" command
        async void RunRamp(int start, int target, int step, int delay, int perStep, int holdMs)
        {
            if (_rampCts != null) { _rStatus.AppendText("already running — STOP first\r\n"); return; }
            start = Math.Clamp(start, 0, 255); target = Math.Clamp(target, 0, 255);
            step = Math.Max(1, step); perStep = Math.Max(1, perStep);
            int dir = target >= start ? 1 : -1;
            var cts = _rampCts = new System.Threading.CancellationTokenSource();
            var ct = cts.Token;
            _inj ??= new UdpClient(0);
            _rStatus.AppendText($"{DateTime.Now:HH:mm:ss.fff}  RAMP {start}->{target} step {step} every {delay}ms x{perStep}, hold {holdMs}ms\r\n");
            _monQ.Enqueue((Theme.Warn, $"{DateTime.Now:HH:mm:ss.fff}  TX RAMP      {start}->{target} step {step} every {delay}ms x{perStep}\n"));
            try
            {
                for (int thr = start; ; thr += dir * step)
                {
                    if (dir > 0 && thr > target) thr = target;
                    if (dir < 0 && thr < target) thr = target;
                    byte[] f = StickFrame(thr);
                    for (int k = 0; k < perStep; k++)
                    {
                        await _inj.SendAsync(f, f.Length, _ip, 19798);
                        if (delay > 0) await Task.Delay(delay, ct);
                    }
                    _rStatus.AppendText($"  thr={thr}\r\n");
                    if (thr == target) break;
                }
                if (holdMs > 0)
                {
                    _rStatus.AppendText($"  holding {target} for {holdMs}ms\r\n");
                    byte[] f = StickFrame(target);
                    long endTick = Environment.TickCount64 + holdMs;
                    while (Environment.TickCount64 < endTick)
                    {
                        await _inj.SendAsync(f, f.Length, _ip, 19798);
                        await Task.Delay(delay > 0 ? delay : 50, ct);
                    }
                }
                _rStatus.AppendText("  DONE\r\n");
            }
            catch (OperationCanceledException) { _rStatus.AppendText("  STOPPED\r\n"); }
            catch (Exception ex) { _rStatus.AppendText("  error: " + ex.Message + "\r\n"); }
            finally { cts.Dispose(); _rampCts = null; }
        }

        byte[] BuildGpsFrame()
        {
            bool orbit = _gMode.SelectedIndex == 1;
            double lat = ParseD(_gLat.Text), lon = ParseD(_gLon.Text);
            int alt = (int)ParseD(_gAlt.Text), speed = (int)ParseD(_gSpeed.Text);
            int radius = (int)(ParseD(_gRadius.Text) * 10);   // m → decimetres
            byte cmdId = (byte)Convert.ToInt32(_gCmdId.Text.Trim().Replace("0x", ""), 16);

            var pl = new List<byte>();
            pl.AddRange(BitConverter.GetBytes((int)(lon * 1e7)));   // int32 LE, deg×1e7 (matches 58 8c)
            pl.AddRange(BitConverter.GetBytes((int)(lat * 1e7)));
            pl.AddRange(BitConverter.GetBytes((short)alt));
            pl.Add((byte)speed);
            if (orbit) pl.AddRange(BitConverter.GetBytes((short)radius));
            pl.Add(0x01);                                            // point/circle number (guess)

            var body = new byte[3 + pl.Count];
            body[0] = 0x68; body[1] = cmdId; body[2] = (byte)pl.Count;
            pl.CopyTo(body, 3);
            return Wrap48(WithXor(body));   // reuse the 68-family XOR + 48-wrapper helpers
        }

        async void SendGps()
        {
            byte[] bytes;
            try { bytes = _gHex.Text.Trim().Length > 0 ? ParseHex(_gHex.Text) : BuildGpsFrame(); }
            catch (Exception ex) { _gReply.Text = "build/parse error: " + ex.Message; return; }
            try
            {
                _inj ??= new UdpClient(0);
                await _inj.SendAsync(bytes, bytes.Length, _ip, 19798);
                _monQ.Enqueue((Theme.Warn, $"{DateTime.Now:HH:mm:ss.fff}  TX GPS       [{bytes.Length,3}]  {Hex(bytes, 48)}\n"));
                _gReply.Text = "sent (UNVERIFIED) — capture in Monitor and compare to a real phone session.";
            }
            catch (Exception ex) { _gReply.Text = "send failed: " + ex.Message; }
        }

        static double ParseD(string s) => double.TryParse(s.Trim(), System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0;

        void UpdateCapLbl()
        {
            int n; lock (_store) n = _store.Count;
            _capLbl.Text = $"{n} frames buffered" + (_capturing ? "  ● REC" : "");
            _capLbl.ForeColor = _capturing ? Theme.Danger : Theme.Accent;
        }

        void SaveBuffer()
        {
            List<PacketRecord> snap; lock (_store) snap = new List<PacketRecord>(_store);
            if (snap.Count == 0) { _replayLbl.Text = "buffer empty — start a capture first"; return; }
            using var sfd = new SaveFileDialog { Filter = "hex log (*.hexlog)|*.hexlog|all|*.*", FileName = $"capture_{DateTime.Now:yyyyMMdd_HHmmss}.hexlog" };
            if (sfd.ShowDialog(this) != DialogResult.OK) return;
            long t0 = snap[0].Ms;
            var sb = new StringBuilder();
            sb.AppendLine("# HS175D capture  ms dir proto hex");
            foreach (var r in snap)
                sb.AppendLine($"{r.Ms - t0} {(r.Dir == PacketDir.Tx ? "TX" : "RX")} {r.Proto} {Hex(r.Data)}");
            File.WriteAllText(sfd.FileName, sb.ToString());
            _replayLbl.Text = $"saved {snap.Count} frames";
        }

        void LoadReplay()
        {
            using var ofd = new OpenFileDialog { Filter = "hex log (*.hexlog)|*.hexlog|all|*.*" };
            if (ofd.ShowDialog(this) != DialogResult.OK) return;
            var list = new List<PacketRecord>();
            foreach (var raw in File.ReadAllLines(ofd.FileName))
            {
                var ln = raw.Trim();
                if (ln.Length == 0 || ln.StartsWith('#')) continue;
                int s1 = ln.IndexOf(' '); int s2 = ln.IndexOf(' ', s1 + 1); int s3 = ln.IndexOf(' ', s2 + 1);
                if (s1 < 0 || s2 < 0 || s3 < 0) continue;
                try
                {
                    list.Add(new PacketRecord
                    {
                        Ms = long.Parse(ln[..s1]),
                        Dir = ln[(s1 + 1)..s2] == "TX" ? PacketDir.Tx : PacketDir.Rx,
                        Proto = ln[(s2 + 1)..s3],
                        Data = ParseHex(ln[(s3 + 1)..]),
                    });
                }
                catch { }
            }
            _replay = list;
            _replayLbl.Text = $"loaded {list.Count} frames from {Path.GetFileName(ofd.FileName)}";
        }

        void StartReplay(int speed, bool txOnly)
        {
            if (_replay.Count == 0) { _replayLbl.Text = "nothing loaded"; return; }
            _replayCts?.Cancel();
            _replayCts = new CancellationTokenSource();
            var ct = _replayCts.Token;
            var frames = _replay.Where(r => (!txOnly || r.Dir == PacketDir.Tx) && r.Proto != "TCP4646").ToList();
            string ip = _ip;
            _ = Task.Run(async () =>
            {
                using var udp = new UdpClient(0);
                long t0 = frames.Count > 0 ? frames[0].Ms : 0;
                long start = Environment.TickCount64;
                int sent = 0;
                foreach (var r in frames)
                {
                    if (ct.IsCancellationRequested) break;
                    long due = start + (r.Ms - t0) / speed;
                    long wait = due - Environment.TickCount64;
                    if (wait > 0) { try { await Task.Delay((int)wait, ct); } catch { break; } }
                    int port = r.Proto == "UDP23333" ? 23333 : 19798;
                    try { await udp.SendAsync(r.Data, r.Data.Length, ip, port); sent++; } catch { }
                    _monQ.Enqueue((Theme.Warn, $"{DateTime.Now:HH:mm:ss.fff}  TX REPLAY    [{r.Data.Length,3}]  {Hex(r.Data, 48)}\n"));
                }
                SetReplayLbl($"replay done — {sent} frames sent");
            }, ct);
            _replayLbl.Text = $"replaying {frames.Count} frames ×{speed} …";
        }

        void SetReplayLbl(string t) { try { BeginInvoke(() => _replayLbl.Text = t); } catch { } }

        void DecodeTelemetry(byte[] f)
        {
            try
            {
                switch (f[1])
                {
                    case 0x8a when f.Length >= 11:
                        string model = Encoding.ASCII.GetString(f, 3, 8).TrimEnd('\0', ' ');
                        SetLbl(_tModel, model);
                        break;
                    case 0x8b when f.Length > 14:
                        byte q = f[14];
                        SetLbl(_tSats, ((q >> 2) & 0x1f).ToString());
                        SetLbl(_tFix, ((q >> 7) & 1) == 1 ? "LOCK" : "no fix");
                        break;
                    case 0x8c when f.Length >= 11:
                        double lon = BitConverter.ToInt32(f, 3) / 1e7;
                        double lat = BitConverter.ToInt32(f, 7) / 1e7;
                        SetLbl(_tGps, $"{lat.ToString("F6", CultureInfo.InvariantCulture)}, {lon.ToString("F6", CultureInfo.InvariantCulture)}");
                        break;
                }
                SetLbl(_tRaw, "58" + f[1].ToString("x2"));
                TrackSubtype(f);
            }
            catch { }
        }

        // Tally every distinct 58xx subtype (count / length / last bytes) so the battery/altitude/
        // motorRunning carriers (named in sibling FlyReceiveInfo) can be spotted and mapped later.
        void TrackSubtype(byte[] f)
        {
            lock (_subSeen)
            {
                int c = _subSeen.TryGetValue(f[1], out var e) ? e.count : 0;
                _subSeen[f[1]] = (c + 1, f.Length, Hex(f, 24));
            }
            long now = Environment.TickCount64;
            if (now - _lastSubRefresh < 500) return;   // throttle UI refresh
            _lastSubRefresh = now;
            List<string> rows;
            lock (_subSeen)
                rows = _subSeen.OrderBy(k => k.Key)
                    .Select(k => $"58{k.Key:x2}  n={k.Value.count,-5} len={k.Value.len,-3} {k.Value.hex}")
                    .ToList();
            try { BeginInvoke(() => { _subList.BeginUpdate(); _subList.Items.Clear(); _subList.Items.AddRange(rows.ToArray()); _subList.EndUpdate(); }); } catch { }
        }

        void SetLbl(Label l, string v) { try { BeginInvoke(() => l.Text = v); } catch { } }

        // ============================================================ helpers
        static string Hex(byte[] b) => Hex(b, int.MaxValue);
        static string Hex(byte[] b, int max)
        {
            int n = Math.Min(b.Length, max);
            var sb = new StringBuilder(n * 3);
            for (int i = 0; i < n; i++) { sb.Append(b[i].ToString("x2")); sb.Append(' '); }
            if (b.Length > max) sb.Append('…');
            return sb.ToString().TrimEnd();
        }

        static byte[] ParseHex(string s)
        {
            var clean = new StringBuilder();
            foreach (char c in s) if (Uri.IsHexDigit(c)) clean.Append(c);
            if (clean.Length % 2 != 0) throw new FormatException("odd number of hex digits");
            var b = new byte[clean.Length / 2];
            for (int i = 0; i < b.Length; i++) b[i] = Convert.ToByte(clean.ToString(i * 2, 2), 16);
            return b;
        }

        // checksum helpers mirror docs/PROTOCOL.md §2/§6
        static byte[] WithAdditive(byte[] inner)
        {
            byte sum = 0;
            for (int i = 1; i < inner.Length; i++) sum += inner[i];
            var outb = new byte[inner.Length + 1];
            Array.Copy(inner, outb, inner.Length);
            outb[^1] = sum;
            return outb;
        }

        static byte[] WithXor(byte[] inner)
        {
            // XS = XOR of inner[1 .. n-1); appended as the last byte (overwrites if already present? we append)
            byte x = 0;
            for (int i = 1; i < inner.Length; i++) x ^= inner[i];
            // The 68-family stores XS as the final byte AFTER the payload. If caller passed the 16-byte
            // body without XS, append it; if they passed a full frame, this still appends a fresh trailer.
            var outb = new byte[inner.Length + 1];
            Array.Copy(inner, outb, inner.Length);
            outb[^1] = x;
            return outb;
        }

        static byte[] Wrap48(byte[] inner)
        {
            var w = new byte[4 + inner.Length];
            w[0] = 0x48; w[1] = 0x00; w[2] = 0x04; w[3] = (byte)inner.Length;
            Array.Copy(inner, 0, w, 4, inner.Length);
            return w;
        }
    }
}

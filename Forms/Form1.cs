using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace HS175D
{
    /// <summary>
    /// HS175D WiFi controller. Single-form app. Protocol reverse-engineered from
    /// docs/PROTOCOL.md (PCAPdroid capture). Drone = 192.168.100.1.
    ///
    /// Channels:
    ///   UDP 19798 : control (stick 68 01 / ping 68 0a, wrapped in 48 00 04 LL),
    ///               gimbal (88 07 ...), telemetry back (58 xx)
    ///   TCP 4646  : JSON command channel  {"CMD":n,"PARAM":..}
    ///   TCP 19200 : RTSP H.264 video (launched in an external player)
    ///
    /// VERIFIED in capture: connect, telemetry, gimbal, neutral sticks, JSON cmds.
    /// UNVERIFIED (inferred from HS156): flight maneuver flags / non-neutral axes.
    ///
    /// Firmware-confirmed (static RE of img_trans_app, see drone_protocol/PROTOCOL.md):
    /// the TCP 4646 dispatcher parses {"CMD":n,"PARAM":...}, requires PARAM to be present,
    /// and matches n against a table whose ids EQUAL the APK enum ordinals (decimal) — so the
    /// command numbers used here are authoritative. Full list: drone_protocol/cmd_table.txt.
    /// CMD 137 = SET_HY_LOGLEVEL: {"loglevel":5} flips the drone to verbose logging (level &amp; 4)
    /// so it dumps the "app ot yt" MCU frames — the route to ground-truthing the stick axis order.
    ///
    /// All UI controls are defined in Form1.Designer.cs (not created at runtime).
    /// </summary>
    public partial class Form1 : Form
    {
        // ---- protocol constants ----
        const string DefaultDroneIp = "192.168.100.1";
        const int UdpPort = 19798;       // drone control + telemetry port
        const int LocalUdpPort = 9961;   // the app binds this exact local port; drone replies here
        const int JsonPort = 4646;       // TCP JSON command channel
        const int RtspPort = 19200;      // TCP RTSP video
        const int SessionPort = 23333;   // phone runs a sustained UDP session here (52 00 1d frames).
                                         //   CONFIRMED: this session GATES Wi-Fi control — without a ~10 Hz
                                         //   heartbeat present, the drone ignores our stick stream.

        // A 32-byte 23333 "session" token captured from the phone (docs/PCAPdroid_*.pcap). byte[23] is a
        // ~1/sec counter; the two 5-byte blocks re-key every ~2 s. CONFIRMED on the bench: the drone does
        // NOT hard-validate the token contents (despite the sw2001 chip) — replaying this STALE captured
        // token while advancing byte[23] is enough to pass the gate and gave post-RC-arm motor control.
        static readonly byte[] Session23333 = Convert.FromHexString(
            "52001dad2f12da724c454044074af7608455c00000504383fe3a6a0000000000");

        const byte AxisNeutral = 0x80;   // roll/pitch/yaw centre at 0x80 (no input)
        const byte ThrottleMin = 0x80;   // throttle (_a0) NEUTRAL / rest = 0x80 (128), same as the other axes.
                                         //   On this drone 0x80 = zero climb-rate = idle/hold (bench-confirmed),
                                         //   NOT takeoff; value 0 would be a motor-cut, the wrong rest state.

        // Control-stream rate. The flight MCU drains its (large) UDP RX buffer at ~66 Hz; the phone runs
        // ~45–63 Hz. We deliberately send UNDER the drain rate so a backlog can't accumulate in the drone's
        // receive buffer — that backlog is what makes throttle changes apply late and "all at once". If the
        // drone ever feels under-driven, raise ControlHz toward 60 (do NOT exceed ~63). Tune here.
        const int PingEveryNTicks = 10;  // ping ~5 Hz keepalive (68 0a), runs in both arm states

        // ---- send-stream tuning (live-adjustable from the "Send tuning" UI group) ----
        // _sendOnChange=true: send a stick frame on every CHANGE (instant while moving) + a slow heartbeat
        //   while held, so identical packets don't pile up in the drone's RX buffer. false: steady stream
        //   every loop tick at _controlHz (the old always-on behaviour, for A/B comparison on the bench).
        volatile bool _sendOnChange = true;
        volatile int _controlHz = 50;    // loop/steady-send rate, Hz
        volatile int _heartbeatHz = 12;  // hold-rate in change-detect mode, Hz

        // ---- networking ----
        Socket? _udp;
        Socket? _udp23333;               // sustained 23333 session (suspected control-auth)
        volatile bool _send23333 = true; // toggled by the "23333 session" checkbox
        IPEndPoint? _droneUdp;
        TcpClient? _json;
        NetworkStream? _jsonStream;
        CancellationTokenSource? _cts;
        volatile bool _connected;

        readonly StringBuilder _jsonBuf = new();   // reassembles TCP fragments into whole JSON objects
        volatile bool _sweeping;                    // true while a CMD sweep is running

        // ---- live control state (written by UI thread, read by control loop thread) ----
        volatile byte _a0 = ThrottleMin, _a1 = AxisNeutral, _a2 = AxisNeutral, _a3 = AxisNeutral;
        volatile byte _flagByte;         // value written into the command region of the stick packet
        volatile int _cmdIndex = 6;      // which control-payload byte (6..12) _flagByte targets.
                                         //   payload[6] = the discrete-command byte (pcap: takeoff=0x02,
                                         //   land/RTH=0x08/0x10). payload[7] held the old gimbal/cal flags.
        volatile int _flagBurstRemaining;  // when >0, the command is pulsed for this many ticks then cleared
        volatile bool _flagHeld;         // when true, the command is sent continuously (press-and-hold)
        volatile bool _flightArmed;      // SAFETY master gate, mirrors _chkArm. While false the control
                                         //   loop streams ONLY neutral sticks + zero command bytes, so
                                         //   leftover slider/flag state can never reach the drone.

        // payload[4]/[5] — normally the 0x20/0x18 "trim/rate" constants, but the Heliway flag bitfield
        // (PROTOCOL.md §6a) puts lockUnlock/levelCor/etc. in byte5. These overrides let the lockUnlock
        // probe drive payload[5] while keeping the live keepalive + :9961 source port. Default = stock.
        volatile byte _payload4 = 0x20;
        volatile byte _payload5 = 0x18;
        volatile bool _probeSweeping;    // true while the lockUnlock sweep coroutine is running
        volatile bool _rampActive;       // true while a throttle.ramp is driving _a0 through the control loop
        volatile int _modeByte = -1;     // last positioning mode from telemetry 0x8b (0e GPS/02 optical/00 init), -1 = none yet
        // --- latest decoded telemetry, snapshotted for the DebugBus HTTP surface (atomic reads) ---
        volatile string? _telModel;
        volatile int _telSats = -1, _telFix = -1;
        volatile int _telBattBars = -1, _telBattRaw = -1;
        volatile bool _telLowBat;
        volatile int _telLatRaw, _telLonRaw;
        volatile bool _telHasGps;
        volatile string? _telLastRaw;
        volatile string? _tel8bRaw, _tel8cRaw;   // full raw 0x8b / 0x8c frames (hex) for the browser HUD's "everything" panel
        volatile int _telRollRaw, _telPitchRaw, _telYawRaw; volatile bool _telHasAttitude;   // 0x8b f[3,4]/f[5,6]/f[7,8]: signed int16 LE, /100 = degrees (bench-confirmed 2026-07-13)

        // ---- pre-allocated send buffers (no per-tick heap allocation) ----
        // Stick packet layout (21 bytes): outer[0..3]=48 00 04 11, inner[4..20]=68 01 0D roll pitch thr yaw 20 18 00 tail[6] XOR
        static byte[] MakeStickBuf()
        {
            var b = new byte[21];
            b[0] = 0x48; b[1] = 0x00; b[2] = 0x04; b[3] = 0x11;   // outer header; 0x11=17=inner length
            b[4] = 0x68; b[5] = 0x01; b[6] = 0x0D;                 // inner: cmd=0x01, payload len=13
            b[11] = 0x20; b[12] = 0x18;                           // payload[4..5] trim; payload[6..12] set per tick
            return b;
        }
        readonly byte[] _stickBuf = MakeStickBuf();  // mutable: [7]roll [8]pitch [9]thr [10]yaw [13..19]cmd region (payload[6..12]) [20]XOR
        readonly byte[] _pingBuf  = WrapUdp(BuildPing());  // ping is fully static — compute once
        readonly byte[] _tailBuf  = new byte[7];     // command region payload[6..12], cleared/set each tick

        // ---- UI rate-limiting ----
        // Telemetry frames arrive fast; pushing every one to the UI thread (BeginInvoke) competes with
        // the mpv video render and adds noticeable latency while streaming. So decode always updates the
        // cached strings below, but the labels are FLUSHED to the UI at most once per _telUiIntervalMs —
        // user-adjustable via the grpTel "UI rate" combo (10/5/2/1 Hz). Default 100 ms = 10 Hz.
        long _lastTelFlushTick;
        int _telUiIntervalMs = 100;
        string? _telModelText, _telSatsText, _telBattText, _telGpsText, _telYawText, _telRollText, _telPitchText;

        public Form1()
        {
            InitializeComponent();

            // Restore the last-used Drone IP (e.g. 10.9.1.1 when going through the AirCard
            // bridge) so it survives restarts, and persist any change the user makes.
            AppSettings.Load();
            if (!string.IsNullOrWhiteSpace(AppSettings.DroneIp)) _txtIp.Text = AppSettings.DroneIp;
            _txtIp.TextChanged += (s, e) => { AppSettings.DroneIp = _txtIp.Text.Trim(); AppSettings.Save(); };

            timeBeginPeriod(1);          // 1ms Windows timer resolution — needed for accurate Thread.Sleep in the control loop
            // SAFETY: mirror the "Enable flight commands" checkbox into a volatile the control
            // loop reads each tick. While unchecked, the loop streams neutral only (see ControlLoop).
            _flightArmed = _chkArm.Checked;
            _chkArm.CheckedChanged += (s, e) =>
            {
                _flightArmed = _chkArm.Checked;
                CenterSticks();   // arm OR disarm: start from zero throttle + centred sticks, so
                                  //   arming can never lurch from a left-over slider position
                Log(_flightArmed ? "*** ARMED — throttle starts at neutral 128 (idle), raise the slider deliberately ***"
                                 : "DISARMED — streaming neutral 128 sticks (throttle included)");
                if (_flightArmed) _armOverlay?.ShowOn(this);   // full-screen red frame while armed
                else              _armOverlay?.Hide();
            };
            _send23333 = _chk23333.Checked;
            _chk23333.CheckedChanged += (s, e) =>
            {
                _send23333 = _chk23333.Checked;
                Log(_send23333 ? "23333 session ON (replaying captured token)" : "23333 session OFF");
            };

            // Mission-control theme + resizable shell. Applied AFTER InitializeComponent so it only
            // recolors existing controls — no layout/logic change. MinimumSize pins the designed layout.
            Theme.Load();                 // restore saved palette + font before first paint
            Theme.Apply(this);
            Font = new Font(Theme.FontName, Theme.FontSize);
            MinimumSize = new Size(Width, Height);

            StartDebugBus();
        }

        // ============================================================
        // DebugBus — localhost JSON control surface (DebugBus.cs). Lets an external tool read live
        // telemetry/log and drive any window via the generic ui.* commands, no screen-clicking.
        // Semantic commands here cover internal state that isn't a single UI control.
        // ============================================================
        void StartDebugBus()
        {
            CommLog.Init();        // persistent SQLite comm log (CommLog.cs) — <app dir>/comm_log.sqlite
            PacketCatalog.Init();  // packet-type novelty catalog (PacketCatalog.cs)
            CommandLibrary.Load(); // reusable command/snippet library (CommandLibrary.cs)
            // Feed every live tx/rx frame into the catalog so a never-before-seen frame shape alerts us.
            PacketTap += (dir, proto, data) => PacketCatalog.Observe(proto, dir, data);
            // A user touching a local slider stamps human activity so the AI/browser yields (Scroll fires
            // only on real user input, never on a programmatic .Value set from ApplyExternalSticks).
            foreach (var tb in new[] { _tbThrottle, _tbYaw, _tbPitch, _tbRoll })
            {
                tb.Scroll += (s, e) => ControlArbiter.Touch(StickSource.LocalUI);
                // Self-centering rate sticks — exactly like the physical remote: release a stick and it
                // snaps back to centre (128 = HOLD, zero rate). Throttle included (altitude-hold model:
                // up=climb, release=hold, below-centre=descend). ValueChanged fires on this programmatic
                // set, so _aN and the 50 Hz control loop follow. Gimbal (_tbGimbal) is excluded — it holds.
                tb.MouseUp += (s, e) => { if (s is TrackBar t) t.Value = AxisNeutral; };
            }
            DebugBus.Start(this, 8731, Log);
            DebugBus.Register("status", "connection + decoded telemetry snapshot", this, _ => Snapshot());
            DebugBus.Register("telemetry", "alias of status (latest decoded telemetry + last raw 0x58)", this, _ => Snapshot());
            DebugBus.Register("send",
                "inject a control-payload byte — args {idx:4..12, val:hex, ms?}. Requires the arm checkbox.",
                this, a =>
                {
                    if (!_flightArmed) return new { sent = false, error = "enable flight commands (arm) first" };
                    int idx = DebugBus.Int(a, "idx") ?? -1;
                    if (idx < 4 || idx > 12) return new { sent = false, error = "idx must be 4..12" };
                    byte val = (byte)(DebugBus.IntHex(a, "val") ?? 0);
                    int ms = DebugBus.Int(a, "ms") ?? 1200;
                    BeginInvoke(async () =>
                    {
                        ApplyCandidate((idx, val, $"http payload[{idx}]=0x{val:X2}"));
                        await Task.Delay(ms);
                        ClearProbe();
                    });
                    return new { sent = true, payload = $"[{idx}]=0x{val:X2}", ms };
                });

            // throttle.ramp — drives the LIVE throttle (_a0) through the existing 50Hz control loop, so it
            // rides the one coherent stream (keepalive + :9961 source) instead of fighting a parallel inject.
            // Pair with the SWD optical-mode hold (tools/hold_optical.cfg). Auto-returns to floor; disarm aborts.
            DebugBus.Register("throttle.ramp",
                "ramp live throttle through the control loop: up start→target, hold, down→settle, then hold/broadcast settle — args {start?=0,target?=40,step?=5,delayMs?=50,holdSec?=2,settle?=0}. Requires arm.",
                this, a =>
                {
                    if (!_flightArmed) return new { started = false, error = "enable flight commands (arm) first" };
                    if (_rampActive) return new { started = false, error = "ramp already running" };
                    int start = Math.Clamp(DebugBus.Int(a, "start") ?? 0, 0, 255);
                    int target = Math.Clamp(DebugBus.Int(a, "target") ?? 40, 0, 255);
                    int step = Math.Max(1, DebugBus.Int(a, "step") ?? 5);
                    int delay = Math.Max(1, DebugBus.Int(a, "delayMs") ?? 50);
                    int holdMs = (DebugBus.Int(a, "holdSec") ?? 2) * 1000;
                    // value to return to after the sweep and then HOLD (control loop keeps broadcasting it).
                    // Default = floor for safety/back-compat; the Ramp tab passes 128 (stick center / idle).
                    int settle = Math.Clamp(DebugBus.Int(a, "settle") ?? ThrottleMin, 0, 255);
                    _rampActive = true;
                    BeginInvoke(async () =>
                    {
                        bool completed = false;
                        try
                        {
                            // up-ramp: start -> target
                            int dir = target >= start ? 1 : -1;
                            for (int t = start; _flightArmed && _rampActive; t += dir * step)
                            {
                                if (dir > 0 && t > target) t = target;
                                if (dir < 0 && t < target) t = target;
                                _a0 = (byte)t; _lblThrottleVal.Text = t.ToString();
                                if (t == target) break;
                                await Task.Delay(delay);
                            }
                            // hold at target
                            long end = Environment.TickCount64 + holdMs;
                            while (Environment.TickCount64 < end && _flightArmed && _rampActive) await Task.Delay(delay);
                            // down-ramp: target -> settle (e.g. 128 = stick center / idle)
                            int ddir = settle >= target ? 1 : -1;
                            for (int t = target; _flightArmed && _rampActive; t += ddir * step)
                            {
                                if (ddir > 0 && t > settle) t = settle;
                                if (ddir < 0 && t < settle) t = settle;
                                _a0 = (byte)t; _lblThrottleVal.Text = t.ToString();
                                if (t == settle) break;
                                await Task.Delay(delay);
                            }
                            completed = _flightArmed && _rampActive;
                        }
                        finally
                        {
                            // normal finish while armed → leave _a0 at settle so the control loop keeps
                            // broadcasting it (e.g. neutral 128 idle); abort/disarm/STOP → neutral 128 (never 0).
                            if (!completed) { _a0 = ThrottleMin; _lblThrottleVal.Text = ThrottleMin.ToString(); }
                            _rampActive = false;
                        }
                    });
                    return new { started = true, start, target, step, delayMs = delay, holdMs, settle };
                });
            DebugBus.Register("throttle.ramp.stop", "abort throttle ramp and return throttle to neutral 128", this,
                _ => { _rampActive = false; _a0 = ThrottleMin; return new { stopped = true }; });

            // ---- session / state ----
            DebugBus.Register("connect", "connect to the drone (async — poll status after)", this, _ =>
            { if (!_connected) _btnConnect.PerformClick(); return new { connected = _connected }; });
            DebugBus.Register("disconnect", "disconnect from the drone", this, _ =>
            { if (_connected) _btnConnect.PerformClick(); return new { connected = _connected }; });
            DebugBus.Register("arm", "set the flight-enable (arm) checkbox — args {on:true|false}", this, a =>
            { _chkArm.Checked = DebugBus.Bool(a, "on") ?? true; return new { armed = _chkArm.Checked }; });
            DebugBus.Register("loglevel", "drone verbose logging via CMD 137 — args {level?=5}", this, a =>
            {
                if (!_connected) return new { error = "not connected" };
                int lvl = DebugBus.Int(a, "level") ?? 5; SetLogLevel(lvl); return new { loglevel = lvl };
            });

            // ---- probes (props-off, gated by the arm checkbox) ----
            DebugBus.Register("probe.mode", "run the GPS/optical mode sweep — watch /log", this, _ =>
            {
                if (!_chkArm.Checked) return new { started = false, error = "enable flight commands (arm) first" };
                BtnProbeMode_Click(null, EventArgs.Empty); return new { started = true };
            });
            DebugBus.Register("probe.arm", "run the lockUnlock arm sweep — watch /log", this, _ =>
            {
                if (!_chkArm.Checked) return new { started = false, error = "enable flight commands (arm) first" };
                BtnProbeLockUnlock_Click(null, EventArgs.Empty); return new { started = true };
            });
            DebugBus.Register("clear", "clear any held probe/flag (back to neutral)", this, _ =>
            { ClearProbe(); return new { cleared = true }; });

            // ---- headless adb shell (no Console window needed) ----
            DebugBus.Register("adb", "run an adb shell command on the drone, returns stdout — args {cmd}", null, a =>
            {
                string cmd = DebugBus.Str(a, "cmd") ?? "";
                if (cmd.Length == 0) return new { error = "args.cmd required" };
                string ip = Invoke(() => _txtIp.Text.Trim());
                return new { cmd, output = DroneConsole.RunAdb(ip, cmd) };
            });

            // ---- raw UDP frame inject (no Workbench needed) ----
            DebugBus.Register("inject", "send raw UDP bytes to the drone — args {hex, port?=19798, count?=1, delayMs?=0}", null, a =>
            {
                byte[] bytes;
                try { bytes = ParseHexBytes(DebugBus.Str(a, "hex") ?? ""); }
                catch (Exception ex) { return new { error = "bad hex: " + ex.Message }; }
                if (bytes.Length == 0) return new { error = "args.hex required" };
                int port = DebugBus.Int(a, "port") ?? 19798;
                int count = Math.Clamp(DebugBus.Int(a, "count") ?? 1, 1, 1000);
                int delay = DebugBus.Int(a, "delayMs") ?? 0;
                string ip = Invoke(() => _txtIp.Text.Trim());
                using var u = new UdpClient();
                var ep = new IPEndPoint(IPAddress.Parse(ip), port);
                for (int i = 0; i < count; i++) { u.Send(bytes, bytes.Length, ep); if (delay > 0 && i < count - 1) Thread.Sleep(delay); }
                CommLog.Event("UDP", $"inject :{port}" + (count > 1 ? $" x{count}" : ""), target: ip, bytes: bytes);
                CommandLibrary.RecordUsed("Inject", Hex(bytes));
                return new { sent = count, port, len = bytes.Length, hex = Hex(bytes) };
            });

            // ---- persistent comm log (CommLog.cs → SQLite) ----
            DebugBus.Register("log.query", "read the comm log (chat) — args {transport?, n?=100, contains?}", null, a =>
                CommLog.Query(DebugBus.Str(a, "transport"), DebugBus.Int(a, "n") ?? 100, DebugBus.Str(a, "contains")));
            DebugBus.Register("log.stats", "comm-log row counts per transport + db path", null, _ => CommLog.Stats());
            DebugBus.Register("log.export", "dump comm log to a JSON file — args {path?, transport?, n?=100000}", null, a =>
                CommLog.Export(DebugBus.Str(a, "path"), DebugBus.Str(a, "transport"), DebugBus.Int(a, "n") ?? 100000));
            DebugBus.Register("log.add", "ingest an external record (standard JSON) — args {transport, direction?, name?, target?, hex?, text?, ok?, error?}", null, a =>
                new { id = CommLog.Add(a) });

            // ---- packet-type catalog + command library ----
            DebugBus.Register("types", "distinct packet types seen per protocol — args {transport?}", null, a =>
                PacketCatalog.Snapshot(DebugBus.Str(a, "transport"))
                    .Select(p => new { p.Transport, p.Direction, p.Sig, p.Count, p.FirstSeen, p.LastSeen, p.SampleLen, sample = p.SampleHex, p.Note, p.NewThisSession })
                    .ToArray());
            DebugBus.Register("lib", "command library entries — args {category?, contains?}", null, a =>
                CommandLibrary.Items(DebugBus.Str(a, "category"), DebugBus.Str(a, "contains")));
        }

        // Parse a loose hex string (spaces / commas / 0x prefixes all tolerated) into bytes.
        static byte[] ParseHexBytes(string s)
        {
            string h = System.Text.RegularExpressions.Regex.Replace(s, "0[xX]", "");
            h = System.Text.RegularExpressions.Regex.Replace(h, "[^0-9a-fA-F]", "");
            if (h.Length % 2 != 0) throw new ArgumentException("odd number of hex digits");
            var b = new byte[h.Length / 2];
            for (int i = 0; i < b.Length; i++) b[i] = Convert.ToByte(h.Substring(i * 2, 2), 16);
            return b;
        }

        object Snapshot() => new
        {
            connected = _connected,
            armed = _flightArmed,
            mode = _modeByte < 0 ? null : FcFrames.ModeName((byte)_modeByte),
            modeByte = _modeByte,
            model = _telModel,
            sats = _telSats < 0 ? (int?)null : _telSats,
            fix = _telFix < 0 ? (bool?)null : _telFix == 1,
            lat = _telHasGps ? _telLatRaw / 1e7 : (double?)null,
            lon = _telHasGps ? _telLonRaw / 1e7 : (double?)null,
            battBars = _telBattBars < 0 ? (int?)null : _telBattBars,
            battRaw = _telBattRaw < 0 ? (int?)null : _telBattRaw,
            lowBat = _telLowBat,
            rollDeg = _telHasAttitude ? _telRollRaw / 100.0 : (double?)null,
            pitchDeg = _telHasAttitude ? _telPitchRaw / 100.0 : (double?)null,
            yawDeg = _telHasAttitude ? _telYawRaw / 100.0 : (double?)null,
            lastRaw = _telLastRaw
        };

        void BtnWorkbench_Click(object? sender, EventArgs e)
            => new ReWorkbench(_txtIp.Text).Show(this);

        // ============================================================
        // Connect / disconnect
        // ============================================================
        async void OnConnectClick(object? sender, EventArgs e)
        {
            if (_connected) { Disconnect(); return; }
            try
            {
                var ip = IPAddress.Parse(_txtIp.Text.Trim());
                _droneUdp = new IPEndPoint(ip, UdpPort);
                _udp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                try { _udp.Bind(new IPEndPoint(IPAddress.Any, LocalUdpPort)); }
                catch (Exception bex) { Log($"Could not bind local UDP {LocalUdpPort} ({bex.Message}); using ephemeral port — telemetry may be limited."); }
                _udp.Connect(_droneUdp);           // sets default peer
                _cts = new CancellationTokenSource();
                _connected = true;
                _udpFailStreak = 0;

                _btnConnect.Text = "Disconnect";
                SetStatus("Connected", Color.SeaGreen);
                Log($"UDP bound, local {_udp.LocalEndPoint}, drone {_droneUdp}");

                // 23333 session socket (same local-port trick as control: bind 23333, send to drone:23333)
                try
                {
                    _udp23333 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    _udp23333.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    _udp23333.Bind(new IPEndPoint(IPAddress.Any, SessionPort));
                    _udp23333.Connect(new IPEndPoint(ip, SessionPort));
                }
                catch (Exception sex) { Log($"23333 session socket failed: {sex.Message}"); _udp23333 = null; }

                _ = Task.Run(() => UdpReceiveLoop(_cts.Token));
                var ct = _cts.Token;
                new Thread(() => ControlLoopThread(ct)) { IsBackground = true, Priority = ThreadPriority.AboveNormal, Name = "ControlLoop" }.Start();
                _ = Task.Run(() => Session23333Loop(_cts.Token));
                await ConnectJsonAsync(ip, _cts.Token);
            }
            catch (Exception ex)
            {
                Log("Connect failed: " + ex.Message);
                Disconnect();
            }
        }

        void Disconnect()
        {
            if (!_connected && _udp == null && _json == null) return;
            _connected = false;
            try { _cts?.Cancel(); } catch { }
            try { _jsonStream?.Dispose(); } catch { }
            try { _json?.Close(); } catch { }
            try { _udp?.Close(); } catch { }
            try { _udp23333?.Close(); } catch { }
            _udp = null; _udp23333 = null; _json = null; _jsonStream = null;
            if (IsHandleCreated)
                BeginInvoke(() => { _btnConnect.Text = "Connect"; SetStatus("Disconnected", Color.Firebrick); });
        }

        async Task ConnectJsonAsync(IPAddress ip, CancellationToken ct)
        {
            try
            {
                _json = new TcpClient();
                await _json.ConnectAsync(ip, JsonPort, ct);
                _jsonStream = _json.GetStream();
                Log($"JSON TCP connected :{JsonPort}");
                _ = Task.Run(() => JsonReceiveLoop(ct));

                // init order observed in capture: 0 -> 138 -> 126 -> 29
                SendJson(0, "-1");
                SendJson(138, "{\"operation\":1,\"format\":\"\"}");
                SendJson(126, "{\"camera\":0}");
                SendTimeSync();
            }
            catch (Exception ex) { Log("JSON connect failed: " + ex.Message); }
        }

        void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            // Only ask on an actual user close (X / Alt+F4) — not on the tray "Exit" item
            // (CloseReason.ApplicationExitCall) or a Windows shutdown.
            if (e.CloseReason == CloseReason.UserClosing)
            {
                var choice = MessageBox.Show("Minimize to tray instead of closing?\n\nYes = minimize to tray\nNo = exit completely",
                    "Close Drone RE Studio", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (choice == DialogResult.Cancel) { e.Cancel = true; return; }
                if (choice == DialogResult.Yes) { e.Cancel = true; Hide(); return; }
            }
            StudioWeb.Stop(); PacketCatalog.Flush(); DebugBus.Stop(); timeEndPeriod(1); Disconnect();
        }

        // ============================================================
        // Event handlers (wired in Form1.Designer.cs)
        // ============================================================
        void BtnTime_Click(object? sender, EventArgs e) => SendTimeSync();
        void BtnRecOn_Click(object? sender, EventArgs e) => SendJson(100, "1");
        void BtnRecOff_Click(object? sender, EventArgs e) => SendJson(100, "0");
        void BtnVideo_Click(object? sender, EventArgs e) => OpenVideo();

        // Opens the drone-side log/config viewer (FTP files + adb shell). Independent of the
        // control connection — only needs the drone's Wi-Fi (DEVICE_ACCESS.md). Non-modal so the
        // 23333 keepalive keeps running behind it and the drone won't idle-power-off mid-browse.
        void BtnConsole_Click(object? sender, EventArgs e)
            => new DroneConsole(_txtIp.Text).Show(this);

        // Live multi-source debug console (logcat/MCU/kernel/UART/tcpdump tailed at once). Non-modal,
        // so the 23333 keepalive keeps the drone awake while it streams.
        void BtnLiveDebug_Click(object? sender, EventArgs e)
            => new LiveDebug(DroneConsole.ResolveAdb(), _txtIp.Text, SetLogLevel).Show(this);

        readonly ToolTip _tip = new() { AutoPopDelay = 30000, InitialDelay = 350, ReshowDelay = 80, ShowAlways = true };

        // Single-screen "mission console": header docked on top, Log pinned at the bottom, and the dense
        // content groups converted into flat collapsible cards (incl. embedded live video) that tile and
        // scroll in the middle. Done at load so every Designer-wired control keeps its handlers — only
        // the containers change. Collapse any card (click its title) to free up space.
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            SuspendLayout();

            foreach (var tb in new[] { _tbThrottle, _tbYaw, _tbPitch, _tbRoll, _tbGimbal })
            { tb.TickStyle = TickStyle.None; tb.BackColor = Theme.Bg1; }

            // Static card chrome — accent header + border painted over the Designer-placed panels.
            // Nothing is created or reparented here; the layout lives in Form1.Designer.cs.
            Theme.ToolbarChrome(grpConn);
            foreach (var card in new[] { grpTel, grpCam, grpGim, grpFly, grpSend, pnlVideo, grpLog })
                Theme.CardChrome(card);

            // Live video is a Designer control inside pnlVideo (visible at design time); it only
            // needs the live Drone IP wired in at runtime so Play/Probe follow the IP box.
            _videoPanel.SetIpProvider(() => _txtIp.Text.Trim());
            _videoPanel.SetCmdSender((cmd, param) => SendJson(cmd, param));   // lets the "2K LowLat" button issue CMD 127

            AddTooltips();
            BuildMenuAndTray();
            _armOverlay = new ArmOverlay();
            FormClosed += (s, ev) => { _tray?.Dispose(); _armOverlay?.Dispose(); };

            MinimumSize = new Size(1000, 640);
            WindowState = FormWindowState.Maximized;
            ResumeLayout();
        }

        // ---- app menu (☰), system-tray icon, settings + the armed-state full-screen warning ----
        ContextMenuStrip? _appMenu;
        NotifyIcon? _tray;
        SettingsForm? _settings;
        ArmOverlay? _armOverlay;

        ContextMenuStrip BuildAppMenu()
        {
            var m = new ContextMenuStrip();
            m.Items.Add("Studio Web (browser control)…", null, (s, e) => new StudioWebForm(this).Show(this));
            m.Items.Add("Drone File Explorer…", null, (s, e) => new DroneFileExplorer(_txtIp.Text).Show(this));
            m.Items.Add("Comm Log Viewer…", null, (s, e) => new CommLogViewer().Show(this));
            m.Items.Add("Command Library…", null, (s, e) => new CommandLibraryForm().Show(this));
            // No owner: an owned window shares its owner's minimize state in Win32, so giving this
            // one none keeps it open/visible when Form1 is minimized (SR: user wants it independent).
            m.Items.Add("Serial Monitor…", null, (s, e) => new SerialMonitorForm().Show());
            m.Items.Add("WiFi Adapter (PC)…", null, (s, e) => new WifiAdapterForm().Show(this));
            m.Items.Add("AirCard 797 (router)…", null, (s, e) => new AirCardForm().Show(this));
            m.Items.Add("Xbox Controller…", null, (s, e) => new GamepadForm(() => FlightArmed, (t, y, p, r) => ApplyExternalSticks(StickSource.Gamepad, t, y, p, r), CenterSticksExternal).Show(this));
            m.Items.Add(new ToolStripSeparator());
            m.Items.Add("Appearance settings…", null, (s, e) => OpenSettings());
            m.Items.Add(new ToolStripSeparator());
            m.Items.Add("Return Home (RTH)", null, (s, e) => BtnGoHome_Click(s, e));
            m.Items.Add("⚠ Emergency Stop…", null, (s, e) =>
            {
                if (MessageBox.Show("This pulses payload[6]=0x04 (stop / motor-spin candidate — UNCONFIRMED wire bit).\n\n" +
                        "Are you sure you want to send Emergency Stop?", "Confirm Emergency Stop",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    BtnStop_Click(s, e);
            });
            m.Items.Add(new ToolStripSeparator());
            m.Items.Add("Hide to tray", null, (s, e) => Hide());
            m.Items.Add("Exit", null, (s, e) => { _tray?.Dispose(); Application.Exit(); });
            Theme.StyleMenu(m);
            return m;
        }

        void BuildMenuAndTray()
        {
            _appMenu = BuildAppMenu();

            _tray = new NotifyIcon
            {
                Icon = Icon ?? System.Drawing.SystemIcons.Application,
                Text = "Drone RE Studio — HS175D",
                Visible = true,
                ContextMenuStrip = BuildAppMenu()        // its own instance (menus can't be shared)
            };
            _tray.DoubleClick += (s, e) => { Show(); WindowState = FormWindowState.Normal; Activate(); };
        }

        void BtnMenu_Click(object? sender, EventArgs e)
            => _appMenu?.Show(_btnMenu, new Point(0, _btnMenu.Height));

        void OpenSettings()
        {
            if (_settings is { IsDisposed: false }) { _settings.Activate(); return; }
            _settings = new SettingsForm(ApplyThemeLive);
            _settings.Show(this);
        }

        // Live-apply the current Theme palette/font to every open surface.
        void ApplyThemeLive()
        {
            Theme.Recolor(this);
            if (_appMenu is not null) Theme.StyleMenu(_appMenu);
            if (_tray?.ContextMenuStrip is not null) Theme.StyleMenu(_tray.ContextMenuStrip);
        }

        void AddTooltips()
        {
            _tip.SetToolTip(_txtIp, "Drone Wi-Fi IP — the SoftAP gateway. Default 192.168.100.1.");
            _tip.SetToolTip(_btnConnect, "Open/close the control session: CMD TCP 4646 + control UDP 19798 + the 23333 keepalive.");
            _tip.SetToolTip(_btnConsole, "Drone Console — FTP file browse + adb root shell.");
            _tip.SetToolTip(_btnWorkbench, "RE Workbench — packet monitor, inject, replay, telemetry.");
            _tip.SetToolTip(_btnLiveDebug, "Live Debug — tail logcat / MCU dumps / kernel / UART / tcpdump at once.");
            _tip.SetToolTip(_chkArm, "SAFETY GATE. Off = stream neutral only. On = your sliders & commands reach the drone. Keep props OFF on the bench.");
            _tip.SetToolTip(_chk23333, "Replay the captured 23333 session token — some firmware gates control on it.");
            _tip.SetToolTip(_tbThrottle, "Throttle 0–255 (128 = centre). Resets to 0 on arm; raise deliberately.");
            _tip.SetToolTip(_tbYaw, "Yaw / rotate, 0–255 (128 = neutral).");
            _tip.SetToolTip(_tbPitch, "Pitch forward/back, 0–255 (128 = neutral).");
            _tip.SetToolTip(_tbRoll, "Roll left/right, 0–255 (128 = neutral).");
            _tip.SetToolTip(_tbGimbal, "Camera gimbal tilt (payload[7] 0x40/0x80 while held).");
            _tip.SetToolTip(_btnCenter, "Re-centre all sticks to neutral (throttle to 0) and clear any held probe.");
            _tip.SetToolTip(_btnCmdTakeoff, "OneKey takeoff — payload[6]=0x02. BENCH: triggered the calibration dance (UNCONFIRMED).");
            _tip.SetToolTip(_btnCmdLand, "Land — payload[6]=0x08. Also the arm / lockUnlock suspect (UNCONFIRMED).");
            _tip.SetToolTip(_btnCmdGoHome, "Return-to-home — payload[6]=0x10 (UNCONFIRMED).");
            _tip.SetToolTip(_btnCmdStop, "Stop — payload[6]=0x04. Bench showed a momentary motor spin (UNCONFIRMED).");
            _tip.SetToolTip(_btnProbeLockUnlock, "Sweep the lockUnlock arm candidates (props OFF) — watch the arm/motor LED for an unlock.");
            _tip.SetToolTip(_btnProbeNext, "Single-step the lockUnlock candidates — holds one until you click again.");
            _tip.SetToolTip(_btnProbeMode, "Closed-loop GPS↔optical mode hunt (props OFF): toggles candidate payload bytes and auto-flags any that flips the telemetry mode (0x8b). Needs live 0x8b frames.");
            _tip.SetToolTip(_btnCtrlPanelHold, "Hold payload[7]=0x01 while moving a stick — tests whether variable sticks are gated by CtrlPanel.");
            _tip.SetToolTip(_btnTakeoff, "Flag probe → payload[7]=0x01.");
            _tip.SetToolTip(_btnLand, "Flag probe → payload[7]=0x02.");
            _tip.SetToolTip(_btnRth, "Flag probe → payload[7]=0x04 (confirmed gyro/level cal).");
            _tip.SetToolTip(_btnEstop, "Flag probe → payload[7]=0x08 (confirmed compass/mag cal).");
            _tip.SetToolTip(_btnBit10, "Flag probe → payload[7]=0x10.");
            _tip.SetToolTip(_btnBit20, "Flag probe → payload[7]=0x20.");

            _tip.SetToolTip(_btnMenu, "App menu — extra tool windows, appearance settings, tray, RTH / Emergency Stop.");
            _tip.SetToolTip(_btnTime, "Send the CMD 0 time-sync frame the phone app sends on connect.");
            _tip.SetToolTip(_btnRecOn, "CMD 100 = \"1\" — start on-drone video recording (to TF card).");
            _tip.SetToolTip(_btnRecOff, "CMD 100 = \"0\" — stop on-drone video recording.");
            _tip.SetToolTip(_btnVideo, "Open the live RTSP video stream in an external/embedded player.");
            _tip.SetToolTip(_btnSendCustom, "Send an arbitrary {\"CMD\":n,\"PARAM\":…} JSON frame — for probing undocumented command IDs.");
            _tip.SetToolTip(_btnSweep, "Sweep the safe read-only info CMD IDs (0/68/84/85/87/88/91/126) and log the replies.");
            _tip.SetToolTip(_btnInfo, "Query the known-safe info commands once (same set BtnSweep loops).");
            _tip.SetToolTip(_btnGimUp, "Step the gimbal tilt slider up (+16) and send it.");
            _tip.SetToolTip(_btnGimDown, "Step the gimbal tilt slider down (-16) and send it.");
            _tip.SetToolTip(_btnGimCenter, "Re-centre the gimbal tilt slider to 128 and send it.");
            _tip.SetToolTip(_btnGimManual, "Send a hand-crafted 3-byte gimbal payload (hex) six times, matching a captured packet's repeat count.");
            _tip.SetToolTip(_btnCap224, "Replay a captured gimbal packet verbatim (tilt value 224).");
            _tip.SetToolTip(_btnCap191, "Replay a captured gimbal packet verbatim (tilt value 191).");
            _tip.SetToolTip(_btnCap255, "Replay a captured gimbal packet verbatim (tilt value 255).");
            _tip.SetToolTip(_btnCap115, "Replay a captured gimbal packet verbatim (tilt value 115).");
            _tip.SetToolTip(_btnCap241, "Replay a captured gimbal packet verbatim (tilt value 241).");
            _tip.SetToolTip(_btnCap99, "Replay a captured gimbal packet verbatim (tilt value 99).");
            _tip.SetToolTip(_btnFlag40, "Hold gimbal tilt DOWN (payload[7]=0x40) while pressed — confirmed live.");
            _tip.SetToolTip(_btnFlag80, "Hold gimbal tilt UP (payload[7]=0x80) while pressed — confirmed live.");
            _tip.SetToolTip(_btnThrUp, "Nudge throttle up by 5 (avoids a flood of intermediate values from dragging the slider).");
            _tip.SetToolTip(_btnThrDn, "Nudge throttle down by 5.");
            _tip.SetToolTip(_btnFlagPulse, "Pulse the hex value in the box into the selected payload byte (index box, default 7) for ~1.2s.");
            _tip.SetToolTip(_btnFlagHold, "Hold the hex value in the box into the selected payload byte while pressed.");
            _tip.SetToolTip(_btnLog5, "SET_HY_LOGLEVEL=5 — turns on firmware frame-dump logging (read /tmp/messages on the drone afterward).");
            _tip.SetToolTip(_btnCmdPhoto, "payload[8]=0x01 guess for Photo — safe to spam, check the gallery.");
            _tip.SetToolTip(_btnCmdVideo, "payload[8]=0x02 guess for Video record toggle — safe to spam, check the gallery.");
            _tip.SetToolTip(_btnEmergLand, "payload[6]=0x20 guess for emergencyLand (UNVERIFIED) — gated by the flight-commands checkbox.");
            _tip.SetToolTip(_btnFlip, "payload[6]=0x40 guess for roll360/flip (UNVERIFIED) — gated by the flight-commands checkbox.");
            _tip.SetToolTip(_btnHeadless, "payload[6]=0x80 guess for headless/course-free mode (UNVERIFIED) — gated by the flight-commands checkbox.");
            _tip.SetToolTip(_btnAltHold, "payload[6]=0x01 guess for altitude-hold (UNVERIFIED) — gated by the flight-commands checkbox.");
        }

        // Query the safe info commands found by the sweep (none of these reset the drone).
        void BtnInfo_Click(object? sender, EventArgs e)
        {
            Log("--- drone info ---");
            foreach (int c in new[] { 0, 68, 84, 85, 87, 88, 91, 126 })
                SendJson(c, "-1");
        }

        void BtnSendCustom_Click(object? sender, EventArgs e)
        {
            if (int.TryParse(_txtCustomCmd.Text, out int c))
                SendJson(c, string.IsNullOrWhiteSpace(_txtCustomParam.Text) ? "-1" : _txtCustomParam.Text);
        }

        // CMD 137 SET_HY_LOGLEVEL. level 5 sets bit 2 (level & 4), which is the gate the firmware's
        // tx paths check before dumping "app ot yt" MCU frames. Read them on-device (FTP/adb root) to
        // recover the stick axis order the capture never showed. Level 0 turns the firehose back off.
        void SetLogLevel(int level)
        {
            if (SendJson(137, $"{{\"loglevel\":{level}}}"))
                Log($"SET_HY_LOGLEVEL={level}" + (level >= 4 ? "  (frame logging ON — read /tmp/messages on the drone)" : ""));
        }

        void BtnLog5_Click(object? sender, EventArgs e) => SetLogLevel(5);

        // ---- gimbal ----
        void TbGimbal_Scroll(object? sender, EventArgs e)
        {
            _lblGimbalVal.Text = _tbGimbal.Value.ToString();
            if (_connected) SendGimbal((byte)_tbGimbal.Value);
        }

        void BtnGimUp_Click(object? sender, EventArgs e)
        {
            _tbGimbal.Value = Math.Min(255, _tbGimbal.Value + 16);
            TbGimbal_Scroll(sender, e);
        }

        void BtnGimDown_Click(object? sender, EventArgs e)
        {
            _tbGimbal.Value = Math.Max(0, _tbGimbal.Value - 16);
            TbGimbal_Scroll(sender, e);
        }

        void BtnGimCenter_Click(object? sender, EventArgs e)
        {
            _tbGimbal.Value = 128;
            TbGimbal_Scroll(sender, e);
        }

        void BtnGimManual_Click(object? sender, EventArgs e)
        {
            if (!_connected) { Log("Not connected."); return; }
            try
            {
                byte p0 = Convert.ToByte(_txtGimP0.Text.Trim(), 16);
                byte p1 = Convert.ToByte(_txtGimP1.Text.Trim(), 16);
                byte p2 = Convert.ToByte(_txtGimP2.Text.Trim(), 16);
                var pkt = BuildGimbal(p0, p1, p2);
                SendUdpBurst(pkt);
                Log("GIMBAL manual x6 " + Hex(pkt));
            }
            catch (Exception ex) { Log("Bad manual gimbal hex: " + ex.Message); }
        }

        // replay an exact captured gimbal packet (Button.Tag holds the hex string)
        void GimCapture_Click(object? sender, EventArgs e)
        {
            if (sender is not Button b || b.Tag is not string hex) return;
            if (!_connected) { Log("Not connected."); return; }
            try
            {
                var pkt = Convert.FromHexString(hex);
                SendUdpBurst(pkt);
                Log("GIMBAL replay x6 " + Hex(pkt));
            }
            catch (Exception ex) { Log("Bad capture hex: " + ex.Message); }
        }

        // Gimbal tilt = stick flag byte (CONFIRMED on hardware). Held while the button is pressed.
        // 0x80 / 0x40 are the two tilt directions; 0x00 = stop.
        void GimUp_MouseDown(object? sender, MouseEventArgs e) => StartGimbalHold(0x80);
        void GimDown_MouseDown(object? sender, MouseEventArgs e) => StartGimbalHold(0x40);
        void Gim_MouseUp(object? sender, MouseEventArgs e) => StopGimbalHold();

        void StartGimbalHold(byte flag)
        {
            if (!_connected) { Log("Not connected."); return; }
            _cmdIndex = 7; _flagByte = flag; _flagHeld = true;   // gimbal/calib live in payload byte 7
            Log($"GIMBAL hold 0x{flag:X2}");
        }

        void StopGimbalHold()
        {
            if (!_flagHeld) return;
            _flagHeld = false; _flagByte = 0;
            Log("GIMBAL release");
        }

        // ---- flight ----
        void TbThrottle_ValueChanged(object? sender, EventArgs e) { _a0 = (byte)_tbThrottle.Value; _lblThrottleVal.Text = _tbThrottle.Value.ToString(); }

        // Discrete throttle steps — repeatable ±5 nudges instead of dragging the slider (which sends a
        // flood of intermediate values). Setting .Value fires TbThrottle_ValueChanged, which updates _a0.
        void BtnThrUp_Click(object? sender, EventArgs e) => _tbThrottle.Value = Math.Min(_tbThrottle.Maximum, _tbThrottle.Value + 5);
        void BtnThrDn_Click(object? sender, EventArgs e) => _tbThrottle.Value = Math.Max(_tbThrottle.Minimum, _tbThrottle.Value - 5);
        void TbYaw_ValueChanged(object? sender, EventArgs e) { _a1 = (byte)_tbYaw.Value; _lblYawVal.Text = _tbYaw.Value.ToString(); }
        void TbPitch_ValueChanged(object? sender, EventArgs e) { _a2 = (byte)_tbPitch.Value; _lblPitchVal.Text = _tbPitch.Value.ToString(); }
        void TbRoll_ValueChanged(object? sender, EventArgs e) { _a3 = (byte)_tbRoll.Value; _lblRollVal.Text = _tbRoll.Value.ToString(); }

        // ---- external stick source (Xbox controller — GamepadForm) ----
        // Writes through the trackbars so the loop, labels and arm gate behave exactly as for manual input.
        public bool FlightArmed => _flightArmed;

        // Stick writes from any non-local source (Gamepad / Browser / AI) funnel through here and the
        // arbiter, so a higher-priority source (a human) transparently locks out a lower one (the AI).
        public void ApplyExternalSticks(StickSource src, byte throttle, byte yaw, byte pitch, byte roll)
        {
            if (!ControlArbiter.TryWrite(src)) return;   // a higher-priority source owns the sticks right now
            if (InvokeRequired) { BeginInvoke(() => ApplyExternalSticksUi(throttle, yaw, pitch, roll)); return; }
            ApplyExternalSticksUi(throttle, yaw, pitch, roll);
        }
        void ApplyExternalSticksUi(byte throttle, byte yaw, byte pitch, byte roll)
        {
            _tbThrottle.Value = Math.Clamp((int)throttle, _tbThrottle.Minimum, _tbThrottle.Maximum);
            _tbYaw.Value      = Math.Clamp((int)yaw,      _tbYaw.Minimum,      _tbYaw.Maximum);
            _tbPitch.Value    = Math.Clamp((int)pitch,    _tbPitch.Minimum,    _tbPitch.Maximum);
            _tbRoll.Value     = Math.Clamp((int)roll,     _tbRoll.Minimum,     _tbRoll.Maximum);
        }
        public void CenterSticksExternal()
        {
            if (InvokeRequired) { BeginInvoke(CenterSticksExternal); return; }
            CenterSticks();
        }

        // ---- StudioWeb (browser) façade — see StudioWeb.cs. All read-only state via fields (thread-safe). ----
        public object WebSnapshot() => new
        {
            connected = _connected,
            armed = _flightArmed,
            mode = _modeByte < 0 ? null : FcFrames.ModeName((byte)_modeByte),
            modeByte = _modeByte,
            model = _telModel,
            sats = _telSats < 0 ? (int?)null : _telSats,
            fix = _telFix < 0 ? (bool?)null : _telFix == 1,
            lat = _telHasGps ? _telLatRaw / 1e7 : (double?)null,
            lon = _telHasGps ? _telLonRaw / 1e7 : (double?)null,
            latRaw = _telLatRaw, lonRaw = _telLonRaw, hasGps = _telHasGps,
            battBars = _telBattBars < 0 ? (int?)null : _telBattBars,
            battRaw = _telBattRaw < 0 ? (int?)null : _telBattRaw,
            lowBat = _telLowBat,
            rollDeg = _telHasAttitude ? _telRollRaw / 100.0 : (double?)null,
            pitchDeg = _telHasAttitude ? _telPitchRaw / 100.0 : (double?)null,
            yawDeg = _telHasAttitude ? _telYawRaw / 100.0 : (double?)null,
            sticks = new { throttle = _a0, yaw = _a1, pitch = _a2, roll = _a3 },
            control = ControlArbiter.Status(),
            lastRaw = _telLastRaw,
            raw8b = _tel8bRaw,           // undecoded attitude region
            raw8c = _tel8cRaw,           // undecoded gps tail
            // every distinct 58xx telemetry subtype seen this session (count/len/sample) — the "show everything" panel
            subtypes = PacketCatalog.Snapshot("UDP19798")
                .Where(p => p.Direction == "rx" && p.Sig.StartsWith("58", StringComparison.OrdinalIgnoreCase))
                .Select(p => new { sig = p.Sig, count = p.Count, len = p.SampleLen, sample = p.SampleHex })
                .ToArray()
        };

        // Discrete commands for the browser — reuse the exact opcodes from the Btn*_Click handlers.
        // Only takeoff(0x02) is confirmed; the rest are UNVERIFIED guesses (labelled as such in the UI).
        public string WebDiscreteCommand(string name)
        {
            if (InvokeRequired) return (string)Invoke(() => WebDiscreteCommand(name));
            switch ((name ?? "").Trim().ToLowerInvariant())
            {
                case "takeoff":   PulseCommand(6, 0x02, true, "Takeoff (web)"); return "takeoff";
                case "land":      PulseCommand(6, 0x08, true, "Land? (web, UNCONFIRMED)"); return "land";
                case "rth":       PulseCommand(6, 0x10, true, "RTH? (web, UNCONFIRMED)"); return "rth";
                case "stop":      PulseCommand(6, 0x04, true, "Stop? (web, UNCONFIRMED)"); return "stop";
                case "althold":   PulseCommand(6, 0x01, true, "AltHold? (web, UNVERIFIED)"); return "althold";
                case "emergency": PulseCommand(6, 0x20, true, "Emergency? (web, UNVERIFIED)"); return "emergency";
                default: return "unknown:" + name;
            }
        }

        public bool WebArm(bool on)
        {
            if (InvokeRequired) return (bool)Invoke(() => WebArm(on));
            _chkArm.Checked = on; return _chkArm.Checked;
        }

        public void LogLine(string s) => Log(s);   // public log sink for StudioWeb etc.

        public string DroneIp => Invoke(() => _txtIp.Text.Trim());

        // Waypoint / orbit frame for the browser map. Byte packing mirrors ReWorkbench.BuildGpsFrame
        // (the single source of truth for the format) — UNVERIFIED native packing, cmdId is a guess.
        public string WebSendGps(bool orbit, double lat, double lon, int alt, int speed, int radiusM, byte cmdId)
        {
            var pl = new List<byte>();
            pl.AddRange(BitConverter.GetBytes((int)(lon * 1e7)));   // int32 LE deg×1e7 (matches 58 8c)
            pl.AddRange(BitConverter.GetBytes((int)(lat * 1e7)));
            pl.AddRange(BitConverter.GetBytes((short)alt));
            pl.Add((byte)speed);
            if (orbit) pl.AddRange(BitConverter.GetBytes((short)(radiusM * 10)));   // m → decimetres
            pl.Add(0x01);                                            // point/circle number (guess)

            var body = new byte[3 + pl.Count];
            body[0] = 0x68; body[1] = cmdId; body[2] = (byte)pl.Count;
            pl.CopyTo(body, 3);
            byte x = 0; for (int i = 1; i < body.Length; i++) x ^= body[i];   // 68-family XOR trailer
            var inner = new byte[body.Length + 1]; Array.Copy(body, inner, body.Length); inner[^1] = x;
            var frame = new byte[4 + inner.Length];
            frame[0] = 0x48; frame[1] = 0x00; frame[2] = 0x04; frame[3] = (byte)inner.Length;
            Array.Copy(inner, 0, frame, 4, inner.Length);

            if (InvokeRequired) return (string)Invoke(() => WebSendGpsRaw(frame));
            return WebSendGpsRaw(frame);
        }
        string WebSendGpsRaw(byte[] frame)
        {
            if (!_connected) return "not connected";
            SendRaw(frame);
            return "sent " + Hex(frame) + " (UNVERIFIED — validate vs a real phone tap-to-fly capture)";
        }

        void BtnCenter_Click(object? sender, EventArgs e) => CenterSticks();

        // Pulse value v into control-payload byte `index` (6..12) for ~1s (matches the phone's ~1s pulse).
        // payload[6] is the discrete-command byte (takeoff/land/RTH). flightGate=true requires the safety
        // checkbox (motor-spinning commands); false for camera/gimbal.
        void PulseCommand(int index, byte v, bool flightGate, string label)
        {
            if (flightGate && !_chkArm.Checked) { Log("Flight commands disabled — tick 'Enable flight commands'."); return; }
            _cmdIndex = index; _flagByte = v; _flagBurstRemaining = 60;   // ~1.2s @50Hz
            Log($"{label}: payload[{index}]=0x{v:X2} pulse");
        }

        // byte index (7..12) for the manual probe; defaults to 7 if blank/out of range
        int CmdByteIndex()
            => int.TryParse(_txtFlagIdx.Text.Trim(), out int i) && i >= 6 && i <= 12 ? i : 6;

        // Flag-byte probe: pulse a value into payload byte 7 (confirmed gimbal/calib region).
        void FlagPulse_Click(object? sender, EventArgs e)
        {
            if (sender is Button b && b.Tag is string hex &&
                byte.TryParse(hex, NumberStyles.HexNumber, null, out byte v))
                PulseCommand(7, v, true, "FLAG");
        }

        // Manual probe: pulse/hold any value into any command byte (7..12) — brute-force the unknown bits.
        void BtnFlagPulse_Click(object? sender, EventArgs e)
        {
            if (byte.TryParse(_txtFlag.Text.Trim(), NumberStyles.HexNumber, null, out byte v))
                PulseCommand(CmdByteIndex(), v, true, "PROBE");
            else Log("Bad flag hex.");
        }

        void FlagHold_MouseDown(object? sender, MouseEventArgs e)
        {
            if (!byte.TryParse(_txtFlag.Text.Trim(), NumberStyles.HexNumber, null, out byte v)) { Log("Bad flag hex."); return; }
            if (!_chkArm.Checked) { Log("Flight commands disabled — tick 'Enable flight commands'."); return; }
            _cmdIndex = CmdByteIndex(); _flagByte = v; _flagHeld = true;
            Log($"PROBE hold payload[{_cmdIndex}]=0x{v:X2}");
        }

        void FlagHold_MouseUp(object? sender, MouseEventArgs e) => StopGimbalHold();

        // EXPERIMENTAL named commands. FlySendInfo packs Photo/Video/AutoTakeoff/AutoLand/GoHome/Stop
        // into control payload bytes 7..12, but the exact byte/bit is set by native libmain.so and is
        // UNVERIFIED — these (index,value) pairs are GUESSES. Photo/Video are safe to spam (they only
        // write to the SD card — check the gallery afterward). Takeoff/Land/GoHome/Stop are gated by the
        // safety checkbox. If a button does nothing, walk the manual probe across bytes 7..12 / bits to
        // find the real position, then tell me and I'll lock the label in.
        void BtnPhoto_Click(object? sender, EventArgs e)   => PulseCommand(8, 0x01, false, "Photo?");
        void BtnVideoRec_Click(object? sender, EventArgs e) => PulseCommand(8, 0x02, false, "Video?");
        // payload[6] discrete commands. 0x02=TAKEOFF (confirmed, phone trace). The rest are UNCONFIRMED:
        // on the 2026-06-25 bench (PROTOCOL.md §2a/§4) a Wi-Fi unlock + ~1 s motor spin were seen, but the
        // session mixed many commands (these buttons AND the lockUnlock sweep ~2×) and did not reproduce,
        // so NO bit is attributed yet. To attribute: cold-lock the FC, send ONE candidate at a time
        // (HOLD via Flag idx 6 / val 08), and read the timestamped log against the LED.
        void BtnTakeoff_Click(object? sender, EventArgs e)  => PulseCommand(6, 0x02, true,  "Takeoff");
        void BtnLand_Click(object? sender, EventArgs e)     => PulseCommand(6, 0x08, true,  "0x08 = land / arm? (UNCONFIRMED)");
        void BtnGoHome_Click(object? sender, EventArgs e)   => PulseCommand(6, 0x10, true,  "0x10 = RTH / arm? (UNCONFIRMED)");
        void BtnStop_Click(object? sender, EventArgs e)     => PulseCommand(6, 0x04, true,  "0x04 = stop / motor spin? (UNCONFIRMED)");

        // Named commands from FlySendInfo / Heliway §6a — wire byte UNVERIFIED for our drone (best-guess
        // payload[6] bits). If one fires, capture it and lock the value in docs/PROTOCOL.md §2a.
        void BtnEmergencyLand_Click(object? sender, EventArgs e) => PulseCommand(6, 0x20, true, "emergencyLand? (UNVERIFIED)");
        void BtnFlip_Click(object? sender, EventArgs e)          => PulseCommand(6, 0x40, true, "roll360/flip? (UNVERIFIED)");
        void BtnHeadless_Click(object? sender, EventArgs e)      => PulseCommand(6, 0x80, true, "headless? (UNVERIFIED)");
        void BtnAltHold_Click(object? sender, EventArgs e)       => PulseCommand(6, 0x01, true, "altHold? (UNVERIFIED)");

        // ============================================================
        // Heliway arm/control probes (PROTOCOL.md §4/§6a). PROPS OFF, clamped, RC in reach.
        // lockUnlock (a motor arm/disarm flag) and ctrlPanel (variable-stick gate) exist in this
        // protocol family; their exact WIRE byte for our drone is inferred, so these are guided search
        // tools. All go through the live control loop (keepalive + :9961 source) so the drone can honor them.
        // ============================================================
        // Candidate (payload index, value, label). idx==5 => override the payload[5] flag byte
        // (Heliway lockUnlock position); idx 6/7 => hold into the command region.
        static readonly (int idx, byte val, string label)[] ArmCandidates =
        {
            // Lead with the operator's (unverified) suspects from the 2026-06-25 bench, where a Wi-Fi
            // unlock was seen but NOT attributed (PROTOCOL.md §4). Test these first, one at a time.
            (6, 0x08, "payload[6]=0x08  (land/arm suspect — UNCONFIRMED)"),
            (6, 0x10, "payload[6]=0x10  (RTH/arm suspect — UNCONFIRMED)"),
            (6, 0x04, "payload[6]=0x04  (stop/spin suspect — UNCONFIRMED)"),
            (5, 0x1C, "payload[5]=0x1C  (0x18 | bit2 = Heliway lockUnlock, bit18)"),
            (6, 0x01, "payload[6]=0x01"),
            (6, 0x20, "payload[6]=0x20"),
            (6, 0x40, "payload[6]=0x40"),
            (6, 0x80, "payload[6]=0x80"),
            (7, 0x02, "payload[7]=0x02"),
            (7, 0x10, "payload[7]=0x10"),
            (7, 0x20, "payload[7]=0x20"),
        };
        int _probeStep = -1;
        bool _ctrlPanelOn;

        void ApplyCandidate((int idx, byte val, string label) c)
        {
            if (c.idx == 4) _payload4 = c.val;
            else if (c.idx == 5) _payload5 = c.val;
            else { _cmdIndex = c.idx; _flagByte = c.val; _flagHeld = true; }
        }
        void ClearProbe() { _payload5 = 0x18; _payload4 = 0x20; _flagHeld = false; }

        // Full auto-sweep: hold each candidate ~1.5 s, then a neutral gap, logging as it goes.
        async void BtnProbeLockUnlock_Click(object? sender, EventArgs e)
        {
            if (!_chkArm.Checked) { Log("Enable flight commands first (PROPS OFF)."); return; }
            if (_probeSweeping) { Log("Sweep already running."); return; }
            _probeSweeping = true;
            Log("=== lockUnlock SWEEP — PROPS OFF. Watch the arm/motor LED; note any candidate that changes it. ===");
            try
            {
                foreach (var c in ArmCandidates)
                {
                    if (!_chkArm.Checked) { Log("Disarmed mid-sweep — aborting."); break; }
                    Log("  try: " + c.label);
                    ApplyCandidate(c);
                    await Task.Delay(1500);
                    ClearProbe();
                    await Task.Delay(400);
                }
            }
            finally { ClearProbe(); _probeSweeping = false; Log("=== lockUnlock SWEEP done ==="); }
        }

        // Manual single-step: hold one candidate until the next click (dwell as long as you like).
        void BtnProbeNext_Click(object? sender, EventArgs e)
        {
            if (!_chkArm.Checked) { Log("Enable flight commands first (PROPS OFF)."); return; }
            ClearProbe();
            _probeStep = (_probeStep + 1) % ArmCandidates.Length;
            var c = ArmCandidates[_probeStep];
            ApplyCandidate(c);
            Log($"lockUnlock step {_probeStep + 1}/{ArmCandidates.Length} (HELD): {c.label}  — click Next to advance, Center to clear.");
        }

        // ============================================================
        // Positioning-MODE probe (GPS <-> optical) — the gate this whole effort targets.
        // gpsMode's exact WIRE byte is NOT derivable from the native Heliway bitfield: our wire uses
        // compacted enum bytes (payload[6]=action, [7]=cal), not the native 32-bit flag field, so
        // bit15 doesn't map 1:1. This is a guided search over the unused payload real estate
        // (payload[8..12], all 0x00 at rest) + spare bits of payload[4]/[5]. CLOSED-LOOP: it watches
        // the live telemetry mode byte (0x8b payload[13], _modeByte) and flags any candidate that
        // flips it — so a HIT is detected, not eyeballed. PROPS OFF — same safety gate as lockUnlock.
        // ============================================================
        static (int idx, byte val, string label)[] BuildModeCandidates()
        {
            var list = new List<(int, byte, string)>
            {
                (5, 0x98, "payload[5]=0x98  (Heliway gpsMode bit15 candidate)"),
                (5, 0x58, "payload[5]=0x58  (flip/noHead-region bit)"),
            };
            foreach (int idx in new[] { 8, 9, 10, 11, 12 })         // unused command real estate
                foreach (byte bit in new byte[] { 0x01, 0x02, 0x04, 0x08 })
                    list.Add((idx, bit, $"payload[{idx}]=0x{bit:X2}"));
            foreach (byte bit in new byte[] { 0x01, 0x02, 0x04, 0x20 })   // spare bits of payload[5] (rest 0x18)
                list.Add((5, (byte)(0x18 | bit), $"payload[5]=0x{(0x18 | bit):X2}"));
            foreach (byte bit in new byte[] { 0x01, 0x02, 0x04, 0x08, 0x40, 0x80 })  // spare bits of payload[4] (rest 0x20)
                list.Add((4, (byte)(0x20 | bit), $"payload[4]=0x{(0x20 | bit):X2}"));
            // --- phase B (wider): high nibble of the unused [8..12], plus the two no-op [7] enum values
            // (0x04/0x08 = cal, 0x40/0x80 = gimbal — skipped to avoid confounding side effects).
            foreach (int idx in new[] { 8, 9, 10, 11, 12 })
                foreach (byte bit in new byte[] { 0x10, 0x20, 0x40, 0x80 })
                    list.Add((idx, bit, $"payload[{idx}]=0x{bit:X2}"));
            foreach (byte v in new byte[] { 0x10, 0x20 })
                list.Add((7, v, $"payload[7]=0x{v:X2}"));
            return list.ToArray();
        }
        static readonly (int idx, byte val, string label)[] ModeCandidates = BuildModeCandidates();

        async void BtnProbeMode_Click(object? sender, EventArgs e)
        {
            if (!_chkArm.Checked) { Log("Enable flight commands first (PROPS OFF)."); return; }
            if (_probeSweeping) { Log("A sweep is already running."); return; }
            if (_modeByte < 0) { Log("No telemetry mode yet — connect and wait for 0x8b frames before probing."); return; }
            _probeSweeping = true;
            int baseline = _modeByte;
            Log($"=== MODE SWEEP — PROPS OFF. baseline = {FcFrames.ModeName((byte)baseline)} (0x{baseline:X2}); watching 0x8b for a flip. {ModeCandidates.Length} candidates. ===");
            try
            {
                foreach (var c in ModeCandidates)
                {
                    if (!_chkArm.Checked) { Log("Disarmed mid-sweep — aborting."); break; }
                    ApplyCandidate(c);
                    await Task.Delay(1300);                 // hold long enough for >=1 telemetry frame
                    int now = _modeByte;
                    ClearProbe();
                    if (now >= 0 && now != baseline)
                    {
                        Log($"  *** HIT *** {c.label}  ->  {FcFrames.ModeName((byte)baseline)} -> {FcFrames.ModeName((byte)now)} (0x{baseline:X2}->0x{now:X2})");
                        baseline = now;                     // re-baseline — the flip may latch
                    }
                    else Log($"  {c.label}  ->  no change ({FcFrames.ModeName((byte)now)})");
                    await Task.Delay(400);
                }
            }
            finally { ClearProbe(); _probeSweeping = false; Log("=== MODE SWEEP done ==="); }
        }

        // ctrlPanel: hold payload[7]=0x01 (Heliway bit32) WHILE you move the sliders. The control loop
        // already streams live axes with a held flag, so this is the exact "do variable sticks take?" test.
        void BtnCtrlPanelHold_Click(object? sender, EventArgs e)
        {
            if (!_chkArm.Checked) { Log("Enable flight commands first (PROPS OFF)."); return; }
            _ctrlPanelOn = !_ctrlPanelOn;
            if (_ctrlPanelOn) { _cmdIndex = 7; _flagByte = 0x01; _flagHeld = true; Log("ctrlPanel HOLD ON (payload[7]=0x01) — now move a stick; watch for a proportional response."); }
            else { _flagHeld = false; Log("ctrlPanel HOLD OFF."); }
            _btnCtrlPanelHold.Text = _ctrlPanelOn ? "ctrlPanel: ON" : "ctrlPanel hold";
        }

        // DIRECTIONAL MOVEMENT has TWO paths over Wi-Fi:
        //  (1) MANUAL sticks — NOTE: the stock HS175D app does NOT expose joysticks (they're gated to the
        //      "HS790" model only; HS175D uses the legacy screen and sends NEUTRAL sticks). So the phone is
        //      not our reference for sticks. BUT the stick ENCODING is the firmware/MCU protocol (verified
        //      in APK u3/e.java/u3/f.java: LINEAR about 128, 0-255, expo 0.6 on roll/pitch/yaw & 1.0
        //      throttle; not degrees/tilt/signed), and img_trans_app relays any well-formed stick frame to
        //      the MCU. So OUR sliders ARE the valid control channel by injection, post-RC-arm. Bench-test:
        //      does the drone honor injected sticks, and is CtrlPanel=1 required (HS790 sets it on panel-open).
        //  (2) AUTONOMOUS GPS frames (decoded from the APK, docs/REFERENCE.md §8; need a GPS fix):
        //      • PointFly / tap-to-fly (FlySendWayPointInfo, 16B): PointGpsLon, PointGpsLat,
        //        PointAltitude, PointSpeed, PointNum, PointTime
        //      • Orbit / CircleFly     (FlySendCircleInfo, 16B): CircleGpsLon, CircleGpsLat,
        //        CircleAltitude, CircleRadius, CircleSpeed, CircleNumber
        //      GPS coords are degrees*1e7 (confirmed: setPointGpsLat(deg*1.0E7)); radius = m*10. The
        //      field set is known; native byte packing (nativeGetFlySendWayPointData) is still TBD —
        //      capture one tap-to-fly + one orbit and diff vs neutral before implementing a builder.

        void CenterSticks()
        {
            _tbThrottle.Value = ThrottleMin;                       // throttle to neutral 128 (zero climb-rate / idle)
            _tbYaw.Value = _tbPitch.Value = _tbRoll.Value = 128;
            _a0 = ThrottleMin; _a1 = _a2 = _a3 = AxisNeutral;
            ClearProbe();                                          // drop any held Heliway probe / payload override
            if (_ctrlPanelOn) { _ctrlPanelOn = false; _btnCtrlPanelHold.Text = "ctrlPanel hold"; }
        }

        // Set EVERY axis to neutral 0x80 (128) — the exact state the phone holds during takeoff/land/RTH
        // (pcap: all four axes stayed 0x80 the whole flight, throttle included). Discrete flight commands
        // call this so our frame matches the phone's; throttle 0x00 = lock/min would otherwise block takeoff.
        void NeutralizeSticks()
        {
            _tbThrottle.Value = AxisNeutral; _tbYaw.Value = AxisNeutral;
            _tbPitch.Value = AxisNeutral; _tbRoll.Value = AxisNeutral;
            _a0 = _a1 = _a2 = _a3 = AxisNeutral;
        }

        // ---- send-stream tuning UI (grpSend) ----
        void ChangeDetect_CheckedChanged(object? sender, EventArgs e)
        {
            _sendOnChange = _chkChangeDetect.Checked;
            Log(_sendOnChange ? "Send mode: on-change + heartbeat" : "Send mode: steady stream (every tick)");
        }

        void SendTuning_Changed(object? sender, EventArgs e)
        {
            _controlHz = (int)_numControlHz.Value;
            _heartbeatHz = (int)_numHeartbeatHz.Value;
            Log($"Send tuning: loop {_controlHz} Hz, heartbeat {_heartbeatHz} Hz");
        }

        // ============================================================
        // Control loop — dedicated AboveNormal thread, Stopwatch timing, no heap allocation.
        // Runs at ControlHz (default 50), deliberately under the drone's ~66 Hz drain to avoid backlog.
        // ============================================================
        void ControlLoopThread(CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();
            long nextTick = sw.ElapsedTicks;
            int loopTick = 0;
            // Last stick values actually sent (-1 = none yet), for change detection during a hold.
            int lastRoll = -1, lastPitch = -1, lastThrottle = -1, lastYaw = -1;
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    // Snapshot the live tuning so the UI can change rate/mode mid-flight.
                    int controlHz = Math.Clamp(_controlHz, 5, 200);
                    long intervalTicks = Stopwatch.Frequency / controlHz;
                    int hbDivisor = Math.Max(1, controlHz / Math.Max(1, _heartbeatHz));
                    bool sendOnChange = _sendOnChange;

                    // SAFETY GATE: stick frames are streamed ONLY while ARMED. While disarmed we send
                    // NO stick/throttle frames at all — so 0x00 (which is an active lock/min command, not
                    // "off") never goes out, and no leftover slider value can reach the drone. The
                    // keepalive ping below still runs in both states so the link doesn't idle-off.
                    if (_flightArmed)
                    {
                        // Command region = control payload bytes 7..12. Apply _flagByte at _cmdIndex
                        // while held or pulsing; otherwise send all-zero command bytes.
                        bool flagActive = _flagHeld || _flagBurstRemaining > 0;
                        Array.Clear(_tailBuf, 0, 7);
                        if (flagActive)
                        {
                            int k = _cmdIndex - 6;   // command region = payload[6..12]
                            if (k >= 0 && k < 7) _tailBuf[k] = _flagByte;
                            if (!_flagHeld && _flagBurstRemaining > 0) _flagBurstRemaining--;
                        }

                        // LIVE-CONFIRMED: byte2 = throttle (FlySendInfo order roll/pitch/throttle/yaw).
                        byte throttle = _a0, yaw = _a1, pitch = _a2, roll = _a3;

                        // Steady mode (sendOnChange=false): send every tick. Change mode: send on a stick
                        // CHANGE (instant while moving), while a command flag is active, or on the heartbeat
                        // tick — so a held stick drops to ~heartbeatHz and the drone's buffer drains.
                        bool changed = roll != lastRoll || pitch != lastPitch ||
                                       throttle != lastThrottle || yaw != lastYaw;
                        if (!sendOnChange || changed || flagActive || loopTick % hbDivisor == 0)
                        {
                            UpdateStickBuf(roll, pitch, throttle, yaw);
                            SendRaw(_stickBuf);
                            lastRoll = roll; lastPitch = pitch; lastThrottle = throttle; lastYaw = yaw;
                        }
                    }
                    else
                    {
                        lastRoll = lastPitch = lastThrottle = lastYaw = -1;   // re-arm forces a fresh send
                    }

                    // Keepalive ping (68 0a — NOT a stick frame) in both states, keeps the link alive.
                    if (loopTick % PingEveryNTicks == 0) SendRaw(_pingBuf);
                    loopTick++;

                    // Timing: sleep-only (no busy spin — that burned a core and starved the UI/gimbal
                    // thread). timeBeginPeriod(1) makes Thread.Sleep accurate to ~1ms, plenty for a 20ms
                    // period. We advance nextTick to keep long-run drift near zero, BUT if the loop ever
                    // falls a whole frame behind (a stall/GC/blocked send), we RESYNC to now instead of
                    // firing a catch-up burst — a burst would spike the send rate and feed the very
                    // receive-buffer backlog that causes the "delayed then all-at-once" throttle lag.
                    nextTick += intervalTicks;
                    long now = sw.ElapsedTicks;
                    if (now < nextTick)
                    {
                        long msLeft = (nextTick - now) * 1000 / Stopwatch.Frequency;
                        if (msLeft > 0) Thread.Sleep((int)msLeft);
                    }
                    else if (now - nextTick > intervalTicks)
                    {
                        nextTick = now;   // fell behind — resync, do not burst to catch up
                    }
                }
            }
            catch (Exception ex) { if (!ct.IsCancellationRequested) Log("Control loop ended: " + ex.Message); }
        }

        // Replays the captured 23333 session token at ~10 Hz (matching the phone) with the byte[23]
        // counter advancing ~1/sec. This is the channel the phone runs and our app didn't — the leading
        // suspect for why the phone controls post-arm and we don't. Toggle via the "23333 session" box.
        async Task Session23333Loop(CancellationToken ct)
        {
            var pkt = (byte[])Session23333.Clone();
            byte ctr = Session23333[23];   // start at the captured counter value
            bool logged = false;
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    if (_send23333 && _udp23333 != null)
                    {
                        pkt[23] = ctr++;   // tick up by 1 EVERY packet (monotonic, wraps at 256) — not a 1/sec jump
                        try { _udp23333.Send(pkt); Tap(PacketDir.Tx, "UDP23333", pkt); if (!logged) { Log("23333 session: token replay, counter ticking per-packet"); logged = true; } } catch { }
                    }
                    await Task.Delay(100, ct);   // ~10 Hz
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { Log("23333 loop ended: " + ex.Message); }
        }

        // ============================================================
        // Packet builders  (see docs/PROTOCOL.md)
        // ============================================================
        static byte Xor(byte[] b, int start, int end)
        {
            byte r = 0;
            for (int i = start; i < end; i++) r ^= b[i];
            return r;
        }

        /// <summary>68 CC PL &lt;payload...&gt; XS  where XS = XOR(bytes[1..len-1)).</summary>
        static byte[] BuildInner(byte cmd, byte[] payload)
        {
            var p = new byte[3 + payload.Length + 1];
            p[0] = 0x68; p[1] = cmd; p[2] = (byte)payload.Length;
            Array.Copy(payload, 0, p, 3, payload.Length);
            p[^1] = Xor(p, 1, p.Length - 1);
            return p;
        }

        /// <summary>48 00 04 LL + inner.</summary>
        static byte[] WrapUdp(byte[] inner)
        {
            var w = new byte[4 + inner.Length];
            w[0] = 0x48; w[1] = 0x00; w[2] = 0x04; w[3] = (byte)inner.Length;
            Array.Copy(inner, 0, w, 4, inner.Length);
            return w;
        }

        // stick payload (13 bytes): roll pitch throttle yaw 20 18 00 [cmd6...]  (FlySendInfo order)
        //   byte2 = throttle  ← LIVE-CONFIRMED (the Pitch slider, wired to byte2, drove the motors)
        //   byte0/1/3 = roll/pitch/yaw per FlySendInfo, corroborated by the byte2=throttle hit
        //               (verify each on the bench: move one slider, watch which axis responds)
        //   bytes 4-6 = 20 18 00 (trim/mode constants from capture)
        //   bytes 7-12 = command region (gyro/compass/gimbal flags; payload[7] is the live one)
        static byte[] BuildStick(byte roll, byte pitch, byte throttle, byte yaw, byte[] tail6)
            => BuildInner(0x01, new byte[] { roll, pitch, throttle, yaw, 0x20, 0x18, 0x00,
                                             tail6[0], tail6[1], tail6[2], tail6[3], tail6[4], tail6[5] });

        // ping payload (15 bytes, 12 zeros + 1d 74 00) — matches capture: inner 68 0a 0f ... 6c
        static byte[] BuildPing()
            => BuildInner(0x0a, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0x1d, 0x74, 0x00 });

        /// <summary>88 07 P0 P1 P2 XS 99 with explicit payload bytes.</summary>
        static byte[] BuildGimbal(byte p0, byte p1, byte p2)
            => new byte[] { 0x88, 0x07, p0, p1, p2, (byte)(p0 ^ p1 ^ p2), 0x99 };

        /// <summary>88 07 0B &lt;tilt&gt; 1B XS 99 — P0/P2 fixed from capture; P1 = tilt.</summary>
        static byte[] BuildGimbal(byte p1) => BuildGimbal(0x0B, p1, 0x1B);

        // Mutates _stickBuf in-place using the current _tailBuf — zero allocations.
        // XOR covers _stickBuf[5..19] (inner bytes cmd through last payload byte).
        void UpdateStickBuf(byte roll, byte pitch, byte throttle, byte yaw)
        {
            _stickBuf[7]  = roll;
            _stickBuf[8]  = pitch;
            _stickBuf[9]  = throttle;
            _stickBuf[10] = yaw;
            _stickBuf[11] = _payload4;     // payload[4] — normally 0x20; overridable for Heliway probes
            _stickBuf[12] = _payload5;     // payload[5] — normally 0x18; lockUnlock lives here (§6a)
            _stickBuf[13] = _tailBuf[0];   // payload[6] — discrete command (takeoff/land/RTH bit)
            _stickBuf[14] = _tailBuf[1];
            _stickBuf[15] = _tailBuf[2];
            _stickBuf[16] = _tailBuf[3];
            _stickBuf[17] = _tailBuf[4];
            _stickBuf[18] = _tailBuf[5];
            _stickBuf[19] = _tailBuf[6];
            _stickBuf[20] = Xor(_stickBuf, 5, 20);
        }

        // ============================================================
        // RE packet tap — lets the workbench observe live traffic without touching the hot paths.
        // Read-only: we hand out a COPY so a subscriber can never mutate a send/recv buffer. No-op
        // (one null check) when nothing is subscribed, so the control loop pays ~nothing.
        // ============================================================
        public static event Action<PacketDir, string, byte[]>? PacketTap;
        static void Tap(PacketDir dir, string proto, byte[] data, int len = -1)
        {
            var h = PacketTap;
            if (h == null) return;
            int n = len < 0 ? data.Length : len;
            var copy = new byte[n];
            Array.Copy(data, copy, n);
            h(dir, proto, copy);
        }

        // ============================================================
        // Send helpers
        // ============================================================
        int _udpFailStreak;

        void SendRaw(byte[] data)
        {
            try { _udp?.Send(data); _udpFailStreak = 0; Tap(PacketDir.Tx, "UDP19798", data); }
            catch (Exception ex)
            {
                if (!_connected) return;
                if (++_udpFailStreak == 1) Log("UDP send error: " + ex.Message);
                if (_udpFailStreak >= 15)   // ~1s of failures at 18Hz → drone/WiFi is gone
                {
                    Log("Lost UDP link to drone — disconnecting. Reconnect once it's back.");
                    if (IsHandleCreated) BeginInvoke(() => Disconnect());
                }
            }
        }

        // The app sends each gimbal packet ~6× in quick succession; a single one may not register.
        void SendUdpBurst(byte[] pkt, int count = 6)
        {
            for (int i = 0; i < count; i++) SendRaw(pkt);
        }

        void SendGimbal(byte tilt)
        {
            var pkt = BuildGimbal(tilt);
            SendUdpBurst(pkt);
            Log("GIMBAL(88 07) x6 " + Hex(pkt));
        }

        bool SendJson(int cmd, string paramJson, bool log = true)
        {
            if (_jsonStream == null) { if (log) Log("JSON not connected."); return false; }
            try
            {
                string msg = $"{{\"CMD\":{cmd},\"PARAM\":{paramJson}}}";
                byte[] b = Encoding.ASCII.GetBytes(msg + "\n");   // capture shows commands are newline-terminated
                _jsonStream.Write(b, 0, b.Length);
                Tap(PacketDir.Tx, "TCP4646", b);
                if (log) Log($"JSON-> {msg}  ({CmdName(cmd)})");
                return true;
            }
            catch (Exception ex) { if (log) Log("JSON send error: " + ex.Message); return false; }
        }

        // Label for a TCP-4646 CMD id (docs/COMMANDS.md). Where the firmware (img_trans_app) and the APK
        // enum diverge, this uses the FIRMWARE name — what the drone actually does with the id. "(native)"
        // = null handler in the bridge (handled by libmain.so, replies "not supported" if probed directly).
        static string CmdName(int cmd) => cmd switch
        {
            0   => "SYS_PARAM_GET",        1   => "STATE_SET",
            4   => "VID_ENC_START",        5   => "VID_ENC_STOP",        11 => "VID_ENC_CAPTURE",
            29  => "DATE_TIME_SET",        56  => "DEL_FILE",            57 => "DEL_ALL_FILE",
            62  => "FORMAT(SD)",           63  => "CARD_INFO_GET",
            68  => "NET_SSID",             69  => "NET_PASSWORD",        71 => "FIRMWARE_UPDATE",
            75  => "CSI_SWITCH",           77  => "TRAIL",               78 => "BACKLIGHT_COMPENSATION",
            81  => "5G_REGION_SEL(WiFi band/region)",
            84  => "COMMIT",               85  => "UPDATE_STATUS",       87 => "MACADDR",
            88  => "GETDEVID",             89  => "BIND_APP_UDP(native)",
            91  => "GET_DRONE_CONF",       92  => "SET_DRONE_CONF",
            97  => "IMG_RC_CAPTURE/photo(native)",   98 => "VID_RC_START(native)",   99 => "VID_RC_STOP(native)",
            100 => "GET_VIDEO_STATUS",     105 => "AP_DISABLE",          106 => "AP_ENABLE",
            112 => "FLASH_CTRL(LED)",      116 => "RGB_LIGHTS_SET",
            126 => "CAMERA_RESO_GET",      127 => "CAMERA_RESO_SET ⚠POISON",
            135 => "RKNN_LIBS_READY",      136 => "RK_CPU_TEMP_REPORT",  137 => "SET_HY_LOGLEVEL",
            138 => "VIDEO_ENCODE_FORMAT",  140 => "BINOCULAR_PARAM",     141 => "TURN_OFF_PARAM",
            162 => "GET_WIFI_SIGNAL_CONF", 200 => "MAX",
            _   => $"CMD {cmd}",
        };

        // Sweep a CMD range with PARAM:-1 and report only the commands that answer (RESULT != -1).
        async void BtnSweep_Click(object? sender, EventArgs e)
        {
            if (_jsonStream == null) { Log("JSON not connected."); return; }
            if (_sweeping) { Log("Sweep already running."); return; }
            if (!int.TryParse(_txtSweepFrom.Text, out int from)) from = 0;
            if (!int.TryParse(_txtSweepTo.Text, out int to)) to = 200;
            if (to < from) (from, to) = (to, from);
            to = Math.Min(to, 1000);

            _sweeping = true;
            _btnSweep.Enabled = false;
            Log($"=== JSON sweep CMD {from}..{to} (PARAM -1). Only ★ answers shown. ===");
            try
            {
                int n = from;
                for (; n <= to; n++)
                {
                    if (!SendJson(n, "-1", log: false))
                    {
                        Log($"✗ JSON dropped while testing CMD {n} — a command near {n - 1}/{n} resets the drone's command server.");
                        Log($"   Click Disconnect → Connect, then run the sweep again (From is set to {n + 1} to skip past it).");
                        if (IsHandleCreated) BeginInvoke(() => _txtSweepFrom.Text = (n + 1).ToString());
                        break;
                    }
                    await Task.Delay(70);
                }
                if (n > to) await Task.Delay(400);   // completed normally; let the last replies land
            }
            finally
            {
                _sweeping = false;
                if (IsHandleCreated) BeginInvoke(() => _btnSweep.Enabled = true);
                Log("=== sweep done ===");
            }
        }

        void SendTimeSync()
        {
            var n = DateTime.Now;
            SendJson(29, $"{{\"YEAR\":{n.Year},\"MONTH\":{n.Month},\"DAY\":{n.Day}," +
                         $"\"HOUR\":{n.Hour},\"MINUTE\":{n.Minute},\"SECOND\":{n.Second}}}");
        }

        void OpenVideo()
        {
            // Exact URL recovered from the HS GPS V5 app (creds + stream path). H265 variant: /HYSymaH265VideoSMS
            string url = $"rtsp://hy_syma:ls155633@{_txtIp.Text.Trim()}:{RtspPort}/HYSymaH264VideoSMS";
            Log("Video URL: " + url + "  (opening external player)");
            // Try ffplay then VLC then default handler. User can copy the URL from the log too.
            foreach (var (exe, args) in new[]
            {
                ("ffplay", $"-fflags nobuffer -rtsp_transport tcp -i \"{url}\""),
                ("vlc", $"\"{url}\""),
            })
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(exe, args) { UseShellExecute = true });
                    return;
                }
                catch { /* try next */ }
            }
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true }); }
            catch (Exception ex) { Log("Could not launch a player: " + ex.Message + ". Copy the URL into VLC."); }
        }

        // ============================================================
        // Receive loops
        // ============================================================
        async Task UdpReceiveLoop(CancellationToken ct)
        {
            var buf = new byte[2048];
            try
            {
                while (!ct.IsCancellationRequested && _udp != null)
                {
                    int n = await _udp.ReceiveAsync(buf, SocketFlags.None, ct);
                    if (n <= 0) continue;
                    var frame = new byte[n];
                    Array.Copy(buf, frame, n);
                    Tap(PacketDir.Rx, "UDP19798", frame);
                    ParseTelemetry(frame);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { if (_connected) Log("UDP recv ended: " + ex.Message); }
        }

        async Task JsonReceiveLoop(CancellationToken ct)
        {
            var buf = new byte[4096];
            try
            {
                while (!ct.IsCancellationRequested && _jsonStream != null)
                {
                    int n = await _jsonStream.ReadAsync(buf, ct);
                    if (n <= 0) break;
                    Tap(PacketDir.Rx, "TCP4646", buf, n);
                    lock (_jsonBuf) { _jsonBuf.Append(Encoding.ASCII.GetString(buf, 0, n)); }
                    DrainJsonObjects();
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { if (_connected) Log("JSON recv ended: " + ex.Message); }
            finally
            {
                if (_connected && !ct.IsCancellationRequested && _jsonStream != null)
                {
                    _jsonStream = null;
                    Log("JSON channel closed by drone — click Disconnect then Connect to re-establish.");
                }
            }
        }

        // ============================================================
        // Telemetry parsing (58 8a model / 58 8b attitude+sats / 58 8c gps)
        // ============================================================
        void ParseTelemetry(byte[] f)
        {
            if (f.Length < 3 || f[0] != 0x58) { Log("RX " + Hex(f)); return; }
            switch (f[1])
            {
                case 0x8a:  // model id
                    if (f.Length >= 11)
                    {
                        string model = Encoding.ASCII.GetString(f, 3, 8).TrimEnd('\0', ' ');
                        _telModel = model;
                        _telModelText = "Model: " + model;
                    }
                    break;
                case 0x8b:  // attitude + GPS quality + positioning mode
                    // Syma app's own decoded log (2026-07-07) shows this frame carries: CalibProgress,
                    // BatVal (drone batt), LowBat, TempOver, CurrOver, Armed, FlyMode, GoHomeStatu,
                    // Takeoff, AutoLand, LandFinish, GpsNum, GpsFine, GpsQuality, mode_gps, RcFastMode.
                    // Byte offsets live in the native .so — TODO: pin each via a differential capture
                    // (vary one input, diff the frame). `Armed` 0->1 is the arm-timing oracle to chase.
                    if (f.Length > 14)
                    {
                        // sats CONFIRMED correct via foil test (2026-07-07): covering the drone drops
                        // sats 1-by-1 to 0. byte[14] is a PACKED field. Do NOT change this to byte[16] —
                        // that "sats=byte[16]" note was a retracted false number-match (byte[16] is a
                        // separate GPS-acquisition counter, not the sat count). See docs/STATUS_2026-07-07 §2.
                        // NOTE: only sats was validated by the foil test — fix is still an unconfirmed
                        // guess (STATUS_2026-07-07 §2: "fix:false bug — still OPEN"). See ATTENTION.md A1.
                        byte q = f[14];
                        int sats = (q >> 2) & 0x1f;
                        int fix = (q >> 7) & 1;
                        _telSats = sats; _telFix = fix;
                        _tel8bRaw = Hex(f);   // attitude/quality region — undecoded, surfaced raw in the HUD

                        // Attitude triplet f[3,4]=roll, f[5,6]=pitch, f[7,8]=yaw: signed int16 LE, /100 = degrees.
                        // Bench-confirmed 2026-07-13 (rotate N/E/S/W: yaw deltas closed to 360°; nose-up/down
                        // flipped pitch sign with mirrored magnitude, roll stayed near-zero).
                        _telRollRaw = BitConverter.ToInt16(f, 3);
                        _telPitchRaw = BitConverter.ToInt16(f, 5);
                        _telYawRaw = BitConverter.ToInt16(f, 7);
                        _telHasAttitude = true;
                        _telRollText = $"Roll: {(_telRollRaw / 100.0).ToString("F1", CultureInfo.InvariantCulture)}°";
                        _telPitchText = $"Pitch: {(_telPitchRaw / 100.0).ToString("F1", CultureInfo.InvariantCulture)}°";
                        _telYawText = $"Yaw: {(_telYawRaw / 100.0).ToString("F1", CultureInfo.InvariantCulture)}°";

                        // Battery: BatVal = byte[11]&0x7F, LowBat = 2-bit level (byte[11] bit7 | byte[12] bit0<<1).
                        // Confirmed 2026-07-07 by disassembling the official Syma AIR app's libmain.so
                        // (Java_org_libsdl_app_SDLActivity_nativeGetFlyReceiveData, 32-bit ARM EABI5 —
                        // real fn @ 0x40694, reached via the JNI thunk @ 0x3cac6; not ARM64/0x61b00 as
                        // previously cited) — matches the app's own FlyReceiveInfo field layout exactly.
                        // Masking off bit7
                        // resolves what looked like a nonlinear jump in bench sweeps: BatVal decreases
                        // linearly in steps of ~20 (80/60/40/20/0 across the 4/3/2/1/critical bar states).
                        byte battRaw = f[11];
                        int battVal = battRaw & 0x7f;
                        int lowBatLevel = (battRaw >> 7) | ((f[12] & 1) << 1);
                        int battBars = Math.Clamp(battVal / 20, 0, 4);
                        _telBattRaw = battVal; _telBattBars = battBars; _telLowBat = lowBatLevel != 0;
                        _telSatsText = $"Sats: {sats}   Fix: {(fix == 1 ? "yes" : "no")}";
                        _telBattText = $"Batt: {battBars} bar (val={battVal}, lowBatLvl={lowBatLevel})";
                    }
                    break;
                case 0x8c:  // gps coords  lon=int32LE@3, lat=int32LE@7
                    if (f.Length >= 11)
                    {
                        int lonRaw = BitConverter.ToInt32(f, 3), latRaw = BitConverter.ToInt32(f, 7);
                        _telLonRaw = lonRaw; _telLatRaw = latRaw; _telHasGps = true;
                        _tel8cRaw = Hex(f);   // tail bytes [11..] undecoded (altitude/speed?) — surfaced raw in the HUD
                        double lon = lonRaw / 1e7;
                        double lat = latRaw / 1e7;
                        _telGpsText = $"GPS: {lat.ToString("F6", CultureInfo.InvariantCulture)}, " +
                                      $"{lon.ToString("F6", CultureInfo.InvariantCulture)}";
                    }
                    break;
            }
            _telLastRaw = "58" + f[1].ToString("x2") + ": " + Hex(f);
            // Coalesced flush: one UI-thread hop per interval, no matter how fast frames arrive.
            long nowMs = Environment.TickCount64;
            if (nowMs - _lastTelFlushTick >= _telUiIntervalMs)
            {
                _lastTelFlushTick = nowMs;
                Ui(FlushTelemetryLabels);
            }
        }

        // Push the latest cached telemetry strings to their labels in ONE UI-thread hop.
        // Null = that field hasn't been seen yet, so leave its label at the "—" placeholder.
        void FlushTelemetryLabels()
        {
            if (_telModelText != null) _lblModel.Text = _telModelText;
            if (_telSatsText != null) _lblSats.Text = _telSatsText;
            if (_telBattText != null) _lblBattery.Text = _telBattText;
            if (_telGpsText != null) _lblGps.Text = _telGpsText;
            if (_telYawText != null) _lblYaw.Text = _telYawText;
            if (_telRollText != null) _lblRoll.Text = _telRollText;
            if (_telPitchText != null) _lblPitch.Text = _telPitchText;
            if (_telLastRaw != null) _lblRaw.Text = "last " + _telLastRaw;
            if (_telHasAttitude) { _dialRoll.Invalidate(); _dialPitch.Invalidate(); _dialYaw.Invalidate(); }
        }

        // Small rotating-needle dial next to each attitude readout — needle points in the direction
        // of the current angle (0° = up), so the eye can read "which way" at a glance.
        static void PaintDial(Graphics g, Size size, double degrees)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            float cx = size.Width / 2f, cy = size.Height / 2f;
            float r = Math.Min(cx, cy) - 1f;
            using var ring = new Pen(Theme.Border, 1f);
            g.DrawEllipse(ring, cx - r, cy - r, r * 2, r * 2);
            g.TranslateTransform(cx, cy);
            g.RotateTransform((float)degrees);
            using var needle = new SolidBrush(Theme.Accent);
            g.FillPolygon(needle, new PointF[] { new(0, -r), new(-r * 0.3f, r * 0.6f), new(r * 0.3f, r * 0.6f) });
            g.ResetTransform();
        }

        void DialRoll_Paint(object? sender, PaintEventArgs e) => PaintDial(e.Graphics, _dialRoll.Size, _telHasAttitude ? _telRollRaw / 100.0 : 0);
        void DialPitch_Paint(object? sender, PaintEventArgs e) => PaintDial(e.Graphics, _dialPitch.Size, _telHasAttitude ? _telPitchRaw / 100.0 : 0);
        void DialYaw_Paint(object? sender, PaintEventArgs e) => PaintDial(e.Graphics, _dialYaw.Size, _telHasAttitude ? _telYawRaw / 100.0 : 0);

        // grpTel "UI rate" combo: how often telemetry labels refresh. Slower = less UI-thread contention
        // with the video render while streaming. Data decode/logging is unaffected — only the label push.
        void cboTelRate_Changed(object? sender, EventArgs e)
        {
            _telUiIntervalMs = cboTelRate.SelectedIndex switch
            {
                1 => 200,    // 5 Hz
                2 => 500,    // 2 Hz
                3 => 1000,   // 1 Hz
                _ => 100,    // 10 Hz
            };
        }

        // Pull complete top-level {...} objects out of the reassembly buffer (TCP may split/merge them).
        void DrainJsonObjects()
        {
            while (true)
            {
                string obj;
                lock (_jsonBuf)
                {
                    string b = _jsonBuf.ToString();
                    int start = b.IndexOf('{');
                    if (start < 0) { _jsonBuf.Clear(); return; }
                    int depth = 0, end = -1;
                    for (int i = start; i < b.Length; i++)
                    {
                        if (b[i] == '{') depth++;
                        else if (b[i] == '}' && --depth == 0) { end = i; break; }
                    }
                    if (end < 0) { if (start > 0) _jsonBuf.Remove(0, start); return; }  // wait for more
                    obj = b.Substring(start, end - start + 1);
                    _jsonBuf.Remove(0, end + 1);
                }
                ProcessJsonObject(obj);
            }
        }

        void ProcessJsonObject(string obj)
        {
            ExtractFirmware(obj);
            if (_sweeping)
            {
                int? result = JsonInt(obj, "RESULT");
                if (result is int r && r != -1)
                    Log($"   ★ CMD {JsonInt(obj, "CMD")} ANSWERED: {obj}");
                return;   // suppress the -1 / noise during a sweep
            }
            Log("JSON<- " + obj);
        }

        // First integer value for "key" (handles negatives); ignores nested duplicates beyond the first.
        static int? JsonInt(string s, string key)
        {
            int i = s.IndexOf("\"" + key + "\":", StringComparison.Ordinal);
            if (i < 0) return null;
            i += key.Length + 3;
            int j = i;
            if (j < s.Length && (s[j] == '-' || s[j] == '+')) j++;
            while (j < s.Length && char.IsDigit(s[j])) j++;
            return j > i && int.TryParse(s.AsSpan(i, j - i), out int v) ? v : null;
        }

        void ExtractFirmware(string s)
        {
            int i = s.IndexOf("\"FirmWare\":\"", StringComparison.Ordinal);
            if (i < 0) return;
            i += 12;
            int j = s.IndexOf('"', i);
            if (j > i) Ui(() => _lblFirmware.Text = "Firmware: " + s.Substring(i, j - i));
        }

        // ============================================================
        // Helpers
        // ============================================================
        static string Hex(byte[] b) => Convert.ToHexString(b);

        void SetStatus(string text, Color c) { _lblStatus.Text = text; _lblStatus.ForeColor = c; }

        void Ui(Action a)
        {
            if (!IsHandleCreated) return;
            try { BeginInvoke(a); } catch { }
        }

        void Log(string msg)
        {
            string line = $"{DateTime.Now:HH:mm:ss.fff}  {msg}";
            DebugBus.Log(line);   // mirror into the HTTP /log buffer
            if (!IsHandleCreated) return;
            try
            {
                BeginInvoke(() =>
                {
                    if (_txtLog.TextLength > 60000) _txtLog.Clear();
                    _txtLog.AppendText(line + "\r\n");
                });
            }
            catch { }
        }

        // ============================================================
        // Windows multimedia timer — sets system timer resolution to 1ms so
        // Thread.Sleep is accurate enough for the control-loop timing strategy.
        // ============================================================
        [DllImport("winmm.dll")] static extern uint timeBeginPeriod(uint uPeriod);
        [DllImport("winmm.dll")] static extern uint timeEndPeriod(uint uPeriod);
    }
}

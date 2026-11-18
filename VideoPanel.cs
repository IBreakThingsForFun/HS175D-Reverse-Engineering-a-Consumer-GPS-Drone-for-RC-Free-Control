using System.Net;
using System.Net.Sockets;

namespace HS175D
{
    /// <summary>
    /// Embedded RTSP viewer for the drone camera. The feed is H.264 (or H.265) RTP interleaved over
    /// TCP on port 19200; URL + creds recovered from the HS GPS V5 app (see Form1.OpenVideo /
    /// docs/PROTOCOL.md §1). Rendered in-app via the <b>mpv</b> engine (FFmpeg-based, low latency) —
    /// its native libmpv-2.dll ships with the Endpne.LibMPV.Windows package and is embedded straight
    /// into <c>_videoHost</c> via mpv's <c>wid</c> option (see <see cref="MpvClient"/>). mpv replaced
    /// LibVLC here because it tracks the live feed with far less delay and cleaner picture — matching
    /// the phone apps — and reconnects/switches codecs without the single-client hangs VLC had.
    ///
    /// The toolbar controls (play/stop/snap/codec/scale/probe/status) are real Designer controls in
    /// <c>VideoPanel.Designer.cs</c>. The mpv player is created lazily on first Play (it needs the
    /// host panel's live HWND) and lives for the control's lifetime; if libmpv fails to load, the
    /// panel degrades to a message.
    /// </summary>
    public sealed partial class VideoPanel : UserControl
    {
        const int RtspPort = 19200;
        const string RtspUser = "hy_syma";
        const string RtspPass = "ls155633";

        Func<string>? _ipProvider;
        Action<int, string>? _cmdSender;   // set by the host → sends a {"CMD":n,"PARAM":p} on TCP 4646
        MpvClient? _mpv;
        bool _runtimeReady;
        bool _lowLat;                      // camera mode toggle: false = 2.7K@25 (default), true = 2K_L@50
        int _csi;                          // current CMD 75 csi_num (0/1/2) — camera source
        System.Windows.Forms.Timer? _flushTimer;   // latency watchdog (drop-to-live)
        double _lastTimePos = double.NaN;          // stall watchdog: last seen playback position
        long _lastTimePosChangeMs;                 // Environment.TickCount64 when it last moved
        long _lastReconnectMs;                     // Environment.TickCount64 of the last (re)connect
        bool _reconnecting;
        long _proactiveReconnectMs = 60_000;       // configurable via _reconnectInterval; 60s confirmed the sweet spot in flight testing
        const long StallToleranceMs = 4_000;       // backup: reconnect immediately if it stalls sooner

        NpuDetector? _npu;
        DetectionOverlay? _overlay;
        System.Windows.Forms.Timer? _aiTimer;      // drives periodic NPU detection, independent of video framerate

        // The ONLY thing the ctor does is build the Designer controls, so this drops cleanly onto
        // Form1's surface and shows its buttons AT DESIGN TIME with zero side effects — no theming,
        // no mpv, no errors. All runtime wiring is deferred to SetIpProvider (below), which the
        // designer never calls. _videoHost stays a plain black placeholder panel until then.
        public VideoPanel()
        {
            InitializeComponent();
        }

        // Called once at RUNTIME by the host (Form1 / ReWorkbench). Supplies the LIVE Drone IP so
        // Play/Probe follow whatever IP is set in the host window (e.g. 10.9.1.1 for the AirCard
        // bridge), and lazily brings up theming. Because only runtime code calls this, the native
        // libs are never touched at design time. mpv itself is created on first Play (below).
        public void SetIpProvider(Func<string> ipProvider)
        {
            _ipProvider = ipProvider;
            if (_runtimeReady) return;
            _runtimeReady = true;
            Disposed += (s, e) => Cleanup();   // release mpv — dropped in the Designer refactor

            Dock = DockStyle.Fill;
            BackColor = Theme.Bg1;

            Theme.Apply(this);                                  // recolor the Designer controls
            // Accent the Play button + mute the status label (special cases Theme.Apply won't infer).
            btnPlay.BackColor = Theme.AccentDim; btnPlay.ForeColor = Color.White;
            btnPlay.FlatAppearance.BorderColor = Theme.Accent;
            btnPlay.FlatAppearance.MouseOverBackColor = Theme.Accent;
            _status.ForeColor = Theme.Muted;
            _videoHost.BackColor = Color.Black;

            // Latency watchdog — polls the demuxer cache and drops-to-live when it builds up (see FlushWatchdog_Tick).
            _flushTimer = new System.Windows.Forms.Timer { Interval = 500 };
            _flushTimer.Tick += FlushWatchdog_Tick;
            _flushTimer.Start();

            // NPU bounding-box detection — separate timer so its cadence never touches video playback.
            _npu = new NpuDetector();
            _overlay = new DetectionOverlay();
            _aiTimer = new System.Windows.Forms.Timer { Interval = (int)(_aiInterval.Value * 1000) };
            _aiTimer.Tick += AiTimer_Tick;
            _videoHost.Resize += (s, e) => { if (_overlay != null && _overlay.Visible) _overlay.ShowOver(_videoHost); };
        }

        // Called once at RUNTIME by the host to supply the TCP-4646 JSON command sender (Form1.SendJson),
        // so the video panel can issue camera commands (e.g. the 2K low-latency mode) on the live session.
        public void SetCmdSender(Action<int, string> sender) => _cmdSender = sender;

        // ── event handlers (wired in VideoPanel.Designer.cs) ─────────────────────────────
        void btnPlay_Click(object? sender, EventArgs e) => Play();
        void btnStop_Click(object? sender, EventArgs e)
        {
            try { _mpv?.Stop(); SetStatus("stopped", Theme.Muted); } catch { }
            _aiTimer?.Stop();
            _overlay?.Hide();
        }
        void btnSnap_Click(object? sender, EventArgs e) => Snapshot();
        void btnProbe_Click(object? sender, EventArgs e) => ProbeUdp1563();
        void scaleMode_Changed(object? sender, EventArgs e) => ApplyScaleMode();

        // Toggle periodic NPU bounding-box detection on the live feed. Needs a live stream to make
        // sense (there's nothing to overlay boxes on otherwise), so it hard-guards on _mpv being up,
        // same pattern as btnLowLat_Click/btnCam_Click.
        void chkAiDetect_CheckedChanged(object? sender, EventArgs e)
        {
            if (_chkAiDetect.Checked)
            {
                if (_mpv == null) { _chkAiDetect.Checked = false; SetStatus("hit ▶ Play first — AI Detect needs a live stream", Theme.Warn); return; }
                _overlay?.ShowOver(_videoHost);
                _aiTimer?.Start();
            }
            else
            {
                _aiTimer?.Stop();
                _overlay?.Hide();
            }
        }

        void aiInterval_ValueChanged(object? sender, EventArgs e)
        {
            if (_aiTimer != null) _aiTimer.Interval = Math.Max(500, (int)(_aiInterval.Value * 1000));
        }

        void reconnectInterval_ValueChanged(object? sender, EventArgs e)
        {
            _proactiveReconnectMs = (long)_reconnectInterval.Value * 1000;
        }

        // Re-push and start the on-device NPU stack. Needed once after every drone reboot/power
        // cycle since it all lives in tmpfs — see NpuBringup for the full sequence. Safe to click
        // any time; it no-ops if the detector's already running.
        async void btnStartNpu_Click(object? sender, EventArgs e)
        {
            btnStartNpu.Enabled = false;
            try
            {
                // onStatus already reports the specific stage/failure reason as it goes — don't
                // stomp that with a generic message; only add one on success.
                bool ok = await NpuBringup.BringUpAsync(Ip(), msg => SetStatus(msg, Theme.Accent));
                if (ok) SetStatus("NPU stack ready — check AI Detect to start", Theme.Good);
            }
            finally { btnStartNpu.Enabled = true; }
        }

        // One detect cycle per tick — never overlaps (NpuDetector.Busy guards that), and never
        // blocks the UI thread or the video: it's a fire-and-forget async cycle that only ever
        // updates the overlay's box list when it finishes.
        async void AiTimer_Tick(object? sender, EventArgs e)
        {
            if (_mpv == null || _npu == null || _overlay == null || _npu.Busy) return;
            var detections = await _npu.CaptureAndDetectAsync(_mpv, Ip());
            _overlay.SetDetections(detections, _scaleMode.SelectedIndex == 1);
        }

        // Toggle the camera between 2.7K@25 (default) and 2K_L@50 (low-latency) via CMD 127.
        // CMD 127 is ONLY safe while a stream is already live: the running session re-keyframes into the new
        // mode, exactly like toggling it in the phone app. Sent cold (no active RTSP session) it wedges the
        // drone's video until a reboot — so we hard-guard on _mpv being up and never reload.
        void btnLowLat_Click(object? sender, EventArgs e)
        {
            if (_mpv == null) { SetStatus("hit ▶ Play first — sending CMD 127 with no live stream wedges the drone", Theme.Warn); return; }
            if (_cmdSender == null) { SetStatus("no command channel — connect in the main window first", Theme.Warn); return; }
            _lowLat = !_lowLat;
            _cmdSender(127, _lowLat ? "{\"camera\":0,\"reso\":\"2K_L\",\"fps\":50}"
                                    : "{\"camera\":0,\"reso\":\"2.7K\",\"fps\":25}");
            btnLowLat.Text = _lowLat ? "⚡ → 2.7K@25" : "⚡ → 2K@50";
            SetStatus(_lowLat ? "switched to 2K_L@50 (low-latency)" : "switched to 2.7K@25 (default)", Theme.Accent);
        }

        // Switch camera source (CMD 75 CSI_SWITCH). The wire value is csi_num ∈ {0,1,2} (from the Syma app's
        // PERSPECTIVE → "csi_num" payload); the front/bottom mapping isn't documented for this unit, so we
        // cycle 0→1→2 and let the operator stop on the one they want. Guarded live like CMD 127 to be safe.
        void btnCam_Click(object? sender, EventArgs e)
        {
            if (_mpv == null) { SetStatus("hit ▶ Play first, then switch camera", Theme.Warn); return; }
            if (_cmdSender == null) { SetStatus("no command channel — connect in the main window first", Theme.Warn); return; }
            _csi = (_csi + 1) % 3;
            _cmdSender(75, $"{{\"csi_num\":{_csi}}}");
            btnCam.Text = $"Cam csi:{_csi}";
            SetStatus($"switched camera → csi_num {_csi}", Theme.Accent);
        }

        // Latency watchdog: every tick, if the demuxer has buffered more than ~0.5 s ahead (we're falling
        // behind the live edge), drop all buffers and jump to "now" instead of playing catch-up. Keeps the
        // feed near real-time after a network/decode hiccup. Toggled by the _chkFlush checkbox.
        void FlushWatchdog_Tick(object? sender, EventArgs e)
        {
            if (_mpv == null) return;

            // This drone's RTSP server (OnDemandRTSPServer) hard-caps sessions at ~60-70s
            // regardless of activity — confirmed separately via a plain ffmpeg capture hitting
            // the identical ~60-75s pattern. adb/AI-detect traffic keeps working fine through it,
            // so it's just the RTSP session dying server-side, not the drone itself.
            //
            // For flying, a reactive "notice the freeze, then reconnect" isn't good enough — any
            // visible stall at all is a real problem mid-flight. So reconnect PROACTIVELY on a
            // timer comfortably under the observed cap, before the server ever tears the session
            // down. The stall check below stays only as a backup in case something drops sooner
            // than that for an unrelated reason (WiFi hiccup, etc.).
            if (!_reconnecting)
            {
                long now = Environment.TickCount64;
                bool dueForProactiveRefresh = now - _lastReconnectMs > _proactiveReconnectMs;
                bool stalled = false;

                double pos = _mpv.GetDouble("time-pos");
                if (!double.IsNaN(pos))
                {
                    if (double.IsNaN(_lastTimePos) || Math.Abs(pos - _lastTimePos) > 0.01)
                    {
                        _lastTimePos = pos;
                        _lastTimePosChangeMs = now;
                    }
                    else if (now - _lastTimePosChangeMs > StallToleranceMs)
                    {
                        stalled = true;
                    }
                }

                if (dueForProactiveRefresh || stalled)
                {
                    _reconnecting = true;
                    SetStatus(stalled ? "stream stalled — reconnecting..." : "refreshing RTSP session before it can time out...", Theme.Accent);
                    Play();
                    _reconnecting = false;
                }
            }

            if (!_chkFlush.Checked) return;
            double buffered = _mpv.GetDouble("demuxer-cache-duration");   // seconds queued ahead of playback
            if (!double.IsNaN(buffered) && buffered > 0.5)
            {
                _mpv.DropBuffers();
                SetStatus($"latency {buffered:F2}s → flushed to live", Theme.Warn);
            }
        }

        // Resolve the Drone IP fresh each time (falls back to the drone's factory AP IP).
        string Ip()
        {
            string v = _ipProvider?.Invoke() ?? "";
            return string.IsNullOrWhiteSpace(v) ? "192.168.100.1" : v.Trim();
        }

        string Url() => _codec.SelectedIndex == 1
            ? $"rtsp://{RtspUser}:{RtspPass}@{Ip()}:{RtspPort}/HYSymaH265VideoSMS"
            : $"rtsp://{RtspUser}:{RtspPass}@{Ip()}:{RtspPort}/HYSymaH264VideoSMS";

        void Play()
        {
            try
            {
                // Create mpv lazily on first Play — _videoHost.Handle is guaranteed live by now and
                // becomes mpv's render target (wid). loadfile replaces any current stream, so it also
                // cleanly closes the old RTSP session when reconnecting or switching H.264<->H.265
                // (the drone's RTSP server is single-client and used to hang under LibVLC on this).
                _mpv ??= new MpvClient(_videoHost.Handle);
                _mpv.Load(Url());
                ApplyScaleMode();
                SetStatus("playing " + Url(), Theme.Good);
                if (_chkAiDetect.Checked) { _overlay?.ShowOver(_videoHost); _aiTimer?.Start(); }
                _lastReconnectMs = Environment.TickCount64;
                _lastTimePos = double.NaN;
            }
            catch (Exception ex)
            {
                SetStatus("play failed: " + ex.Message, Theme.Danger);
                if (_mpv == null)
                    _videoHost.Controls.Add(new Label
                    {
                        Dock = DockStyle.Fill, ForeColor = Theme.Danger, BackColor = Color.Black,
                        TextAlign = ContentAlignment.MiddleCenter, Font = Theme.Mono,
                        Text = "mpv (libmpv) failed to load:\r\n" + ex.Message + "\r\n\r\nUse the main window's 'Open Video' (external player) instead."
                    });
            }
        }

        // Fit = preserve source aspect (letterbox). Fill = crop the source to fill the whole box with
        // no black bars and no stretching — mpv's panscan does exactly this (1.0 = fully cropped-to-fill).
        void ApplyScaleMode()
        {
            try { _mpv?.SetProperty("panscan", _scaleMode.SelectedIndex == 1 ? "1.0" : "0.0"); }
            catch { /* best-effort */ }
        }

        void Snapshot()
        {
            if (_mpv == null) { SetStatus("snapshot failed (is it playing?)", Theme.Warn); return; }
            try
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "HS175D");
                Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, $"snap_{DateTime.Now:yyyyMMdd_HHmmss}.png");
                _mpv.Command("screenshot-to-file", path, "video");   // "video" = raw frame, no OSD
                SetStatus("saved " + path, Theme.Accent);
            }
            catch (Exception ex) { SetStatus("snapshot error: " + ex.Message, Theme.Danger); }
        }

        // Exploratory: the sibling hs260 streams raw H.264 NAL over UDP 1563 after a D8 C0 D9 handshake.
        // Our PROTOCOL.md §1 saw d8c0d9 on :1563 but dismissed it. This sends the handshake and dumps
        // whatever comes back (Annex-B start codes prepended) to a .h264 file mpv can open — proving or
        // closing the 2nd video path before any decoder work. Does NOT touch the working RTSP player.
        void ProbeUdp1563()
        {
            SetStatus("probing UDP 1563 …", Theme.Accent);
            new Thread(() =>
            {
                UdpClient? udp = null;
                try
                {
                    udp = new UdpClient();
                    udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    udp.Client.Bind(new IPEndPoint(IPAddress.Any, 1563));
                    udp.Connect(Ip(), 1563);
                    udp.Client.ReceiveTimeout = 1000;
                    udp.Send(new byte[] { 0xD8, 0xC0, 0xD9 }, 3);

                    string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "HS175D");
                    Directory.CreateDirectory(dir);
                    string path = Path.Combine(dir, $"udp1563_{DateTime.Now:yyyyMMdd_HHmmss}.h264");
                    int packets = 0; long bytes = 0; string first = "";
                    byte[] startCode = { 0x00, 0x00, 0x00, 0x01 };

                    using (var fs = new FileStream(path, FileMode.Create))
                    {
                        long deadline = Environment.TickCount64 + 5000;   // capture ~5 s
                        var from = new IPEndPoint(IPAddress.Any, 0);
                        while (Environment.TickCount64 < deadline)
                        {
                            byte[] data;
                            try { data = udp.Receive(ref from); }
                            catch (SocketException) { continue; }   // timeout — keep waiting until deadline
                            if (data.Length == 0) continue;
                            if (packets == 0)
                            {
                                var sb = new System.Text.StringBuilder();
                                for (int i = 0; i < Math.Min(16, data.Length); i++) sb.Append(data[i].ToString("x2")).Append(' ');
                                first = sb.ToString().TrimEnd();
                            }
                            packets++; bytes += data.Length;
                            fs.Write(startCode, 0, 4);                 // re-frame to Annex-B for the decoder
                            fs.Write(data, 0, data.Length);
                        }
                    }

                    if (packets == 0) { File.Delete(path); SetStatus("UDP 1563: no data (lead closed / drone not streaming there)", Theme.Warn); }
                    else SetStatus($"UDP 1563: {packets} pkts / {bytes} B → {path}  (first: {first})", Theme.Good);
                }
                catch (Exception ex) { SetStatus("UDP 1563 probe failed: " + ex.Message, Theme.Danger); }
                finally { try { udp?.Dispose(); } catch { } }
            })
            { IsBackground = true }.Start();
        }

        void SetStatus(string t, Color c)
        {
            if (!IsHandleCreated) { _status.Text = t; _status.ForeColor = c; return; }
            try { BeginInvoke(() => { _status.Text = t; _status.ForeColor = c; }); } catch { }
        }

        void Cleanup()
        {
            try { _flushTimer?.Stop(); _flushTimer?.Dispose(); } catch { }
            _flushTimer = null;
            try { _aiTimer?.Stop(); _aiTimer?.Dispose(); } catch { }
            _aiTimer = null;
            try { _overlay?.Close(); _overlay?.Dispose(); } catch { }
            _overlay = null;
            _npu = null;
            try { _mpv?.Dispose(); } catch { }
            _mpv = null;
        }
    }
}

namespace HS175D
{
    partial class VideoPanel
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Cleanup();                       // release the mpv player before the control goes away
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            _bar = new Panel();
            btnPlay = new Button();
            btnStop = new Button();
            btnSnap = new Button();
            _codec = new ComboBox();
            _scaleMode = new ComboBox();
            btnProbe = new Button();
            btnLowLat = new Button();
            _chkFlush = new CheckBox();
            btnCam = new Button();
            _status = new Label();
            _chkAiDetect = new CheckBox();
            _lblAiInterval = new Label();
            _aiInterval = new NumericUpDown();
            btnStartNpu = new Button();
            _lblReconnectInterval = new Label();
            _reconnectInterval = new NumericUpDown();
            _videoHost = new Panel();
            _tip = new ToolTip(components);
            _bar.SuspendLayout();
            SuspendLayout();
            //
            // _bar
            //
            _bar.Controls.Add(btnPlay);
            _bar.Controls.Add(btnStop);
            _bar.Controls.Add(btnSnap);
            _bar.Controls.Add(_codec);
            _bar.Controls.Add(_scaleMode);
            _bar.Controls.Add(btnProbe);
            _bar.Controls.Add(btnLowLat);
            _bar.Controls.Add(_chkFlush);
            _bar.Controls.Add(btnCam);
            _bar.Controls.Add(_status);
            _bar.Controls.Add(_chkAiDetect);
            _bar.Controls.Add(_lblAiInterval);
            _bar.Controls.Add(_aiInterval);
            _bar.Controls.Add(btnStartNpu);
            _bar.Controls.Add(_lblReconnectInterval);
            _bar.Controls.Add(_reconnectInterval);
            _bar.Dock = DockStyle.Top;
            _bar.Name = "_bar";
            _bar.Size = new Size(448, 142);
            //
            // btnPlay
            //
            btnPlay.Location = new Point(8, 6);
            btnPlay.Name = "btnPlay";
            btnPlay.Size = new Size(70, 26);
            btnPlay.Text = "▶ Play";
            btnPlay.Click += btnPlay_Click;
            //
            // btnStop
            //
            btnStop.Location = new Point(82, 6);
            btnStop.Name = "btnStop";
            btnStop.Size = new Size(62, 26);
            btnStop.Text = "■ Stop";
            btnStop.Click += btnStop_Click;
            //
            // btnSnap
            //
            btnSnap.Location = new Point(148, 6);
            btnSnap.Name = "btnSnap";
            btnSnap.Size = new Size(34, 26);
            btnSnap.Text = "📷";
            btnSnap.Click += btnSnap_Click;
            //
            // _codec
            //
            _codec.DropDownStyle = ComboBoxStyle.DropDownList;
            _codec.Items.AddRange(new object[] { "H.264  /HYSymaH264VideoSMS", "H.265  /HYSymaH265VideoSMS" });
            _codec.Location = new Point(186, 7);
            _codec.Name = "_codec";
            _codec.Size = new Size(188, 24);
            _codec.SelectedIndex = 0;
            //
            // _scaleMode
            //
            _scaleMode.DropDownStyle = ComboBoxStyle.DropDownList;
            _scaleMode.Items.AddRange(new object[] { "Fit", "Fill" });
            _scaleMode.Location = new Point(378, 7);
            _scaleMode.Name = "_scaleMode";
            _scaleMode.Size = new Size(62, 24);
            _scaleMode.SelectedIndex = 0;
            _scaleMode.SelectedIndexChanged += scaleMode_Changed;
            //
            // btnProbe
            //
            btnProbe.Location = new Point(8, 32);
            btnProbe.Name = "btnProbe";
            btnProbe.Size = new Size(130, 26);
            btnProbe.Text = "Probe 1563 (raw)";
            btnProbe.Click += btnProbe_Click;
            //
            // btnLowLat  (toggles 2.7K@25 ⇄ 2K_L@50 — text shows the mode a tap switches TO)
            //
            btnLowLat.Location = new Point(144, 32);
            btnLowLat.Name = "btnLowLat";
            btnLowLat.Size = new Size(140, 26);
            btnLowLat.Text = "⚡ → 2K@50";
            btnLowLat.Click += btnLowLat_Click;
            //
            // _chkFlush  (latency watchdog — drop buffered frames and jump to live when delay builds)
            //
            _chkFlush.Location = new Point(8, 62);
            _chkFlush.Name = "_chkFlush";
            _chkFlush.Size = new Size(300, 20);
            _chkFlush.Text = "Auto-flush when latency builds (skip to live)";
            _chkFlush.Checked = true;
            //
            // btnCam  (CMD 75 CSI_SWITCH — cycles csi_num 0→1→2 so you can find front vs bottom on this unit)
            //
            btnCam.Location = new Point(312, 60);
            btnCam.Name = "btnCam";
            btnCam.Size = new Size(128, 24);
            btnCam.Text = "Cam csi:0";
            btnCam.Click += btnCam_Click;
            //
            // _chkAiDetect  (enable/disable NPU bounding-box detection on the live feed)
            //
            _chkAiDetect.Location = new Point(8, 90);
            _chkAiDetect.Name = "_chkAiDetect";
            _chkAiDetect.Size = new Size(160, 20);
            _chkAiDetect.Text = "AI Detect (NPU)";
            _chkAiDetect.CheckedChanged += chkAiDetect_CheckedChanged;
            //
            // _lblAiInterval
            //
            _lblAiInterval.AutoSize = true;
            _lblAiInterval.Location = new Point(174, 92);
            _lblAiInterval.Name = "_lblAiInterval";
            _lblAiInterval.Size = new Size(70, 15);
            _lblAiInterval.Text = "interval (s):";
            //
            // _aiInterval  (seconds between detection cycles — decoupled from live video framerate)
            //
            _aiInterval.DecimalPlaces = 1;
            _aiInterval.Increment = new decimal(new int[] { 5, 0, 0, 65536 });   // 0.5
            _aiInterval.Minimum = new decimal(new int[] { 5, 0, 0, 65536 });     // 0.5
            _aiInterval.Maximum = new decimal(new int[] { 10, 0, 0, 0 });        // 10
            _aiInterval.Value = new decimal(new int[] { 2, 0, 0, 0 });          // 2.0 default
            _aiInterval.Location = new Point(248, 89);
            _aiInterval.Name = "_aiInterval";
            _aiInterval.Size = new Size(60, 23);
            _aiInterval.ValueChanged += aiInterval_ValueChanged;
            //
            // btnStartNpu  (re-pushes + starts the on-device NPU stack — needed after every drone
            // reboot/power-cycle, since it all lives in tmpfs and is wiped on power loss)
            //
            btnStartNpu.Location = new Point(316, 88);
            btnStartNpu.Name = "btnStartNpu";
            btnStartNpu.Size = new Size(124, 24);
            btnStartNpu.Text = "⚙ Start NPU";
            btnStartNpu.Click += btnStartNpu_Click;
            //
            // _lblReconnectInterval
            //
            _lblReconnectInterval.AutoSize = true;
            _lblReconnectInterval.Location = new Point(8, 122);
            _lblReconnectInterval.Name = "_lblReconnectInterval";
            _lblReconnectInterval.Size = new Size(140, 15);
            _lblReconnectInterval.Text = "Video reconnect (s):";
            //
            // _reconnectInterval  (proactive RTSP reconnect — fires before the drone's own
            // ~60-70s session cap so the video panel refreshes itself instead of freezing)
            //
            _reconnectInterval.Minimum = new decimal(new int[] { 15, 0, 0, 0 });
            _reconnectInterval.Maximum = new decimal(new int[] { 120, 0, 0, 0 });
            _reconnectInterval.Value = new decimal(new int[] { 60, 0, 0, 0 });   // sweet spot confirmed in flight testing — 65/70 still showed a delay
            _reconnectInterval.Location = new Point(154, 119);
            _reconnectInterval.Name = "_reconnectInterval";
            _reconnectInterval.Size = new Size(60, 23);
            _reconnectInterval.ValueChanged += reconnectInterval_ValueChanged;
            //
            // _status
            //
            _status.AutoEllipsis = true;
            _status.Location = new Point(290, 36);
            _status.Name = "_status";
            _status.Size = new Size(154, 18);
            _status.Text = "idle";
            //
            // _videoHost
            //
            _videoHost.BackColor = Color.Black;
            _videoHost.Dock = DockStyle.Fill;
            _videoHost.Name = "_videoHost";
            _videoHost.Size = new Size(448, 240);
            //
            // tooltips
            //
            _tip.SetToolTip(btnPlay, "Start playing the drone's RTSP video stream.");
            _tip.SetToolTip(btnStop, "Stop playback.");
            _tip.SetToolTip(btnSnap, "Snapshot → Pictures\\HS175D");
            _tip.SetToolTip(_scaleMode, "Fit = letterbox (whole frame). Fill = crop to fill the box.");
            _tip.SetToolTip(_codec, "Which RTSP stream path to request (H.264 or H.265).");
            _tip.SetToolTip(btnProbe, "Probe the raw UDP-1563 video-transport candidate directly (bypasses RTSP).");
            _tip.SetToolTip(btnLowLat, "While the stream is LIVE, toggle the camera between 2.7K@25 and 2K_L@50 (CMD 127). Sending it live is safe; sending it with no stream up wedges the drone's video until reboot. Text shows the mode a tap switches TO.");
            _tip.SetToolTip(_chkFlush, "When buffered latency exceeds ~0.5 s, drop the demuxer/decoder buffers and jump to the live edge instead of playing catch-up (mpv 'drop-buffers'). Keeps the feed near real-time.");
            _tip.SetToolTip(btnCam, "Switch camera source (CMD 75 CSI_SWITCH). Cycles csi_num 0→1→2 — click while streaming until you get the front/main camera. The number shows the current csi_num sent.");
            _tip.SetToolTip(_chkAiDetect, "Periodically grab a frame, run it through the drone's on-device NPU object detector, and draw bounding boxes over the live video. The live feed itself is never delayed by this — only the boxes update on the interval below.");
            _tip.SetToolTip(_aiInterval, "Seconds between NPU detection cycles (0.5–10). Lower = fresher boxes but more load; the live video framerate is unaffected either way.");
            _tip.SetToolTip(btnStartNpu, "Re-push and start the on-device NPU stack (kernel module, OpenVX libs, detector). Needed once after every drone reboot/power-cycle — it all lives in tmpfs and is wiped on power loss. Safe to click even if it's already running.");
            _tip.SetToolTip(_reconnectInterval, "Seconds between proactive video reconnects. The drone's RTSP server hard-caps sessions at roughly 60-70s regardless of activity, so this refreshes the connection before that happens — set it comfortably under that, or a bit over it if you're relying on the reactive stall-catch as backup instead.");
            //
            // VideoPanel
            //
            // This control's toolbar is laid out with absolute pixel Locations (not anchored/docked
            // per-button), so it must never go through WinForms' font/DPI auto-scale pass - without
            // an explicit baseline it inherits Form1's scale factor against an undefined comparison,
            // which skews the toolbar at runtime even though the Designer surface (not DPI-aware the
            // same way) renders it correctly.
            AutoScaleMode = AutoScaleMode.None;
            Controls.Add(_videoHost);
            Controls.Add(_bar);
            Name = "VideoPanel";
            Size = new Size(448, 300);
            _bar.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Panel _bar;
        private Button btnPlay;
        private Button btnStop;
        private Button btnSnap;
        private ComboBox _codec;
        private ComboBox _scaleMode;
        private Button btnProbe;
        private Button btnLowLat;
        private CheckBox _chkFlush;
        private Button btnCam;
        private Label _status;
        private CheckBox _chkAiDetect;
        private Label _lblAiInterval;
        private NumericUpDown _aiInterval;
        private Button btnStartNpu;
        private Label _lblReconnectInterval;
        private NumericUpDown _reconnectInterval;
        private Panel _videoHost;
        private ToolTip _tip;
    }
}

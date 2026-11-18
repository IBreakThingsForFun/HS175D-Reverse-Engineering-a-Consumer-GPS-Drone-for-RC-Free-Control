namespace HS175D
{
    partial class ReWorkbench
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private TabControl _tabs;

        // ---- toolbar ----
        private Panel _barToolbar;
        private Label _lblTitle;
        private Label _lblSub;
        private Button _btnConsole;
        private Button _btnSwd;
        private Button _btnWifi;

        // ---- tabs ----
        private TabPage _tabVideo;
        private TabPage _tabMonitor;
        private TabPage _tabInject;
        private TabPage _tabRamp;
        private TabPage _tabGps;
        private TabPage _tabReplay;
        private TabPage _tabTelemetry;
        private TabPage _tabEsp32;
        private TabPage _tabNrf24;

        // ---- monitor ----
        private RichTextBox _mon;
        private Panel _pnlMonTop;
        private CheckBox _fTx;
        private CheckBox _fRx;
        private CheckBox _fHideStream;
        private CheckBox _fHideSession;
        private CheckBox _fPause;
        private Button _btnMonClear;
        private Label _lblMonLegend;
        private Label _lblMonHelp;

        // ---- inject ----
        private Panel _pnlInject;
        private Label _lblInjHelp;
        private Label _lblInjTarget;
        private TextBox _injIp;
        private Label _lblInjPort;
        private TextBox _injPort;
        private Label _lblInjChecksum;
        private ComboBox _injCksum;
        private Label _lblInjWireBytes;
        private TextBox _injHex;
        private Button _btnInjXor;
        private Button _btnInjWrap;
        private Label _lblInjStickBuilder;
        private NumericUpDown _injR;
        private NumericUpDown _injP;
        private NumericUpDown _injT;
        private NumericUpDown _injY;
        private Button _btnInjStick;
        private Label _lblInjCount;
        private NumericUpDown _injCount;
        private Button _btnInjSend;
        private Label _lblInjReplyCaption;
        private TextBox _injReply;

        // ---- ramp ----
        private Panel _pnlRamp;
        private Label _lblRampHelp;
        private Label _lblRampHint1;
        private Label _lblRampHint2;
        private Label _lblRampStart;
        private NumericUpDown _rStart;
        private Label _lblRampTarget;
        private NumericUpDown _rTarget;
        private Label _lblRampStep;
        private NumericUpDown _rStep;
        private Label _lblRampDelay;
        private NumericUpDown _rDelay;
        private Label _lblRampPerStep;
        private NumericUpDown _rPerStep;
        private Label _lblRampHold;
        private NumericUpDown _rHold;
        private Button _btnRamp;
        private Button _btnRampStop;
        private TextBox _rStatus;

        // ---- gps ----
        private Panel _pnlGps;
        private Label _lblGpsHelp;
        private Label _lblGpsHint1;
        private Label _lblGpsHint2;
        private Label _lblGpsMode;
        private ComboBox _gMode;
        private Label _lblGpsCmdId;
        private TextBox _gCmdId;
        private Label _lblGpsLat;
        private Label _lblGpsLon;
        private TextBox _gLat;
        private TextBox _gLon;
        private Label _lblGpsAlt;
        private Label _lblGpsSpeed;
        private Label _lblGpsRadius;
        private TextBox _gAlt;
        private TextBox _gSpeed;
        private TextBox _gRadius;
        private Button _btnGpsBuild;
        private Button _btnGpsSend;
        private Label _lblGpsWireBytes;
        private TextBox _gHex;
        private TextBox _gReply;

        // ---- replay ----
        private Panel _pnlReplay;
        private Label _lblReplayHelp;
        private Label _lblReplayHint;
        private Button _btnCapStart;
        private Button _btnCapStop;
        private Label _capLbl;
        private Button _btnCapSave;
        private Button _btnCapLoad;
        private Label _replayLbl;
        private Label _lblReplaySpeed;
        private NumericUpDown _numReplayRate;
        private CheckBox _chkTxOnly;
        private Button _btnReplayGo;
        private Button _btnReplayStop;
        private System.Windows.Forms.Timer _capRefreshTimer;

        // ---- telemetry ----
        private Panel _pnlTelemetry;
        private Label _lblTelModelCap, _lblTelSatsCap, _lblTelFixCap, _lblTelGpsCap;
        private Label _lblTelBatCap, _lblTelAltCap, _lblTelMotorsCap, _lblTelStateCap, _lblTelRawCap;
        private Label _tModel, _tSats, _tFix, _tGps, _tRaw;
        private Label _tBattery, _tAlt, _tMotors, _tState;
        private Label _lblTelHint;
        private Label _lblTelHelp;
        private Label _lblTelSubtypes;
        private ListBox _subList;

        // ---- rc-link (nrf24) tab ----
        private Label _lblNrf24Help;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            _tabs = new TabControl();
            _barToolbar = new Panel();
            _lblTitle = new Label();
            _lblSub = new Label();
            _btnConsole = new Button();
            _btnSwd = new Button();
            _btnWifi = new Button();
            _tabVideo = new TabPage();
            _tabMonitor = new TabPage();
            _mon = new RichTextBox();
            _pnlMonTop = new Panel();
            _fTx = new CheckBox();
            _fRx = new CheckBox();
            _fHideStream = new CheckBox();
            _fHideSession = new CheckBox();
            _fPause = new CheckBox();
            _btnMonClear = new Button();
            _lblMonLegend = new Label();
            _lblMonHelp = new Label();
            _tabInject = new TabPage();
            _pnlInject = new Panel();
            _lblInjHelp = new Label();
            _lblInjTarget = new Label();
            _injIp = new TextBox();
            _lblInjPort = new Label();
            _injPort = new TextBox();
            _lblInjChecksum = new Label();
            _injCksum = new ComboBox();
            _lblInjWireBytes = new Label();
            _injHex = new TextBox();
            _btnInjXor = new Button();
            _btnInjWrap = new Button();
            _lblInjStickBuilder = new Label();
            _injR = new NumericUpDown();
            _injP = new NumericUpDown();
            _injT = new NumericUpDown();
            _injY = new NumericUpDown();
            _btnInjStick = new Button();
            _lblInjCount = new Label();
            _injCount = new NumericUpDown();
            _btnInjSend = new Button();
            _lblInjReplyCaption = new Label();
            _injReply = new TextBox();
            _tabRamp = new TabPage();
            _pnlRamp = new Panel();
            _lblRampHelp = new Label();
            _lblRampHint1 = new Label();
            _lblRampHint2 = new Label();
            _lblRampStart = new Label();
            _rStart = new NumericUpDown();
            _lblRampTarget = new Label();
            _rTarget = new NumericUpDown();
            _lblRampStep = new Label();
            _rStep = new NumericUpDown();
            _lblRampDelay = new Label();
            _rDelay = new NumericUpDown();
            _lblRampPerStep = new Label();
            _rPerStep = new NumericUpDown();
            _lblRampHold = new Label();
            _rHold = new NumericUpDown();
            _btnRamp = new Button();
            _btnRampStop = new Button();
            _rStatus = new TextBox();
            _tabGps = new TabPage();
            _pnlGps = new Panel();
            _lblGpsHelp = new Label();
            _lblGpsHint1 = new Label();
            _lblGpsHint2 = new Label();
            _lblGpsMode = new Label();
            _gMode = new ComboBox();
            _lblGpsCmdId = new Label();
            _gCmdId = new TextBox();
            _lblGpsLat = new Label();
            _lblGpsLon = new Label();
            _gLat = new TextBox();
            _gLon = new TextBox();
            _lblGpsAlt = new Label();
            _lblGpsSpeed = new Label();
            _lblGpsRadius = new Label();
            _gAlt = new TextBox();
            _gSpeed = new TextBox();
            _gRadius = new TextBox();
            _btnGpsBuild = new Button();
            _btnGpsSend = new Button();
            _lblGpsWireBytes = new Label();
            _gHex = new TextBox();
            _gReply = new TextBox();
            _tabReplay = new TabPage();
            _pnlReplay = new Panel();
            _lblReplayHelp = new Label();
            _lblReplayHint = new Label();
            _btnCapStart = new Button();
            _btnCapStop = new Button();
            _capLbl = new Label();
            _btnCapSave = new Button();
            _btnCapLoad = new Button();
            _replayLbl = new Label();
            _lblReplaySpeed = new Label();
            _numReplayRate = new NumericUpDown();
            _chkTxOnly = new CheckBox();
            _btnReplayGo = new Button();
            _btnReplayStop = new Button();
            _capRefreshTimer = new System.Windows.Forms.Timer(components);
            _tabTelemetry = new TabPage();
            _pnlTelemetry = new Panel();
            _lblTelModelCap = new Label();
            _tModel = new Label();
            _lblTelSatsCap = new Label();
            _tSats = new Label();
            _lblTelFixCap = new Label();
            _tFix = new Label();
            _lblTelGpsCap = new Label();
            _tGps = new Label();
            _lblTelBatCap = new Label();
            _tBattery = new Label();
            _lblTelAltCap = new Label();
            _tAlt = new Label();
            _lblTelMotorsCap = new Label();
            _tMotors = new Label();
            _lblTelStateCap = new Label();
            _tState = new Label();
            _lblTelRawCap = new Label();
            _tRaw = new Label();
            _lblTelHint = new Label();
            _lblTelHelp = new Label();
            _lblTelSubtypes = new Label();
            _subList = new ListBox();
            _tabEsp32 = new TabPage();
            _tabNrf24 = new TabPage();
            _lblNrf24Help = new Label();

            ((System.ComponentModel.ISupportInitialize)_injR).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_injP).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_injT).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_injY).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_injCount).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_rStart).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_rTarget).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_rStep).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_rDelay).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_rPerStep).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_rHold).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_numReplayRate).BeginInit();
            _tabs.SuspendLayout();
            _barToolbar.SuspendLayout();
            _tabMonitor.SuspendLayout();
            _pnlMonTop.SuspendLayout();
            _tabInject.SuspendLayout();
            _pnlInject.SuspendLayout();
            _tabRamp.SuspendLayout();
            _pnlRamp.SuspendLayout();
            _tabGps.SuspendLayout();
            _pnlGps.SuspendLayout();
            _tabReplay.SuspendLayout();
            _pnlReplay.SuspendLayout();
            _tabTelemetry.SuspendLayout();
            _pnlTelemetry.SuspendLayout();
            SuspendLayout();
            //
            // _tabs
            //
            _tabs.Controls.Add(_tabVideo);
            _tabs.Controls.Add(_tabMonitor);
            _tabs.Controls.Add(_tabInject);
            _tabs.Controls.Add(_tabRamp);
            _tabs.Controls.Add(_tabGps);
            _tabs.Controls.Add(_tabReplay);
            _tabs.Controls.Add(_tabTelemetry);
            _tabs.Controls.Add(_tabEsp32);
            _tabs.Controls.Add(_tabNrf24);
            _tabs.Dock = DockStyle.Fill;
            _tabs.Location = new Point(0, 0);
            _tabs.Name = "_tabs";
            _tabs.SelectedIndex = 0;
            _tabs.Size = new Size(1040, 720);
            _tabs.TabIndex = 0;
            //
            // _barToolbar
            //
            _barToolbar.Controls.Add(_lblTitle);
            _barToolbar.Controls.Add(_lblSub);
            _barToolbar.Controls.Add(_btnConsole);
            _barToolbar.Controls.Add(_btnSwd);
            _barToolbar.Controls.Add(_btnWifi);
            _barToolbar.Dock = DockStyle.Top;
            _barToolbar.Location = new Point(0, 0);
            _barToolbar.Name = "_barToolbar";
            _barToolbar.Size = new Size(1040, 34);
            _barToolbar.TabIndex = 1;
            _barToolbar.Resize += BarToolbar_Resize;
            //
            // _lblTitle
            //
            _lblTitle.AutoSize = true;
            _lblTitle.Font = new Font("Consolas", 10F, FontStyle.Bold);
            _lblTitle.ForeColor = Color.FromArgb(34, 211, 238);
            _lblTitle.Location = new Point(10, 9);
            _lblTitle.Name = "_lblTitle";
            _lblTitle.Size = new Size(160, 17);
            _lblTitle.TabIndex = 0;
            _lblTitle.Text = "◢ MISSION CONTROL";
            //
            // _lblSub
            //
            _lblSub.AutoSize = true;
            _lblSub.ForeColor = Color.Gray;
            _lblSub.Location = new Point(180, 11);
            _lblSub.Name = "_lblSub";
            _lblSub.Size = new Size(40, 15);
            _lblSub.TabIndex = 1;
            _lblSub.Text = "drone";
            //
            // _btnConsole
            //
            _btnConsole.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _btnConsole.Location = new Point(852, 4);
            _btnConsole.Name = "_btnConsole";
            _btnConsole.Size = new Size(180, 26);
            _btnConsole.TabIndex = 2;
            _btnConsole.Text = "Drone Console (FTP/adb)";
            _btnConsole.Click += BtnConsole_Click;
            //
            // _btnSwd
            //
            _btnSwd.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _btnSwd.Location = new Point(660, 4);
            _btnSwd.Name = "_btnSwd";
            _btnSwd.Size = new Size(184, 26);
            _btnSwd.TabIndex = 3;
            _btnSwd.Text = "SWD: Hold Optical ▷";
            _btnSwd.Click += BtnSwd_Click;
            //
            // _btnWifi
            //
            _btnWifi.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _btnWifi.Location = new Point(582, 4);
            _btnWifi.Name = "_btnWifi";
            _btnWifi.Size = new Size(70, 26);
            _btnWifi.TabIndex = 4;
            _btnWifi.Text = "WiFi";
            _btnWifi.Click += BtnWifi_Click;
            //
            // _tabVideo
            //
            _tabVideo.Location = new Point(4, 24);
            _tabVideo.Name = "_tabVideo";
            _tabVideo.Size = new Size(1032, 692);
            _tabVideo.TabIndex = 0;
            _tabVideo.Text = "Video";
            //
            // _tabMonitor
            //
            _tabMonitor.Controls.Add(_mon);
            _tabMonitor.Controls.Add(_pnlMonTop);
            _tabMonitor.Location = new Point(4, 24);
            _tabMonitor.Name = "_tabMonitor";
            _tabMonitor.Size = new Size(1032, 692);
            _tabMonitor.TabIndex = 1;
            _tabMonitor.Text = "Monitor";
            //
            // _mon
            //
            _mon.BackColor = Color.FromArgb(55, 62, 71);
            _mon.BorderStyle = BorderStyle.None;
            _mon.Dock = DockStyle.Fill;
            _mon.Font = new Font("Consolas", 8.25F);
            _mon.ForeColor = Color.FromArgb(201, 209, 217);
            _mon.Location = new Point(0, 92);
            _mon.Name = "_mon";
            _mon.ReadOnly = true;
            _mon.Size = new Size(1032, 600);
            _mon.TabIndex = 1;
            _mon.Text = "";
            _mon.WordWrap = false;
            //
            // _pnlMonTop
            //
            _pnlMonTop.Controls.Add(_fTx);
            _pnlMonTop.Controls.Add(_fRx);
            _pnlMonTop.Controls.Add(_fHideStream);
            _pnlMonTop.Controls.Add(_fHideSession);
            _pnlMonTop.Controls.Add(_fPause);
            _pnlMonTop.Controls.Add(_btnMonClear);
            _pnlMonTop.Controls.Add(_lblMonLegend);
            _pnlMonTop.Controls.Add(_lblMonHelp);
            _pnlMonTop.Dock = DockStyle.Top;
            _pnlMonTop.Location = new Point(0, 0);
            _pnlMonTop.Name = "_pnlMonTop";
            _pnlMonTop.Size = new Size(1032, 92);
            _pnlMonTop.TabIndex = 0;
            //
            // _fTx
            //
            _fTx.AutoSize = true;
            _fTx.Checked = true;
            _fTx.CheckState = CheckState.Checked;
            _fTx.Location = new Point(10, 9);
            _fTx.Name = "_fTx";
            _fTx.Size = new Size(41, 19);
            _fTx.TabIndex = 0;
            _fTx.Text = "TX";
            //
            // _fRx
            //
            _fRx.AutoSize = true;
            _fRx.Checked = true;
            _fRx.CheckState = CheckState.Checked;
            _fRx.Location = new Point(58, 9);
            _fRx.Name = "_fRx";
            _fRx.Size = new Size(41, 19);
            _fRx.TabIndex = 1;
            _fRx.Text = "RX";
            //
            // _fHideStream
            //
            _fHideStream.AutoSize = true;
            _fHideStream.Location = new Point(110, 9);
            _fHideStream.Name = "_fHideStream";
            _fHideStream.Size = new Size(103, 19);
            _fHideStream.TabIndex = 2;
            _fHideStream.Text = "hide stick/ping";
            //
            // _fHideSession
            //
            _fHideSession.AutoSize = true;
            _fHideSession.Location = new Point(240, 9);
            _fHideSession.Name = "_fHideSession";
            _fHideSession.Size = new Size(85, 19);
            _fHideSession.TabIndex = 3;
            _fHideSession.Text = "hide 23333";
            //
            // _fPause
            //
            _fPause.AutoSize = true;
            _fPause.Location = new Point(350, 9);
            _fPause.Name = "_fPause";
            _fPause.Size = new Size(58, 19);
            _fPause.TabIndex = 4;
            _fPause.Text = "pause";
            //
            // _btnMonClear
            //
            _btnMonClear.Location = new Point(430, 6);
            _btnMonClear.Name = "_btnMonClear";
            _btnMonClear.Size = new Size(70, 24);
            _btnMonClear.TabIndex = 5;
            _btnMonClear.Text = "Clear";
            _btnMonClear.Click += BtnMonClear_Click;
            //
            // _lblMonLegend
            //
            _lblMonLegend.AutoSize = true;
            _lblMonLegend.Font = new Font("Consolas", 8.25F);
            _lblMonLegend.ForeColor = Color.Gray;
            _lblMonLegend.Location = new Point(520, 10);
            _lblMonLegend.Name = "_lblMonLegend";
            _lblMonLegend.Size = new Size(343, 13);
            _lblMonLegend.TabIndex = 6;
            _lblMonLegend.Text = "48/68 ctrl   58 telem   52 session   88 gimbal   tcp json";
            //
            // _lblMonHelp
            //
            _lblMonHelp.ForeColor = Color.Gray;
            _lblMonHelp.Location = new Point(10, 32);
            _lblMonHelp.Name = "_lblMonHelp";
            _lblMonHelp.Size = new Size(1010, 56);
            _lblMonHelp.TabIndex = 7;
            _lblMonHelp.Text = "Every WiFi packet this app sends (TX) or receives (RX) from the drone, decoded live as it happens — the raw-truth view behind every other tab. \"hide stick/ping\" drops the repetitive ~45-60Hz neutral-stick frames and the periodic ping so real traffic stands out; \"hide 23333\" drops the ~10Hz session-keepalive heartbeat, which only needs to be present, not read. Pause freezes the view without stopping capture; Clear empties it. See docs/PROTOCOL.md and docs/METHODS_PLAYBOOK.md §3.1 for the full frame reference behind the legend above.";
            //
            // _tabInject
            //
            _tabInject.Controls.Add(_pnlInject);
            _tabInject.Location = new Point(4, 24);
            _tabInject.Name = "_tabInject";
            _tabInject.Size = new Size(1032, 692);
            _tabInject.TabIndex = 2;
            _tabInject.Text = "Inject";
            //
            // _pnlInject
            //
            _pnlInject.Controls.Add(_lblInjTarget);
            _pnlInject.Controls.Add(_injIp);
            _pnlInject.Controls.Add(_lblInjPort);
            _pnlInject.Controls.Add(_injPort);
            _pnlInject.Controls.Add(_lblInjChecksum);
            _pnlInject.Controls.Add(_injCksum);
            _pnlInject.Controls.Add(_lblInjWireBytes);
            _pnlInject.Controls.Add(_injHex);
            _pnlInject.Controls.Add(_btnInjXor);
            _pnlInject.Controls.Add(_btnInjWrap);
            _pnlInject.Controls.Add(_lblInjStickBuilder);
            _pnlInject.Controls.Add(_injR);
            _pnlInject.Controls.Add(_injP);
            _pnlInject.Controls.Add(_injT);
            _pnlInject.Controls.Add(_injY);
            _pnlInject.Controls.Add(_btnInjStick);
            _pnlInject.Controls.Add(_lblInjCount);
            _pnlInject.Controls.Add(_injCount);
            _pnlInject.Controls.Add(_btnInjSend);
            _pnlInject.Controls.Add(_lblInjReplyCaption);
            _pnlInject.Controls.Add(_injReply);
            _pnlInject.Controls.Add(_lblInjHelp);
            _pnlInject.Dock = DockStyle.Fill;
            _pnlInject.Location = new Point(0, 0);
            _pnlInject.Name = "_pnlInject";
            _pnlInject.Padding = new Padding(12);
            _pnlInject.Size = new Size(1032, 692);
            _pnlInject.TabIndex = 0;
            //
            // _lblInjTarget
            //
            _lblInjTarget.AutoSize = true;
            _lblInjTarget.ForeColor = Color.FromArgb(34, 211, 238);
            _lblInjTarget.Location = new Point(12, 14);
            _lblInjTarget.Name = "_lblInjTarget";
            _lblInjTarget.Size = new Size(41, 15);
            _lblInjTarget.TabIndex = 0;
            _lblInjTarget.Text = "Target";
            //
            // _injIp
            //
            _injIp.Font = new Font("Consolas", 9F);
            _injIp.Location = new Point(70, 11);
            _injIp.Name = "_injIp";
            _injIp.Size = new Size(130, 23);
            _injIp.TabIndex = 1;
            //
            // _lblInjPort
            //
            _lblInjPort.AutoSize = true;
            _lblInjPort.Location = new Point(210, 14);
            _lblInjPort.Name = "_lblInjPort";
            _lblInjPort.Size = new Size(28, 15);
            _lblInjPort.TabIndex = 2;
            _lblInjPort.Text = "port";
            //
            // _injPort
            //
            _injPort.Font = new Font("Consolas", 9F);
            _injPort.Location = new Point(244, 11);
            _injPort.Name = "_injPort";
            _injPort.Size = new Size(60, 23);
            _injPort.TabIndex = 3;
            _injPort.Text = "19798";
            //
            // _lblInjChecksum
            //
            _lblInjChecksum.AutoSize = true;
            _lblInjChecksum.Location = new Point(320, 14);
            _lblInjChecksum.Name = "_lblInjChecksum";
            _lblInjChecksum.Size = new Size(60, 15);
            _lblInjChecksum.TabIndex = 4;
            _lblInjChecksum.Text = "checksum";
            //
            // _injCksum
            //
            _injCksum.DropDownStyle = ComboBoxStyle.DropDownList;
            _injCksum.Items.AddRange(new object[] { "XOR inner[1..n-1)", "Additive sum", "None" });
            _injCksum.Location = new Point(390, 11);
            _injCksum.Name = "_injCksum";
            _injCksum.Size = new Size(170, 23);
            _injCksum.TabIndex = 5;
            //
            // _lblInjWireBytes
            //
            _lblInjWireBytes.AutoSize = true;
            _lblInjWireBytes.ForeColor = Color.Gray;
            _lblInjWireBytes.Location = new Point(12, 48);
            _lblInjWireBytes.Name = "_lblInjWireBytes";
            _lblInjWireBytes.Size = new Size(420, 15);
            _lblInjWireBytes.TabIndex = 6;
            _lblInjWireBytes.Text = "Wire bytes (hex) — sent literally; use the builders below to prepare it:";
            //
            // _injHex
            //
            _injHex.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _injHex.Font = new Font("Consolas", 9F);
            _injHex.Location = new Point(12, 70);
            _injHex.Multiline = true;
            _injHex.Name = "_injHex";
            _injHex.ScrollBars = ScrollBars.Both;
            _injHex.Size = new Size(720, 90);
            _injHex.TabIndex = 7;
            _injHex.Text = "48 00 04 11 68 01 0d 80 80 00 80 20 18 00 00 00 00 00 00 00 34";
            //
            // _btnInjXor
            //
            _btnInjXor.Location = new Point(12, 168);
            _btnInjXor.Name = "_btnInjXor";
            _btnInjXor.Size = new Size(130, 26);
            _btnInjXor.TabIndex = 8;
            _btnInjXor.Text = "Append checksum";
            _btnInjXor.Click += BtnInjXor_Click;
            //
            // _btnInjWrap
            //
            _btnInjWrap.Location = new Point(148, 168);
            _btnInjWrap.Name = "_btnInjWrap";
            _btnInjWrap.Size = new Size(120, 26);
            _btnInjWrap.TabIndex = 9;
            _btnInjWrap.Text = "Wrap 48 00 04";
            _btnInjWrap.Click += BtnInjWrap_Click;
            //
            // _lblInjStickBuilder
            //
            _lblInjStickBuilder.AutoSize = true;
            _lblInjStickBuilder.ForeColor = Color.FromArgb(34, 211, 238);
            _lblInjStickBuilder.Location = new Point(12, 206);
            _lblInjStickBuilder.Name = "_lblInjStickBuilder";
            _lblInjStickBuilder.Size = new Size(640, 15);
            _lblInjStickBuilder.TabIndex = 10;
            _lblInjStickBuilder.Text = "Stick builder (roll / pitch / throttle / yaw, 0–255 — all self-center at 128; throttle >128 climb, <128 descend):";
            //
            // _injR
            //
            _injR.Location = new Point(12, 228);
            _injR.Maximum = new decimal(new int[] { 255, 0, 0, 0 });
            _injR.Name = "_injR";
            _injR.Size = new Size(60, 23);
            _injR.TabIndex = 11;
            _injR.Value = new decimal(new int[] { 128, 0, 0, 0 });
            //
            // _injP
            //
            _injP.Location = new Point(78, 228);
            _injP.Maximum = new decimal(new int[] { 255, 0, 0, 0 });
            _injP.Name = "_injP";
            _injP.Size = new Size(60, 23);
            _injP.TabIndex = 12;
            _injP.Value = new decimal(new int[] { 128, 0, 0, 0 });
            //
            // _injT
            //
            _injT.Location = new Point(144, 228);
            _injT.Maximum = new decimal(new int[] { 255, 0, 0, 0 });
            _injT.Name = "_injT";
            _injT.Size = new Size(60, 23);
            _injT.TabIndex = 13;
            _injT.Value = new decimal(new int[] { 128, 0, 0, 0 });
            //
            // _injY
            //
            _injY.Location = new Point(210, 228);
            _injY.Maximum = new decimal(new int[] { 255, 0, 0, 0 });
            _injY.Name = "_injY";
            _injY.Size = new Size(60, 23);
            _injY.TabIndex = 14;
            _injY.Value = new decimal(new int[] { 128, 0, 0, 0 });
            //
            // _btnInjStick
            //
            _btnInjStick.Location = new Point(280, 227);
            _btnInjStick.Name = "_btnInjStick";
            _btnInjStick.Size = new Size(140, 26);
            _btnInjStick.TabIndex = 15;
            _btnInjStick.Text = "Build stick → hex";
            _btnInjStick.Click += BtnInjStick_Click;
            //
            // _lblInjCount
            //
            _lblInjCount.AutoSize = true;
            _lblInjCount.Location = new Point(12, 268);
            _lblInjCount.Name = "_lblInjCount";
            _lblInjCount.Size = new Size(38, 15);
            _lblInjCount.TabIndex = 16;
            _lblInjCount.Text = "count";
            //
            // _injCount
            //
            _injCount.Location = new Point(58, 265);
            _injCount.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            _injCount.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            _injCount.Name = "_injCount";
            _injCount.Size = new Size(60, 23);
            _injCount.TabIndex = 17;
            _injCount.Value = new decimal(new int[] { 1, 0, 0, 0 });
            //
            // _btnInjSend
            //
            _btnInjSend.BackColor = Color.FromArgb(206, 240, 206);
            _btnInjSend.ForeColor = Color.FromArgb(20, 40, 20);
            _btnInjSend.Location = new Point(130, 264);
            _btnInjSend.Name = "_btnInjSend";
            _btnInjSend.Size = new Size(110, 28);
            _btnInjSend.TabIndex = 18;
            _btnInjSend.Text = "▶ SEND";
            _btnInjSend.UseVisualStyleBackColor = false;
            _btnInjSend.Click += BtnInjSend_Click;
            //
            // _lblInjReplyCaption
            //
            _lblInjReplyCaption.AutoSize = true;
            _lblInjReplyCaption.ForeColor = Color.Gray;
            _lblInjReplyCaption.Location = new Point(12, 304);
            _lblInjReplyCaption.Name = "_lblInjReplyCaption";
            _lblInjReplyCaption.Size = new Size(500, 15);
            _lblInjReplyCaption.TabIndex = 19;
            _lblInjReplyCaption.Text = "Reply (≤500 ms; note: drone telemetry usually returns to the app's :9961, not here):";
            //
            // _injReply
            //
            _injReply.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _injReply.Font = new Font("Consolas", 9F);
            _injReply.Location = new Point(12, 326);
            _injReply.Multiline = true;
            _injReply.Name = "_injReply";
            _injReply.ReadOnly = true;
            _injReply.ScrollBars = ScrollBars.Both;
            _injReply.Size = new Size(720, 90);
            _injReply.TabIndex = 20;
            //
            // _lblInjHelp
            //
            _lblInjHelp.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            _lblInjHelp.ForeColor = Color.Gray;
            _lblInjHelp.Location = new Point(752, 14);
            _lblInjHelp.Name = "_lblInjHelp";
            _lblInjHelp.Size = new Size(268, 640);
            _lblInjHelp.TabIndex = 21;
            _lblInjHelp.Text = "Hand-craft and send ONE packet directly to the drone (or anywhere else) — the lowest-level tool here.\r\n\r\nTarget/port: where it goes. 19798 = the stick/control port, 4646 = the JSON command port.\r\n\r\nWire bytes: the exact payload, in hex, sent byte for byte. \"Append checksum\" computes and appends the frame's checksum (pick XOR or additive from the dropdown first — the 48/68/58 WiFi family uses XOR, the internal MCU/optical-flow frames use additive, see docs/PROTOCOL.md §6). \"Wrap 48 00 04\" prepends the stick-frame wrapper so you don't have to hand-count lengths.\r\n\r\nStick builder: four roll/pitch/throttle/yaw values (0–255, all self-center at 128; throttle >128 climbs, <128 descends) — set the sliders, click \"Build stick → hex\" instead of hand-editing bytes.\r\n\r\ncount repeats the same send that many times. SEND fires it; Reply shows whatever answers THIS socket within 500ms — drone telemetry usually answers the main app's :9961 listener instead, so a blank Reply doesn't mean nothing happened. Check the Monitor tab.\r\n\r\nSee docs/PROTOCOL.md §2 for the frame layout this is built from.";
            //
            // _tabRamp
            //
            _tabRamp.Controls.Add(_pnlRamp);
            _tabRamp.Location = new Point(4, 24);
            _tabRamp.Name = "_tabRamp";
            _tabRamp.Size = new Size(1032, 692);
            _tabRamp.TabIndex = 3;
            _tabRamp.Text = "Ramp";
            //
            // _pnlRamp
            //
            _pnlRamp.Controls.Add(_lblRampHint1);
            _pnlRamp.Controls.Add(_lblRampHint2);
            _pnlRamp.Controls.Add(_lblRampStart);
            _pnlRamp.Controls.Add(_rStart);
            _pnlRamp.Controls.Add(_lblRampTarget);
            _pnlRamp.Controls.Add(_rTarget);
            _pnlRamp.Controls.Add(_lblRampStep);
            _pnlRamp.Controls.Add(_rStep);
            _pnlRamp.Controls.Add(_lblRampDelay);
            _pnlRamp.Controls.Add(_rDelay);
            _pnlRamp.Controls.Add(_lblRampPerStep);
            _pnlRamp.Controls.Add(_rPerStep);
            _pnlRamp.Controls.Add(_lblRampHold);
            _pnlRamp.Controls.Add(_rHold);
            _pnlRamp.Controls.Add(_btnRamp);
            _pnlRamp.Controls.Add(_btnRampStop);
            _pnlRamp.Controls.Add(_rStatus);
            _pnlRamp.Controls.Add(_lblRampHelp);
            _pnlRamp.Dock = DockStyle.Fill;
            _pnlRamp.Location = new Point(0, 0);
            _pnlRamp.Name = "_pnlRamp";
            _pnlRamp.Padding = new Padding(12);
            _pnlRamp.Size = new Size(1032, 692);
            _pnlRamp.TabIndex = 0;
            //
            // _lblRampHint1
            //
            _lblRampHint1.AutoSize = true;
            _lblRampHint1.ForeColor = Color.Gray;
            _lblRampHint1.Location = new Point(12, 10);
            _lblRampHint1.Name = "_lblRampHint1";
            _lblRampHint1.Size = new Size(700, 15);
            _lblRampHint1.TabIndex = 0;
            _lblRampHint1.Text = "Throttle ramp — drives the main window's single control loop (one coherent stream: 23333 keepalive + :9961 source).";
            //
            // _lblRampHint2
            //
            _lblRampHint2.AutoSize = true;
            _lblRampHint2.ForeColor = Color.Firebrick;
            _lblRampHint2.Location = new Point(12, 30);
            _lblRampHint2.Name = "_lblRampHint2";
            _lblRampHint2.Size = new Size(900, 15);
            _lblRampHint2.TabIndex = 1;
            _lblRampHint2.Text = "Requires Connect + Enable flight commands (arm) + 23333 session ON. step=255 ⇒ jump; step=1 ⇒ full ramp; delayMs = ms between steps. frames/step unused here. Sweeps 128→target→back to 128, then broadcasts 128 (stick center / idle).";
            //
            // _lblRampStart
            //
            _lblRampStart.AutoSize = true;
            _lblRampStart.Location = new Point(12, 64);
            _lblRampStart.Name = "_lblRampStart";
            _lblRampStart.Size = new Size(30, 15);
            _lblRampStart.TabIndex = 2;
            _lblRampStart.Text = "start";
            //
            // _rStart
            //
            _rStart.Location = new Point(60, 61);
            _rStart.Maximum = new decimal(new int[] { 255, 0, 0, 0 });
            _rStart.Name = "_rStart";
            _rStart.Size = new Size(60, 23);
            _rStart.TabIndex = 3;
            _rStart.Value = new decimal(new int[] { 128, 0, 0, 0 });
            //
            // _lblRampTarget
            //
            _lblRampTarget.AutoSize = true;
            _lblRampTarget.Location = new Point(140, 64);
            _lblRampTarget.Name = "_lblRampTarget";
            _lblRampTarget.Size = new Size(40, 15);
            _lblRampTarget.TabIndex = 4;
            _lblRampTarget.Text = "target";
            //
            // _rTarget
            //
            _rTarget.Location = new Point(190, 61);
            _rTarget.Maximum = new decimal(new int[] { 255, 0, 0, 0 });
            _rTarget.Name = "_rTarget";
            _rTarget.Size = new Size(60, 23);
            _rTarget.TabIndex = 5;
            _rTarget.Value = new decimal(new int[] { 200, 0, 0, 0 });
            //
            // _lblRampStep
            //
            _lblRampStep.AutoSize = true;
            _lblRampStep.Location = new Point(270, 64);
            _lblRampStep.Name = "_lblRampStep";
            _lblRampStep.Size = new Size(30, 15);
            _lblRampStep.TabIndex = 6;
            _lblRampStep.Text = "step";
            //
            // _rStep
            //
            _rStep.Location = new Point(315, 61);
            _rStep.Maximum = new decimal(new int[] { 255, 0, 0, 0 });
            _rStep.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            _rStep.Name = "_rStep";
            _rStep.Size = new Size(60, 23);
            _rStep.TabIndex = 7;
            _rStep.Value = new decimal(new int[] { 5, 0, 0, 0 });
            //
            // _lblRampDelay
            //
            _lblRampDelay.AutoSize = true;
            _lblRampDelay.Location = new Point(12, 100);
            _lblRampDelay.Name = "_lblRampDelay";
            _lblRampDelay.Size = new Size(80, 15);
            _lblRampDelay.TabIndex = 8;
            _lblRampDelay.Text = "step delay ms";
            //
            // _rDelay
            //
            _rDelay.Location = new Point(110, 97);
            _rDelay.Maximum = new decimal(new int[] { 5000, 0, 0, 0 });
            _rDelay.Name = "_rDelay";
            _rDelay.Size = new Size(60, 23);
            _rDelay.TabIndex = 9;
            _rDelay.Value = new decimal(new int[] { 50, 0, 0, 0 });
            //
            // _lblRampPerStep
            //
            _lblRampPerStep.AutoSize = true;
            _lblRampPerStep.Location = new Point(190, 100);
            _lblRampPerStep.Name = "_lblRampPerStep";
            _lblRampPerStep.Size = new Size(72, 15);
            _lblRampPerStep.TabIndex = 10;
            _lblRampPerStep.Text = "frames/step";
            //
            // _rPerStep
            //
            _rPerStep.Location = new Point(280, 97);
            _rPerStep.Maximum = new decimal(new int[] { 50, 0, 0, 0 });
            _rPerStep.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            _rPerStep.Name = "_rPerStep";
            _rPerStep.Size = new Size(60, 23);
            _rPerStep.TabIndex = 11;
            _rPerStep.Value = new decimal(new int[] { 1, 0, 0, 0 });
            //
            // _lblRampHold
            //
            _lblRampHold.AutoSize = true;
            _lblRampHold.Location = new Point(12, 136);
            _lblRampHold.Name = "_lblRampHold";
            _lblRampHold.Size = new Size(84, 15);
            _lblRampHold.TabIndex = 12;
            _lblRampHold.Text = "hold target (s)";
            //
            // _rHold
            //
            _rHold.Location = new Point(110, 133);
            _rHold.Maximum = new decimal(new int[] { 120, 0, 0, 0 });
            _rHold.Name = "_rHold";
            _rHold.Size = new Size(60, 23);
            _rHold.TabIndex = 13;
            _rHold.Value = new decimal(new int[] { 3, 0, 0, 0 });
            //
            // _btnRamp
            //
            _btnRamp.BackColor = Color.FromArgb(206, 240, 206);
            _btnRamp.ForeColor = Color.FromArgb(20, 40, 20);
            _btnRamp.Location = new Point(12, 176);
            _btnRamp.Name = "_btnRamp";
            _btnRamp.Size = new Size(110, 28);
            _btnRamp.TabIndex = 14;
            _btnRamp.Text = "▶ RAMP";
            _btnRamp.UseVisualStyleBackColor = false;
            _btnRamp.Click += BtnRamp_Click;
            //
            // _btnRampStop
            //
            _btnRampStop.Location = new Point(132, 176);
            _btnRampStop.Name = "_btnRampStop";
            _btnRampStop.Size = new Size(90, 26);
            _btnRampStop.TabIndex = 15;
            _btnRampStop.Text = "■ STOP";
            _btnRampStop.Click += BtnRampStop_Click;
            //
            // _rStatus
            //
            _rStatus.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _rStatus.Font = new Font("Consolas", 9F);
            _rStatus.Location = new Point(12, 216);
            _rStatus.Multiline = true;
            _rStatus.Name = "_rStatus";
            _rStatus.ReadOnly = true;
            _rStatus.ScrollBars = ScrollBars.Both;
            _rStatus.Size = new Size(720, 200);
            _rStatus.TabIndex = 16;
            //
            // _lblRampHelp
            //
            _lblRampHelp.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            _lblRampHelp.ForeColor = Color.Gray;
            _lblRampHelp.Location = new Point(752, 10);
            _lblRampHelp.Name = "_lblRampHelp";
            _lblRampHelp.Size = new Size(268, 640);
            _lblRampHelp.TabIndex = 17;
            _lblRampHelp.Text = "Sweeps throttle through the SAME control stream the main window's Connect already set up (session keepalive + control socket) — for testing a real climb/descend/arm gesture over time, not a one-off packet like the Inject tab.\r\n\r\nRequires: Connect + \"Enable flight commands\" (arm) + the 23333 session already running on the main form.\r\n\r\nstart / target / step are raw stick units (128 = center/hold). step delay ms paces each step; frames/step is currently unused. hold (s) is how long it dwells at target before sweeping back down.\r\n\r\nRAMP sweeps start→target→start, then leaves the drone at neutral (128) when finished. STOP aborts immediately and also returns to neutral — it never leaves the drone mid-climb.\r\n\r\nSee docs/PROTOCOL.md §2 (throttle self-centers to 128 = HOLD, >128 climbs, <128 descends) and §4/§5 (the arm gesture: ramp up past ~240 then snap back to 128).";
            //
            // _tabGps
            //
            _tabGps.Controls.Add(_pnlGps);
            _tabGps.Location = new Point(4, 24);
            _tabGps.Name = "_tabGps";
            _tabGps.Size = new Size(1032, 692);
            _tabGps.TabIndex = 4;
            _tabGps.Text = "GPS";
            //
            // _pnlGps
            //
            _pnlGps.Controls.Add(_lblGpsHint1);
            _pnlGps.Controls.Add(_lblGpsHint2);
            _pnlGps.Controls.Add(_lblGpsMode);
            _pnlGps.Controls.Add(_gMode);
            _pnlGps.Controls.Add(_lblGpsCmdId);
            _pnlGps.Controls.Add(_gCmdId);
            _pnlGps.Controls.Add(_lblGpsLat);
            _pnlGps.Controls.Add(_lblGpsLon);
            _pnlGps.Controls.Add(_gLat);
            _pnlGps.Controls.Add(_gLon);
            _pnlGps.Controls.Add(_lblGpsAlt);
            _pnlGps.Controls.Add(_lblGpsSpeed);
            _pnlGps.Controls.Add(_lblGpsRadius);
            _pnlGps.Controls.Add(_gAlt);
            _pnlGps.Controls.Add(_gSpeed);
            _pnlGps.Controls.Add(_gRadius);
            _pnlGps.Controls.Add(_btnGpsBuild);
            _pnlGps.Controls.Add(_btnGpsSend);
            _pnlGps.Controls.Add(_lblGpsWireBytes);
            _pnlGps.Controls.Add(_gHex);
            _pnlGps.Controls.Add(_gReply);
            _pnlGps.Controls.Add(_lblGpsHelp);
            _pnlGps.Dock = DockStyle.Fill;
            _pnlGps.Location = new Point(0, 0);
            _pnlGps.Name = "_pnlGps";
            _pnlGps.Padding = new Padding(12);
            _pnlGps.Size = new Size(1032, 692);
            _pnlGps.TabIndex = 0;
            //
            // _lblGpsHint1
            //
            _lblGpsHint1.AutoSize = true;
            _lblGpsHint1.ForeColor = Color.Gray;
            _lblGpsHint1.Location = new Point(12, 10);
            _lblGpsHint1.Name = "_lblGpsHint1";
            _lblGpsHint1.Size = new Size(520, 15);
            _lblGpsHint1.TabIndex = 0;
            _lblGpsHint1.Text = "Autonomous GPS frame builder — coords deg×1e7, radius m×10 (PROTOCOL.md §8 #6).";
            //
            // _lblGpsHint2
            //
            _lblGpsHint2.AutoSize = true;
            _lblGpsHint2.ForeColor = Color.Firebrick;
            _lblGpsHint2.Location = new Point(12, 30);
            _lblGpsHint2.Name = "_lblGpsHint2";
            _lblGpsHint2.Size = new Size(560, 15);
            _lblGpsHint2.TabIndex = 1;
            _lblGpsHint2.Text = "⚠ UNVERIFIED native packing — build, then diff against a real phone tap-to-fly/orbit capture.";
            //
            // _lblGpsMode
            //
            _lblGpsMode.AutoSize = true;
            _lblGpsMode.Location = new Point(12, 62);
            _lblGpsMode.Name = "_lblGpsMode";
            _lblGpsMode.Size = new Size(38, 15);
            _lblGpsMode.TabIndex = 2;
            _lblGpsMode.Text = "mode";
            //
            // _gMode
            //
            _gMode.DropDownStyle = ComboBoxStyle.DropDownList;
            _gMode.Items.AddRange(new object[] { "Waypoint (tap-to-fly)", "Orbit (circle-fly)" });
            _gMode.Location = new Point(60, 59);
            _gMode.Name = "_gMode";
            _gMode.Size = new Size(160, 23);
            _gMode.TabIndex = 3;
            //
            // _lblGpsCmdId
            //
            _lblGpsCmdId.AutoSize = true;
            _lblGpsCmdId.Location = new Point(240, 62);
            _lblGpsCmdId.Name = "_lblGpsCmdId";
            _lblGpsCmdId.Size = new Size(110, 15);
            _lblGpsCmdId.TabIndex = 4;
            _lblGpsCmdId.Text = "inner cmd id (hex)";
            //
            // _gCmdId
            //
            _gCmdId.Font = new Font("Consolas", 9F);
            _gCmdId.Location = new Point(360, 59);
            _gCmdId.Name = "_gCmdId";
            _gCmdId.Size = new Size(50, 23);
            _gCmdId.TabIndex = 5;
            _gCmdId.Text = "20";
            //
            // _lblGpsLat
            //
            _lblGpsLat.AutoSize = true;
            _lblGpsLat.Location = new Point(12, 95);
            _lblGpsLat.Name = "_lblGpsLat";
            _lblGpsLat.Size = new Size(22, 15);
            _lblGpsLat.TabIndex = 6;
            _lblGpsLat.Text = "lat";
            //
            // _lblGpsLon
            //
            _lblGpsLon.AutoSize = true;
            _lblGpsLon.Location = new Point(202, 95);
            _lblGpsLon.Name = "_lblGpsLon";
            _lblGpsLon.Size = new Size(25, 15);
            _lblGpsLon.TabIndex = 7;
            _lblGpsLon.Text = "lon";
            //
            // _gLat
            //
            _gLat.Font = new Font("Consolas", 9F);
            _gLat.Location = new Point(60, 92);
            _gLat.Name = "_gLat";
            _gLat.Size = new Size(130, 23);
            _gLat.TabIndex = 8;
            _gLat.Text = "0.0000000";
            //
            // _gLon
            //
            _gLon.Font = new Font("Consolas", 9F);
            _gLon.Location = new Point(250, 92);
            _gLon.Name = "_gLon";
            _gLon.Size = new Size(130, 23);
            _gLon.TabIndex = 9;
            _gLon.Text = "0.0000000";
            //
            // _lblGpsAlt
            //
            _lblGpsAlt.AutoSize = true;
            _lblGpsAlt.Location = new Point(12, 125);
            _lblGpsAlt.Name = "_lblGpsAlt";
            _lblGpsAlt.Size = new Size(35, 15);
            _lblGpsAlt.TabIndex = 10;
            _lblGpsAlt.Text = "alt m";
            //
            // _lblGpsSpeed
            //
            _lblGpsSpeed.AutoSize = true;
            _lblGpsSpeed.Location = new Point(150, 125);
            _lblGpsSpeed.Name = "_lblGpsSpeed";
            _lblGpsSpeed.Size = new Size(40, 15);
            _lblGpsSpeed.TabIndex = 11;
            _lblGpsSpeed.Text = "speed";
            //
            // _lblGpsRadius
            //
            _lblGpsRadius.AutoSize = true;
            _lblGpsRadius.Location = new Point(300, 125);
            _lblGpsRadius.Name = "_lblGpsRadius";
            _lblGpsRadius.Size = new Size(54, 15);
            _lblGpsRadius.TabIndex = 12;
            _lblGpsRadius.Text = "radius m";
            //
            // _gAlt
            //
            _gAlt.Font = new Font("Consolas", 9F);
            _gAlt.Location = new Point(60, 122);
            _gAlt.Name = "_gAlt";
            _gAlt.Size = new Size(70, 23);
            _gAlt.TabIndex = 13;
            _gAlt.Text = "10";
            //
            // _gSpeed
            //
            _gSpeed.Font = new Font("Consolas", 9F);
            _gSpeed.Location = new Point(200, 122);
            _gSpeed.Name = "_gSpeed";
            _gSpeed.Size = new Size(70, 23);
            _gSpeed.TabIndex = 14;
            _gSpeed.Text = "5";
            //
            // _gRadius
            //
            _gRadius.Font = new Font("Consolas", 9F);
            _gRadius.Location = new Point(350, 122);
            _gRadius.Name = "_gRadius";
            _gRadius.Size = new Size(70, 23);
            _gRadius.TabIndex = 15;
            _gRadius.Text = "10";
            //
            // _btnGpsBuild
            //
            _btnGpsBuild.Location = new Point(12, 156);
            _btnGpsBuild.Name = "_btnGpsBuild";
            _btnGpsBuild.Size = new Size(110, 26);
            _btnGpsBuild.TabIndex = 16;
            _btnGpsBuild.Text = "Build → hex";
            _btnGpsBuild.Click += BtnGpsBuild_Click;
            //
            // _btnGpsSend
            //
            _btnGpsSend.BackColor = Color.FromArgb(206, 240, 206);
            _btnGpsSend.ForeColor = Color.FromArgb(20, 40, 20);
            _btnGpsSend.Location = new Point(130, 154);
            _btnGpsSend.Name = "_btnGpsSend";
            _btnGpsSend.Size = new Size(110, 28);
            _btnGpsSend.TabIndex = 17;
            _btnGpsSend.Text = "▶ SEND";
            _btnGpsSend.UseVisualStyleBackColor = false;
            _btnGpsSend.Click += BtnGpsSend_Click;
            //
            // _lblGpsWireBytes
            //
            _lblGpsWireBytes.AutoSize = true;
            _lblGpsWireBytes.ForeColor = Color.Gray;
            _lblGpsWireBytes.Location = new Point(12, 192);
            _lblGpsWireBytes.Name = "_lblGpsWireBytes";
            _lblGpsWireBytes.Size = new Size(100, 15);
            _lblGpsWireBytes.TabIndex = 18;
            _lblGpsWireBytes.Text = "Wire bytes (hex):";
            //
            // _gHex
            //
            _gHex.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _gHex.Font = new Font("Consolas", 9F);
            _gHex.Location = new Point(12, 212);
            _gHex.Multiline = true;
            _gHex.Name = "_gHex";
            _gHex.ScrollBars = ScrollBars.Both;
            _gHex.Size = new Size(720, 70);
            _gHex.TabIndex = 19;
            //
            // _gReply
            //
            _gReply.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _gReply.Font = new Font("Consolas", 9F);
            _gReply.Location = new Point(12, 290);
            _gReply.Multiline = true;
            _gReply.Name = "_gReply";
            _gReply.ReadOnly = true;
            _gReply.ScrollBars = ScrollBars.Both;
            _gReply.Size = new Size(720, 70);
            _gReply.TabIndex = 20;
            //
            // _lblGpsHelp
            //
            _lblGpsHelp.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            _lblGpsHelp.ForeColor = Color.Gray;
            _lblGpsHelp.Location = new Point(752, 10);
            _lblGpsHelp.Name = "_lblGpsHelp";
            _lblGpsHelp.Size = new Size(268, 640);
            _lblGpsHelp.TabIndex = 21;
            _lblGpsHelp.Text = "Builder for the AUTONOMOUS waypoint/orbit frames — \"fly to this GPS point\" or \"circle around this GPS point\", not the manual stick control used on the other tabs.\r\n\r\nmode picks waypoint (tap-to-fly) or orbit (circle-fly); inner cmd id is the native sub-command byte (default 0x20 is a best guess, unconfirmed).\r\n\r\nlat/lon here are plain decimal degrees — Build multiplies by 1e7 to match the recovered native scaling (docs/PROTOCOL.md §8 #6). alt/speed are metres; radius is metres (×10 internally for orbit).\r\n\r\nBuild → hex fills the wire-bytes box without sending; SEND fires it immediately.\r\n\r\n⚠ UNVERIFIED byte packing. The field names and scaling come from the decompiled app, but the exact byte layout inside the frame has not been validated against a real captured waypoint/orbit command. Treat results here as a starting point to diff against a real phone-app capture, not as ground truth.";
            //
            // _tabReplay
            //
            _tabReplay.Controls.Add(_pnlReplay);
            _tabReplay.Location = new Point(4, 24);
            _tabReplay.Name = "_tabReplay";
            _tabReplay.Size = new Size(1032, 692);
            _tabReplay.TabIndex = 5;
            _tabReplay.Text = "Replay";
            //
            // _pnlReplay
            //
            _pnlReplay.Controls.Add(_lblReplayHint);
            _pnlReplay.Controls.Add(_btnCapStart);
            _pnlReplay.Controls.Add(_btnCapStop);
            _pnlReplay.Controls.Add(_capLbl);
            _pnlReplay.Controls.Add(_btnCapSave);
            _pnlReplay.Controls.Add(_btnCapLoad);
            _pnlReplay.Controls.Add(_replayLbl);
            _pnlReplay.Controls.Add(_lblReplaySpeed);
            _pnlReplay.Controls.Add(_numReplayRate);
            _pnlReplay.Controls.Add(_chkTxOnly);
            _pnlReplay.Controls.Add(_btnReplayGo);
            _pnlReplay.Controls.Add(_btnReplayStop);
            _pnlReplay.Controls.Add(_lblReplayHelp);
            _pnlReplay.Dock = DockStyle.Fill;
            _pnlReplay.Location = new Point(0, 0);
            _pnlReplay.Name = "_pnlReplay";
            _pnlReplay.Padding = new Padding(12);
            _pnlReplay.Size = new Size(1032, 692);
            _pnlReplay.TabIndex = 0;
            //
            // _lblReplayHint
            //
            _lblReplayHint.AutoSize = true;
            _lblReplayHint.ForeColor = Color.Gray;
            _lblReplayHint.Location = new Point(12, 12);
            _lblReplayHint.Name = "_lblReplayHint";
            _lblReplayHint.Size = new Size(420, 15);
            _lblReplayHint.TabIndex = 0;
            _lblReplayHint.Text = "Capture the live stream, save it, and replay it back to the drone.";
            //
            // _btnCapStart
            //
            _btnCapStart.Location = new Point(12, 40);
            _btnCapStart.Name = "_btnCapStart";
            _btnCapStart.Size = new Size(120, 26);
            _btnCapStart.TabIndex = 1;
            _btnCapStart.Text = "● Start capture";
            _btnCapStart.Click += BtnCapStart_Click;
            //
            // _btnCapStop
            //
            _btnCapStop.Location = new Point(138, 40);
            _btnCapStop.Name = "_btnCapStop";
            _btnCapStop.Size = new Size(80, 26);
            _btnCapStop.TabIndex = 2;
            _btnCapStop.Text = "■ Stop";
            _btnCapStop.Click += BtnCapStop_Click;
            //
            // _capLbl
            //
            _capLbl.AutoSize = true;
            _capLbl.ForeColor = Color.FromArgb(34, 211, 238);
            _capLbl.Location = new Point(230, 45);
            _capLbl.Name = "_capLbl";
            _capLbl.Size = new Size(100, 15);
            _capLbl.TabIndex = 3;
            _capLbl.Text = "0 frames buffered";
            //
            // _btnCapSave
            //
            _btnCapSave.Location = new Point(12, 76);
            _btnCapSave.Name = "_btnCapSave";
            _btnCapSave.Size = new Size(120, 26);
            _btnCapSave.TabIndex = 4;
            _btnCapSave.Text = "Save buffer…";
            _btnCapSave.Click += BtnCapSave_Click;
            //
            // _btnCapLoad
            //
            _btnCapLoad.Location = new Point(138, 76);
            _btnCapLoad.Name = "_btnCapLoad";
            _btnCapLoad.Size = new Size(100, 26);
            _btnCapLoad.TabIndex = 5;
            _btnCapLoad.Text = "Load file…";
            _btnCapLoad.Click += BtnCapLoad_Click;
            //
            // _replayLbl
            //
            _replayLbl.AutoSize = true;
            _replayLbl.ForeColor = Color.Gray;
            _replayLbl.Location = new Point(250, 81);
            _replayLbl.Name = "_replayLbl";
            _replayLbl.Size = new Size(96, 15);
            _replayLbl.TabIndex = 6;
            _replayLbl.Text = "no replay loaded";
            //
            // _lblReplaySpeed
            //
            _lblReplaySpeed.AutoSize = true;
            _lblReplaySpeed.Location = new Point(12, 120);
            _lblReplaySpeed.Name = "_lblReplaySpeed";
            _lblReplaySpeed.Size = new Size(48, 15);
            _lblReplaySpeed.TabIndex = 7;
            _lblReplaySpeed.Text = "speed ×";
            //
            // _numReplayRate
            //
            _numReplayRate.Location = new Point(70, 117);
            _numReplayRate.Maximum = new decimal(new int[] { 20, 0, 0, 0 });
            _numReplayRate.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            _numReplayRate.Name = "_numReplayRate";
            _numReplayRate.Size = new Size(60, 23);
            _numReplayRate.TabIndex = 8;
            _numReplayRate.Value = new decimal(new int[] { 1, 0, 0, 0 });
            //
            // _chkTxOnly
            //
            _chkTxOnly.AutoSize = true;
            _chkTxOnly.Checked = true;
            _chkTxOnly.CheckState = CheckState.Checked;
            _chkTxOnly.Location = new Point(140, 119);
            _chkTxOnly.Name = "_chkTxOnly";
            _chkTxOnly.Size = new Size(103, 19);
            _chkTxOnly.TabIndex = 9;
            _chkTxOnly.Text = "TX frames only";
            //
            // _btnReplayGo
            //
            _btnReplayGo.BackColor = Color.FromArgb(206, 240, 206);
            _btnReplayGo.ForeColor = Color.FromArgb(20, 40, 20);
            _btnReplayGo.Location = new Point(280, 115);
            _btnReplayGo.Name = "_btnReplayGo";
            _btnReplayGo.Size = new Size(110, 28);
            _btnReplayGo.TabIndex = 10;
            _btnReplayGo.Text = "▶ REPLAY";
            _btnReplayGo.UseVisualStyleBackColor = false;
            _btnReplayGo.Click += BtnReplayGo_Click;
            //
            // _btnReplayStop
            //
            _btnReplayStop.Location = new Point(396, 116);
            _btnReplayStop.Name = "_btnReplayStop";
            _btnReplayStop.Size = new Size(70, 26);
            _btnReplayStop.TabIndex = 11;
            _btnReplayStop.Text = "Stop";
            _btnReplayStop.Click += BtnReplayStop_Click;
            //
            // _lblReplayHelp
            //
            _lblReplayHelp.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _lblReplayHelp.ForeColor = Color.Gray;
            _lblReplayHelp.Location = new Point(12, 160);
            _lblReplayHelp.Name = "_lblReplayHelp";
            _lblReplayHelp.Size = new Size(1008, 500);
            _lblReplayHelp.TabIndex = 12;
            _lblReplayHelp.Text = "Record a live session and play it back later — for reproducing a result without re-driving the drone by hand, or for feeding the exact same packet sequence back to it again (e.g. a working arm sequence).\r\n\r\nStart capture / Stop buffers every packet this app sends while it's running (the counter next to Stop shows how many frames are held). Save buffer… writes that capture to a file; Load file… reads a previously-saved one back in without needing a fresh live capture first.\r\n\r\n\"TX frames only\", checked by default, replays just what this app sent and skips any captured drone replies — this is what you want for almost every replay use-case (re-sending a command sequence), since replaying the drone's own old replies back at it makes no sense.\r\n\r\nspeed × scales playback timing — 2× replays twice as fast, 0.5× half speed, 1× is the original real-time pacing it was captured at.\r\n\r\n▶ REPLAY starts sending the loaded capture to the drone at that rate; Stop cancels mid-replay.\r\n\r\nTip: open the Monitor tab (in a second window, or just switch back and forth) while a replay runs, to watch the replayed traffic go out and compare it against the original capture.";
            //
            // _capRefreshTimer
            //
            _capRefreshTimer.Interval = 400;
            _capRefreshTimer.Tick += CapRefreshTimer_Tick;
            //
            // _tabTelemetry
            //
            _tabTelemetry.Controls.Add(_pnlTelemetry);
            _tabTelemetry.Location = new Point(4, 24);
            _tabTelemetry.Name = "_tabTelemetry";
            _tabTelemetry.Size = new Size(1032, 692);
            _tabTelemetry.TabIndex = 6;
            _tabTelemetry.Text = "Telemetry";
            //
            // _pnlTelemetry
            //
            _pnlTelemetry.Controls.Add(_lblTelModelCap);
            _pnlTelemetry.Controls.Add(_tModel);
            _pnlTelemetry.Controls.Add(_lblTelSatsCap);
            _pnlTelemetry.Controls.Add(_tSats);
            _pnlTelemetry.Controls.Add(_lblTelFixCap);
            _pnlTelemetry.Controls.Add(_tFix);
            _pnlTelemetry.Controls.Add(_lblTelGpsCap);
            _pnlTelemetry.Controls.Add(_tGps);
            _pnlTelemetry.Controls.Add(_lblTelBatCap);
            _pnlTelemetry.Controls.Add(_tBattery);
            _pnlTelemetry.Controls.Add(_lblTelAltCap);
            _pnlTelemetry.Controls.Add(_tAlt);
            _pnlTelemetry.Controls.Add(_lblTelMotorsCap);
            _pnlTelemetry.Controls.Add(_tMotors);
            _pnlTelemetry.Controls.Add(_lblTelStateCap);
            _pnlTelemetry.Controls.Add(_tState);
            _pnlTelemetry.Controls.Add(_lblTelRawCap);
            _pnlTelemetry.Controls.Add(_tRaw);
            _pnlTelemetry.Controls.Add(_lblTelHint);
            _pnlTelemetry.Controls.Add(_lblTelHelp);
            _pnlTelemetry.Controls.Add(_lblTelSubtypes);
            _pnlTelemetry.Controls.Add(_subList);
            _pnlTelemetry.Dock = DockStyle.Fill;
            _pnlTelemetry.Location = new Point(0, 0);
            _pnlTelemetry.Name = "_pnlTelemetry";
            _pnlTelemetry.Padding = new Padding(16);
            _pnlTelemetry.Size = new Size(1032, 692);
            _pnlTelemetry.TabIndex = 0;
            //
            // _lblTelModelCap
            //
            _lblTelModelCap.AutoSize = true;
            _lblTelModelCap.Font = new Font("Consolas", 8.5F);
            _lblTelModelCap.ForeColor = Color.Gray;
            _lblTelModelCap.Location = new Point(18, 20);
            _lblTelModelCap.Name = "_lblTelModelCap";
            _lblTelModelCap.Size = new Size(50, 13);
            _lblTelModelCap.Text = "MODEL";
            //
            // _tModel
            //
            _tModel.AutoSize = true;
            _tModel.Font = new Font("Consolas", 15F, FontStyle.Bold);
            _tModel.ForeColor = Color.FromArgb(34, 211, 238);
            _tModel.Location = new Point(200, 14);
            _tModel.Name = "_tModel";
            _tModel.Size = new Size(25, 24);
            _tModel.Text = "—";
            //
            // _lblTelSatsCap
            //
            _lblTelSatsCap.AutoSize = true;
            _lblTelSatsCap.Font = new Font("Consolas", 8.5F);
            _lblTelSatsCap.ForeColor = Color.Gray;
            _lblTelSatsCap.Location = new Point(18, 62);
            _lblTelSatsCap.Name = "_lblTelSatsCap";
            _lblTelSatsCap.Size = new Size(80, 13);
            _lblTelSatsCap.Text = "SATELLITES";
            //
            // _tSats
            //
            _tSats.AutoSize = true;
            _tSats.Font = new Font("Consolas", 15F, FontStyle.Bold);
            _tSats.ForeColor = Color.FromArgb(34, 211, 238);
            _tSats.Location = new Point(200, 56);
            _tSats.Name = "_tSats";
            _tSats.Size = new Size(25, 24);
            _tSats.Text = "—";
            //
            // _lblTelFixCap
            //
            _lblTelFixCap.AutoSize = true;
            _lblTelFixCap.Font = new Font("Consolas", 8.5F);
            _lblTelFixCap.ForeColor = Color.Gray;
            _lblTelFixCap.Location = new Point(18, 104);
            _lblTelFixCap.Name = "_lblTelFixCap";
            _lblTelFixCap.Size = new Size(56, 13);
            _lblTelFixCap.Text = "GPS FIX";
            //
            // _tFix
            //
            _tFix.AutoSize = true;
            _tFix.Font = new Font("Consolas", 15F, FontStyle.Bold);
            _tFix.ForeColor = Color.FromArgb(34, 211, 238);
            _tFix.Location = new Point(200, 98);
            _tFix.Name = "_tFix";
            _tFix.Size = new Size(25, 24);
            _tFix.Text = "—";
            //
            // _lblTelGpsCap
            //
            _lblTelGpsCap.AutoSize = true;
            _lblTelGpsCap.Font = new Font("Consolas", 8.5F);
            _lblTelGpsCap.ForeColor = Color.Gray;
            _lblTelGpsCap.Location = new Point(18, 146);
            _lblTelGpsCap.Name = "_lblTelGpsCap";
            _lblTelGpsCap.Size = new Size(64, 13);
            _lblTelGpsCap.Text = "POSITION";
            //
            // _tGps
            //
            _tGps.AutoSize = true;
            _tGps.Font = new Font("Consolas", 15F, FontStyle.Bold);
            _tGps.ForeColor = Color.FromArgb(34, 211, 238);
            _tGps.Location = new Point(200, 140);
            _tGps.Name = "_tGps";
            _tGps.Size = new Size(25, 24);
            _tGps.Text = "—";
            //
            // _lblTelBatCap
            //
            _lblTelBatCap.AutoSize = true;
            _lblTelBatCap.Font = new Font("Consolas", 8.5F);
            _lblTelBatCap.ForeColor = Color.Gray;
            _lblTelBatCap.Location = new Point(18, 188);
            _lblTelBatCap.Name = "_lblTelBatCap";
            _lblTelBatCap.Size = new Size(56, 13);
            _lblTelBatCap.Text = "BATTERY";
            //
            // _tBattery
            //
            _tBattery.AutoSize = true;
            _tBattery.Font = new Font("Consolas", 15F, FontStyle.Bold);
            _tBattery.ForeColor = Color.Gray;
            _tBattery.Location = new Point(200, 182);
            _tBattery.Name = "_tBattery";
            _tBattery.Size = new Size(85, 24);
            _tBattery.Text = "— (map)";
            //
            // _lblTelAltCap
            //
            _lblTelAltCap.AutoSize = true;
            _lblTelAltCap.Font = new Font("Consolas", 8.5F);
            _lblTelAltCap.ForeColor = Color.Gray;
            _lblTelAltCap.Location = new Point(18, 230);
            _lblTelAltCap.Name = "_lblTelAltCap";
            _lblTelAltCap.Size = new Size(64, 13);
            _lblTelAltCap.Text = "ALTITUDE";
            //
            // _tAlt
            //
            _tAlt.AutoSize = true;
            _tAlt.Font = new Font("Consolas", 15F, FontStyle.Bold);
            _tAlt.ForeColor = Color.Gray;
            _tAlt.Location = new Point(200, 224);
            _tAlt.Name = "_tAlt";
            _tAlt.Size = new Size(85, 24);
            _tAlt.Text = "— (map)";
            //
            // _lblTelMotorsCap
            //
            _lblTelMotorsCap.AutoSize = true;
            _lblTelMotorsCap.Font = new Font("Consolas", 8.5F);
            _lblTelMotorsCap.ForeColor = Color.Gray;
            _lblTelMotorsCap.Location = new Point(18, 272);
            _lblTelMotorsCap.Name = "_lblTelMotorsCap";
            _lblTelMotorsCap.Size = new Size(56, 13);
            _lblTelMotorsCap.Text = "MOTORS";
            //
            // _tMotors
            //
            _tMotors.AutoSize = true;
            _tMotors.Font = new Font("Consolas", 15F, FontStyle.Bold);
            _tMotors.ForeColor = Color.Gray;
            _tMotors.Location = new Point(200, 266);
            _tMotors.Name = "_tMotors";
            _tMotors.Size = new Size(85, 24);
            _tMotors.Text = "— (map)";
            //
            // _lblTelStateCap
            //
            _lblTelStateCap.AutoSize = true;
            _lblTelStateCap.Font = new Font("Consolas", 8.5F);
            _lblTelStateCap.ForeColor = Color.Gray;
            _lblTelStateCap.Location = new Point(18, 314);
            _lblTelStateCap.Name = "_lblTelStateCap";
            _lblTelStateCap.Size = new Size(96, 13);
            _lblTelStateCap.Text = "FLIGHT STATE";
            //
            // _tState
            //
            _tState.AutoSize = true;
            _tState.Font = new Font("Consolas", 15F, FontStyle.Bold);
            _tState.ForeColor = Color.Gray;
            _tState.Location = new Point(200, 308);
            _tState.Name = "_tState";
            _tState.Size = new Size(85, 24);
            _tState.Text = "— (map)";
            //
            // _lblTelRawCap
            //
            _lblTelRawCap.AutoSize = true;
            _lblTelRawCap.Font = new Font("Consolas", 8.5F);
            _lblTelRawCap.ForeColor = Color.Gray;
            _lblTelRawCap.Location = new Point(18, 356);
            _lblTelRawCap.Name = "_lblTelRawCap";
            _lblTelRawCap.Size = new Size(72, 13);
            _lblTelRawCap.Text = "LAST 58xx";
            //
            // _tRaw
            //
            _tRaw.AutoSize = true;
            _tRaw.Font = new Font("Consolas", 15F, FontStyle.Bold);
            _tRaw.ForeColor = Color.FromArgb(34, 211, 238);
            _tRaw.Location = new Point(200, 350);
            _tRaw.Name = "_tRaw";
            _tRaw.Size = new Size(25, 24);
            _tRaw.Text = "—";
            //
            // _lblTelHint
            //
            _lblTelHint.AutoSize = true;
            _lblTelHint.ForeColor = Color.Gray;
            _lblTelHint.Location = new Point(18, 406);
            _lblTelHint.Name = "_lblTelHint";
            _lblTelHint.Size = new Size(600, 15);
            _lblTelHint.TabIndex = 18;
            _lblTelHint.Text = "Live 58xx telemetry (drone → :9961). battery/altitude/motors await offset mapping — use the subtype list →";
            //
            // _lblTelHelp
            //
            _lblTelHelp.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            _lblTelHelp.ForeColor = Color.Gray;
            _lblTelHelp.Location = new Point(18, 432);
            _lblTelHelp.Name = "_lblTelHelp";
            _lblTelHelp.Size = new Size(440, 230);
            _lblTelHelp.TabIndex = 21;
            _lblTelHelp.Text = "Decoded live readout of the drone's 58xx telemetry family (drone → this app's :9961 listener) — the same frames the Monitor tab shows as raw hex, parsed into values here.\r\n\r\nMODEL / SATELLITES / GPS FIX / POSITION come from the 58 8a/8b/8c frames and are confirmed-decoded (docs/PROTOCOL.md §5).\r\n\r\nBATTERY / ALTITUDE / MOTORS / FLIGHT STATE show \"— (map)\" because their exact byte offset inside 58xx isn't confirmed for every subtype yet — see docs/REFERENCE.md §7 for the full field inventory of what should eventually land here.\r\n\r\nLAST 58xx is the most recent subtype byte seen (e.g. 58 8b). The SUBTYPES SEEN list → is a running tally of every distinct subtype this session has observed — useful for spotting a never-before-seen frame worth decoding next (cross-reference docs/PROTOCOL.md §8, \"open items\").";
            //
            // _lblTelSubtypes
            //
            _lblTelSubtypes.AutoSize = true;
            _lblTelSubtypes.ForeColor = Color.FromArgb(34, 211, 238);
            _lblTelSubtypes.Location = new Point(470, 18);
            _lblTelSubtypes.Name = "_lblTelSubtypes";
            _lblTelSubtypes.Size = new Size(140, 15);
            _lblTelSubtypes.TabIndex = 19;
            _lblTelSubtypes.Text = "58xx SUBTYPES SEEN";
            //
            // _subList
            //
            _subList.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _subList.Font = new Font("Consolas", 8.25F);
            _subList.IntegralHeight = false;
            _subList.Location = new Point(470, 40);
            _subList.Name = "_subList";
            _subList.Size = new Size(420, 360);
            _subList.TabIndex = 20;
            //
            // _tabEsp32
            //
            _tabEsp32.Location = new Point(4, 24);
            _tabEsp32.Name = "_tabEsp32";
            _tabEsp32.Size = new Size(1032, 692);
            _tabEsp32.TabIndex = 7;
            _tabEsp32.Text = "ESP32";
            //
            // _tabNrf24
            //
            _tabNrf24.Controls.Add(_lblNrf24Help);
            _tabNrf24.Location = new Point(4, 24);
            _tabNrf24.Name = "_tabNrf24";
            _tabNrf24.Size = new Size(1032, 692);
            _tabNrf24.TabIndex = 8;
            _tabNrf24.Text = "RC-Link (nRF24)";
            //
            // _lblNrf24Help
            //
            _lblNrf24Help.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            _lblNrf24Help.ForeColor = Color.Gray;
            _lblNrf24Help.Location = new Point(612, 10);
            _lblNrf24Help.Name = "_lblNrf24Help";
            _lblNrf24Help.Size = new Size(408, 672);
            _lblNrf24Help.TabIndex = 0;
            _lblNrf24Help.Text = "Drives the ESP32 + nRF24L01+ RC-link impersonator (esp32_nrf24_optical_tx.ino) — the hardware that reads and replays the drone's own 2.4GHz RC transmitter protocol (see docs/RC_LINK_SNIFF.md for the SPI-tap method and docs/WRITEUP.md for the full writeup of how that protocol was reverse-engineered).\r\n\r\nConnect to its serial port first (left panel, same as the ESP32 tab's connect controls, but this is a different sketch/radio role — do not mix up the two tabs' hardware).\r\n\r\nCold Start (Bind+Ctrl): runs the full cold-boot bind sequence, then starts normal control — the closest one-click equivalent of \"power on a fresh controller.\"\r\nContinuous Bind: re-sends just the bind address on a loop.\r\nOptical / Neutral-GPS: send the corresponding mode-flag frame (the drone's optical-vs-GPS positioning mode — see the main README §5/§6 and docs/PROTOCOL.md §4).\r\nPause / Resume: halts output without disconnecting.\r\n\r\n⚠ ARM Burst (props off!) spins the motors — the button is red for a reason. Always confirm props are off before clicking it.\r\n\r\nBrute-Force Pair Scan: sweeps channel/address candidates when the known parameters don't lock on (e.g. a different RC unit than the one this was reverse-engineered against).\r\nRadio Details: reads back the nRF24's current configuration for sanity-checking.\r\nAuto Sequence (c → o): runs cold-start then switches straight to optical in one click.\r\n\r\nThis tab talks to its own ESP32 over USB serial and needs no WiFi connection to the drone at all — the drone receives everything over its own 2.4GHz RC radio, exactly as it would from the real transmitter.";
            //
            // ReWorkbench
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1040, 720);
            Controls.Add(_tabs);
            Controls.Add(_barToolbar);
            Font = new Font("Segoe UI", 9F);
            MinimumSize = new Size(736, 519);
            Name = "ReWorkbench";
            Text = "HS175D — RE Workbench";

            ((System.ComponentModel.ISupportInitialize)_injR).EndInit();
            ((System.ComponentModel.ISupportInitialize)_injP).EndInit();
            ((System.ComponentModel.ISupportInitialize)_injT).EndInit();
            ((System.ComponentModel.ISupportInitialize)_injY).EndInit();
            ((System.ComponentModel.ISupportInitialize)_injCount).EndInit();
            ((System.ComponentModel.ISupportInitialize)_rStart).EndInit();
            ((System.ComponentModel.ISupportInitialize)_rTarget).EndInit();
            ((System.ComponentModel.ISupportInitialize)_rStep).EndInit();
            ((System.ComponentModel.ISupportInitialize)_rDelay).EndInit();
            ((System.ComponentModel.ISupportInitialize)_rPerStep).EndInit();
            ((System.ComponentModel.ISupportInitialize)_rHold).EndInit();
            ((System.ComponentModel.ISupportInitialize)_numReplayRate).EndInit();
            _tabs.ResumeLayout(false);
            _barToolbar.ResumeLayout(false);
            _barToolbar.PerformLayout();
            _tabMonitor.ResumeLayout(false);
            _pnlMonTop.ResumeLayout(false);
            _pnlMonTop.PerformLayout();
            _tabInject.ResumeLayout(false);
            _pnlInject.ResumeLayout(false);
            _pnlInject.PerformLayout();
            _tabRamp.ResumeLayout(false);
            _pnlRamp.ResumeLayout(false);
            _pnlRamp.PerformLayout();
            _tabGps.ResumeLayout(false);
            _pnlGps.ResumeLayout(false);
            _pnlGps.PerformLayout();
            _tabReplay.ResumeLayout(false);
            _pnlReplay.ResumeLayout(false);
            _pnlReplay.PerformLayout();
            _tabTelemetry.ResumeLayout(false);
            _pnlTelemetry.ResumeLayout(false);
            _pnlTelemetry.PerformLayout();
            ResumeLayout(false);
        }

        #endregion
    }
}

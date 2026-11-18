namespace HS175D
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        // ---- Connection ----
        private System.Windows.Forms.Panel grpConn;
        private System.Windows.Forms.Label lblIpCaption;
        private System.Windows.Forms.TextBox _txtIp;
        private System.Windows.Forms.Button _btnConnect;
        private System.Windows.Forms.Label _lblStatus;
        private System.Windows.Forms.Button _btnConsole;
        private System.Windows.Forms.Button _btnWorkbench;
        private System.Windows.Forms.Button _btnLiveDebug;
        private System.Windows.Forms.Button _btnMenu;

        // ---- Telemetry ----
        private System.Windows.Forms.Panel grpTel;
        private System.Windows.Forms.Label _lblModel;
        private System.Windows.Forms.Label _lblFirmware;
        private System.Windows.Forms.Label _lblGps;
        private System.Windows.Forms.Label _lblSats;
        private System.Windows.Forms.Label _lblBattery;
        private System.Windows.Forms.Label _lblYaw;
        private System.Windows.Forms.Label _lblRoll;
        private System.Windows.Forms.Label _lblPitch;
        private System.Windows.Forms.Panel _dialYaw;
        private System.Windows.Forms.Panel _dialRoll;
        private System.Windows.Forms.Panel _dialPitch;
        private System.Windows.Forms.Label _lblMode;
        private System.Windows.Forms.Label _lblRaw;
        private System.Windows.Forms.Label lblTelRate;
        private System.Windows.Forms.ComboBox cboTelRate;

        // ---- Camera / JSON ----
        private System.Windows.Forms.Panel grpCam;
        private System.Windows.Forms.Panel pnlVideo;
        private VideoPanel _videoPanel;
        private System.Windows.Forms.Button _btnTime;
        private System.Windows.Forms.Button _btnRecOn;
        private System.Windows.Forms.Button _btnRecOff;
        private System.Windows.Forms.Button _btnVideo;
        private System.Windows.Forms.Label lblCustomCmd;
        private System.Windows.Forms.TextBox _txtCustomCmd;
        private System.Windows.Forms.Label lblCustomParam;
        private System.Windows.Forms.TextBox _txtCustomParam;
        private System.Windows.Forms.Button _btnSendCustom;
        private System.Windows.Forms.Label lblParamHint;
        private System.Windows.Forms.Label lblSweep;
        private System.Windows.Forms.TextBox _txtSweepFrom;
        private System.Windows.Forms.TextBox _txtSweepTo;
        private System.Windows.Forms.Button _btnSweep;
        private System.Windows.Forms.Button _btnInfo;

        // ---- Gimbal ----
        private System.Windows.Forms.Panel grpGim;
        private System.Windows.Forms.Label lblTilt;
        private System.Windows.Forms.TrackBar _tbGimbal;
        private System.Windows.Forms.Label _lblGimbalVal;
        private System.Windows.Forms.Button _btnGimDown;
        private System.Windows.Forms.Button _btnGimUp;
        private System.Windows.Forms.Button _btnGimCenter;
        private System.Windows.Forms.Label lblManual;
        private System.Windows.Forms.TextBox _txtGimP0;
        private System.Windows.Forms.TextBox _txtGimP1;
        private System.Windows.Forms.TextBox _txtGimP2;
        private System.Windows.Forms.Button _btnGimManual;
        private System.Windows.Forms.Label lblReplay;
        private System.Windows.Forms.Button _btnCap224;
        private System.Windows.Forms.Button _btnCap191;
        private System.Windows.Forms.Button _btnCap255;
        private System.Windows.Forms.Button _btnCap115;
        private System.Windows.Forms.Button _btnCap241;
        private System.Windows.Forms.Button _btnCap99;
        private System.Windows.Forms.Button _btnFlag40;
        private System.Windows.Forms.Button _btnFlag80;
        private System.Windows.Forms.Label lblWorks;
        private System.Windows.Forms.Label lblGimHint;

        // ---- Send tuning ----
        private System.Windows.Forms.Panel grpSend;
        private System.Windows.Forms.CheckBox _chkChangeDetect;
        private System.Windows.Forms.Label lblLoopHz;
        private System.Windows.Forms.NumericUpDown _numControlHz;
        private System.Windows.Forms.Label lblHbHz;
        private System.Windows.Forms.NumericUpDown _numHeartbeatHz;

        // ---- Flight ----
        private System.Windows.Forms.Panel grpFly;
        private System.Windows.Forms.CheckBox _chkArm;
        private System.Windows.Forms.CheckBox _chk23333;
        private System.Windows.Forms.Label lblThrottle;
        private System.Windows.Forms.TrackBar _tbThrottle;
        private System.Windows.Forms.Label _lblThrottleVal;
        private System.Windows.Forms.Button _btnThrUp;
        private System.Windows.Forms.Button _btnThrDn;
        private System.Windows.Forms.Label lblYaw;
        private System.Windows.Forms.TrackBar _tbYaw;
        private System.Windows.Forms.Label _lblYawVal;
        private System.Windows.Forms.Label lblPitch;
        private System.Windows.Forms.TrackBar _tbPitch;
        private System.Windows.Forms.Label _lblPitchVal;
        private System.Windows.Forms.Label lblRoll;
        private System.Windows.Forms.TrackBar _tbRoll;
        private System.Windows.Forms.Label _lblRollVal;
        private System.Windows.Forms.Button _btnCenter;
        private System.Windows.Forms.Label lblFlagProbe;
        private System.Windows.Forms.Button _btnTakeoff;   // repurposed: flag pulse 0x01
        private System.Windows.Forms.Button _btnLand;      // repurposed: flag pulse 0x02
        private System.Windows.Forms.Button _btnRth;       // repurposed: flag pulse 0x04
        private System.Windows.Forms.Button _btnEstop;     // repurposed: flag pulse 0x08
        private System.Windows.Forms.Button _btnBit10;
        private System.Windows.Forms.Button _btnBit20;
        private System.Windows.Forms.Label lblCustomFlag;
        private System.Windows.Forms.TextBox _txtFlagIdx;
        private System.Windows.Forms.TextBox _txtFlag;
        private System.Windows.Forms.Button _btnFlagPulse;
        private System.Windows.Forms.Button _btnFlagHold;
        private System.Windows.Forms.Button _btnLog5;
        // experimental FlySendInfo command guesses (byte/bit positions UNVERIFIED)
        private System.Windows.Forms.Label lblCmdExp;
        private System.Windows.Forms.Button _btnCmdPhoto;
        private System.Windows.Forms.Button _btnCmdVideo;
        private System.Windows.Forms.Button _btnCmdTakeoff;
        private System.Windows.Forms.Button _btnCmdLand;
        private System.Windows.Forms.Button _btnCmdGoHome;
        private System.Windows.Forms.Button _btnCmdStop;
        // named extras (FlySendInfo / Heliway §6a — UNVERIFIED wire bytes)
        private System.Windows.Forms.Button _btnEmergLand;
        private System.Windows.Forms.Button _btnFlip;
        private System.Windows.Forms.Button _btnHeadless;
        private System.Windows.Forms.Button _btnAltHold;
        // Heliway arm/control probes
        private System.Windows.Forms.Label lblHeliway;
        private System.Windows.Forms.Button _btnProbeLockUnlock;
        private System.Windows.Forms.Button _btnProbeNext;
        private System.Windows.Forms.Button _btnProbeMode;
        private System.Windows.Forms.Button _btnCtrlPanelHold;

        // ---- Log ----
        private System.Windows.Forms.Panel grpLog;
        private System.Windows.Forms.TextBox _txtLog;

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
            grpConn = new Panel();
            lblIpCaption = new Label();
            _txtIp = new TextBox();
            _btnConnect = new Button();
            _lblStatus = new Label();
            _btnMenu = new Button();
            _btnConsole = new Button();
            _btnWorkbench = new Button();
            _btnLiveDebug = new Button();
            grpTel = new Panel();
            _lblModel = new Label();
            _lblFirmware = new Label();
            _lblGps = new Label();
            _lblSats = new Label();
            _lblBattery = new Label();
            _lblYaw = new Label();
            _lblRoll = new Label();
            _lblPitch = new Label();
            _dialYaw = new Panel();
            _dialRoll = new Panel();
            _dialPitch = new Panel();
            _lblMode = new Label();
            _lblRaw = new Label();
            lblTelRate = new Label();
            cboTelRate = new ComboBox();
            grpCam = new Panel();
            _btnTime = new Button();
            _btnRecOn = new Button();
            _btnRecOff = new Button();
            _btnVideo = new Button();
            lblCustomCmd = new Label();
            _txtCustomCmd = new TextBox();
            lblCustomParam = new Label();
            _txtCustomParam = new TextBox();
            _btnSendCustom = new Button();
            lblParamHint = new Label();
            lblSweep = new Label();
            _txtSweepFrom = new TextBox();
            _txtSweepTo = new TextBox();
            _btnSweep = new Button();
            _btnInfo = new Button();
            pnlVideo = new Panel();
            _videoPanel = new VideoPanel();
            grpGim = new Panel();
            lblTilt = new Label();
            _tbGimbal = new TrackBar();
            _lblGimbalVal = new Label();
            _btnGimDown = new Button();
            _btnGimUp = new Button();
            _btnGimCenter = new Button();
            lblManual = new Label();
            _txtGimP0 = new TextBox();
            _txtGimP1 = new TextBox();
            _txtGimP2 = new TextBox();
            _btnGimManual = new Button();
            lblReplay = new Label();
            _btnCap224 = new Button();
            _btnCap191 = new Button();
            _btnCap255 = new Button();
            _btnCap115 = new Button();
            _btnCap241 = new Button();
            _btnCap99 = new Button();
            _btnFlag40 = new Button();
            _btnFlag80 = new Button();
            lblWorks = new Label();
            lblGimHint = new Label();
            grpFly = new Panel();
            _chkArm = new CheckBox();
            _chk23333 = new CheckBox();
            lblThrottle = new Label();
            _tbThrottle = new TrackBar();
            _lblThrottleVal = new Label();
            _btnThrUp = new Button();
            _btnThrDn = new Button();
            lblYaw = new Label();
            _tbYaw = new TrackBar();
            _lblYawVal = new Label();
            lblPitch = new Label();
            _tbPitch = new TrackBar();
            _lblPitchVal = new Label();
            lblRoll = new Label();
            _tbRoll = new TrackBar();
            _lblRollVal = new Label();
            _btnCenter = new Button();
            lblFlagProbe = new Label();
            _btnTakeoff = new Button();
            _btnLand = new Button();
            _btnRth = new Button();
            _btnEstop = new Button();
            _btnBit10 = new Button();
            _btnBit20 = new Button();
            lblCustomFlag = new Label();
            _txtFlagIdx = new TextBox();
            _txtFlag = new TextBox();
            _btnFlagPulse = new Button();
            _btnFlagHold = new Button();
            _btnLog5 = new Button();
            lblCmdExp = new Label();
            _btnCmdPhoto = new Button();
            _btnCmdVideo = new Button();
            _btnCmdTakeoff = new Button();
            _btnCmdLand = new Button();
            _btnCmdGoHome = new Button();
            _btnCmdStop = new Button();
            _btnEmergLand = new Button();
            _btnFlip = new Button();
            _btnHeadless = new Button();
            _btnAltHold = new Button();
            lblHeliway = new Label();
            _btnProbeLockUnlock = new Button();
            _btnProbeNext = new Button();
            _btnProbeMode = new Button();
            _btnCtrlPanelHold = new Button();
            grpLog = new Panel();
            _txtLog = new TextBox();
            grpSend = new Panel();
            _chkChangeDetect = new CheckBox();
            lblLoopHz = new Label();
            _numControlHz = new NumericUpDown();
            lblHbHz = new Label();
            _numHeartbeatHz = new NumericUpDown();
            grpConn.SuspendLayout();
            grpTel.SuspendLayout();
            grpCam.SuspendLayout();
            pnlVideo.SuspendLayout();
            grpGim.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_tbGimbal).BeginInit();
            grpFly.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_tbThrottle).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_tbYaw).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_tbPitch).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_tbRoll).BeginInit();
            grpLog.SuspendLayout();
            grpSend.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_numControlHz).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_numHeartbeatHz).BeginInit();
            SuspendLayout();
            // 
            // grpConn
            // 
            grpConn.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            grpConn.Controls.Add(lblIpCaption);
            grpConn.Controls.Add(_txtIp);
            grpConn.Controls.Add(_btnConnect);
            grpConn.Controls.Add(_lblStatus);
            grpConn.Controls.Add(_btnMenu);
            grpConn.Controls.Add(_btnConsole);
            grpConn.Controls.Add(_btnWorkbench);
            grpConn.Controls.Add(_btnLiveDebug);
            grpConn.Location = new Point(7, 13);
            grpConn.Margin = new Padding(3, 4, 3, 4);
            grpConn.Name = "grpConn";
            grpConn.Size = new Size(1550, 69);
            grpConn.TabIndex = 0;
            grpConn.Text = "Connection";
            // 
            // lblIpCaption
            // 
            lblIpCaption.AutoSize = true;
            lblIpCaption.Location = new Point(14, 35);
            lblIpCaption.Name = "lblIpCaption";
            lblIpCaption.Size = new Size(69, 20);
            lblIpCaption.TabIndex = 0;
            lblIpCaption.Text = "Drone IP:";
            // 
            // _txtIp
            // 
            _txtIp.Location = new Point(89, 31);
            _txtIp.Margin = new Padding(3, 4, 3, 4);
            _txtIp.Name = "_txtIp";
            _txtIp.Size = new Size(137, 27);
            _txtIp.TabIndex = 1;
            _txtIp.Text = "192.168.100.1";
            // 
            // _btnConnect
            // 
            _btnConnect.Location = new Point(240, 28);
            _btnConnect.Margin = new Padding(3, 4, 3, 4);
            _btnConnect.Name = "_btnConnect";
            _btnConnect.Size = new Size(103, 31);
            _btnConnect.TabIndex = 2;
            _btnConnect.Text = "Connect";
            _btnConnect.Click += OnConnectClick;
            // 
            // _lblStatus
            // 
            _lblStatus.AutoSize = true;
            _lblStatus.ForeColor = Color.Firebrick;
            _lblStatus.Location = new Point(357, 35);
            _lblStatus.Name = "_lblStatus";
            _lblStatus.Size = new Size(99, 20);
            _lblStatus.TabIndex = 3;
            _lblStatus.Text = "Disconnected";
            // 
            // _btnMenu
            // 
            _btnMenu.Location = new Point(469, 19);
            _btnMenu.Margin = new Padding(3, 4, 3, 4);
            _btnMenu.Name = "_btnMenu";
            _btnMenu.Size = new Size(96, 32);
            _btnMenu.TabIndex = 7;
            _btnMenu.Text = "☰ Menu";
            _btnMenu.Click += BtnMenu_Click;
            // 
            // _btnConsole
            // 
            _btnConsole.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _btnConsole.Location = new Point(914, 28);
            _btnConsole.Margin = new Padding(3, 4, 3, 4);
            _btnConsole.Name = "_btnConsole";
            _btnConsole.Size = new Size(151, 31);
            _btnConsole.TabIndex = 4;
            _btnConsole.Text = "Drone Console…";
            _btnConsole.Click += BtnConsole_Click;
            // 
            // _btnWorkbench
            // 
            _btnWorkbench.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _btnWorkbench.Location = new Point(754, 28);
            _btnWorkbench.Margin = new Padding(3, 4, 3, 4);
            _btnWorkbench.Name = "_btnWorkbench";
            _btnWorkbench.Size = new Size(151, 31);
            _btnWorkbench.TabIndex = 5;
            _btnWorkbench.Text = "RE Workbench…";
            _btnWorkbench.Click += BtnWorkbench_Click;
            // 
            // _btnLiveDebug
            // 
            _btnLiveDebug.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _btnLiveDebug.Location = new Point(594, 28);
            _btnLiveDebug.Margin = new Padding(3, 4, 3, 4);
            _btnLiveDebug.Name = "_btnLiveDebug";
            _btnLiveDebug.Size = new Size(151, 31);
            _btnLiveDebug.TabIndex = 6;
            _btnLiveDebug.Text = "Live Debug ▶";
            _btnLiveDebug.Click += BtnLiveDebug_Click;
            // 
            // grpTel
            // 
            grpTel.Controls.Add(_lblModel);
            grpTel.Controls.Add(_lblFirmware);
            grpTel.Controls.Add(_lblGps);
            grpTel.Controls.Add(_lblSats);
            grpTel.Controls.Add(_lblBattery);
            grpTel.Controls.Add(_lblYaw);
            grpTel.Controls.Add(_lblRoll);
            grpTel.Controls.Add(_lblPitch);
            grpTel.Controls.Add(_dialYaw);
            grpTel.Controls.Add(_dialRoll);
            grpTel.Controls.Add(_dialPitch);
            grpTel.Controls.Add(_lblMode);
            grpTel.Controls.Add(_lblRaw);
            grpTel.Controls.Add(lblTelRate);
            grpTel.Controls.Add(cboTelRate);
            grpTel.Location = new Point(7, 93);
            grpTel.Margin = new Padding(3, 4, 3, 4);
            grpTel.Name = "grpTel";
            grpTel.Size = new Size(512, 224);
            grpTel.TabIndex = 1;
            grpTel.Text = "Telemetry (UDP 58xx)";
            // 
            // _lblModel
            // 
            _lblModel.AutoSize = true;
            _lblModel.Location = new Point(14, 32);
            _lblModel.Name = "_lblModel";
            _lblModel.Size = new Size(74, 20);
            _lblModel.TabIndex = 0;
            _lblModel.Text = "Model: —";
            // 
            // _lblFirmware
            // 
            _lblFirmware.AutoSize = true;
            _lblFirmware.Location = new Point(14, 64);
            _lblFirmware.Name = "_lblFirmware";
            _lblFirmware.Size = new Size(92, 20);
            _lblFirmware.TabIndex = 1;
            _lblFirmware.Text = "Firmware: —";
            // 
            // _lblGps
            // 
            _lblGps.AutoSize = true;
            _lblGps.Location = new Point(14, 96);
            _lblGps.Name = "_lblGps";
            _lblGps.Size = new Size(57, 20);
            _lblGps.TabIndex = 2;
            _lblGps.Text = "GPS: —";
            // 
            // _lblSats
            // 
            _lblSats.AutoSize = true;
            _lblSats.Location = new Point(14, 128);
            _lblSats.Name = "_lblSats";
            _lblSats.Size = new Size(110, 20);
            _lblSats.TabIndex = 3;
            _lblSats.Text = "Sats: —   Fix: —";
            // 
            // _lblBattery
            // 
            _lblBattery.AutoSize = true;
            _lblBattery.Location = new Point(251, 128);
            _lblBattery.Name = "_lblBattery";
            _lblBattery.Size = new Size(58, 20);
            _lblBattery.TabIndex = 6;
            _lblBattery.Text = "Batt: —";
            // 
            // _lblYaw
            // 
            _lblYaw.AutoSize = true;
            _lblYaw.Location = new Point(251, 96);
            _lblYaw.Name = "_lblYaw";
            _lblYaw.Size = new Size(57, 20);
            _lblYaw.TabIndex = 7;
            _lblYaw.Text = "Yaw: —";
            // 
            // _lblRoll
            // 
            _lblRoll.AutoSize = true;
            _lblRoll.Location = new Point(251, 32);
            _lblRoll.Name = "_lblRoll";
            _lblRoll.Size = new Size(57, 20);
            _lblRoll.TabIndex = 8;
            _lblRoll.Text = "Roll: —";
            // 
            // _lblPitch
            // 
            _lblPitch.AutoSize = true;
            _lblPitch.Location = new Point(251, 64);
            _lblPitch.Name = "_lblPitch";
            _lblPitch.Size = new Size(63, 20);
            _lblPitch.TabIndex = 9;
            _lblPitch.Text = "Pitch: —";
            // 
            // _dialYaw
            // 
            _dialYaw.Location = new Point(331, 93);
            _dialYaw.Margin = new Padding(3, 4, 3, 4);
            _dialYaw.Name = "_dialYaw";
            _dialYaw.Size = new Size(23, 27);
            _dialYaw.TabIndex = 12;
            _dialYaw.Paint += DialYaw_Paint;
            // 
            // _dialRoll
            // 
            _dialRoll.Location = new Point(331, 29);
            _dialRoll.Margin = new Padding(3, 4, 3, 4);
            _dialRoll.Name = "_dialRoll";
            _dialRoll.Size = new Size(23, 27);
            _dialRoll.TabIndex = 10;
            _dialRoll.Paint += DialRoll_Paint;
            // 
            // _dialPitch
            // 
            _dialPitch.Location = new Point(331, 61);
            _dialPitch.Margin = new Padding(3, 4, 3, 4);
            _dialPitch.Name = "_dialPitch";
            _dialPitch.Size = new Size(23, 27);
            _dialPitch.TabIndex = 11;
            _dialPitch.Paint += DialPitch_Paint;
            // 
            // _lblMode
            // 
            _lblMode.AutoSize = true;
            _lblMode.Location = new Point(14, 160);
            _lblMode.Name = "_lblMode";
            _lblMode.Size = new Size(70, 20);
            _lblMode.TabIndex = 4;
            _lblMode.Text = "Mode: —";
            // 
            // _lblRaw
            // 
            _lblRaw.AutoSize = true;
            _lblRaw.ForeColor = Color.Gray;
            _lblRaw.Location = new Point(14, 192);
            _lblRaw.Name = "_lblRaw";
            _lblRaw.Size = new Size(54, 20);
            _lblRaw.TabIndex = 5;
            _lblRaw.Text = "last: —";
            // 
            // lblTelRate
            // 
            lblTelRate.AutoSize = true;
            lblTelRate.Location = new Point(286, 32);
            lblTelRate.Name = "lblTelRate";
            lblTelRate.Size = new Size(56, 20);
            lblTelRate.TabIndex = 13;
            lblTelRate.Text = "UI rate:";
            // 
            // cboTelRate
            // 
            cboTelRate.DropDownStyle = ComboBoxStyle.DropDownList;
            cboTelRate.Items.AddRange(new object[] { "10 Hz", "5 Hz", "2 Hz", "1 Hz" });
            cboTelRate.Location = new Point(347, 27);
            cboTelRate.Margin = new Padding(3, 4, 3, 4);
            cboTelRate.Name = "cboTelRate";
            cboTelRate.Size = new Size(102, 28);
            cboTelRate.TabIndex = 7;
            cboTelRate.SelectedIndexChanged += cboTelRate_Changed;
            // 
            // grpCam
            // 
            grpCam.Controls.Add(_btnTime);
            grpCam.Controls.Add(_btnRecOn);
            grpCam.Controls.Add(_btnRecOff);
            grpCam.Controls.Add(_btnVideo);
            grpCam.Controls.Add(lblCustomCmd);
            grpCam.Controls.Add(_txtCustomCmd);
            grpCam.Controls.Add(lblCustomParam);
            grpCam.Controls.Add(_txtCustomParam);
            grpCam.Controls.Add(_btnSendCustom);
            grpCam.Controls.Add(lblParamHint);
            grpCam.Controls.Add(lblSweep);
            grpCam.Controls.Add(_txtSweepFrom);
            grpCam.Controls.Add(_txtSweepTo);
            grpCam.Controls.Add(_btnSweep);
            grpCam.Controls.Add(_btnInfo);
            grpCam.Location = new Point(526, 424);
            grpCam.Margin = new Padding(3, 4, 3, 4);
            grpCam.Name = "grpCam";
            grpCam.Size = new Size(512, 200);
            grpCam.TabIndex = 2;
            grpCam.Text = "Camera / JSON (TCP 4646)";
            // 
            // _btnTime
            // 
            _btnTime.Location = new Point(14, 32);
            _btnTime.Margin = new Padding(3, 4, 3, 4);
            _btnTime.Name = "_btnTime";
            _btnTime.Size = new Size(103, 31);
            _btnTime.TabIndex = 0;
            _btnTime.Text = "Time Sync";
            _btnTime.Click += BtnTime_Click;
            // 
            // _btnRecOn
            // 
            _btnRecOn.Location = new Point(123, 32);
            _btnRecOn.Margin = new Padding(3, 4, 3, 4);
            _btnRecOn.Name = "_btnRecOn";
            _btnRecOn.Size = new Size(91, 31);
            _btnRecOn.TabIndex = 1;
            _btnRecOn.Text = "Rec Start";
            _btnRecOn.Click += BtnRecOn_Click;
            // 
            // _btnRecOff
            // 
            _btnRecOff.Location = new Point(222, 32);
            _btnRecOff.Margin = new Padding(3, 4, 3, 4);
            _btnRecOff.Name = "_btnRecOff";
            _btnRecOff.Size = new Size(91, 31);
            _btnRecOff.TabIndex = 2;
            _btnRecOff.Text = "Rec Stop";
            _btnRecOff.Click += BtnRecOff_Click;
            // 
            // _btnVideo
            // 
            _btnVideo.Location = new Point(325, 32);
            _btnVideo.Margin = new Padding(3, 4, 3, 4);
            _btnVideo.Name = "_btnVideo";
            _btnVideo.Size = new Size(171, 31);
            _btnVideo.TabIndex = 3;
            _btnVideo.Text = "Open Video (RTSP)";
            _btnVideo.Click += BtnVideo_Click;
            // 
            // lblCustomCmd
            // 
            lblCustomCmd.AutoSize = true;
            lblCustomCmd.Location = new Point(14, 85);
            lblCustomCmd.Name = "lblCustomCmd";
            lblCustomCmd.Size = new Size(99, 20);
            lblCustomCmd.TabIndex = 4;
            lblCustomCmd.Text = "Custom CMD:";
            // 
            // _txtCustomCmd
            // 
            _txtCustomCmd.Location = new Point(105, 81);
            _txtCustomCmd.Margin = new Padding(3, 4, 3, 4);
            _txtCustomCmd.Name = "_txtCustomCmd";
            _txtCustomCmd.Size = new Size(57, 27);
            _txtCustomCmd.TabIndex = 5;
            _txtCustomCmd.Text = "0";
            // 
            // lblCustomParam
            // 
            lblCustomParam.AutoSize = true;
            lblCustomParam.Location = new Point(171, 85);
            lblCustomParam.Name = "lblCustomParam";
            lblCustomParam.Size = new Size(61, 20);
            lblCustomParam.TabIndex = 6;
            lblCustomParam.Text = "PARAM:";
            // 
            // _txtCustomParam
            // 
            _txtCustomParam.Location = new Point(229, 81);
            _txtCustomParam.Margin = new Padding(3, 4, 3, 4);
            _txtCustomParam.Name = "_txtCustomParam";
            _txtCustomParam.Size = new Size(171, 27);
            _txtCustomParam.TabIndex = 7;
            _txtCustomParam.Text = "-1";
            // 
            // _btnSendCustom
            // 
            _btnSendCustom.Location = new Point(407, 79);
            _btnSendCustom.Margin = new Padding(3, 4, 3, 4);
            _btnSendCustom.Name = "_btnSendCustom";
            _btnSendCustom.Size = new Size(98, 31);
            _btnSendCustom.TabIndex = 8;
            _btnSendCustom.Text = "Send";
            _btnSendCustom.Click += BtnSendCustom_Click;
            // 
            // lblParamHint
            // 
            lblParamHint.AutoSize = true;
            lblParamHint.ForeColor = Color.Gray;
            lblParamHint.Location = new Point(14, 123);
            lblParamHint.Name = "lblParamHint";
            lblParamHint.Size = new Size(303, 20);
            lblParamHint.TabIndex = 9;
            lblParamHint.Text = "PARAM is raw JSON, e.g.  -1  or  {\"camera\":0}";
            // 
            // lblSweep
            // 
            lblSweep.AutoSize = true;
            lblSweep.Location = new Point(14, 157);
            lblSweep.Name = "lblSweep";
            lblSweep.Size = new Size(90, 20);
            lblSweep.TabIndex = 10;
            lblSweep.Text = "Sweep CMD";
            // 
            // _txtSweepFrom
            // 
            _txtSweepFrom.Location = new Point(105, 153);
            _txtSweepFrom.Margin = new Padding(3, 4, 3, 4);
            _txtSweepFrom.Name = "_txtSweepFrom";
            _txtSweepFrom.Size = new Size(50, 27);
            _txtSweepFrom.TabIndex = 11;
            _txtSweepFrom.Text = "0";
            // 
            // _txtSweepTo
            // 
            _txtSweepTo.Location = new Point(183, 153);
            _txtSweepTo.Margin = new Padding(3, 4, 3, 4);
            _txtSweepTo.Name = "_txtSweepTo";
            _txtSweepTo.Size = new Size(50, 27);
            _txtSweepTo.TabIndex = 12;
            _txtSweepTo.Text = "200";
            // 
            // _btnSweep
            // 
            _btnSweep.Location = new Point(242, 151);
            _btnSweep.Margin = new Padding(3, 4, 3, 4);
            _btnSweep.Name = "_btnSweep";
            _btnSweep.Size = new Size(126, 33);
            _btnSweep.TabIndex = 13;
            _btnSweep.Text = "Run sweep";
            _btnSweep.Click += BtnSweep_Click;
            // 
            // _btnInfo
            // 
            _btnInfo.Location = new Point(377, 151);
            _btnInfo.Margin = new Padding(3, 4, 3, 4);
            _btnInfo.Name = "_btnInfo";
            _btnInfo.Size = new Size(128, 33);
            _btnInfo.TabIndex = 14;
            _btnInfo.Text = "Drone Info";
            _btnInfo.Click += BtnInfo_Click;
            // 
            // pnlVideo
            // 
            pnlVideo.Controls.Add(_videoPanel);
            pnlVideo.Location = new Point(526, 93);
            pnlVideo.Margin = new Padding(3, 4, 3, 4);
            pnlVideo.Name = "pnlVideo";
            pnlVideo.Padding = new Padding(1, 31, 1, 12);
            pnlVideo.Size = new Size(512, 320);
            pnlVideo.TabIndex = 5;
            pnlVideo.Text = "Live video";
            // 
            // _videoPanel
            // 
            _videoPanel.Dock = DockStyle.Fill;
            _videoPanel.Location = new Point(1, 31);
            _videoPanel.Margin = new Padding(3, 4, 3, 4);
            _videoPanel.Name = "_videoPanel";
            _videoPanel.Size = new Size(510, 277);
            _videoPanel.TabIndex = 0;
            // 
            // grpGim
            // 
            grpGim.Controls.Add(lblTilt);
            grpGim.Controls.Add(_tbGimbal);
            grpGim.Controls.Add(_lblGimbalVal);
            grpGim.Controls.Add(_btnGimDown);
            grpGim.Controls.Add(_btnGimUp);
            grpGim.Controls.Add(_btnGimCenter);
            grpGim.Controls.Add(lblManual);
            grpGim.Controls.Add(_txtGimP0);
            grpGim.Controls.Add(_txtGimP1);
            grpGim.Controls.Add(_txtGimP2);
            grpGim.Controls.Add(_btnGimManual);
            grpGim.Controls.Add(lblReplay);
            grpGim.Controls.Add(_btnCap224);
            grpGim.Controls.Add(_btnCap191);
            grpGim.Controls.Add(_btnCap255);
            grpGim.Controls.Add(_btnCap115);
            grpGim.Controls.Add(_btnCap241);
            grpGim.Controls.Add(_btnCap99);
            grpGim.Controls.Add(_btnFlag40);
            grpGim.Controls.Add(_btnFlag80);
            grpGim.Controls.Add(lblWorks);
            grpGim.Controls.Add(lblGimHint);
            grpGim.Location = new Point(7, 327);
            grpGim.Margin = new Padding(3, 4, 3, 4);
            grpGim.Name = "grpGim";
            grpGim.Size = new Size(512, 387);
            grpGim.TabIndex = 3;
            grpGim.Text = "Gimbal";
            // 
            // lblTilt
            // 
            lblTilt.AutoSize = true;
            lblTilt.Location = new Point(14, 37);
            lblTilt.Name = "lblTilt";
            lblTilt.Size = new Size(63, 20);
            lblTilt.TabIndex = 0;
            lblTilt.Text = "Tilt (P1):";
            // 
            // _tbGimbal
            // 
            _tbGimbal.AutoSize = false;
            _tbGimbal.Enabled = false;
            _tbGimbal.Location = new Point(86, 29);
            _tbGimbal.Margin = new Padding(3, 4, 3, 4);
            _tbGimbal.Maximum = 255;
            _tbGimbal.Name = "_tbGimbal";
            _tbGimbal.Size = new Size(320, 45);
            _tbGimbal.TabIndex = 1;
            _tbGimbal.TickFrequency = 32;
            _tbGimbal.Value = 128;
            _tbGimbal.Scroll += TbGimbal_Scroll;
            // 
            // _lblGimbalVal
            // 
            _lblGimbalVal.AutoSize = true;
            _lblGimbalVal.Location = new Point(411, 37);
            _lblGimbalVal.Name = "_lblGimbalVal";
            _lblGimbalVal.Size = new Size(33, 20);
            _lblGimbalVal.TabIndex = 2;
            _lblGimbalVal.Text = "128";
            // 
            // _btnGimDown
            // 
            _btnGimDown.Enabled = false;
            _btnGimDown.Location = new Point(14, 83);
            _btnGimDown.Margin = new Padding(3, 4, 3, 4);
            _btnGimDown.Name = "_btnGimDown";
            _btnGimDown.Size = new Size(126, 31);
            _btnGimDown.TabIndex = 3;
            _btnGimDown.Text = "◀ Tilt Down";
            _btnGimDown.Click += BtnGimDown_Click;
            // 
            // _btnGimUp
            // 
            _btnGimUp.Enabled = false;
            _btnGimUp.Location = new Point(146, 83);
            _btnGimUp.Margin = new Padding(3, 4, 3, 4);
            _btnGimUp.Name = "_btnGimUp";
            _btnGimUp.Size = new Size(126, 31);
            _btnGimUp.TabIndex = 4;
            _btnGimUp.Text = "Tilt Up ▶";
            _btnGimUp.Click += BtnGimUp_Click;
            // 
            // _btnGimCenter
            // 
            _btnGimCenter.Enabled = false;
            _btnGimCenter.Location = new Point(279, 83);
            _btnGimCenter.Margin = new Padding(3, 4, 3, 4);
            _btnGimCenter.Name = "_btnGimCenter";
            _btnGimCenter.Size = new Size(126, 31);
            _btnGimCenter.TabIndex = 5;
            _btnGimCenter.Text = "Center (128)";
            _btnGimCenter.Click += BtnGimCenter_Click;
            // 
            // lblManual
            // 
            lblManual.AutoSize = true;
            lblManual.Location = new Point(14, 139);
            lblManual.Name = "lblManual";
            lblManual.Size = new Size(158, 20);
            lblManual.TabIndex = 6;
            lblManual.Text = "Manual P0 P1 P2 (hex):";
            // 
            // _txtGimP0
            // 
            _txtGimP0.Enabled = false;
            _txtGimP0.Location = new Point(171, 135);
            _txtGimP0.Margin = new Padding(3, 4, 3, 4);
            _txtGimP0.Name = "_txtGimP0";
            _txtGimP0.Size = new Size(41, 27);
            _txtGimP0.TabIndex = 7;
            _txtGimP0.Text = "0B";
            // 
            // _txtGimP1
            // 
            _txtGimP1.Enabled = false;
            _txtGimP1.Location = new Point(219, 135);
            _txtGimP1.Margin = new Padding(3, 4, 3, 4);
            _txtGimP1.Name = "_txtGimP1";
            _txtGimP1.Size = new Size(41, 27);
            _txtGimP1.TabIndex = 8;
            _txtGimP1.Text = "80";
            // 
            // _txtGimP2
            // 
            _txtGimP2.Enabled = false;
            _txtGimP2.Location = new Point(267, 135);
            _txtGimP2.Margin = new Padding(3, 4, 3, 4);
            _txtGimP2.Name = "_txtGimP2";
            _txtGimP2.Size = new Size(41, 27);
            _txtGimP2.TabIndex = 9;
            _txtGimP2.Text = "1B";
            // 
            // _btnGimManual
            // 
            _btnGimManual.Enabled = false;
            _btnGimManual.Location = new Point(322, 132);
            _btnGimManual.Margin = new Padding(3, 4, 3, 4);
            _btnGimManual.Name = "_btnGimManual";
            _btnGimManual.Size = new Size(126, 31);
            _btnGimManual.TabIndex = 10;
            _btnGimManual.Text = "Send manual";
            _btnGimManual.Click += BtnGimManual_Click;
            // 
            // lblReplay
            // 
            lblReplay.AutoSize = true;
            lblReplay.Location = new Point(14, 187);
            lblReplay.Name = "lblReplay";
            lblReplay.Size = new Size(120, 20);
            lblReplay.TabIndex = 11;
            lblReplay.Text = "Replay captured:";
            // 
            // _btnCap224
            // 
            _btnCap224.Enabled = false;
            _btnCap224.Location = new Point(137, 183);
            _btnCap224.Margin = new Padding(3, 4, 3, 4);
            _btnCap224.Name = "_btnCap224";
            _btnCap224.Size = new Size(66, 31);
            _btnCap224.TabIndex = 12;
            _btnCap224.Tag = "88070BE01CF799";
            _btnCap224.Text = "224";
            _btnCap224.Click += GimCapture_Click;
            // 
            // _btnCap191
            // 
            _btnCap191.Enabled = false;
            _btnCap191.Location = new Point(208, 183);
            _btnCap191.Margin = new Padding(3, 4, 3, 4);
            _btnCap191.Name = "_btnCap191";
            _btnCap191.Size = new Size(66, 31);
            _btnCap191.TabIndex = 13;
            _btnCap191.Tag = "88070BBF1BAF99";
            _btnCap191.Text = "191";
            _btnCap191.Click += GimCapture_Click;
            // 
            // _btnCap255
            // 
            _btnCap255.Enabled = false;
            _btnCap255.Location = new Point(279, 183);
            _btnCap255.Margin = new Padding(3, 4, 3, 4);
            _btnCap255.Name = "_btnCap255";
            _btnCap255.Size = new Size(66, 31);
            _btnCap255.TabIndex = 14;
            _btnCap255.Tag = "88070BFF1DE999";
            _btnCap255.Text = "255";
            _btnCap255.Click += GimCapture_Click;
            // 
            // _btnCap115
            // 
            _btnCap115.Enabled = false;
            _btnCap115.Location = new Point(137, 225);
            _btnCap115.Margin = new Padding(3, 4, 3, 4);
            _btnCap115.Name = "_btnCap115";
            _btnCap115.Size = new Size(66, 31);
            _btnCap115.TabIndex = 15;
            _btnCap115.Tag = "88070C731C6399";
            _btnCap115.Text = "115";
            _btnCap115.Click += GimCapture_Click;
            // 
            // _btnCap241
            // 
            _btnCap241.Enabled = false;
            _btnCap241.Location = new Point(208, 225);
            _btnCap241.Margin = new Padding(3, 4, 3, 4);
            _btnCap241.Name = "_btnCap241";
            _btnCap241.Size = new Size(66, 31);
            _btnCap241.TabIndex = 16;
            _btnCap241.Tag = "88070BF11BE199";
            _btnCap241.Text = "241";
            _btnCap241.Click += GimCapture_Click;
            // 
            // _btnCap99
            // 
            _btnCap99.Enabled = false;
            _btnCap99.Location = new Point(279, 225);
            _btnCap99.Margin = new Padding(3, 4, 3, 4);
            _btnCap99.Name = "_btnCap99";
            _btnCap99.Size = new Size(66, 31);
            _btnCap99.TabIndex = 17;
            _btnCap99.Tag = "88070763177399";
            _btnCap99.Text = "99";
            _btnCap99.Click += GimCapture_Click;
            // 
            // _btnFlag40
            // 
            _btnFlag40.BackColor = Color.FromArgb(206, 240, 206);
            _btnFlag40.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold);
            _btnFlag40.ForeColor = Color.FromArgb(20, 40, 20);
            _btnFlag40.Location = new Point(192, 296);
            _btnFlag40.Margin = new Padding(3, 4, 3, 4);
            _btnFlag40.Name = "_btnFlag40";
            _btnFlag40.Size = new Size(171, 37);
            _btnFlag40.TabIndex = 18;
            _btnFlag40.Text = "▼ Tilt Down (hold)";
            _btnFlag40.UseVisualStyleBackColor = false;
            _btnFlag40.MouseDown += GimDown_MouseDown;
            _btnFlag40.MouseUp += Gim_MouseUp;
            // 
            // _btnFlag80
            // 
            _btnFlag80.BackColor = Color.FromArgb(206, 240, 206);
            _btnFlag80.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold);
            _btnFlag80.ForeColor = Color.FromArgb(20, 40, 20);
            _btnFlag80.Location = new Point(14, 296);
            _btnFlag80.Margin = new Padding(3, 4, 3, 4);
            _btnFlag80.Name = "_btnFlag80";
            _btnFlag80.Size = new Size(171, 37);
            _btnFlag80.TabIndex = 19;
            _btnFlag80.Text = "▲ Tilt Up (hold)";
            _btnFlag80.UseVisualStyleBackColor = false;
            _btnFlag80.MouseDown += GimUp_MouseDown;
            _btnFlag80.MouseUp += Gim_MouseUp;
            // 
            // lblWorks
            // 
            lblWorks.AutoSize = true;
            lblWorks.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblWorks.ForeColor = Color.Green;
            lblWorks.Location = new Point(14, 267);
            lblWorks.Name = "lblWorks";
            lblWorks.Size = new Size(351, 20);
            lblWorks.TabIndex = 20;
            lblWorks.Text = "Gimbal tilt — press & hold (CONFIRMED working):";
            // 
            // lblGimHint
            // 
            lblGimHint.AutoSize = true;
            lblGimHint.ForeColor = Color.Gray;
            lblGimHint.Location = new Point(14, 352);
            lblGimHint.Name = "lblGimHint";
            lblGimHint.Size = new Size(392, 20);
            lblGimHint.TabIndex = 21;
            lblGimHint.Text = "88 07 controls above showed no effect — kept for testing.";
            // 
            // grpFly
            // 
            grpFly.Controls.Add(_chkArm);
            grpFly.Controls.Add(_chk23333);
            grpFly.Controls.Add(lblThrottle);
            grpFly.Controls.Add(_tbThrottle);
            grpFly.Controls.Add(_lblThrottleVal);
            grpFly.Controls.Add(_btnThrUp);
            grpFly.Controls.Add(_btnThrDn);
            grpFly.Controls.Add(lblYaw);
            grpFly.Controls.Add(_tbYaw);
            grpFly.Controls.Add(_lblYawVal);
            grpFly.Controls.Add(lblPitch);
            grpFly.Controls.Add(_tbPitch);
            grpFly.Controls.Add(_lblPitchVal);
            grpFly.Controls.Add(lblRoll);
            grpFly.Controls.Add(_tbRoll);
            grpFly.Controls.Add(_lblRollVal);
            grpFly.Controls.Add(_btnCenter);
            grpFly.Controls.Add(lblFlagProbe);
            grpFly.Controls.Add(_btnTakeoff);
            grpFly.Controls.Add(_btnLand);
            grpFly.Controls.Add(_btnRth);
            grpFly.Controls.Add(_btnEstop);
            grpFly.Controls.Add(_btnBit10);
            grpFly.Controls.Add(_btnBit20);
            grpFly.Controls.Add(lblCustomFlag);
            grpFly.Controls.Add(_txtFlagIdx);
            grpFly.Controls.Add(_txtFlag);
            grpFly.Controls.Add(_btnFlagPulse);
            grpFly.Controls.Add(_btnFlagHold);
            grpFly.Controls.Add(_btnLog5);
            grpFly.Controls.Add(lblCmdExp);
            grpFly.Controls.Add(_btnCmdPhoto);
            grpFly.Controls.Add(_btnCmdVideo);
            grpFly.Controls.Add(_btnCmdTakeoff);
            grpFly.Controls.Add(_btnCmdLand);
            grpFly.Controls.Add(_btnCmdGoHome);
            grpFly.Controls.Add(_btnCmdStop);
            grpFly.Controls.Add(_btnEmergLand);
            grpFly.Controls.Add(_btnFlip);
            grpFly.Controls.Add(_btnHeadless);
            grpFly.Controls.Add(_btnAltHold);
            grpFly.Controls.Add(lblHeliway);
            grpFly.Controls.Add(_btnProbeLockUnlock);
            grpFly.Controls.Add(_btnProbeNext);
            grpFly.Controls.Add(_btnProbeMode);
            grpFly.Controls.Add(_btnCtrlPanelHold);
            grpFly.ForeColor = Color.Firebrick;
            grpFly.Location = new Point(1045, 93);
            grpFly.Margin = new Padding(3, 4, 3, 4);
            grpFly.Name = "grpFly";
            grpFly.Size = new Size(512, 640);
            grpFly.TabIndex = 4;
            grpFly.Text = "Flight control — UNVERIFIED · HOLD THE DRONE";
            // 
            // _chkArm
            // 
            _chkArm.AutoSize = true;
            _chkArm.ForeColor = Color.Black;
            _chkArm.Location = new Point(14, 32);
            _chkArm.Margin = new Padding(3, 4, 3, 4);
            _chkArm.Name = "_chkArm";
            _chkArm.Size = new Size(245, 24);
            _chkArm.TabIndex = 0;
            _chkArm.Text = "Enable flight commands (safety)";
            // 
            // _chk23333
            // 
            _chk23333.AutoSize = true;
            _chk23333.Checked = true;
            _chk23333.CheckState = CheckState.Checked;
            _chk23333.ForeColor = Color.Black;
            _chk23333.Location = new Point(251, 32);
            _chk23333.Margin = new Padding(3, 4, 3, 4);
            _chk23333.Name = "_chk23333";
            _chk23333.Size = new Size(160, 24);
            _chk23333.TabIndex = 27;
            _chk23333.Text = "23333 session (test)";
            // 
            // lblThrottle
            // 
            lblThrottle.AutoSize = true;
            lblThrottle.ForeColor = Color.Black;
            lblThrottle.Location = new Point(14, 75);
            lblThrottle.Name = "lblThrottle";
            lblThrottle.Size = new Size(61, 20);
            lblThrottle.TabIndex = 1;
            lblThrottle.Text = "Throttle";
            // 
            // _tbThrottle
            // 
            _tbThrottle.AutoSize = false;
            _tbThrottle.Location = new Point(91, 69);
            _tbThrottle.Margin = new Padding(3, 4, 3, 4);
            _tbThrottle.Maximum = 255;
            _tbThrottle.Name = "_tbThrottle";
            _tbThrottle.Size = new Size(343, 33);
            _tbThrottle.TabIndex = 2;
            _tbThrottle.TickFrequency = 32;
            _tbThrottle.Value = 128;
            _tbThrottle.ValueChanged += TbThrottle_ValueChanged;
            // 
            // _lblThrottleVal
            // 
            _lblThrottleVal.AutoSize = true;
            _lblThrottleVal.ForeColor = Color.Black;
            _lblThrottleVal.Location = new Point(443, 75);
            _lblThrottleVal.Name = "_lblThrottleVal";
            _lblThrottleVal.Size = new Size(33, 20);
            _lblThrottleVal.TabIndex = 3;
            _lblThrottleVal.Text = "128";
            // 
            // _btnThrUp
            // 
            _btnThrUp.Location = new Point(475, 67);
            _btnThrUp.Margin = new Padding(3, 4, 3, 4);
            _btnThrUp.Name = "_btnThrUp";
            _btnThrUp.Size = new Size(55, 29);
            _btnThrUp.TabIndex = 28;
            _btnThrUp.Text = "+5";
            _btnThrUp.UseVisualStyleBackColor = true;
            _btnThrUp.Click += BtnThrUp_Click;
            // 
            // _btnThrDn
            // 
            _btnThrDn.Location = new Point(475, 99);
            _btnThrDn.Margin = new Padding(3, 4, 3, 4);
            _btnThrDn.Name = "_btnThrDn";
            _btnThrDn.Size = new Size(55, 29);
            _btnThrDn.TabIndex = 29;
            _btnThrDn.Text = "-5";
            _btnThrDn.UseVisualStyleBackColor = true;
            _btnThrDn.Click += BtnThrDn_Click;
            // 
            // lblYaw
            // 
            lblYaw.AutoSize = true;
            lblYaw.ForeColor = Color.Black;
            lblYaw.Location = new Point(14, 128);
            lblYaw.Name = "lblYaw";
            lblYaw.Size = new Size(35, 20);
            lblYaw.TabIndex = 4;
            lblYaw.Text = "Yaw";
            // 
            // _tbYaw
            // 
            _tbYaw.Location = new Point(91, 123);
            _tbYaw.Margin = new Padding(3, 4, 3, 4);
            _tbYaw.Maximum = 255;
            _tbYaw.Name = "_tbYaw";
            _tbYaw.Size = new Size(343, 56);
            _tbYaw.TabIndex = 5;
            _tbYaw.TickFrequency = 32;
            _tbYaw.Value = 128;
            _tbYaw.ValueChanged += TbYaw_ValueChanged;
            // 
            // _lblYawVal
            // 
            _lblYawVal.AutoSize = true;
            _lblYawVal.ForeColor = Color.Black;
            _lblYawVal.Location = new Point(443, 128);
            _lblYawVal.Name = "_lblYawVal";
            _lblYawVal.Size = new Size(33, 20);
            _lblYawVal.TabIndex = 6;
            _lblYawVal.Text = "128";
            // 
            // lblPitch
            // 
            lblPitch.AutoSize = true;
            lblPitch.ForeColor = Color.Black;
            lblPitch.Location = new Point(14, 181);
            lblPitch.Name = "lblPitch";
            lblPitch.Size = new Size(41, 20);
            lblPitch.TabIndex = 7;
            lblPitch.Text = "Pitch";
            // 
            // _tbPitch
            // 
            _tbPitch.Location = new Point(91, 176);
            _tbPitch.Margin = new Padding(3, 4, 3, 4);
            _tbPitch.Maximum = 255;
            _tbPitch.Name = "_tbPitch";
            _tbPitch.Size = new Size(343, 56);
            _tbPitch.TabIndex = 8;
            _tbPitch.TickFrequency = 32;
            _tbPitch.Value = 128;
            _tbPitch.ValueChanged += TbPitch_ValueChanged;
            // 
            // _lblPitchVal
            // 
            _lblPitchVal.AutoSize = true;
            _lblPitchVal.ForeColor = Color.Black;
            _lblPitchVal.Location = new Point(443, 181);
            _lblPitchVal.Name = "_lblPitchVal";
            _lblPitchVal.Size = new Size(33, 20);
            _lblPitchVal.TabIndex = 9;
            _lblPitchVal.Text = "128";
            // 
            // lblRoll
            // 
            lblRoll.AutoSize = true;
            lblRoll.ForeColor = Color.Black;
            lblRoll.Location = new Point(14, 235);
            lblRoll.Name = "lblRoll";
            lblRoll.Size = new Size(35, 20);
            lblRoll.TabIndex = 10;
            lblRoll.Text = "Roll";
            // 
            // _tbRoll
            // 
            _tbRoll.AutoSize = false;
            _tbRoll.Location = new Point(91, 229);
            _tbRoll.Margin = new Padding(3, 4, 3, 4);
            _tbRoll.Maximum = 255;
            _tbRoll.Name = "_tbRoll";
            _tbRoll.Size = new Size(343, 37);
            _tbRoll.TabIndex = 11;
            _tbRoll.TickFrequency = 32;
            _tbRoll.Value = 128;
            _tbRoll.ValueChanged += TbRoll_ValueChanged;
            // 
            // _lblRollVal
            // 
            _lblRollVal.AutoSize = true;
            _lblRollVal.ForeColor = Color.Black;
            _lblRollVal.Location = new Point(443, 235);
            _lblRollVal.Name = "_lblRollVal";
            _lblRollVal.Size = new Size(33, 20);
            _lblRollVal.TabIndex = 12;
            _lblRollVal.Text = "128";
            // 
            // _btnCenter
            // 
            _btnCenter.ForeColor = Color.Black;
            _btnCenter.Location = new Point(14, 349);
            _btnCenter.Margin = new Padding(3, 4, 3, 4);
            _btnCenter.Name = "_btnCenter";
            _btnCenter.Size = new Size(114, 31);
            _btnCenter.TabIndex = 13;
            _btnCenter.Text = "Center sticks";
            _btnCenter.Click += BtnCenter_Click;
            // 
            // lblFlagProbe
            // 
            lblFlagProbe.AutoSize = true;
            lblFlagProbe.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblFlagProbe.ForeColor = Color.Black;
            lblFlagProbe.Location = new Point(14, 271);
            lblFlagProbe.Name = "lblFlagProbe";
            lblFlagProbe.Size = new Size(414, 40);
            lblFlagProbe.TabIndex = 14;
            lblFlagProbe.Text = "Flag probe @payload[7] (tick Enable). \r\nConfirmed: 04=gyro-cal, 08=compass-cal, 40/80=gimbal.";
            // 
            // _btnTakeoff
            // 
            _btnTakeoff.ForeColor = Color.Black;
            _btnTakeoff.Location = new Point(14, 312);
            _btnTakeoff.Margin = new Padding(3, 4, 3, 4);
            _btnTakeoff.Name = "_btnTakeoff";
            _btnTakeoff.Size = new Size(50, 31);
            _btnTakeoff.TabIndex = 15;
            _btnTakeoff.Tag = "01";
            _btnTakeoff.Text = "01";
            _btnTakeoff.Click += FlagPulse_Click;
            // 
            // _btnLand
            // 
            _btnLand.ForeColor = Color.Black;
            _btnLand.Location = new Point(69, 312);
            _btnLand.Margin = new Padding(3, 4, 3, 4);
            _btnLand.Name = "_btnLand";
            _btnLand.Size = new Size(50, 31);
            _btnLand.TabIndex = 16;
            _btnLand.Tag = "02";
            _btnLand.Text = "02";
            _btnLand.Click += FlagPulse_Click;
            // 
            // _btnRth
            // 
            _btnRth.ForeColor = Color.Black;
            _btnRth.Location = new Point(123, 312);
            _btnRth.Margin = new Padding(3, 4, 3, 4);
            _btnRth.Name = "_btnRth";
            _btnRth.Size = new Size(50, 31);
            _btnRth.TabIndex = 17;
            _btnRth.Tag = "04";
            _btnRth.Text = "04";
            _btnRth.Click += FlagPulse_Click;
            // 
            // _btnEstop
            // 
            _btnEstop.ForeColor = Color.Black;
            _btnEstop.Location = new Point(178, 312);
            _btnEstop.Margin = new Padding(3, 4, 3, 4);
            _btnEstop.Name = "_btnEstop";
            _btnEstop.Size = new Size(50, 31);
            _btnEstop.TabIndex = 18;
            _btnEstop.Tag = "08";
            _btnEstop.Text = "08";
            _btnEstop.Click += FlagPulse_Click;
            // 
            // _btnBit10
            // 
            _btnBit10.ForeColor = Color.Black;
            _btnBit10.Location = new Point(233, 312);
            _btnBit10.Margin = new Padding(3, 4, 3, 4);
            _btnBit10.Name = "_btnBit10";
            _btnBit10.Size = new Size(50, 31);
            _btnBit10.TabIndex = 19;
            _btnBit10.Tag = "10";
            _btnBit10.Text = "10";
            _btnBit10.Click += FlagPulse_Click;
            // 
            // _btnBit20
            // 
            _btnBit20.ForeColor = Color.Black;
            _btnBit20.Location = new Point(288, 312);
            _btnBit20.Margin = new Padding(3, 4, 3, 4);
            _btnBit20.Name = "_btnBit20";
            _btnBit20.Size = new Size(50, 31);
            _btnBit20.TabIndex = 20;
            _btnBit20.Tag = "20";
            _btnBit20.Text = "20";
            _btnBit20.Click += FlagPulse_Click;
            // 
            // lblCustomFlag
            // 
            lblCustomFlag.AutoSize = true;
            lblCustomFlag.ForeColor = Color.Black;
            lblCustomFlag.Location = new Point(133, 355);
            lblCustomFlag.Name = "lblCustomFlag";
            lblCustomFlag.Size = new Size(66, 20);
            lblCustomFlag.TabIndex = 21;
            lblCustomFlag.Text = "byte/val:";
            // 
            // _txtFlagIdx
            // 
            _txtFlagIdx.Location = new Point(203, 351);
            _txtFlagIdx.Margin = new Padding(3, 4, 3, 4);
            _txtFlagIdx.Name = "_txtFlagIdx";
            _txtFlagIdx.Size = new Size(27, 27);
            _txtFlagIdx.TabIndex = 22;
            _txtFlagIdx.Text = "7";
            // 
            // _txtFlag
            // 
            _txtFlag.Location = new Point(235, 351);
            _txtFlag.Margin = new Padding(3, 4, 3, 4);
            _txtFlag.Name = "_txtFlag";
            _txtFlag.Size = new Size(34, 27);
            _txtFlag.TabIndex = 23;
            _txtFlag.Text = "10";
            // 
            // _btnFlagPulse
            // 
            _btnFlagPulse.ForeColor = Color.Black;
            _btnFlagPulse.Location = new Point(274, 348);
            _btnFlagPulse.Margin = new Padding(3, 4, 3, 4);
            _btnFlagPulse.Name = "_btnFlagPulse";
            _btnFlagPulse.Size = new Size(66, 31);
            _btnFlagPulse.TabIndex = 24;
            _btnFlagPulse.Text = "Pulse";
            _btnFlagPulse.Click += BtnFlagPulse_Click;
            // 
            // _btnFlagHold
            // 
            _btnFlagHold.ForeColor = Color.Black;
            _btnFlagHold.Location = new Point(345, 348);
            _btnFlagHold.Margin = new Padding(3, 4, 3, 4);
            _btnFlagHold.Name = "_btnFlagHold";
            _btnFlagHold.Size = new Size(66, 31);
            _btnFlagHold.TabIndex = 25;
            _btnFlagHold.Text = "Hold";
            _btnFlagHold.MouseDown += FlagHold_MouseDown;
            _btnFlagHold.MouseUp += FlagHold_MouseUp;
            // 
            // _btnLog5
            // 
            _btnLog5.ForeColor = Color.Black;
            _btnLog5.Location = new Point(418, 348);
            _btnLog5.Margin = new Padding(3, 4, 3, 4);
            _btnLog5.Name = "_btnLog5";
            _btnLog5.Size = new Size(89, 31);
            _btnLog5.TabIndex = 26;
            _btnLog5.Text = "Log5";
            _btnLog5.Click += BtnLog5_Click;
            // 
            // lblCmdExp
            // 
            lblCmdExp.AutoSize = true;
            lblCmdExp.ForeColor = Color.Black;
            lblCmdExp.Location = new Point(14, 387);
            lblCmdExp.Name = "lblCmdExp";
            lblCmdExp.Size = new Size(449, 40);
            lblCmdExp.TabIndex = 26;
            lblCmdExp.Text = "Stick-flag guesses Photo/Video are really JSON CMD 97/98 native; \r\ntakeoff/land native, flag UNVERIFIED):";
            // 
            // _btnCmdPhoto
            // 
            _btnCmdPhoto.BackColor = Color.FromArgb(206, 240, 206);
            _btnCmdPhoto.ForeColor = Color.Black;
            _btnCmdPhoto.Location = new Point(14, 431);
            _btnCmdPhoto.Margin = new Padding(3, 4, 3, 4);
            _btnCmdPhoto.Name = "_btnCmdPhoto";
            _btnCmdPhoto.Size = new Size(80, 35);
            _btnCmdPhoto.TabIndex = 27;
            _btnCmdPhoto.Text = "Photo?";
            _btnCmdPhoto.UseVisualStyleBackColor = false;
            _btnCmdPhoto.Click += BtnPhoto_Click;
            // 
            // _btnCmdVideo
            // 
            _btnCmdVideo.BackColor = Color.FromArgb(206, 240, 206);
            _btnCmdVideo.ForeColor = Color.Black;
            _btnCmdVideo.Location = new Point(98, 431);
            _btnCmdVideo.Margin = new Padding(3, 4, 3, 4);
            _btnCmdVideo.Name = "_btnCmdVideo";
            _btnCmdVideo.Size = new Size(80, 35);
            _btnCmdVideo.TabIndex = 28;
            _btnCmdVideo.Text = "Video?";
            _btnCmdVideo.UseVisualStyleBackColor = false;
            _btnCmdVideo.Click += BtnVideoRec_Click;
            // 
            // _btnCmdTakeoff
            // 
            _btnCmdTakeoff.ForeColor = Color.Black;
            _btnCmdTakeoff.Location = new Point(183, 431);
            _btnCmdTakeoff.Margin = new Padding(3, 4, 3, 4);
            _btnCmdTakeoff.Name = "_btnCmdTakeoff";
            _btnCmdTakeoff.Size = new Size(85, 35);
            _btnCmdTakeoff.TabIndex = 29;
            _btnCmdTakeoff.Text = "OneKey T/O";
            _btnCmdTakeoff.Click += BtnTakeoff_Click;
            // 
            // _btnCmdLand
            // 
            _btnCmdLand.ForeColor = Color.Black;
            _btnCmdLand.Location = new Point(272, 431);
            _btnCmdLand.Margin = new Padding(3, 4, 3, 4);
            _btnCmdLand.Name = "_btnCmdLand";
            _btnCmdLand.Size = new Size(73, 35);
            _btnCmdLand.TabIndex = 30;
            _btnCmdLand.Text = "Land?";
            _btnCmdLand.Click += BtnLand_Click;
            // 
            // _btnCmdGoHome
            // 
            _btnCmdGoHome.ForeColor = Color.Black;
            _btnCmdGoHome.Location = new Point(350, 431);
            _btnCmdGoHome.Margin = new Padding(3, 4, 3, 4);
            _btnCmdGoHome.Name = "_btnCmdGoHome";
            _btnCmdGoHome.Size = new Size(85, 35);
            _btnCmdGoHome.TabIndex = 31;
            _btnCmdGoHome.Text = "GoHome?";
            _btnCmdGoHome.Click += BtnGoHome_Click;
            // 
            // _btnCmdStop
            // 
            _btnCmdStop.BackColor = Color.Firebrick;
            _btnCmdStop.ForeColor = Color.White;
            _btnCmdStop.Location = new Point(434, 431);
            _btnCmdStop.Margin = new Padding(3, 4, 3, 4);
            _btnCmdStop.Name = "_btnCmdStop";
            _btnCmdStop.Size = new Size(73, 35);
            _btnCmdStop.TabIndex = 32;
            _btnCmdStop.Text = "Stop?";
            _btnCmdStop.UseVisualStyleBackColor = false;
            _btnCmdStop.Click += BtnStop_Click;
            // 
            // _btnEmergLand
            // 
            _btnEmergLand.ForeColor = Color.Black;
            _btnEmergLand.Location = new Point(14, 473);
            _btnEmergLand.Margin = new Padding(3, 4, 3, 4);
            _btnEmergLand.Name = "_btnEmergLand";
            _btnEmergLand.Size = new Size(96, 35);
            _btnEmergLand.TabIndex = 33;
            _btnEmergLand.Text = "EmergLand?";
            _btnEmergLand.Click += BtnEmergencyLand_Click;
            // 
            // _btnFlip
            // 
            _btnFlip.ForeColor = Color.Black;
            _btnFlip.Location = new Point(114, 473);
            _btnFlip.Margin = new Padding(3, 4, 3, 4);
            _btnFlip.Name = "_btnFlip";
            _btnFlip.Size = new Size(73, 35);
            _btnFlip.TabIndex = 34;
            _btnFlip.Text = "Flip?";
            _btnFlip.Click += BtnFlip_Click;
            // 
            // _btnHeadless
            // 
            _btnHeadless.ForeColor = Color.Black;
            _btnHeadless.Location = new Point(192, 473);
            _btnHeadless.Margin = new Padding(3, 4, 3, 4);
            _btnHeadless.Name = "_btnHeadless";
            _btnHeadless.Size = new Size(96, 35);
            _btnHeadless.TabIndex = 35;
            _btnHeadless.Text = "Headless?";
            _btnHeadless.Click += BtnHeadless_Click;
            // 
            // _btnAltHold
            // 
            _btnAltHold.ForeColor = Color.Black;
            _btnAltHold.Location = new Point(293, 473);
            _btnAltHold.Margin = new Padding(3, 4, 3, 4);
            _btnAltHold.Name = "_btnAltHold";
            _btnAltHold.Size = new Size(85, 35);
            _btnAltHold.TabIndex = 36;
            _btnAltHold.Text = "AltHold?";
            _btnAltHold.Click += BtnAltHold_Click;
            // 
            // lblHeliway
            // 
            lblHeliway.AutoSize = true;
            lblHeliway.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblHeliway.ForeColor = Color.Black;
            lblHeliway.Location = new Point(14, 564);
            lblHeliway.Name = "lblHeliway";
            lblHeliway.Size = new Size(405, 20);
            lblHeliway.TabIndex = 37;
            lblHeliway.Text = "Heliway arm probes — PROPS OFF, clamped, RC in reach:";
            // 
            // _btnProbeLockUnlock
            // 
            _btnProbeLockUnlock.ForeColor = Color.Black;
            _btnProbeLockUnlock.Location = new Point(14, 589);
            _btnProbeLockUnlock.Margin = new Padding(3, 4, 3, 4);
            _btnProbeLockUnlock.Name = "_btnProbeLockUnlock";
            _btnProbeLockUnlock.Size = new Size(194, 35);
            _btnProbeLockUnlock.TabIndex = 38;
            _btnProbeLockUnlock.Text = "Probe lockUnlock (sweep)";
            _btnProbeLockUnlock.Click += BtnProbeLockUnlock_Click;
            // 
            // _btnProbeNext
            // 
            _btnProbeNext.ForeColor = Color.Black;
            _btnProbeNext.Location = new Point(213, 589);
            _btnProbeNext.Margin = new Padding(3, 4, 3, 4);
            _btnProbeNext.Name = "_btnProbeNext";
            _btnProbeNext.Size = new Size(69, 35);
            _btnProbeNext.TabIndex = 39;
            _btnProbeNext.Text = "Next";
            _btnProbeNext.Click += BtnProbeNext_Click;
            // 
            // _btnProbeMode
            // 
            _btnProbeMode.ForeColor = Color.Black;
            _btnProbeMode.Location = new Point(286, 591);
            _btnProbeMode.Margin = new Padding(3, 4, 3, 4);
            _btnProbeMode.Name = "_btnProbeMode";
            _btnProbeMode.Size = new Size(213, 35);
            _btnProbeMode.TabIndex = 40;
            _btnProbeMode.Text = "Probe mode (GPS↔optical)";
            _btnProbeMode.Click += BtnProbeMode_Click;
            // 
            // _btnCtrlPanelHold
            // 
            _btnCtrlPanelHold.ForeColor = Color.Black;
            _btnCtrlPanelHold.Location = new Point(379, 513);
            _btnCtrlPanelHold.Margin = new Padding(3, 4, 3, 4);
            _btnCtrlPanelHold.Name = "_btnCtrlPanelHold";
            _btnCtrlPanelHold.Size = new Size(126, 35);
            _btnCtrlPanelHold.TabIndex = 40;
            _btnCtrlPanelHold.Text = "ctrlPanel hold";
            _btnCtrlPanelHold.Click += BtnCtrlPanelHold_Click;
            // 
            // grpLog
            // 
            grpLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            grpLog.Controls.Add(_txtLog);
            grpLog.Location = new Point(7, 741);
            grpLog.Margin = new Padding(3, 4, 3, 4);
            grpLog.Name = "grpLog";
            grpLog.Size = new Size(1550, 181);
            grpLog.TabIndex = 5;
            grpLog.Text = "Log";
            // 
            // _txtLog
            // 
            _txtLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _txtLog.BorderStyle = BorderStyle.None;
            _txtLog.Font = new Font("Consolas", 8.5F);
            _txtLog.Location = new Point(9, 35);
            _txtLog.Margin = new Padding(3, 4, 3, 4);
            _txtLog.Multiline = true;
            _txtLog.Name = "_txtLog";
            _txtLog.ReadOnly = true;
            _txtLog.ScrollBars = ScrollBars.Vertical;
            _txtLog.Size = new Size(1531, 136);
            _txtLog.TabIndex = 0;
            // 
            // grpSend
            // 
            grpSend.Controls.Add(_chkChangeDetect);
            grpSend.Controls.Add(lblLoopHz);
            grpSend.Controls.Add(_numControlHz);
            grpSend.Controls.Add(lblHbHz);
            grpSend.Controls.Add(_numHeartbeatHz);
            grpSend.Location = new Point(526, 635);
            grpSend.Margin = new Padding(3, 4, 3, 4);
            grpSend.Name = "grpSend";
            grpSend.Size = new Size(512, 96);
            grpSend.TabIndex = 6;
            grpSend.Text = "Send tuning (control stream)";
            // 
            // _chkChangeDetect
            // 
            _chkChangeDetect.AutoSize = true;
            _chkChangeDetect.Checked = true;
            _chkChangeDetect.CheckState = CheckState.Checked;
            _chkChangeDetect.Location = new Point(14, 29);
            _chkChangeDetect.Margin = new Padding(3, 4, 3, 4);
            _chkChangeDetect.Name = "_chkChangeDetect";
            _chkChangeDetect.Size = new Size(306, 24);
            _chkChangeDetect.TabIndex = 0;
            _chkChangeDetect.Text = "Send on change + heartbeat (else steady)";
            _chkChangeDetect.CheckedChanged += ChangeDetect_CheckedChanged;
            // 
            // lblLoopHz
            // 
            lblLoopHz.AutoSize = true;
            lblLoopHz.Location = new Point(14, 63);
            lblLoopHz.Name = "lblLoopHz";
            lblLoopHz.Size = new Size(65, 20);
            lblLoopHz.TabIndex = 1;
            lblLoopHz.Text = "Loop Hz";
            // 
            // _numControlHz
            // 
            _numControlHz.Location = new Point(80, 60);
            _numControlHz.Margin = new Padding(3, 4, 3, 4);
            _numControlHz.Maximum = new decimal(new int[] { 120, 0, 0, 0 });
            _numControlHz.Minimum = new decimal(new int[] { 10, 0, 0, 0 });
            _numControlHz.Name = "_numControlHz";
            _numControlHz.Size = new Size(63, 27);
            _numControlHz.TabIndex = 2;
            _numControlHz.Value = new decimal(new int[] { 50, 0, 0, 0 });
            _numControlHz.ValueChanged += SendTuning_Changed;
            // 
            // lblHbHz
            // 
            lblHbHz.AutoSize = true;
            lblHbHz.Location = new Point(160, 63);
            lblHbHz.Name = "lblHbHz";
            lblHbHz.Size = new Size(98, 20);
            lblHbHz.TabIndex = 3;
            lblHbHz.Text = "Heartbeat Hz";
            // 
            // _numHeartbeatHz
            // 
            _numHeartbeatHz.Location = new Point(257, 60);
            _numHeartbeatHz.Margin = new Padding(3, 4, 3, 4);
            _numHeartbeatHz.Maximum = new decimal(new int[] { 60, 0, 0, 0 });
            _numHeartbeatHz.Minimum = new decimal(new int[] { 2, 0, 0, 0 });
            _numHeartbeatHz.Name = "_numHeartbeatHz";
            _numHeartbeatHz.Size = new Size(63, 27);
            _numHeartbeatHz.TabIndex = 4;
            _numHeartbeatHz.Value = new decimal(new int[] { 12, 0, 0, 0 });
            _numHeartbeatHz.ValueChanged += SendTuning_Changed;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1563, 935);
            Controls.Add(grpConn);
            Controls.Add(grpTel);
            Controls.Add(pnlVideo);
            Controls.Add(grpCam);
            Controls.Add(grpGim);
            Controls.Add(grpFly);
            Controls.Add(grpSend);
            Controls.Add(grpLog);
            Font = new Font("Segoe UI", 9F);
            Margin = new Padding(3, 4, 3, 4);
            Name = "Form1";
            Text = "Drone RE Studio — HS175D";
            FormClosing += Form1_FormClosing;
            grpConn.ResumeLayout(false);
            grpConn.PerformLayout();
            grpTel.ResumeLayout(false);
            grpTel.PerformLayout();
            grpCam.ResumeLayout(false);
            grpCam.PerformLayout();
            pnlVideo.ResumeLayout(false);
            grpGim.ResumeLayout(false);
            grpGim.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_tbGimbal).EndInit();
            grpFly.ResumeLayout(false);
            grpFly.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_tbThrottle).EndInit();
            ((System.ComponentModel.ISupportInitialize)_tbYaw).EndInit();
            ((System.ComponentModel.ISupportInitialize)_tbPitch).EndInit();
            ((System.ComponentModel.ISupportInitialize)_tbRoll).EndInit();
            grpLog.ResumeLayout(false);
            grpLog.PerformLayout();
            grpSend.ResumeLayout(false);
            grpSend.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_numControlHz).EndInit();
            ((System.ComponentModel.ISupportInitialize)_numHeartbeatHz).EndInit();
            ResumeLayout(false);
        }

        #endregion
    }
}

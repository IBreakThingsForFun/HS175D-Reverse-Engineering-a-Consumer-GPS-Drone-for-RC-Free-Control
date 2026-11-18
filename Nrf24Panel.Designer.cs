namespace HS175D
{
    partial class Nrf24Panel
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Panel _top;
        private System.Windows.Forms.ComboBox _cboPort;
        private System.Windows.Forms.Button _btnRefresh;
        private System.Windows.Forms.ComboBox _cboBaud;
        private System.Windows.Forms.Button _btnConnect;
        private System.Windows.Forms.Label _status;
        private System.Windows.Forms.Button _btnColdStart;
        private System.Windows.Forms.Button _btnBind;
        private System.Windows.Forms.Button _btnOptical;
        private System.Windows.Forms.Button _btnNeutral;
        private System.Windows.Forms.Button _btnPause;
        private System.Windows.Forms.Button _btnArm;
        private System.Windows.Forms.Button _btnBruteForce;
        private System.Windows.Forms.Button _btnRadioDetails;
        private System.Windows.Forms.Button _btnAutoSeq;
        private System.Windows.Forms.Label _lblSafety;
        private System.Windows.Forms.TextBox _rx;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._top = new System.Windows.Forms.Panel();
            this._cboPort = new System.Windows.Forms.ComboBox();
            this._btnRefresh = new System.Windows.Forms.Button();
            this._cboBaud = new System.Windows.Forms.ComboBox();
            this._btnConnect = new System.Windows.Forms.Button();
            this._status = new System.Windows.Forms.Label();
            this._btnColdStart = new System.Windows.Forms.Button();
            this._btnBind = new System.Windows.Forms.Button();
            this._btnOptical = new System.Windows.Forms.Button();
            this._btnNeutral = new System.Windows.Forms.Button();
            this._btnPause = new System.Windows.Forms.Button();
            this._btnArm = new System.Windows.Forms.Button();
            this._btnBruteForce = new System.Windows.Forms.Button();
            this._btnRadioDetails = new System.Windows.Forms.Button();
            this._btnAutoSeq = new System.Windows.Forms.Button();
            this._lblSafety = new System.Windows.Forms.Label();
            this._rx = new System.Windows.Forms.TextBox();
            this._top.SuspendLayout();
            this.SuspendLayout();
            //
            // _top
            //
            this._top.Controls.Add(this._cboPort);
            this._top.Controls.Add(this._btnRefresh);
            this._top.Controls.Add(this._cboBaud);
            this._top.Controls.Add(this._btnConnect);
            this._top.Controls.Add(this._status);
            this._top.Controls.Add(this._btnColdStart);
            this._top.Controls.Add(this._btnBind);
            this._top.Controls.Add(this._btnOptical);
            this._top.Controls.Add(this._btnNeutral);
            this._top.Controls.Add(this._btnPause);
            this._top.Controls.Add(this._btnArm);
            this._top.Controls.Add(this._btnBruteForce);
            this._top.Controls.Add(this._btnRadioDetails);
            this._top.Controls.Add(this._btnAutoSeq);
            this._top.Controls.Add(this._lblSafety);
            this._top.Dock = System.Windows.Forms.DockStyle.Top;
            this._top.Height = 116;
            this._top.Name = "_top";
            //
            // _cboPort
            //
            this._cboPort.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cboPort.Location = new System.Drawing.Point(8, 9);
            this._cboPort.Name = "_cboPort";
            this._cboPort.Size = new System.Drawing.Size(120, 23);
            this._cboPort.TabIndex = 0;
            //
            // _btnRefresh
            //
            this._btnRefresh.Location = new System.Drawing.Point(132, 8);
            this._btnRefresh.Name = "_btnRefresh";
            this._btnRefresh.Size = new System.Drawing.Size(32, 25);
            this._btnRefresh.TabIndex = 1;
            this._btnRefresh.Text = "⟳";
            this._btnRefresh.UseVisualStyleBackColor = true;
            //
            // _cboBaud
            //
            this._cboBaud.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cboBaud.Location = new System.Drawing.Point(172, 9);
            this._cboBaud.Name = "_cboBaud";
            this._cboBaud.Size = new System.Drawing.Size(90, 23);
            this._cboBaud.TabIndex = 2;
            //
            // _btnConnect
            //
            this._btnConnect.Location = new System.Drawing.Point(268, 8);
            this._btnConnect.Name = "_btnConnect";
            this._btnConnect.Size = new System.Drawing.Size(96, 25);
            this._btnConnect.TabIndex = 3;
            this._btnConnect.Text = "Connect";
            this._btnConnect.UseVisualStyleBackColor = true;
            //
            // _status
            //
            this._status.AutoSize = true;
            this._status.Location = new System.Drawing.Point(372, 14);
            this._status.Name = "_status";
            this._status.Size = new System.Drawing.Size(80, 15);
            this._status.TabIndex = 4;
            this._status.Text = "Disconnected";
            //
            // _btnColdStart
            //
            this._btnColdStart.Location = new System.Drawing.Point(8, 42);
            this._btnColdStart.Name = "_btnColdStart";
            this._btnColdStart.Size = new System.Drawing.Size(150, 26);
            this._btnColdStart.TabIndex = 5;
            this._btnColdStart.Text = "Cold Start (Bind+Ctrl)";
            this._btnColdStart.UseVisualStyleBackColor = true;
            //
            // _btnBind
            //
            this._btnBind.Location = new System.Drawing.Point(164, 42);
            this._btnBind.Name = "_btnBind";
            this._btnBind.Size = new System.Drawing.Size(110, 26);
            this._btnBind.TabIndex = 6;
            this._btnBind.Text = "Continuous Bind";
            this._btnBind.UseVisualStyleBackColor = true;
            //
            // _btnOptical
            //
            this._btnOptical.Location = new System.Drawing.Point(280, 42);
            this._btnOptical.Name = "_btnOptical";
            this._btnOptical.Size = new System.Drawing.Size(90, 26);
            this._btnOptical.TabIndex = 7;
            this._btnOptical.Text = "Optical";
            this._btnOptical.UseVisualStyleBackColor = true;
            //
            // _btnNeutral
            //
            this._btnNeutral.Location = new System.Drawing.Point(376, 42);
            this._btnNeutral.Name = "_btnNeutral";
            this._btnNeutral.Size = new System.Drawing.Size(90, 26);
            this._btnNeutral.TabIndex = 8;
            this._btnNeutral.Text = "Neutral / GPS";
            this._btnNeutral.UseVisualStyleBackColor = true;
            //
            // _btnPause
            //
            this._btnPause.Location = new System.Drawing.Point(472, 42);
            this._btnPause.Name = "_btnPause";
            this._btnPause.Size = new System.Drawing.Size(110, 26);
            this._btnPause.TabIndex = 9;
            this._btnPause.Text = "Pause / Resume";
            this._btnPause.UseVisualStyleBackColor = true;
            //
            // _btnArm
            //
            this._btnArm.Location = new System.Drawing.Point(8, 74);
            this._btnArm.Name = "_btnArm";
            this._btnArm.Size = new System.Drawing.Size(150, 30);
            this._btnArm.TabIndex = 10;
            this._btnArm.Text = "⚠ ARM Burst (props off!)";
            this._btnArm.UseVisualStyleBackColor = false;
            //
            // _btnBruteForce
            //
            this._btnBruteForce.Location = new System.Drawing.Point(164, 74);
            this._btnBruteForce.Name = "_btnBruteForce";
            this._btnBruteForce.Size = new System.Drawing.Size(150, 30);
            this._btnBruteForce.TabIndex = 11;
            this._btnBruteForce.Text = "Brute-Force Pair Scan";
            this._btnBruteForce.UseVisualStyleBackColor = true;
            //
            // _btnRadioDetails
            //
            this._btnRadioDetails.Location = new System.Drawing.Point(320, 74);
            this._btnRadioDetails.Name = "_btnRadioDetails";
            this._btnRadioDetails.Size = new System.Drawing.Size(110, 30);
            this._btnRadioDetails.TabIndex = 12;
            this._btnRadioDetails.Text = "Radio Details";
            this._btnRadioDetails.UseVisualStyleBackColor = true;
            //
            // _btnAutoSeq
            //
            this._btnAutoSeq.Location = new System.Drawing.Point(436, 74);
            this._btnAutoSeq.Name = "_btnAutoSeq";
            this._btnAutoSeq.Size = new System.Drawing.Size(146, 30);
            this._btnAutoSeq.TabIndex = 14;
            this._btnAutoSeq.Text = "Auto Sequence (c → o)";
            this._btnAutoSeq.UseVisualStyleBackColor = true;
            //
            // _lblSafety
            //
            this._lblSafety.AutoSize = true;
            this._lblSafety.Location = new System.Drawing.Point(8, 100);
            this._lblSafety.Name = "_lblSafety";
            this._lblSafety.Size = new System.Drawing.Size(300, 15);
            this._lblSafety.TabIndex = 13;
            this._lblSafety.Text = "ARM burst spins the motors — confirm props are off first.";
            //
            // _rx
            //
            this._rx.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._rx.Dock = System.Windows.Forms.DockStyle.Fill;
            this._rx.Font = new System.Drawing.Font("Consolas", 9F);
            this._rx.Location = new System.Drawing.Point(0, 116);
            this._rx.Multiline = true;
            this._rx.Name = "_rx";
            this._rx.ReadOnly = true;
            this._rx.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this._rx.Size = new System.Drawing.Size(600, 300);
            this._rx.TabIndex = 1;
            //
            // Nrf24Panel
            //
            // Same absolute-pixel-layout convention as VideoPanel/Esp32Panel - must not auto-scale.
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.Controls.Add(this._top);
            this.Controls.Add(this._rx);
            this.Name = "Nrf24Panel";
            this.Size = new System.Drawing.Size(600, 416);
            this._top.ResumeLayout(false);
            this._top.PerformLayout();
            this.ResumeLayout(false);
        }
    }
}

namespace HS175D
{
    partial class AutoTuneForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.grpAlt = new System.Windows.Forms.GroupBox();
            this.lblAltInfo = new System.Windows.Forms.Label();
            this.lblTestAlt = new System.Windows.Forms.Label();
            this.numTestAlt = new System.Windows.Forms.NumericUpDown();
            this.btnAltGo = new System.Windows.Forms.Button();
            this.btnAltStop = new System.Windows.Forms.Button();
            this.lblAltStatus = new System.Windows.Forms.Label();
            this.lblAltResult = new System.Windows.Forms.Label();
            this.btnAltApply = new System.Windows.Forms.Button();
            this.grpTilt = new System.Windows.Forms.GroupBox();
            this.lblTiltInfo = new System.Windows.Forms.Label();
            this.lblMaxTilt = new System.Windows.Forms.Label();
            this.numMaxTilt = new System.Windows.Forms.NumericUpDown();
            this.btnTiltGo = new System.Windows.Forms.Button();
            this.btnTiltStop = new System.Windows.Forms.Button();
            this.lblTiltStatus = new System.Windows.Forms.Label();
            this.lblTiltResult = new System.Windows.Forms.Label();
            this.btnTiltApply = new System.Windows.Forms.Button();
            this.grpHoriz = new System.Windows.Forms.GroupBox();
            this.lblHorizInfo = new System.Windows.Forms.Label();
            this.btnHorizGo = new System.Windows.Forms.Button();
            this.btnHorizStop = new System.Windows.Forms.Button();
            this.lblHorizStatus = new System.Windows.Forms.Label();
            this.lblHorizResult = new System.Windows.Forms.Label();
            this.btnHorizApply = new System.Windows.Forms.Button();
            this.grpYaw = new System.Windows.Forms.GroupBox();
            this.lblYawInfo = new System.Windows.Forms.Label();
            this.btnYawGo = new System.Windows.Forms.Button();
            this.btnYawStop = new System.Windows.Forms.Button();
            this.lblYawStatus = new System.Windows.Forms.Label();
            this.lblYawResult = new System.Windows.Forms.Label();
            this.btnYawApply = new System.Windows.Forms.Button();
            this.flowMain = new System.Windows.Forms.FlowLayoutPanel();
            this.grpAlt.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numTestAlt)).BeginInit();
            this.grpTilt.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxTilt)).BeginInit();
            this.grpHoriz.SuspendLayout();
            this.grpYaw.SuspendLayout();
            this.SuspendLayout();
            //
            // grpAlt
            //
            this.grpAlt.Controls.Add(this.lblAltInfo);
            this.grpAlt.Controls.Add(this.lblTestAlt);
            this.grpAlt.Controls.Add(this.numTestAlt);
            this.grpAlt.Controls.Add(this.btnAltGo);
            this.grpAlt.Controls.Add(this.btnAltStop);
            this.grpAlt.Controls.Add(this.lblAltStatus);
            this.grpAlt.Controls.Add(this.lblAltResult);
            this.grpAlt.Controls.Add(this.btnAltApply);
            this.grpAlt.Location = new System.Drawing.Point(12, 12);
            this.grpAlt.Name = "grpAlt";
            this.grpAlt.Size = new System.Drawing.Size(390, 236);
            this.grpAlt.TabStop = false;
            this.grpAlt.Text = "Altitude Hold";
            //
            // lblAltInfo
            //
            this.lblAltInfo.AutoSize = true;
            this.lblAltInfo.Location = new System.Drawing.Point(14, 22);
            this.lblAltInfo.MaximumSize = new System.Drawing.Size(360, 0);
            this.lblAltInfo.Name = "lblAltInfo";
            this.lblAltInfo.Size = new System.Drawing.Size(300, 30);
            this.lblAltInfo.Text = "Climbs to a test altitude and repeatedly displaces/recovers, nudging Kp up or down each pass until it finds the fastest response with no altitude loss or overshoot (up to 6 tries), then hands back to normal hover.";
            //
            // lblTestAlt
            //
            this.lblTestAlt.AutoSize = true;
            this.lblTestAlt.Location = new System.Drawing.Point(14, 62);
            this.lblTestAlt.Name = "lblTestAlt";
            this.lblTestAlt.Size = new System.Drawing.Size(100, 15);
            this.lblTestAlt.Text = "Test altitude (m):";
            //
            // numTestAlt
            //
            this.numTestAlt.Location = new System.Drawing.Point(160, 59);
            this.numTestAlt.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.numTestAlt.Minimum = new decimal(new int[] { 2, 0, 0, 0 });
            this.numTestAlt.Name = "numTestAlt";
            this.numTestAlt.Size = new System.Drawing.Size(80, 23);
            this.numTestAlt.TabIndex = 0;
            this.numTestAlt.Value = new decimal(new int[] { 20, 0, 0, 0 });
            this.numTestAlt.ValueChanged += new System.EventHandler(this.numTestAlt_ValueChanged);
            //
            // btnAltGo
            //
            this.btnAltGo.Location = new System.Drawing.Point(14, 92);
            this.btnAltGo.Name = "btnAltGo";
            this.btnAltGo.Size = new System.Drawing.Size(130, 28);
            this.btnAltGo.TabIndex = 1;
            this.btnAltGo.Text = "▶ Run Test";
            this.btnAltGo.Click += new System.EventHandler(this.btnAltGo_Click);
            //
            // btnAltStop
            //
            this.btnAltStop.Location = new System.Drawing.Point(150, 92);
            this.btnAltStop.Name = "btnAltStop";
            this.btnAltStop.Size = new System.Drawing.Size(100, 28);
            this.btnAltStop.TabIndex = 2;
            this.btnAltStop.Text = "■ Stop";
            this.btnAltStop.Click += new System.EventHandler(this.btnAltStop_Click);
            //
            // lblAltStatus
            //
            this.lblAltStatus.AutoSize = true;
            this.lblAltStatus.Location = new System.Drawing.Point(14, 128);
            this.lblAltStatus.MaximumSize = new System.Drawing.Size(360, 0);
            this.lblAltStatus.Name = "lblAltStatus";
            this.lblAltStatus.Size = new System.Drawing.Size(160, 15);
            this.lblAltStatus.Text = "Ready.";
            //
            // lblAltResult
            //
            this.lblAltResult.Location = new System.Drawing.Point(14, 150);
            this.lblAltResult.Name = "lblAltResult";
            this.lblAltResult.Size = new System.Drawing.Size(360, 50);
            this.lblAltResult.TabIndex = 3;
            this.lblAltResult.Text = "";
            //
            // btnAltApply
            //
            this.btnAltApply.Enabled = false;
            this.btnAltApply.Location = new System.Drawing.Point(14, 200);
            this.btnAltApply.Name = "btnAltApply";
            this.btnAltApply.Size = new System.Drawing.Size(150, 26);
            this.btnAltApply.TabIndex = 4;
            this.btnAltApply.Text = "Apply gains";
            this.btnAltApply.Click += new System.EventHandler(this.btnAltApply_Click);
            //
            // grpTilt
            //
            this.grpTilt.Controls.Add(this.lblTiltInfo);
            this.grpTilt.Controls.Add(this.lblMaxTilt);
            this.grpTilt.Controls.Add(this.numMaxTilt);
            this.grpTilt.Controls.Add(this.btnTiltGo);
            this.grpTilt.Controls.Add(this.btnTiltStop);
            this.grpTilt.Controls.Add(this.lblTiltStatus);
            this.grpTilt.Controls.Add(this.lblTiltResult);
            this.grpTilt.Controls.Add(this.btnTiltApply);
            this.grpTilt.Location = new System.Drawing.Point(12, 256);
            this.grpTilt.Name = "grpTilt";
            this.grpTilt.Size = new System.Drawing.Size(390, 246);
            this.grpTilt.TabStop = false;
            this.grpTilt.Text = "Tilt Response";
            //
            // lblTiltInfo
            //
            this.lblTiltInfo.AutoSize = true;
            this.lblTiltInfo.Location = new System.Drawing.Point(14, 22);
            this.lblTiltInfo.MaximumSize = new System.Drawing.Size(360, 0);
            this.lblTiltInfo.Name = "lblTiltInfo";
            this.lblTiltInfo.Size = new System.Drawing.Size(300, 30);
            this.lblTiltInfo.Text = "Flies the current Line Pass distance/heading/speed, measuring actual tilt during accel and decel.";
            //
            // lblMaxTilt
            //
            this.lblMaxTilt.AutoSize = true;
            this.lblMaxTilt.Location = new System.Drawing.Point(14, 62);
            this.lblMaxTilt.Name = "lblMaxTilt";
            this.lblMaxTilt.Size = new System.Drawing.Size(120, 15);
            this.lblMaxTilt.Text = "Max tilt target (deg):";
            //
            // numMaxTilt
            //
            this.numMaxTilt.Location = new System.Drawing.Point(160, 59);
            this.numMaxTilt.Maximum = new decimal(new int[] { 45, 0, 0, 0 });
            this.numMaxTilt.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numMaxTilt.Name = "numMaxTilt";
            this.numMaxTilt.Size = new System.Drawing.Size(80, 23);
            this.numMaxTilt.TabIndex = 0;
            this.numMaxTilt.Value = new decimal(new int[] { 8, 0, 0, 0 });
            this.numMaxTilt.ValueChanged += new System.EventHandler(this.numMaxTilt_ValueChanged);
            //
            // btnTiltGo
            //
            this.btnTiltGo.Location = new System.Drawing.Point(14, 92);
            this.btnTiltGo.Name = "btnTiltGo";
            this.btnTiltGo.Size = new System.Drawing.Size(130, 28);
            this.btnTiltGo.TabIndex = 1;
            this.btnTiltGo.Text = "▶ Run Test";
            this.btnTiltGo.Click += new System.EventHandler(this.btnTiltGo_Click);
            //
            // btnTiltStop
            //
            this.btnTiltStop.Location = new System.Drawing.Point(150, 92);
            this.btnTiltStop.Name = "btnTiltStop";
            this.btnTiltStop.Size = new System.Drawing.Size(100, 28);
            this.btnTiltStop.TabIndex = 2;
            this.btnTiltStop.Text = "■ Stop";
            this.btnTiltStop.Click += new System.EventHandler(this.btnTiltStop_Click);
            //
            // lblTiltStatus
            //
            this.lblTiltStatus.AutoSize = true;
            this.lblTiltStatus.Location = new System.Drawing.Point(14, 128);
            this.lblTiltStatus.MaximumSize = new System.Drawing.Size(360, 0);
            this.lblTiltStatus.Name = "lblTiltStatus";
            this.lblTiltStatus.Size = new System.Drawing.Size(160, 15);
            this.lblTiltStatus.Text = "Ready.";
            //
            // lblTiltResult
            //
            this.lblTiltResult.Location = new System.Drawing.Point(14, 150);
            this.lblTiltResult.Name = "lblTiltResult";
            this.lblTiltResult.Size = new System.Drawing.Size(360, 60);
            this.lblTiltResult.TabIndex = 3;
            this.lblTiltResult.Text = "";
            //
            // btnTiltApply
            //
            this.btnTiltApply.Enabled = false;
            this.btnTiltApply.Location = new System.Drawing.Point(14, 214);
            this.btnTiltApply.Name = "btnTiltApply";
            this.btnTiltApply.Size = new System.Drawing.Size(150, 26);
            this.btnTiltApply.TabIndex = 4;
            this.btnTiltApply.Text = "Apply aggressiveness";
            this.btnTiltApply.Click += new System.EventHandler(this.btnTiltApply_Click);
            //
            // grpHoriz
            //
            this.grpHoriz.Controls.Add(this.lblHorizInfo);
            this.grpHoriz.Controls.Add(this.btnHorizGo);
            this.grpHoriz.Controls.Add(this.btnHorizStop);
            this.grpHoriz.Controls.Add(this.lblHorizStatus);
            this.grpHoriz.Controls.Add(this.lblHorizResult);
            this.grpHoriz.Controls.Add(this.btnHorizApply);
            this.grpHoriz.Location = new System.Drawing.Point(12, 512);
            this.grpHoriz.Name = "grpHoriz";
            this.grpHoriz.Size = new System.Drawing.Size(390, 206);
            this.grpHoriz.TabStop = false;
            this.grpHoriz.Text = "Horizontal Hold";
            //
            // lblHorizInfo
            //
            this.lblHorizInfo.AutoSize = true;
            this.lblHorizInfo.Location = new System.Drawing.Point(14, 22);
            this.lblHorizInfo.MaximumSize = new System.Drawing.Size(360, 0);
            this.lblHorizInfo.Name = "lblHorizInfo";
            this.lblHorizInfo.Size = new System.Drawing.Size(300, 30);
            this.lblHorizInfo.Text = "Nudges ~2m forward from wherever it's hovering and repeatedly recovers, sweeping one combined pitch/roll Kp toward the fastest response with no overshoot.";
            //
            // btnHorizGo
            //
            this.btnHorizGo.Location = new System.Drawing.Point(14, 60);
            this.btnHorizGo.Name = "btnHorizGo";
            this.btnHorizGo.Size = new System.Drawing.Size(130, 28);
            this.btnHorizGo.TabIndex = 0;
            this.btnHorizGo.Text = "▶ Run Test";
            this.btnHorizGo.Click += new System.EventHandler(this.btnHorizGo_Click);
            //
            // btnHorizStop
            //
            this.btnHorizStop.Location = new System.Drawing.Point(150, 60);
            this.btnHorizStop.Name = "btnHorizStop";
            this.btnHorizStop.Size = new System.Drawing.Size(100, 28);
            this.btnHorizStop.TabIndex = 1;
            this.btnHorizStop.Text = "■ Stop";
            this.btnHorizStop.Click += new System.EventHandler(this.btnHorizStop_Click);
            //
            // lblHorizStatus
            //
            this.lblHorizStatus.AutoSize = true;
            this.lblHorizStatus.Location = new System.Drawing.Point(14, 96);
            this.lblHorizStatus.MaximumSize = new System.Drawing.Size(360, 0);
            this.lblHorizStatus.Name = "lblHorizStatus";
            this.lblHorizStatus.Size = new System.Drawing.Size(160, 15);
            this.lblHorizStatus.Text = "Ready.";
            //
            // lblHorizResult
            //
            this.lblHorizResult.Location = new System.Drawing.Point(14, 118);
            this.lblHorizResult.Name = "lblHorizResult";
            this.lblHorizResult.Size = new System.Drawing.Size(360, 50);
            this.lblHorizResult.TabIndex = 2;
            this.lblHorizResult.Text = "";
            //
            // btnHorizApply
            //
            this.btnHorizApply.Enabled = false;
            this.btnHorizApply.Location = new System.Drawing.Point(14, 170);
            this.btnHorizApply.Name = "btnHorizApply";
            this.btnHorizApply.Size = new System.Drawing.Size(150, 26);
            this.btnHorizApply.TabIndex = 3;
            this.btnHorizApply.Text = "Apply gains";
            this.btnHorizApply.Click += new System.EventHandler(this.btnHorizApply_Click);
            //
            // grpYaw
            //
            this.grpYaw.Controls.Add(this.lblYawInfo);
            this.grpYaw.Controls.Add(this.btnYawGo);
            this.grpYaw.Controls.Add(this.btnYawStop);
            this.grpYaw.Controls.Add(this.lblYawStatus);
            this.grpYaw.Controls.Add(this.lblYawResult);
            this.grpYaw.Controls.Add(this.btnYawApply);
            this.grpYaw.Location = new System.Drawing.Point(12, 726);
            this.grpYaw.Name = "grpYaw";
            this.grpYaw.Size = new System.Drawing.Size(390, 206);
            this.grpYaw.TabStop = false;
            this.grpYaw.Text = "Yaw Hold";
            //
            // lblYawInfo
            //
            this.lblYawInfo.AutoSize = true;
            this.lblYawInfo.Location = new System.Drawing.Point(14, 22);
            this.lblYawInfo.MaximumSize = new System.Drawing.Size(360, 0);
            this.lblYawInfo.Name = "lblYawInfo";
            this.lblYawInfo.Size = new System.Drawing.Size(300, 30);
            this.lblYawInfo.Text = "Rotates a few degrees from the current heading and repeatedly recovers, sweeping Kp toward the fastest response with no overshoot. Lowest-risk of the three tests - no translation at all.";
            //
            // btnYawGo
            //
            this.btnYawGo.Location = new System.Drawing.Point(14, 60);
            this.btnYawGo.Name = "btnYawGo";
            this.btnYawGo.Size = new System.Drawing.Size(130, 28);
            this.btnYawGo.TabIndex = 0;
            this.btnYawGo.Text = "▶ Run Test";
            this.btnYawGo.Click += new System.EventHandler(this.btnYawGo_Click);
            //
            // btnYawStop
            //
            this.btnYawStop.Location = new System.Drawing.Point(150, 60);
            this.btnYawStop.Name = "btnYawStop";
            this.btnYawStop.Size = new System.Drawing.Size(100, 28);
            this.btnYawStop.TabIndex = 1;
            this.btnYawStop.Text = "■ Stop";
            this.btnYawStop.Click += new System.EventHandler(this.btnYawStop_Click);
            //
            // lblYawStatus
            //
            this.lblYawStatus.AutoSize = true;
            this.lblYawStatus.Location = new System.Drawing.Point(14, 96);
            this.lblYawStatus.MaximumSize = new System.Drawing.Size(360, 0);
            this.lblYawStatus.Name = "lblYawStatus";
            this.lblYawStatus.Size = new System.Drawing.Size(160, 15);
            this.lblYawStatus.Text = "Ready.";
            //
            // lblYawResult
            //
            this.lblYawResult.Location = new System.Drawing.Point(14, 118);
            this.lblYawResult.Name = "lblYawResult";
            this.lblYawResult.Size = new System.Drawing.Size(360, 50);
            this.lblYawResult.TabIndex = 2;
            this.lblYawResult.Text = "";
            //
            // btnYawApply
            //
            this.btnYawApply.Enabled = false;
            this.btnYawApply.Location = new System.Drawing.Point(14, 170);
            this.btnYawApply.Name = "btnYawApply";
            this.btnYawApply.Size = new System.Drawing.Size(150, 26);
            this.btnYawApply.TabIndex = 3;
            this.btnYawApply.Text = "Apply gains";
            this.btnYawApply.Click += new System.EventHandler(this.btnYawApply_Click);
            //
            // flowMain
            //
            this.flowMain.AutoScroll = true;
            this.flowMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowMain.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowMain.WrapContents = false;
            this.flowMain.Padding = new System.Windows.Forms.Padding(12);
            this.flowMain.Controls.Add(this.grpAlt);
            this.flowMain.Controls.Add(this.grpTilt);
            this.flowMain.Controls.Add(this.grpHoriz);
            this.flowMain.Controls.Add(this.grpYaw);
            this.flowMain.Name = "flowMain";
            //
            // AutoTuneForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(480, 460);
            this.MinimumSize = new System.Drawing.Size(480, 300);
            // (480, 0) here was meant as "no height cap" but WinForms doesn't special-case a 0
            // component within an otherwise non-empty Size - it's read literally as max height = 0,
            // which is the actual bug that made this form render as nothing but its title bar (39px,
            // just the caption chrome) both at runtime and in the Designer. Only Size.Empty (0,0) -
            // the WHOLE Size, not one axis - means "no maximum" at all; confirmed empirically. This
            // does mean width is no longer hard-capped at 480 (only floored via MinimumSize), which is
            // an acceptable trade for "the form actually renders."
            this.MaximumSize = new System.Drawing.Size(0, 0);
            this.Controls.Add(this.flowMain);
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.Name = "AutoTuneForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Auto-Tune";
            this.grpAlt.ResumeLayout(false);
            this.grpAlt.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numTestAlt)).EndInit();
            this.grpTilt.ResumeLayout(false);
            this.grpTilt.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxTilt)).EndInit();
            this.grpHoriz.ResumeLayout(false);
            this.grpHoriz.PerformLayout();
            this.grpYaw.ResumeLayout(false);
            this.grpYaw.PerformLayout();
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.GroupBox grpAlt;
        private System.Windows.Forms.Label lblAltInfo;
        private System.Windows.Forms.Label lblTestAlt;
        private System.Windows.Forms.NumericUpDown numTestAlt;
        private System.Windows.Forms.Button btnAltGo;
        private System.Windows.Forms.Button btnAltStop;
        private System.Windows.Forms.Label lblAltStatus;
        private System.Windows.Forms.Label lblAltResult;
        private System.Windows.Forms.Button btnAltApply;
        private System.Windows.Forms.GroupBox grpTilt;
        private System.Windows.Forms.Label lblTiltInfo;
        private System.Windows.Forms.Label lblMaxTilt;
        private System.Windows.Forms.NumericUpDown numMaxTilt;
        private System.Windows.Forms.Button btnTiltGo;
        private System.Windows.Forms.Button btnTiltStop;
        private System.Windows.Forms.Label lblTiltStatus;
        private System.Windows.Forms.Label lblTiltResult;
        private System.Windows.Forms.Button btnTiltApply;
        private System.Windows.Forms.GroupBox grpHoriz;
        private System.Windows.Forms.Label lblHorizInfo;
        private System.Windows.Forms.Button btnHorizGo;
        private System.Windows.Forms.Button btnHorizStop;
        private System.Windows.Forms.Label lblHorizStatus;
        private System.Windows.Forms.Label lblHorizResult;
        private System.Windows.Forms.Button btnHorizApply;
        private System.Windows.Forms.GroupBox grpYaw;
        private System.Windows.Forms.Label lblYawInfo;
        private System.Windows.Forms.Button btnYawGo;
        private System.Windows.Forms.Button btnYawStop;
        private System.Windows.Forms.Label lblYawStatus;
        private System.Windows.Forms.Label lblYawResult;
        private System.Windows.Forms.Button btnYawApply;
        private System.Windows.Forms.FlowLayoutPanel flowMain;
    }
}

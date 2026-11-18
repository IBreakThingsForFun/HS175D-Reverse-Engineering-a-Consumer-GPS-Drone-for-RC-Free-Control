namespace HS175D
{
    partial class WifiAdapterForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.lblAdapter = new System.Windows.Forms.Label();
            this.cboAdapter = new System.Windows.Forms.ComboBox();
            this.btnRefreshAdapters = new System.Windows.Forms.Button();
            this.lblLink = new System.Windows.Forms.Label();
            this.lstNetworks = new System.Windows.Forms.ListView();
            this.colSsid = new System.Windows.Forms.ColumnHeader();
            this.colSignal = new System.Windows.Forms.ColumnHeader();
            this.colRssi = new System.Windows.Forms.ColumnHeader();
            this.colSec = new System.Windows.Forms.ColumnHeader();
            this.colProfile = new System.Windows.Forms.ColumnHeader();
            this.btnScan = new System.Windows.Forms.Button();
            this.lblPwd = new System.Windows.Forms.Label();
            this.txtPassword = new System.Windows.Forms.TextBox();
            this.chkShowPwd = new System.Windows.Forms.CheckBox();
            this.btnConnect = new System.Windows.Forms.Button();
            this.btnDisconnect = new System.Windows.Forms.Button();
            this.lblSignalCaption = new System.Windows.Forms.Label();
            this.lblRssi = new System.Windows.Forms.Label();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.rssiTimer = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            //
            // lblAdapter
            //
            this.lblAdapter.AutoSize = true;
            this.lblAdapter.Location = new System.Drawing.Point(12, 15);
            this.lblAdapter.Name = "lblAdapter";
            this.lblAdapter.Size = new System.Drawing.Size(52, 15);
            this.lblAdapter.Text = "Adapter:";
            //
            // cboAdapter
            //
            this.cboAdapter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboAdapter.Location = new System.Drawing.Point(76, 12);
            this.cboAdapter.Name = "cboAdapter";
            this.cboAdapter.Size = new System.Drawing.Size(452, 23);
            this.cboAdapter.TabIndex = 0;
            this.cboAdapter.SelectedIndexChanged += new System.EventHandler(this.cboAdapter_SelectedIndexChanged);
            //
            // btnRefreshAdapters
            //
            this.btnRefreshAdapters.Location = new System.Drawing.Point(534, 11);
            this.btnRefreshAdapters.Name = "btnRefreshAdapters";
            this.btnRefreshAdapters.Size = new System.Drawing.Size(74, 26);
            this.btnRefreshAdapters.TabIndex = 1;
            this.btnRefreshAdapters.Text = "Refresh";
            this.btnRefreshAdapters.Click += new System.EventHandler(this.btnRefreshAdapters_Click);
            //
            // lblLink
            //
            this.lblLink.AutoSize = true;
            this.lblLink.Location = new System.Drawing.Point(12, 46);
            this.lblLink.Name = "lblLink";
            this.lblLink.Size = new System.Drawing.Size(48, 15);
            this.lblLink.Text = "Link: —";
            //
            // lstNetworks
            //
            this.lstNetworks.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                this.colSsid, this.colSignal, this.colRssi, this.colSec, this.colProfile});
            this.lstNetworks.FullRowSelect = true;
            this.lstNetworks.GridLines = true;
            this.lstNetworks.HideSelection = false;
            this.lstNetworks.Location = new System.Drawing.Point(12, 72);
            this.lstNetworks.MultiSelect = false;
            this.lstNetworks.Name = "lstNetworks";
            this.lstNetworks.Size = new System.Drawing.Size(596, 232);
            this.lstNetworks.TabIndex = 2;
            this.lstNetworks.UseCompatibleStateImageBehavior = false;
            this.lstNetworks.View = System.Windows.Forms.View.Details;
            this.lstNetworks.SelectedIndexChanged += new System.EventHandler(this.lstNetworks_SelectedIndexChanged);
            this.lstNetworks.DoubleClick += new System.EventHandler(this.btnConnect_Click);
            //
            // colSsid
            //
            this.colSsid.Text = "SSID";
            this.colSsid.Width = 250;
            //
            // colSignal
            //
            this.colSignal.Text = "Signal %";
            this.colSignal.Width = 70;
            //
            // colRssi
            //
            this.colRssi.Text = "RSSI dBm";
            this.colRssi.Width = 80;
            //
            // colSec
            //
            this.colSec.Text = "Security";
            this.colSec.Width = 100;
            //
            // colProfile
            //
            this.colProfile.Text = "Profile";
            this.colProfile.Width = 90;
            //
            // btnScan
            //
            this.btnScan.Location = new System.Drawing.Point(12, 314);
            this.btnScan.Name = "btnScan";
            this.btnScan.Size = new System.Drawing.Size(90, 30);
            this.btnScan.TabIndex = 3;
            this.btnScan.Text = "Scan";
            this.btnScan.Click += new System.EventHandler(this.btnScan_Click);
            //
            // lblPwd
            //
            this.lblPwd.AutoSize = true;
            this.lblPwd.Location = new System.Drawing.Point(116, 322);
            this.lblPwd.Name = "lblPwd";
            this.lblPwd.Size = new System.Drawing.Size(60, 15);
            this.lblPwd.Text = "Password:";
            //
            // txtPassword
            //
            this.txtPassword.Location = new System.Drawing.Point(180, 318);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.Size = new System.Drawing.Size(170, 23);
            this.txtPassword.TabIndex = 4;
            this.txtPassword.UseSystemPasswordChar = true;
            //
            // chkShowPwd
            //
            this.chkShowPwd.AutoSize = true;
            this.chkShowPwd.Location = new System.Drawing.Point(356, 320);
            this.chkShowPwd.Name = "chkShowPwd";
            this.chkShowPwd.Size = new System.Drawing.Size(53, 19);
            this.chkShowPwd.TabIndex = 5;
            this.chkShowPwd.Text = "Show";
            this.chkShowPwd.CheckedChanged += new System.EventHandler(this.chkShowPwd_CheckedChanged);
            //
            // btnConnect
            //
            this.btnConnect.Location = new System.Drawing.Point(420, 314);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(90, 30);
            this.btnConnect.TabIndex = 6;
            this.btnConnect.Text = "Connect";
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
            //
            // btnDisconnect
            //
            this.btnDisconnect.Location = new System.Drawing.Point(516, 314);
            this.btnDisconnect.Name = "btnDisconnect";
            this.btnDisconnect.Size = new System.Drawing.Size(92, 30);
            this.btnDisconnect.TabIndex = 7;
            this.btnDisconnect.Text = "Disconnect";
            this.btnDisconnect.Click += new System.EventHandler(this.btnDisconnect_Click);
            //
            // lblSignalCaption
            //
            this.lblSignalCaption.AutoSize = true;
            this.lblSignalCaption.Location = new System.Drawing.Point(12, 362);
            this.lblSignalCaption.Name = "lblSignalCaption";
            this.lblSignalCaption.Size = new System.Drawing.Size(70, 15);
            this.lblSignalCaption.Text = "Live signal:";
            //
            // lblRssi
            //
            this.lblRssi.AutoSize = true;
            this.lblRssi.Font = new System.Drawing.Font("Segoe UI", 15F, System.Drawing.FontStyle.Bold);
            this.lblRssi.Location = new System.Drawing.Point(92, 352);
            this.lblRssi.Name = "lblRssi";
            this.lblRssi.Size = new System.Drawing.Size(120, 28);
            this.lblRssi.Text = "— dBm (—%)";
            //
            // txtLog
            //
            this.txtLog.Font = new System.Drawing.Font("Consolas", 9F);
            this.txtLog.Location = new System.Drawing.Point(12, 390);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(596, 68);
            this.txtLog.TabIndex = 8;
            //
            // rssiTimer
            //
            this.rssiTimer.Interval = 1500;
            this.rssiTimer.Tick += new System.EventHandler(this.rssiTimer_Tick);
            //
            // WifiAdapterForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(620, 470);
            this.Controls.Add(this.lblAdapter);
            this.Controls.Add(this.cboAdapter);
            this.Controls.Add(this.btnRefreshAdapters);
            this.Controls.Add(this.lblLink);
            this.Controls.Add(this.lstNetworks);
            this.Controls.Add(this.btnScan);
            this.Controls.Add(this.lblPwd);
            this.Controls.Add(this.txtPassword);
            this.Controls.Add(this.chkShowPwd);
            this.Controls.Add(this.btnConnect);
            this.Controls.Add(this.btnDisconnect);
            this.Controls.Add(this.lblSignalCaption);
            this.Controls.Add(this.lblRssi);
            this.Controls.Add(this.txtLog);
            this.MinimumSize = new System.Drawing.Size(636, 509);
            this.Name = "WifiAdapterForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "WiFi Adapter (PC) — Native WiFi";
            this.Load += new System.EventHandler(this.WifiAdapterForm_Load);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.WifiAdapterForm_FormClosed);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Label lblAdapter;
        private System.Windows.Forms.ComboBox cboAdapter;
        private System.Windows.Forms.Button btnRefreshAdapters;
        private System.Windows.Forms.Label lblLink;
        private System.Windows.Forms.ListView lstNetworks;
        private System.Windows.Forms.ColumnHeader colSsid;
        private System.Windows.Forms.ColumnHeader colSignal;
        private System.Windows.Forms.ColumnHeader colRssi;
        private System.Windows.Forms.ColumnHeader colSec;
        private System.Windows.Forms.ColumnHeader colProfile;
        private System.Windows.Forms.Button btnScan;
        private System.Windows.Forms.Label lblPwd;
        private System.Windows.Forms.TextBox txtPassword;
        private System.Windows.Forms.CheckBox chkShowPwd;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.Button btnDisconnect;
        private System.Windows.Forms.Label lblSignalCaption;
        private System.Windows.Forms.Label lblRssi;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.Timer rssiTimer;
    }
}

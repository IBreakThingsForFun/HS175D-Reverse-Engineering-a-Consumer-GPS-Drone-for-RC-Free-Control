namespace HS175D
{
    partial class WifiSettingsForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label _lblTitle;
        private System.Windows.Forms.TextBox _status;
        private System.Windows.Forms.Button _btnRefresh;
        private System.Windows.Forms.CheckBox _persist;
        private System.Windows.Forms.Button _btn24;
        private System.Windows.Forms.Button _btn5;
        private System.Windows.Forms.Button _btnMulti;
        private System.Windows.Forms.Button _btnFactory;
        private System.Windows.Forms.Label _lblWarn;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._lblTitle = new System.Windows.Forms.Label();
            this._status = new System.Windows.Forms.TextBox();
            this._btnRefresh = new System.Windows.Forms.Button();
            this._persist = new System.Windows.Forms.CheckBox();
            this._btn24 = new System.Windows.Forms.Button();
            this._btn5 = new System.Windows.Forms.Button();
            this._btnMulti = new System.Windows.Forms.Button();
            this._btnFactory = new System.Windows.Forms.Button();
            this._lblWarn = new System.Windows.Forms.Label();
            this.SuspendLayout();
            //
            // _lblTitle
            //
            this._lblTitle.AutoSize = true;
            this._lblTitle.Location = new System.Drawing.Point(12, 10);
            this._lblTitle.Name = "_lblTitle";
            this._lblTitle.Size = new System.Drawing.Size(120, 15);
            this._lblTitle.TabIndex = 0;
            this._lblTitle.Text = "AP control — drone —";
            //
            // _status
            //
            this._status.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._status.Font = new System.Drawing.Font("Consolas", 9F);
            this._status.Location = new System.Drawing.Point(12, 36);
            this._status.Multiline = true;
            this._status.Name = "_status";
            this._status.ReadOnly = true;
            this._status.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this._status.Size = new System.Drawing.Size(446, 150);
            this._status.TabIndex = 1;
            //
            // _btnRefresh
            //
            this._btnRefresh.Location = new System.Drawing.Point(12, 194);
            this._btnRefresh.Name = "_btnRefresh";
            this._btnRefresh.Size = new System.Drawing.Size(130, 28);
            this._btnRefresh.TabIndex = 2;
            this._btnRefresh.Text = "Refresh status";
            this._btnRefresh.UseVisualStyleBackColor = true;
            //
            // _persist
            //
            this._persist.AutoSize = true;
            this._persist.Location = new System.Drawing.Point(156, 198);
            this._persist.Name = "_persist";
            this._persist.Size = new System.Drawing.Size(210, 19);
            this._persist.TabIndex = 3;
            this._persist.Text = "Make persistent (survives reboot)";
            this._persist.UseVisualStyleBackColor = true;
            //
            // _btn24
            //
            this._btn24.Location = new System.Drawing.Point(12, 236);
            this._btn24.Name = "_btn24";
            this._btn24.Size = new System.Drawing.Size(220, 30);
            this._btn24.TabIndex = 4;
            this._btn24.Text = "2.4 GHz + multi-station";
            this._btn24.UseVisualStyleBackColor = true;
            //
            // _btn5
            //
            this._btn5.Location = new System.Drawing.Point(240, 236);
            this._btn5.Name = "_btn5";
            this._btn5.Size = new System.Drawing.Size(218, 30);
            this._btn5.TabIndex = 5;
            this._btn5.Text = "5 GHz (stock)";
            this._btn5.UseVisualStyleBackColor = true;
            //
            // _btnMulti
            //
            this._btnMulti.Location = new System.Drawing.Point(12, 274);
            this._btnMulti.Name = "_btnMulti";
            this._btnMulti.Size = new System.Drawing.Size(446, 30);
            this._btnMulti.TabIndex = 6;
            this._btnMulti.Text = "Multi-station only (isolation off)";
            this._btnMulti.UseVisualStyleBackColor = true;
            //
            // _btnFactory
            //
            this._btnFactory.Location = new System.Drawing.Point(12, 312);
            this._btnFactory.Name = "_btnFactory";
            this._btnFactory.Size = new System.Drawing.Size(446, 30);
            this._btnFactory.TabIndex = 7;
            this._btnFactory.Text = "⟲ Restore factory AP (stock 5 GHz)";
            this._btnFactory.UseVisualStyleBackColor = false;
            //
            // _lblWarn
            //
            this._lblWarn.AutoSize = true;
            this._lblWarn.Location = new System.Drawing.Point(12, 350);
            this._lblWarn.Name = "_lblWarn";
            this._lblWarn.Size = new System.Drawing.Size(440, 30);
            this._lblWarn.TabIndex = 8;
            this._lblWarn.Text = "Applying restarts hostapd → WiFi drops ~5–8 s. If the band/SSID changed,\r\nrejoin the AP, then Refresh. Self-heal reverts to stock 5 GHz if hostapd fails.";
            //
            // WifiSettingsForm
            //
            this.ClientSize = new System.Drawing.Size(470, 430);
            this.Controls.Add(this._lblWarn);
            this.Controls.Add(this._btnFactory);
            this.Controls.Add(this._btnMulti);
            this.Controls.Add(this._btn5);
            this.Controls.Add(this._btn24);
            this.Controls.Add(this._persist);
            this.Controls.Add(this._btnRefresh);
            this.Controls.Add(this._status);
            this.Controls.Add(this._lblTitle);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "WifiSettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Drone WiFi — SoftAP control";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}

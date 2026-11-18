namespace HS175D
{
    /// <summary>
    /// PC-side WiFi adapter control (station mode) over the Native WiFi API (<see cref="WlanClient"/>):
    /// pick an adapter, scan, connect (open / WPA2-PSK), disconnect, and watch live signal. Intended for
    /// dedicating a second adapter (e.g. the AM10) to the drone's AP while the built-in WiFi stays online.
    /// Unrelated to <see cref="DroneWifi"/> (that controls the drone's own SoftAP over adb).
    /// </summary>
    public partial class WifiAdapterForm : Form
    {
        WlanClient? _wlan;
        Guid _iface;
        bool _hasIface;

        // Preselect the AM10 / Ralink adapter when present (matched against the driver description).
        static readonly string[] AdapterHints =
            { "AM10", "Valet", "Ralink", "RT3072", "RT2870", "RT2860", "802.11 n usb", "wireless-n usb", "wireless n usb" };

        readonly ToolTip _tip = new() { AutoPopDelay = 30000, InitialDelay = 350, ReshowDelay = 80, ShowAlways = true };

        public WifiAdapterForm()
        {
            InitializeComponent();
            Theme.Apply(this);
            lblRssi.ForeColor = Theme.Muted;

            _tip.SetToolTip(btnRefreshAdapters, "Re-enumerate WiFi adapters (Native WiFi API).");
            _tip.SetToolTip(btnScan, "Scan for visible SSIDs on the selected adapter.");
            _tip.SetToolTip(btnConnect, "Connect the selected adapter to the chosen SSID (open or WPA2-PSK).");
            _tip.SetToolTip(btnDisconnect, "Disconnect the selected adapter.");
            _tip.SetToolTip(cboAdapter, "Which WiFi adapter to control — pick a dedicated second adapter (e.g. the AM10) to keep this separate from your main WiFi.");
        }

        void WifiAdapterForm_Load(object? sender, EventArgs e)
        {
            try { _wlan = new WlanClient(); }
            catch (Exception ex) { Log("WLAN service unavailable: " + ex.Message); SetEnabled(false); return; }
            LoadAdapters();
            rssiTimer.Start();
        }

        void LoadAdapters()
        {
            if (_wlan is null) return;
            cboAdapter.Items.Clear();

            List<WifiInterface> ifaces;
            try { ifaces = _wlan.GetInterfaces(); }
            catch (Exception ex) { Log("Enumerate failed: " + ex.Message); return; }

            if (ifaces.Count == 0) { Log("No WiFi adapters found."); SetEnabled(false); return; }
            SetEnabled(true);

            foreach (var i in ifaces) cboAdapter.Items.Add(i);
            cboAdapter.DisplayMember = nameof(WifiInterface.Description);

            int pick = 0;
            for (int k = 0; k < ifaces.Count; k++)
                if (AdapterHints.Any(h => ifaces[k].Description.Contains(h, StringComparison.OrdinalIgnoreCase))) { pick = k; break; }
            cboAdapter.SelectedIndex = pick;    // fires SelectedIndexChanged → sets _iface, populates
            Log($"{ifaces.Count} adapter(s). Using: {ifaces[pick].Description}");
        }

        void cboAdapter_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cboAdapter.SelectedItem is WifiInterface wi)
            {
                _iface = wi.Id; _hasIface = true;
                PopulateNetworks();
                RefreshLink();
            }
        }

        void btnRefreshAdapters_Click(object? sender, EventArgs e) => LoadAdapters();

        async void btnScan_Click(object? sender, EventArgs e)
        {
            if (_wlan is null || !_hasIface) return;
            btnScan.Enabled = false;
            try
            {
                _wlan.Scan(_iface);
                Log("Scanning…");
                await Task.Delay(4000);         // scan completes asynchronously in the driver
                PopulateNetworks();
                Log($"{lstNetworks.Items.Count} network(s).");
            }
            catch (Exception ex) { Log("Scan failed: " + ex.Message); }
            finally { if (!IsDisposed) btnScan.Enabled = true; }
        }

        void PopulateNetworks()
        {
            if (_wlan is null || !_hasIface) return;
            lstNetworks.BeginUpdate();
            lstNetworks.Items.Clear();
            try
            {
                foreach (var n in _wlan.GetNetworks(_iface))
                {
                    var it = new ListViewItem(n.Ssid);
                    it.SubItems.Add(n.SignalQuality.ToString());
                    it.SubItems.Add(n.RssiDbm.ToString());
                    it.SubItems.Add(n.SecurityEnabled ? n.Auth : "Open");
                    it.SubItems.Add(string.IsNullOrEmpty(n.ProfileName) ? "" : "saved");
                    it.Tag = n;
                    lstNetworks.Items.Add(it);
                }
            }
            catch (Exception ex) { Log("List failed: " + ex.Message); }
            finally { lstNetworks.EndUpdate(); }
        }

        void lstNetworks_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (lstNetworks.SelectedItems.Count > 0 && lstNetworks.SelectedItems[0].Tag is WifiNetwork { SecurityEnabled: false })
                txtPassword.Clear();            // open networks take no password
        }

        void chkShowPwd_CheckedChanged(object? sender, EventArgs e)
            => txtPassword.UseSystemPasswordChar = !chkShowPwd.Checked;

        async void btnConnect_Click(object? sender, EventArgs e)
        {
            if (_wlan is null || !_hasIface) return;
            if (lstNetworks.SelectedItems.Count == 0) { Log("Select a network first."); return; }

            var n = (WifiNetwork)lstNetworks.SelectedItems[0].Tag!;
            string? pwd = n.SecurityEnabled ? txtPassword.Text : null;
            if (n.SecurityEnabled && string.IsNullOrEmpty(pwd)) { Log($"\"{n.Ssid}\" is secured — enter a password."); return; }

            btnConnect.Enabled = false;
            Log($"Connecting to \"{n.Ssid}\"…");
            try
            {
                var wlan = _wlan; var iface = _iface; var ssid = n.Ssid;
                uint reason = await Task.Run(() => wlan.Connect(iface, ssid, pwd));
                Log(reason == 0 ? "Profile set, associating…" : $"Profile set (reason {reason}), associating…");
            }
            catch (Exception ex) { Log("Connect failed: " + ex.Message); }
            finally { if (!IsDisposed) btnConnect.Enabled = true; }
        }

        void btnDisconnect_Click(object? sender, EventArgs e)
        {
            if (_wlan is null || !_hasIface) return;
            try { _wlan.Disconnect(_iface); Log("Disconnected."); }
            catch (Exception ex) { Log("Disconnect failed: " + ex.Message); }
        }

        void rssiTimer_Tick(object? sender, EventArgs e) => RefreshLink();

        void RefreshLink()
        {
            if (_wlan is null || !_hasIface) return;
            try
            {
                var link = _wlan.GetCurrentLink(_iface);
                if (link.Connected)
                {
                    lblLink.Text = $"Link: {link.Ssid}  ({link.State})";
                    lblRssi.Text = $"{link.RssiDbm} dBm ({link.SignalQuality}%)";
                    lblRssi.ForeColor = link.SignalQuality >= 50 ? Theme.Good
                                      : link.SignalQuality >= 25 ? Theme.Warn : Theme.Danger;
                }
                else
                {
                    lblLink.Text = $"Link: — ({link.State})";
                    lblRssi.Text = "— dBm (—%)";
                    lblRssi.ForeColor = Theme.Muted;
                }
            }
            catch { /* transient query failures between state changes are non-fatal */ }
        }

        void SetEnabled(bool on)
        {
            btnScan.Enabled = on; btnConnect.Enabled = on; btnDisconnect.Enabled = on;
            txtPassword.Enabled = on; lstNetworks.Enabled = on;
        }

        void Log(string s) => txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {s}\r\n");

        void WifiAdapterForm_FormClosed(object? sender, FormClosedEventArgs e)
        {
            rssiTimer.Stop();
            _wlan?.Dispose();
        }
    }
}

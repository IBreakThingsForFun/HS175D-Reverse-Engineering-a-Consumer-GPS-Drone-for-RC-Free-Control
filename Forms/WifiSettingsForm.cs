using System.ComponentModel;

namespace HS175D
{
    /// <summary>
    /// One-click SoftAP control: 2.4/5 GHz band, multi-station (ap_isolate off), factory restore.
    /// All actions run through <see cref="DroneWifi"/> over adb — no file editing. Applying restarts
    /// hostapd and briefly drops WiFi; if the SSID/band changed you may need to rejoin the AP.
    /// </summary>
    public sealed partial class WifiSettingsForm : Form
    {
        readonly string _ip;
        Button[] _actions = Array.Empty<Button>();
        readonly ToolTip _tip = new() { AutoPopDelay = 30000, InitialDelay = 350, ReshowDelay = 80, ShowAlways = true };

        public WifiSettingsForm(string ip)
        {
            _ip = ip;
            InitializeComponent();
            Theme.Apply(this);

            _lblTitle.Text = $"AP control — drone {ip}";
            _btnFactory.BackColor = Theme.Warn;
            _btnFactory.ForeColor = Color.Black;

            _actions = new[] { _btnRefresh, _btn24, _btn5, _btnMulti, _btnFactory };

            _tip.SetToolTip(_btnRefresh, "Query the drone's current AP status over adb.");
            _tip.SetToolTip(_btn24, "Switch the drone's SoftAP to 2.4 GHz (restarts hostapd — briefly drops WiFi).");
            _tip.SetToolTip(_btn5, "Switch the drone's SoftAP to 5 GHz, the factory default band.");
            _tip.SetToolTip(_btnMulti, "Turn off ap_isolate so more than one station can join the AP at once.");
            _tip.SetToolTip(_btnFactory, "Restore the stock 5 GHz AP and drop the persistent 2.4 GHz override.");
            _tip.SetToolTip(_persist, "Write the change to /netPrivate (mtd5) so it survives a reboot — otherwise it only applies to the running session.");

            _btnRefresh.Click += (s, e) => Run("Status", () => DroneWifi.Status(_ip), refreshAfter: false);
            _btn24.Click += (s, e) => Run("Set 2.4 GHz", () => DroneWifi.Set24G(_ip, _persist.Checked));
            _btn5.Click += (s, e) => Run("Set 5 GHz", () => DroneWifi.Set5G(_ip, _persist.Checked));
            _btnMulti.Click += (s, e) => Run("Multi-station", () => DroneWifi.EnsureMultiStation(_ip));
            _btnFactory.Click += (s, e) =>
            {
                if (MessageBox.Show("Restore factory AP (stock 5 GHz, drop the persistent 2.4 override)?",
                        "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    Run("Restore factory", () => DroneWifi.RestoreFactory(_ip));
            };

            Shown += (s, e) => Run("Status", () => DroneWifi.Status(_ip), refreshAfter: false);
        }

        void Log(string s) => _status.AppendText(s.TrimEnd() + "\r\n");

        void Run(string label, Func<string> action, bool refreshAfter = true)
        {
            foreach (var b in _actions) b.Enabled = false;
            Log($"--- {label} @ {DateTime.Now:HH:mm:ss} ---");
            var bg = new BackgroundWorker();
            bg.DoWork += (s, e) => e.Result = action();
            bg.RunWorkerCompleted += (s, e) =>
            {
                Log(e.Error != null ? "(error) " + e.Error.Message : (string)(e.Result ?? ""));
                if (refreshAfter)
                {
                    Log("(waiting ~8 s for hostapd to come back…)");
                    var t = new System.Windows.Forms.Timer { Interval = 8000 };
                    t.Tick += (ts, te) =>
                    {
                        t.Stop(); t.Dispose();
                        var bg2 = new BackgroundWorker();
                        bg2.DoWork += (s2, e2) => e2.Result = DroneWifi.Status(_ip);
                        bg2.RunWorkerCompleted += (s2, e2) =>
                        {
                            Log("--- status ---"); Log((string)(e2.Result ?? ""));
                            foreach (var b in _actions) b.Enabled = true;
                        };
                        bg2.RunWorkerAsync();
                    };
                    t.Start();
                }
                else foreach (var b in _actions) b.Enabled = true;
            };
            bg.RunWorkerAsync();
        }
    }
}

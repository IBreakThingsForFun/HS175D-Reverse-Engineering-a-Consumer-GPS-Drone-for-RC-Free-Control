using System.IO.Ports;
using System.Text;

namespace HS175D
{
    /// <summary>
    /// Serial bridge to the ESP32 ground station (WiFiClientBasic.ino). The firmware exposes a
    /// 115200-baud line console (type 'h' for help): h/s/n, t/y/p/r &lt;hex&gt; axis bytes, f&lt;hex&gt; flag,
    /// 0/i/l&lt;v&gt; CMDs, 'c&lt;id&gt; &lt;param&gt;' arbitrary. This panel drives exactly that protocol, so it
    /// works against the current .ino with zero firmware changes. USB-CDC, no drone connection needed.
    /// </summary>
    public sealed class Esp32Panel : UserControl
    {
        SerialPort? _port;
        readonly ComboBox _cboPort = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        readonly ComboBox _cboBaud = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        readonly Button _btnRefresh = Theme.Btn("⟳", 0, 0, 32);
        readonly Button _btnConnect = Theme.AccentBtn("Connect", 0, 0, 96);
        readonly Label _status = Theme.Lbl("Disconnected", 0, 0, Theme.Danger);
        readonly TextBox _rx = Theme.Mono_(readOnly: true);
        readonly TextBox _tx = Theme.Mono_(readOnly: false, multiline: false);
        readonly ToolTip _tip = new() { AutoPopDelay = 30000, InitialDelay = 350, ReshowDelay = 80, ShowAlways = true };

        public Esp32Panel()
        {
            // Hand-built with absolute pixel Locations throughout - must never go through WinForms'
            // font/DPI auto-scale pass (see VideoPanel.Designer.cs's identical fix) or the toolbar
            // and quick-command row get skewed once this is docked into ReWorkbench at runtime.
            AutoScaleMode = AutoScaleMode.None;
            Dock = DockStyle.Fill;
            BackColor = Theme.Bg1;

            var top = new Panel { Dock = DockStyle.Top, Height = 76, BackColor = Theme.Bg1 };
            _cboPort.SetBounds(8, 10, 120, 24); _cboPort.BackColor = Theme.Bg2; _cboPort.ForeColor = Theme.Text; _cboPort.FlatStyle = FlatStyle.Flat;
            _btnRefresh.Location = new Point(132, 9);
            _cboBaud.SetBounds(172, 10, 90, 24); _cboBaud.BackColor = Theme.Bg2; _cboBaud.ForeColor = Theme.Text; _cboBaud.FlatStyle = FlatStyle.Flat;
            _cboBaud.Items.AddRange(new object[] { 9600, 57600, 74880, 115200, 230400, 921600 });
            _cboBaud.SelectedItem = 115200;
            _btnConnect.Location = new Point(268, 9);
            _status.Location = new Point(372, 14);
            _tip.SetToolTip(_btnRefresh, "Rescan available COM ports.");
            _tip.SetToolTip(_btnConnect, "Open/close the serial connection to the ESP32.");
            _tip.SetToolTip(_cboPort, "ESP32's COM port.");
            _tip.SetToolTip(_cboBaud, "Serial baud rate — must match the .ino's Serial.begin(115200).");

            // quick-command row mapped 1:1 to the .ino console
            var quick = new (string label, string send)[]
            {
                ("Help (h)", "h"), ("Stream (s)", "s"), ("Neutral (n)", "n"),
                ("Hello CMD0", "0"), ("DroneID (i)", "i"), ("Log5 (l5)", "l5"),
                ("GyroCal (f04)", "f04"), ("MagCal (f08)", "f08"),
            };
            int qx = 8;
            foreach (var (label, send) in quick)
            {
                var b = Theme.Btn(label, qx, 44, 96, 24);
                b.Click += (s, e) => SendLine(send);
                _tip.SetToolTip(b, $"Send '{send}' to the ESP32 console.");
                top.Controls.Add(b);
                qx += 100;
            }
            top.Controls.AddRange(new Control[] { _cboPort, _btnRefresh, _cboBaud, _btnConnect, _status });

            // send line
            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 34, BackColor = Theme.Bg1 };
            var lbl = Theme.Lbl("send ▸", 8, 9, Theme.Accent);
            _tx.SetBounds(60, 6, 580, 23);
            _tx.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            var btnSend = Theme.Btn("Send", 0, 5, 80);
            btnSend.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            bottom.Resize += (s, e) => { _tx.Width = bottom.Width - 150; btnSend.Left = bottom.Width - 86; };
            _tx.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { SendLine(_tx.Text); _tx.Clear(); e.SuppressKeyPress = true; } };
            btnSend.Click += (s, e) => { SendLine(_tx.Text); _tx.Clear(); };
            _tip.SetToolTip(btnSend, "Send the raw line above to the ESP32 console (Enter also works).");
            bottom.Controls.AddRange(new Control[] { lbl, _tx, btnSend });

            _rx.Dock = DockStyle.Fill;

            Controls.Add(_rx);
            Controls.Add(bottom);
            Controls.Add(top);

            _btnRefresh.Click += (s, e) => RefreshPorts();
            _btnConnect.Click += (s, e) => Toggle();
            RefreshPorts();
            Disposed += (s, e) => ClosePort();
        }

        void RefreshPorts()
        {
            _cboPort.Items.Clear();
            var ports = PortInfo.List();
            _cboPort.Items.AddRange(ports);
            if (ports.Length > 0 && _cboPort.SelectedIndex < 0) _cboPort.SelectedIndex = ports.Length - 1;
            Append($"[ports] {(ports.Length == 0 ? "none found" : string.Join(", ", ports.Select(p => p.ToString())))}\r\n");
        }

        void Toggle()
        {
            if (_port is { IsOpen: true }) { ClosePort(); return; }
            if (_cboPort.SelectedItem is not PortInfo.Entry sel) { Append("[err] pick a COM port\r\n"); return; }
            string name = sel.Port;
            try
            {
                _port = new SerialPort(name, (int)_cboBaud.SelectedItem!, Parity.None, 8, StopBits.One)
                { Encoding = Encoding.ASCII, NewLine = "\n", DtrEnable = true, RtsEnable = true, ReadTimeout = 500 };
                _port.DataReceived += OnData;
                _port.Open();
                SetStatus($"Connected {name} @ {_cboBaud.SelectedItem}", Theme.Good);
                _btnConnect.Text = "Disconnect";
                SendLine("h");   // greet: dump the firmware help banner
            }
            catch (Exception ex) { Append("[err] open failed: " + ex.Message + "\r\n"); ClosePort(); }
        }

        void ClosePort()
        {
            try { if (_port != null) { _port.DataReceived -= OnData; if (_port.IsOpen) _port.Close(); _port.Dispose(); } }
            catch { }
            _port = null;
            SetStatus("Disconnected", Theme.Danger);
            if (_btnConnect.IsHandleCreated) _btnConnect.Text = "Connect";
        }

        void OnData(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string s = _port?.ReadExisting() ?? "";
                if (s.Length > 0) Append(s.Replace("\r\n", "\n").Replace("\n", "\r\n"));
            }
            catch { }
        }

        void SendLine(string line)
        {
            if (line.Length == 0) return;
            if (_port is not { IsOpen: true }) { Append("[err] not connected\r\n"); return; }
            try { _port.Write(line + "\n"); Append("» " + line + "\r\n"); }
            catch (Exception ex) { Append("[err] write: " + ex.Message + "\r\n"); }
        }

        void Append(string s)
        {
            if (!IsHandleCreated) return;
            try
            {
                BeginInvoke(() =>
                {
                    if (_rx.TextLength > 80000) _rx.Clear();
                    _rx.AppendText(s);
                });
            }
            catch { }
        }

        void SetStatus(string t, Color c)
        {
            if (!IsHandleCreated) { _status.Text = t; _status.ForeColor = c; return; }
            try { BeginInvoke(() => { _status.Text = t; _status.ForeColor = c; }); } catch { }
        }
    }
}

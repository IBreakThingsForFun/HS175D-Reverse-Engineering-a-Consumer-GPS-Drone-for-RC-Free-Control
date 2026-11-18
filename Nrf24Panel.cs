using System.IO.Ports;
using System.Text;

namespace HS175D
{
    /// <summary>
    /// Serial bridge to the ESP32 + nRF24L01+ RC-link impersonator
    /// (<c>esp32_nrf24_optical_tx/esp32_nrf24_optical_tx.ino</c>) — real RC-free bind/arm/control
    /// over the BK2425 link, confirmed working 2026-07-03/04 (see docs/WRITEUP.md). This is a
    /// DIFFERENT firmware/protocol from <see cref="Esp32Panel"/> (which targets the generic
    /// WiFiClientBasic.ino console) — the two are separate ESP32 sketches and separate serial
    /// connections. Buttons send exactly the single-char commands the .ino listens for:
    /// c=cold-start bind+control, b=continuous bind, o=optical, n=neutral/GPS, a=ARM burst,
    /// x=brute-force pair scan, s=pause/resume streaming, ?=radio details.
    /// </summary>
    public sealed partial class Nrf24Panel : UserControl
    {
        SerialPort? _port;
        readonly ToolTip _tip = new() { AutoPopDelay = 30000, InitialDelay = 350, ReshowDelay = 80, ShowAlways = true };

        public Nrf24Panel()
        {
            InitializeComponent();
            Theme.Apply(this);

            _btnArm.BackColor = Theme.Danger;
            _btnArm.ForeColor = Color.White;
            _lblSafety.ForeColor = Theme.Warn;

            _cboBaud.Items.AddRange(new object[] { 9600, 57600, 74880, 115200, 230400, 921600 });
            _cboBaud.SelectedItem = 115200;

            _btnRefresh.Click += (s, e) => RefreshPorts();
            _btnConnect.Click += (s, e) => Toggle();
            _btnColdStart.Click += (s, e) => SendCmd('c', "COLD START (bind+control)");
            _btnBind.Click += (s, e) => SendCmd('b', "CONTINUOUS BIND");
            _btnOptical.Click += (s, e) => SendCmd('o', "OPTICAL");
            _btnNeutral.Click += (s, e) => SendCmd('n', "NEUTRAL / GPS");
            _btnPause.Click += (s, e) => SendCmd('s', "pause/resume");
            _btnBruteForce.Click += (s, e) => SendCmd('x', "BRUTE-FORCE pair scan");
            _btnRadioDetails.Click += (s, e) => SendCmd('?', "radio details");
            _btnArm.Click += (s, e) =>
            {
                if (MessageBox.Show("This fires the ARM burst — the drone WILL spin its motors.\n\nAre props OFF?",
                        "Confirm ARM", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    SendCmd('a', "ARM BURST");
            };
            _btnAutoSeq.Click += (s, e) => RunAutoSequence();

            _tip.SetToolTip(_btnRefresh, "Rescan available COM ports.");
            _tip.SetToolTip(_btnConnect, "Open/close the serial connection to the ESP32 impersonator.");
            _tip.SetToolTip(_btnColdStart, "Send 'c' — bind announce like a fresh remote. Power the drone on after, watch for RED blink = paired.");
            _tip.SetToolTip(_btnBind, "Send 'b' — continuous bind sweep (bind channels withheld) without taking over control yet.");
            _tip.SetToolTip(_btnOptical, "Send 'o' — control/optical mode (byte6=0x40 held).");
            _tip.SetToolTip(_btnNeutral, "Send 'n' — control/neutral (GPS-style) mode.");
            _tip.SetToolTip(_btnPause, "Send 's' — pause/resume the streaming loop.");
            _tip.SetToolTip(_btnArm, "Send 'a' — fires the ARM burst. Confirms props-off first; the drone WILL spin its motors.");
            _tip.SetToolTip(_btnBruteForce, "Send 'x' — brute-force pairing address scan; watch the drone LED for RED on a candidate.");
            _tip.SetToolTip(_btnRadioDetails, "Send '?' — dump the nRF24 radio config/details to the log below.");
            _tip.SetToolTip(_btnAutoSeq, "Cold-start bind, then (after you confirm the RED blink) auto-switch to Optical mode. Arm stays a separate manual step.");
            _tip.SetToolTip(_cboPort, "ESP32's COM port.");
            _tip.SetToolTip(_cboBaud, "Serial baud rate — must match the .ino's Serial.begin().");

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

        void RunAutoSequence()
        {
            if (_port is not { IsOpen: true }) { Append("[err] not connected\r\n"); return; }
            SendCmd('c', "COLD START (auto-sequence step 1/2)");
            var result = MessageBox.Show(
                "Cold-start bind announce is now streaming.\n\n" +
                "Power on the drone now. Watch its LED — it blinks RED once paired to us.\n\n" +
                "Click OK once you see the RED blink to switch to Optical control.\nClick Cancel to stop here.",
                "Confirm bind", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
            if (result != DialogResult.OK) { Append("[auto-seq] stopped after cold start\r\n"); return; }
            SendCmd('o', "OPTICAL (auto-sequence step 2/2)");
        }

        void SendCmd(char c, string label)
        {
            if (_port is not { IsOpen: true }) { Append("[err] not connected\r\n"); return; }
            try { _port.Write(c.ToString()); Append($"» {label} ('{c}')\r\n"); }
            catch (Exception ex) { Append("[err] write: " + ex.Message + "\r\n"); }
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

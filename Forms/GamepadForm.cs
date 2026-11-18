namespace HS175D
{
    /// <summary>
    /// Drives the drone's four sticks from an Xbox controller (Mode 2). Polls XInput at ~60 Hz and
    /// writes throttle/yaw/pitch/roll into the main window's existing control path — so the live
    /// control loop, arm gate and telemetry are all unchanged; the pad is just another stick source.
    ///
    /// SAFETY: output only flows when BOTH "Enable" is ticked here AND the main window is ARMED.
    /// Losing the controller, un-ticking Enable, or pressing Back re-centres the sticks immediately.
    /// </summary>
    public sealed class GamepadForm : Form
    {
        readonly Func<bool> _isArmed;
        readonly Action<byte, byte, byte, byte> _applySticks;   // throttle, yaw, pitch, roll
        readonly Action _center;

        readonly CheckBox _enable = new() { Text = "Enable controller → sticks", AutoSize = true };
        readonly Label _status = new() { AutoSize = true, MaximumSize = new Size(400, 0) };
        readonly NumericUpDown _deadzone = new() { Minimum = 0, Maximum = 40, Value = 12, Width = 56 };   // %
        readonly NumericUpDown _expo = new() { Minimum = 0, Maximum = 80, Value = 30, Width = 56 };       // %
        readonly CheckBox _invertPitch = new() { Text = "Invert pitch", AutoSize = true, Checked = true };
        readonly ComboBox _throttleMode = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };

        readonly ProgressBar _barThr = NewBar(), _barYaw = NewBar(), _barPitch = NewBar(), _barRoll = NewBar();
        readonly Label _vThr = NewVal(), _vYaw = NewVal(), _vPitch = NewVal(), _vRoll = NewVal();

        readonly System.Windows.Forms.Timer _timer = new() { Interval = 16 };
        readonly ToolTip _tip = new() { AutoPopDelay = 30000, InitialDelay = 350, ReshowDelay = 80, ShowAlways = true };
        bool _wasConnected;
        double _thrAcc;          // ratchet-mode accumulated throttle (0..255)
        const double ThrRate = 2.6;   // ratchet climb/descend per tick at full deflection

        public GamepadForm(Func<bool> isArmed, Action<byte, byte, byte, byte> applySticks, Action center)
        {
            _isArmed = isArmed; _applySticks = applySticks; _center = center;

            Text = "Xbox Controller — Drone RE Studio";
            ClientSize = new Size(440, 372);
            MinimumSize = new Size(440, 372);
            Font = new Font("Segoe UI", 9F);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;

            _enable.Location = new Point(14, 12);
            _enable.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            _status.Location = new Point(16, 40);

            // mapping options
            var l1 = new Label { Text = "Deadzone %", AutoSize = true, Location = new Point(16, 78) };
            _deadzone.Location = new Point(96, 74);
            var l2 = new Label { Text = "Expo %", AutoSize = true, Location = new Point(172, 78) };
            _expo.Location = new Point(224, 74);
            _invertPitch.Location = new Point(296, 76);
            var l3 = new Label { Text = "Throttle", AutoSize = true, Location = new Point(16, 108) };
            _throttleMode.Location = new Point(96, 104);
            _throttleMode.Items.AddRange(new object[] { "Centered (hover on release)", "Ratchet (holds last)" });
            _throttleMode.SelectedIndex = 0;

            // axis meters
            int y = 146;
            AddAxis("Throttle", _barThr, _vThr, ref y);
            AddAxis("Yaw", _barYaw, _vYaw, ref y);
            AddAxis("Pitch", _barPitch, _vPitch, ref y);
            AddAxis("Roll", _barRoll, _vRoll, ref y);

            var btnCenter = new Button { Text = "Center / Stop", Location = new Point(16, y + 8), Size = new Size(120, 28) };
            btnCenter.Click += (s, e) => { _enable.Checked = false; _center(); };
            _tip.SetToolTip(btnCenter, "Disable the controller and immediately re-centre all sticks (throttle to 0).");
            _tip.SetToolTip(_enable, "Master switch — pad only drives the sticks when this AND the main window's flight-commands checkbox are both on.");
            _tip.SetToolTip(_deadzone, "Stick deadzone around center, as a % of full travel.");
            _tip.SetToolTip(_expo, "Expo curve % — softens response near center for finer control.");
            _tip.SetToolTip(_invertPitch, "Flip the pitch axis if forward/back feels reversed.");
            _tip.SetToolTip(_throttleMode, "Centered = throttle springs back to hover on release. Ratchet = holds the last throttle value.");
            var hint = new Label
            {
                AutoSize = true, Location = new Point(150, y + 14), ForeColor = Color.DimGray,
                Text = "Mode 2 · Left=throttle/yaw · Right=pitch/roll · Back=panic center"
            };

            Controls.AddRange(new Control[]
            {
                _enable, _status, l1, _deadzone, l2, _expo, _invertPitch, l3, _throttleMode,
                btnCenter, hint
            });

            _enable.CheckedChanged += (s, e) => { _thrAcc = 0; if (!_enable.Checked) _center(); };
            _timer.Tick += (s, e) => Tick();

            Theme.Apply(this);
            FormClosed += (s, e) => { _timer.Stop(); try { _center(); } catch { } };
            _timer.Start();
        }

        void AddAxis(string name, ProgressBar bar, Label val, ref int y)
        {
            var lbl = new Label { Text = name, AutoSize = true, Location = new Point(16, y + 2) };
            bar.Location = new Point(96, y); bar.Size = new Size(260, 18);
            val.Location = new Point(364, y + 1);
            Controls.AddRange(new Control[] { lbl, bar, val });
            y += 28;
        }

        static ProgressBar NewBar() => new() { Minimum = 0, Maximum = 255, Value = 128 };
        static Label NewVal() => new() { AutoSize = true, Text = "128", Font = new Font("Consolas", 9F) };

        // ---- poll loop ----
        void Tick()
        {
            bool connected = XInput.TryGetState(0, out var st);
            if (!connected)
            {
                if (_wasConnected && _enable.Checked) { _enable.Checked = false; SafeCenter(); }
                _wasConnected = false;
                SetBars(0, 128, 128, 128);
                SetStatus("No controller on port 1 — connect an Xbox controller (or check the cable / pairing).", Theme.Warn);
                return;
            }
            _wasConnected = true;
            var g = st.Gamepad;

            float yawN = Curve(g.sThumbLX / 32767f);
            float rollN = Curve(g.sThumbRX / 32767f);
            float pitchN = Curve(g.sThumbRY / 32767f); if (_invertPitch.Checked) pitchN = -pitchN;
            float thrStick = Curve(g.sThumbLY / 32767f);

            byte yaw = ToByte(yawN), roll = ToByte(rollN), pitch = ToByte(pitchN);
            byte throttle;
            if (_throttleMode.SelectedIndex == 1)   // ratchet: integrate deflection, hold on release
            {
                _thrAcc = Math.Clamp(_thrAcc + thrStick * ThrRate, 0, 255);
                throttle = (byte)Math.Round(_thrAcc);
            }
            else                                     // centered: stick maps directly, releases to hover
            {
                throttle = ToByte(thrStick);
            }

            SetBars(throttle, yaw, pitch, roll);

            if ((g.wButtons & XInput.Back) != 0) { if (_enable.Checked) _enable.Checked = false; SafeCenter(); }

            if (_enable.Checked)
            {
                if (_isArmed()) { _applySticks(throttle, yaw, pitch, roll); SetStatus("● LIVE — controlling sticks.", Theme.Good); }
                else { SetStatus("Enabled, holding — ARM in the main window to take control.", Theme.Warn); }
            }
            else SetStatus("Controller connected. Tick Enable, then ARM in the main window to fly.", Theme.Text);
        }

        // deadzone + expo shaping, input/output in -1..1
        float Curve(float x)
        {
            x = Math.Clamp(x, -1f, 1f);
            float dz = (float)_deadzone.Value / 100f;
            float ax = Math.Abs(x);
            if (ax <= dz) return 0f;
            float n = (ax - dz) / (1f - dz);                 // rescale past deadzone
            float e = (float)_expo.Value / 100f;
            n = (1f - e) * n + e * n * n * n;                // cubic expo blend
            return Math.Sign(x) * n;
        }

        static byte ToByte(float n) => (byte)Math.Clamp((int)Math.Round(128 + n * 127f), 0, 255);

        void SetBars(byte thr, byte yaw, byte pitch, byte roll)
        {
            _barThr.Value = thr; _vThr.Text = thr.ToString();
            _barYaw.Value = yaw; _vYaw.Text = yaw.ToString();
            _barPitch.Value = pitch; _vPitch.Text = pitch.ToString();
            _barRoll.Value = roll; _vRoll.Text = roll.ToString();
        }

        void SafeCenter() { try { _center(); } catch { } }
        void SetStatus(string text, Color c) { _status.ForeColor = c; _status.Text = text; }
    }
}

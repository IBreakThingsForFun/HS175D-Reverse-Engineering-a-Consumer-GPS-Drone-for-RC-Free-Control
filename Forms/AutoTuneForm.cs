namespace HS175D
{
    /// <summary>
    /// Front end over AirSimPanel's four auto-tune routines (StartAltitudeTune/StartTiltTune/
    /// StartHorizontalTune/StartYawTune) - this form owns no flight state itself, it only reads/writes
    /// AirSimPanel's fields through the get/set delegates passed into the constructor, same pattern
    /// OrbitWizardForm already uses. Modeless singleton, opened from AirSimPanel like SceneManagerForm/
    /// OrbitWizardForm/WorldMapForm.
    ///
    /// All four routines are single-pass/iterative "measure, report, you click Apply" by design - see
    /// AirSimPanel's own doc comments on each Start* method for why (reviewing the numbers before they
    /// take effect is the actual safety point, not an afterthought).
    /// </summary>
    public partial class AutoTuneForm : Form
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool ShowScrollBar(IntPtr hWnd, int wBar, [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)] bool bShow);
        const int SB_HORZ = 0;
        public readonly record struct AltResult(double Kp, double Ki, double Kd, double VelocityGain, double DeadTimeSec, string Note);
        public readonly record struct TiltResult(float PeakAccelDeg, float PeakDecelDeg, double SettleSec, float RecommendedAggressiveness, string Note);
        public readonly record struct HorizResult(double Kp, double Ki, double Kd, double VelocityGain, double DeadTimeSec, string Note);
        public readonly record struct YawResult(double Kp, double Ki, double Kd, double VelocityGain, double DeadTimeSec, string Note);

        readonly Func<float> _getTestAlt; readonly Action<float> _setTestAlt;
        readonly Action _startAltTune; readonly Action _stopAltTune;
        readonly Func<AltResult?> _getAltResult; readonly Action _applyAltResult;

        readonly Func<float> _getMaxTiltDeg; readonly Action<float> _setMaxTiltDeg;
        readonly Action _startTiltTune; readonly Action _stopTiltTune;
        readonly Func<TiltResult?> _getTiltResult; readonly Action _applyTiltResult;

        readonly Action _startHorizTune; readonly Action _stopHorizTune;
        readonly Func<HorizResult?> _getHorizResult; readonly Action _applyHorizResult;

        readonly Action _startYawTune; readonly Action _stopYawTune;
        readonly Func<YawResult?> _getYawResult; readonly Action _applyYawResult;

        public AutoTuneForm(
            Func<float> getTestAlt, Action<float> setTestAlt,
            Action startAltTune, Action stopAltTune,
            Func<AltResult?> getAltResult, Action applyAltResult,
            Func<float> getMaxTiltDeg, Action<float> setMaxTiltDeg,
            Action startTiltTune, Action stopTiltTune,
            Func<TiltResult?> getTiltResult, Action applyTiltResult,
            Action startHorizTune, Action stopHorizTune,
            Func<HorizResult?> getHorizResult, Action applyHorizResult,
            Action startYawTune, Action stopYawTune,
            Func<YawResult?> getYawResult, Action applyYawResult)
        {
            _getTestAlt = getTestAlt; _setTestAlt = setTestAlt;
            _startAltTune = startAltTune; _stopAltTune = stopAltTune;
            _getAltResult = getAltResult; _applyAltResult = applyAltResult;
            _getMaxTiltDeg = getMaxTiltDeg; _setMaxTiltDeg = setMaxTiltDeg;
            _startTiltTune = startTiltTune; _stopTiltTune = stopTiltTune;
            _getTiltResult = getTiltResult; _applyTiltResult = applyTiltResult;
            _startHorizTune = startHorizTune; _stopHorizTune = stopHorizTune;
            _getHorizResult = getHorizResult; _applyHorizResult = applyHorizResult;
            _startYawTune = startYawTune; _stopYawTune = stopYawTune;
            _getYawResult = getYawResult; _applyYawResult = applyYawResult;

            InitializeComponent();
            Theme.Apply(this);

            numTestAlt.Value = Math.Clamp((decimal)_getTestAlt(), numTestAlt.Minimum, numTestAlt.Maximum);
            numMaxTilt.Value = Math.Clamp((decimal)_getMaxTiltDeg(), numMaxTilt.Minimum, numMaxTilt.Maximum);

            // A result from an earlier tune run this session (before this form was last closed) should
            // show up as soon as the form reopens, not just after a fresh Go/Apply - otherwise it looks
            // like settings "didn't load" even though AirSimPanel still has them in memory.
            if (_getAltResult() is { } ar) ShowAltResult(ar);
            if (_getTiltResult() is { } tr) ShowTiltResult(tr);
            if (_getHorizResult() is { } hr) ShowHorizResult(hr);
            if (_getYawResult() is { } yr) ShowYawResult(yr);

            // The designer's fixed Y positions assumed each wrapping label was ~1-2 lines tall;
            // several actually wrap to 3-4 lines at their MaximumSize width (info labels always,
            // status labels once real progress text lands in them), so the label overlaps the
            // controls below it. Push those down (and grow the GroupBox to match) using the
            // label's real auto-sized height instead of hand-tuning per-section magic numbers.
            FixInfoOverlap(lblAltInfo, lblTestAlt, numTestAlt, btnAltGo, btnAltStop, lblAltStatus, lblAltResult, btnAltApply);
            FixInfoOverlap(lblTiltInfo, lblMaxTilt, numMaxTilt, btnTiltGo, btnTiltStop, lblTiltStatus, lblTiltResult, btnTiltApply);
            FixInfoOverlap(lblHorizInfo, btnHorizGo, btnHorizStop, lblHorizStatus, lblHorizResult, btnHorizApply);
            FixInfoOverlap(lblYawInfo, btnYawGo, btnYawStop, lblYawStatus, lblYawResult, btnYawApply);

            // Status labels change at runtime (Go click / tune-finished callbacks) to text far
            // longer than the "Ready." placeholder they start with, so a one-time layout pass isn't
            // enough - pin the result label/Apply button to the status label's live bottom edge.
            // Plain always-expanded GroupBoxes (as laid out in the Designer) rather than the collapsible
            // runtime-built "card" chrome this used to reparent everything into - every control here is
            // a real Designer field, editable/movable in the VS Designer like a normal form. flowMain's
            // own AutoScroll (see Designer) handles the case where all four don't fit the window at once.
            int altBodyH = grpAlt.Height, tiltBodyH = grpTilt.Height, horizBodyH = grpHoriz.Height, yawBodyH = grpYaw.Height;

            PinBelowStatus(lblAltStatus, grpAlt, altBodyH, lblAltResult, btnAltApply);
            PinBelowStatus(lblTiltStatus, grpTilt, tiltBodyH, lblTiltResult, btnTiltApply);
            PinBelowStatus(lblHorizStatus, grpHoriz, horizBodyH, lblHorizResult, btnHorizApply);
            PinBelowStatus(lblYawStatus, grpYaw, yawBodyH, lblYawResult, btnYawApply);

            // AutoScroll on flowMain occasionally decides it needs a horizontal bar too (its own
            // vertical-scrollbar reservation nibbles into the width it measures against) - the form
            // is a fixed width by design (MinimumSize == MaximumSize below), so just force it off.
            flowMain.HandleCreated += (s, e) => ShowScrollBar(flowMain.Handle, SB_HORZ, false);
            flowMain.Layout += (s, e) => { if (flowMain.IsHandleCreated) ShowScrollBar(flowMain.Handle, SB_HORZ, false); };
        }

        static void FixInfoOverlap(Label info, params Control[] following)
        {
            if (following.Length == 0) return;
            int shift = (info.Bottom + 8) - following[0].Top;
            if (shift <= 0) return;
            foreach (var c in following) c.Top += shift;
            if (info.Parent is GroupBox g) g.Height += shift;
        }

        /// <summary>Keeps `following` pinned just below `status` even as its AutoSize height changes
        /// at runtime, growing `container` (the section's own GroupBox) to match.</summary>
        static void PinBelowStatus(Label status, Control container, int baseBodyHeight, params Control[] following)
        {
            var gap = new int[following.Length];
            for (int i = 0; i < following.Length; i++) gap[i] = following[i].Top - status.Bottom;
            int baseBottom = following.Length > 0 ? following[^1].Bottom : status.Bottom;

            status.SizeChanged += (s, e) =>
            {
                int y = status.Bottom;
                for (int i = 0; i < following.Length; i++) { following[i].Top = y + gap[i]; y = following[i].Bottom; }
                container.Height = baseBodyHeight + (y - baseBottom);
            };
        }

        void numTestAlt_ValueChanged(object? sender, EventArgs e) => _setTestAlt((float)numTestAlt.Value);
        void numMaxTilt_ValueChanged(object? sender, EventArgs e) => _setMaxTiltDeg((float)numMaxTilt.Value);

        void btnAltGo_Click(object? sender, EventArgs e)
        {
            lblAltStatus.Text = "Running - watch the main panel status line for live phase updates.";
            lblAltResult.Text = "";
            btnAltApply.Enabled = false;
            _startAltTune();
        }

        void btnAltStop_Click(object? sender, EventArgs e) => _stopAltTune();

        /// <summary>Called by AirSimPanel's one-shot completion callback (wired once in
        /// EnsureAutoTuneForm) whenever an altitude tune run ends, clean or aborted.</summary>
        public void OnAltTuneFinished(string why)
        {
            lblAltStatus.Text = why;
            if (_getAltResult() is { } r) ShowAltResult(r);
        }

        // Also called from the constructor - a previously-measured result (from an earlier tune run
        // this session) should show up the moment the form opens, not just after a fresh Go/Apply.
        void ShowAltResult(AltResult r)
        {
            lblAltResult.Text = $"Kp={r.Kp:F3}  Ki={r.Ki:F3}  Kd={r.Kd:F3}\r\n" +
                                 $"measured: {r.VelocityGain:F3} m/s per unit throttle, dead time {r.DeadTimeSec:F2}s" +
                                 (r.Note.Length > 0 ? $"\r\n{r.Note}" : "");
            btnAltApply.Enabled = true;
        }

        void btnAltApply_Click(object? sender, EventArgs e)
        {
            _applyAltResult();
            lblAltStatus.Text = "Applied - new gains are live for Orbit Scan and Line Pass.";
        }

        void btnTiltGo_Click(object? sender, EventArgs e)
        {
            lblTiltStatus.Text = "Running - flies the current Line Pass distance/heading/speed settings.";
            lblTiltResult.Text = "";
            btnTiltApply.Enabled = false;
            _startTiltTune();
        }

        void btnTiltStop_Click(object? sender, EventArgs e) => _stopTiltTune();

        /// <summary>Called by AirSimPanel's one-shot completion callback whenever a tilt tune run ends.</summary>
        public void OnTiltTuneFinished(string why)
        {
            lblTiltStatus.Text = why;
            if (_getTiltResult() is { } r) ShowTiltResult(r);
        }

        void ShowTiltResult(TiltResult r)
        {
            lblTiltResult.Text = $"Peak accel {r.PeakAccelDeg:F1}°  Peak decel {r.PeakDecelDeg:F1}°  settled in {r.SettleSec:F1}s\r\n" +
                                 $"Recommended Tilt aggressiveness: {r.RecommendedAggressiveness * 100:F0}%\r\n{r.Note}";
            btnTiltApply.Enabled = true;
        }

        void btnTiltApply_Click(object? sender, EventArgs e)
        {
            _applyTiltResult();
            lblTiltStatus.Text = "Applied - Tilt aggressiveness updated in Flight settings.";
        }

        void btnHorizGo_Click(object? sender, EventArgs e)
        {
            lblHorizStatus.Text = "Running - watch the main panel status line for live phase updates.";
            lblHorizResult.Text = "";
            btnHorizApply.Enabled = false;
            _startHorizTune();
        }

        void btnHorizStop_Click(object? sender, EventArgs e) => _stopHorizTune();

        /// <summary>Called by AirSimPanel's one-shot completion callback whenever a horizontal tune run ends.</summary>
        public void OnHorizTuneFinished(string why)
        {
            lblHorizStatus.Text = why;
            if (_getHorizResult() is { } r) ShowHorizResult(r);
        }

        void ShowHorizResult(HorizResult r)
        {
            lblHorizResult.Text = $"Kp={r.Kp:F3}  Ki={r.Ki:F3}  Kd={r.Kd:F3}\r\n" +
                                   $"measured: {r.VelocityGain:F3} m/s per unit stick, dead time {r.DeadTimeSec:F2}s" +
                                   (r.Note.Length > 0 ? $"\r\n{r.Note}" : "");
            btnHorizApply.Enabled = true;
        }

        void btnHorizApply_Click(object? sender, EventArgs e)
        {
            _applyHorizResult();
            lblHorizStatus.Text = "Applied - one combined pitch/roll gain set is now live.";
        }

        void btnYawGo_Click(object? sender, EventArgs e)
        {
            lblYawStatus.Text = "Running - watch the main panel status line for live phase updates.";
            lblYawResult.Text = "";
            btnYawApply.Enabled = false;
            _startYawTune();
        }

        void btnYawStop_Click(object? sender, EventArgs e) => _stopYawTune();

        /// <summary>Called by AirSimPanel's one-shot completion callback whenever a yaw tune run ends.</summary>
        public void OnYawTuneFinished(string why)
        {
            lblYawStatus.Text = why;
            if (_getYawResult() is { } r) ShowYawResult(r);
        }

        void ShowYawResult(YawResult r)
        {
            lblYawResult.Text = $"Kp={r.Kp:F3}  Ki={r.Ki:F3}  Kd={r.Kd:F3}\r\n" +
                                 $"measured: {r.VelocityGain:F3} deg/s per unit stick, dead time {r.DeadTimeSec:F2}s" +
                                 (r.Note.Length > 0 ? $"\r\n{r.Note}" : "");
            btnYawApply.Enabled = true;
        }

        void btnYawApply_Click(object? sender, EventArgs e)
        {
            _applyYawResult();
            lblYawStatus.Text = "Applied - heading hold gains are now live.";
        }
    }
}

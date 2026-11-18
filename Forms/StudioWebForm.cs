namespace HS175D
{
    /// <summary>
    /// Control panel for <see cref="StudioWeb"/> — start/stop the LAN server, show the URL(s) + access token
    /// to open on the Quest/phone, and surface the one-time admin step if LAN binding was denied.
    /// </summary>
    public sealed class StudioWebForm : Form
    {
        const int Port = 8732;
        readonly Form1 _form;

        readonly Button _btnToggle = new() { Size = new Size(120, 30) };
        readonly Label _status = new() { AutoSize = true };
        readonly TextBox _urls = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Font = new Font("Consolas", 10F) };
        readonly Button _btnCopy = new() { Text = "Copy first URL", Size = new Size(120, 26) };
        readonly Label _hint = new() { AutoSize = true, MaximumSize = new Size(560, 0) };
        readonly ToolTip _tip = new() { AutoPopDelay = 30000, InitialDelay = 350, ReshowDelay = 80, ShowAlways = true };

        public StudioWebForm(Form1 form)
        {
            _form = form;
            Text = "Studio Web — browser cockpit";
            ClientSize = new Size(600, 380);
            MinimumSize = new Size(520, 340);
            Font = new Font("Segoe UI", 9F);

            _btnToggle.Location = new Point(14, 12);
            _status.Location = new Point(150, 20);

            var lblUrls = new Label { Text = "Open this on your Quest / phone browser (same Wi-Fi as this PC):", AutoSize = true, Location = new Point(14, 52) };
            _urls.Location = new Point(14, 74); _urls.Size = new Size(572, 96);
            _urls.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _btnCopy.Location = new Point(14, 178); _btnCopy.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            _hint.Location = new Point(14, 216);
            _hint.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            Controls.AddRange(new Control[] { _btnToggle, _status, lblUrls, _urls, _btnCopy, _hint });
            _tip.SetToolTip(_btnToggle, "Start/stop the LAN browser-cockpit server (HUD + control from a phone/headset browser).");
            _tip.SetToolTip(_btnCopy, "Copy the first URL (with access token) to the clipboard.");

            _btnToggle.Click += (s, e) => Toggle();
            _btnCopy.Click += (s, e) =>
            {
                var first = StudioWeb.Urls().FirstOrDefault();
                if (!string.IsNullOrEmpty(first)) { try { Clipboard.SetText(first); } catch { } }
            };

            Theme.Apply(this);
            _urls.BackColor = Theme.Bg0; _urls.ForeColor = Theme.Good;
            Refresh_();
        }

        void Toggle()
        {
            if (StudioWeb.Running) StudioWeb.Stop();
            else StudioWeb.Start(_form, Port, _form.LogLine);
            Refresh_();
        }

        void Refresh_()
        {
            bool on = StudioWeb.Running;
            _btnToggle.Text = on ? "■ Stop server" : "▶ Start server";
            _status.ForeColor = on ? Theme.Good : Theme.Warn;
            _status.Text = on
                ? (StudioWeb.LanBound ? "Running — reachable on the LAN" : "Running — LOCALHOST ONLY (LAN bind denied)")
                : "Stopped";

            _urls.Text = on ? string.Join("\r\n", StudioWeb.Urls()) : "(start the server to get the URL + token)";

            var sb = new System.Text.StringBuilder();
            if (on && !StudioWeb.LanBound)
                sb.AppendLine($"⚠ LAN binding was denied — only this PC can reach it. To allow other devices, run ONCE in an\r\n" +
                              $"   admin PowerShell, then restart the server:\r\n" +
                              $"   netsh http add urlacl url=http://+:{Port}/ user=Everyone\r\n");
            sb.AppendLine("• The URL carries an access token — anyone with it on your network can fly. Keep it private.");
            sb.AppendLine("• Video needs mediamtx.exe next to the app (WebRTC on :8889). HUD + control work without it.");
            sb.AppendLine("• Topology: the headset/phone and this PC must share a network — both on the drone's Wi-Fi AP,");
            sb.AppendLine("  or this PC bridging (second adapter / hotspot) so it can reach the drone AND serve the headset.");
            sb.AppendLine("• Flight control stays gated by the main-window ARM checkbox; releasing/closing the page re-centres.");
            _hint.Text = sb.ToString();
        }
    }
}

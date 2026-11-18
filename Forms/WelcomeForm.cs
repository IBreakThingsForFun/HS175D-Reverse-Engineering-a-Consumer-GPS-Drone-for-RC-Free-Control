using HS175D;

namespace HS175D.Forms
{
    /// <summary>
    /// Startup launcher: a small always-first window that just picks which area of the app to open.
    /// Kept deliberately dumb - it owns no sockets, no timers and no drone/sim state; it only
    /// constructs the chosen Form, shows it modally, and closes itself when that Form closes (so
    /// the process exits normally with the launcher still being Application.Run's main form).
    /// </summary>
    public sealed partial class WelcomeForm : Form
    {
        public WelcomeForm()
        {
            InitializeComponent();
            Theme.Apply(this);
        }

        void OnStudioClick(object sender, EventArgs e) => Launch(new Form1());

        // ReWorkbench() (parameterless) is a design-time-only stub - it skips Theme.Apply and every
        // panel that needs a live drone IP (Esp32Panel included), so the Designer can open the form
        // without crashing. The real, fully-populated form is only built by ReWorkbench(string ip) -
        // same one Form1 uses via its _txtIp.Text - so that's the constructor to call here too.
        void OnWorkbenchClick(object sender, EventArgs e) => Launch(new ReWorkbench(AppSettings.DroneIp ?? ""));

        void Launch(Form target)
        {
            Hide();
            using (target) target.ShowDialog();
            Close();
        }


    }
}

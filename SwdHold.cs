using System.Diagnostics;

namespace HS175D
{
    /// <summary>
    /// One-click launcher for the SWD "hold optical mode" loop. Spawns a SINGLE bundled OpenOCD
    /// session that drives the Pico CMSIS-DAP and continuously re-writes the 10 minimal optical-mode
    /// SRAM flags on the running MM32F103 (tools/hold_optical.cfg). Writes go to the running target
    /// via the debug AP — NO core halt — so it does not freeze the flight firmware.
    ///
    /// IMPORTANT: only ONE OpenOCD may own the probe at a time. Do NOT open a second OpenOCD/gdb/telnet
    /// "watch" session against the same target while this is holding — a second debugger attach can halt
    /// the core (see memory three-confounds-2026-06-29). The inline mrb readback inside hold_optical.cfg
    /// is in-session and non-halting; use that if you need to see values.
    /// </summary>
    public static class SwdHold
    {
        static Process? _proc;
        public static bool Running => _proc is { HasExited: false };

        // Walk up from the app's output dir to the repo root (the folder that holds openocd-bin\ + tools\).
        static string? FindRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
                if (Directory.Exists(Path.Combine(dir.FullName, "openocd-bin")) &&
                    File.Exists(Path.Combine(dir.FullName, "tools", "hold_optical.cfg")))
                    return dir.FullName;
            return null;
        }

        /// <summary>Start the single non-halting optical-hold session. False if already running or files missing.</summary>
        public static bool Start(Action<string>? log)
        {
            if (Running) { log?.Invoke("already running — release it first (one probe owner)."); return false; }

            string? root = FindRoot();
            if (root == null) { log?.Invoke("repo root with openocd-bin\\ + tools\\hold_optical.cfg not found."); return false; }

            string exe = Path.Combine(root, "openocd-bin", "xpack-openocd-0.12.0-7", "bin", "openocd.exe");
            if (!File.Exists(exe)) { log?.Invoke("openocd.exe not found under openocd-bin\\xpack-openocd-0.12.0-7\\bin\\."); return false; }

            string scripts = Path.Combine(root, "openocd-bin", "xpack-openocd-0.12.0-7", "openocd", "scripts");
            string args = $"-s \"{scripts}\" " +
                          "-f interface/cmsis-dap.cfg -c \"transport select swd\" " +
                          "-f target/stm32f1x.cfg -f tools/hold_optical.cfg -c init -c hold_optical";
            try
            {
                var psi = new ProcessStartInfo(exe, args)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = root
                };
                _proc = Process.Start(psi);
                if (_proc == null) { log?.Invoke("OpenOCD failed to start."); return false; }
                _proc.OutputDataReceived += (s, e) => { if (e.Data != null) log?.Invoke("[openocd] " + e.Data); };
                _proc.ErrorDataReceived  += (s, e) => { if (e.Data != null) log?.Invoke("[openocd] " + e.Data); };
                _proc.BeginOutputReadLine();
                _proc.BeginErrorReadLine();
                log?.Invoke("HOLD STARTED — forcing 10 optical-mode flags @20ms (single non-halting session). " +
                            "Now send takeoff (Inject idx 6 = 02), THEN throttle.");
                return true;
            }
            catch (Exception ex) { log?.Invoke("start error: " + ex.Message); return false; }
        }

        public static void Stop(Action<string>? log = null)
        {
            try { if (_proc is { HasExited: false }) { _proc.Kill(entireProcessTree: true); _proc.WaitForExit(2000); log?.Invoke("HOLD RELEASED."); } }
            catch { }
            finally { try { _proc?.Dispose(); } catch { } _proc = null; }
        }
    }
}

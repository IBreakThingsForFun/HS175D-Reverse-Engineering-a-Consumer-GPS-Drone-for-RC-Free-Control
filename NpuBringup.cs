using System.Diagnostics;

namespace HS175D
{
    /// <summary>
    /// Brings up the on-device NPU stack from a cold boot: pushes the kernel module + OpenVX
    /// userspace libs + rknn_server + the persistent detector binary/model (all shipped in
    /// <c>npu_runtime/</c> next to the repo/app), loads the module, starts rknn_server,
    /// OOM-protects <c>img_trans_app</c> (the camera/command process — it's a prime OOM-kill
    /// target once the NPU stack's libraries are loaded into this device's tight ~230MB RAM),
    /// and starts the persistent <c>rknn_yolov5_loop</c> detector that <see cref="NpuDetector"/>
    /// talks to. Everything lands in <c>/tmp</c> (tmpfs) — nothing here touches flash/rootfs, so
    /// it's fully recoverable with a reboot if anything goes wrong. This exists because all of
    /// that state is lost every time the drone powers off (idle timeout or battery swap), and
    /// doing it by hand needs a WSL/adb setup — this button redoes the same steps from the app.
    /// </summary>
    public static class NpuBringup
    {
        const string RemoteBase = "/tmp/npu";

        public static async Task<bool> BringUpAsync(string ip, Action<string> onStatus)
        {
            string adb = DroneConsole.ResolveAdb();
            string? localDir = ResolveRuntimeDir();
            if (localDir == null) { onStatus("npu_runtime/ not found next to the app — can't bring the NPU stack up"); return false; }

            try
            {
                onStatus("connecting...");
                await Task.Run(() => Run(adb, $"connect {ip}:5555"));

                onStatus("checking if already up...");
                string psOut = await Task.Run(() => Run(adb, $"shell \"ps | grep rknn_yolov5_loop | grep -v grep\""));
                if (!string.IsNullOrWhiteSpace(psOut))
                {
                    onStatus("NPU stack already running.");
                    return true;
                }

                onStatus("making remote dirs...");
                await Task.Run(() => Run(adb, $"shell \"mkdir -p {RemoteBase}/mini {RemoteBase}/plugins {RemoteBase}/yolov5/model\""));

                onStatus("pushing kernel module...");
                await Task.Run(() => Run(adb, $"push \"{Path.Combine(localDir, "galcore_puma.ko")}\" {RemoteBase}/galcore_puma.ko"));

                onStatus("pushing OpenVX libs...");
                await Task.Run(() => Run(adb, $"push \"{Path.Combine(localDir, "mini")}\\.\" {RemoteBase}/mini"));

                onStatus("pushing plugins/server...");
                await Task.Run(() => Run(adb, $"push \"{Path.Combine(localDir, "plugins")}\\.\" {RemoteBase}/plugins"));

                onStatus("pushing detector binary + model...");
                await Task.Run(() => Run(adb, $"push \"{Path.Combine(localDir, "yolov5")}\\.\" {RemoteBase}/yolov5/model"));
                await Task.Run(() => Run(adb, $"shell \"mv {RemoteBase}/yolov5/model/rknn_yolov5_loop {RemoteBase}/yolov5/rknn_yolov5_loop && chmod +x {RemoteBase}/yolov5/rknn_yolov5_loop {RemoteBase}/plugins/rknn_server\""));

                onStatus("protecting img_trans_app from OOM killer...");
                string pidOut = await Task.Run(() => Run(adb, $"shell \"ps | grep img_trans_app | grep -v grep | awk '{{print $1}}'\""));
                string pid = pidOut.Trim().Split('\n', '\r')[0].Trim();
                if (!string.IsNullOrEmpty(pid))
                    await Task.Run(() => Run(adb, $"shell \"echo -1000 > /proc/{pid}/oom_score_adj\""));

                onStatus("dropping caches for headroom...");
                await Task.Run(() => Run(adb, $"shell \"echo 1 > /proc/sys/vm/drop_caches\""));

                onStatus("loading kernel module...");
                await Task.Run(() => Run(adb, $"shell \"insmod {RemoteBase}/galcore_puma.ko 2>&1\""));

                // Both of these background a process with "&" and nothing else follows in the shell
                // session — if the adb shell command returns before the child fully detaches, session
                // teardown can take it down with it. A "sleep 1" after the "&", in the SAME shell
                // invocation, usually avoids this, but it's been observed to still occasionally leave
                // the log file completely empty (process never actually survived to run) — hence the
                // ps-check-and-retry in EnsureRunningAsync rather than trusting the launch blindly.
                string startServerCmd = $"cd {RemoteBase}/plugins && LD_LIBRARY_PATH={RemoteBase}/mini:{RemoteBase}/plugins RKNN_SERVER_PLUGINS={RemoteBase}/plugins nohup ./rknn_server > {RemoteBase}/rknn_server.log 2>&1 & sleep 1";
                onStatus("starting rknn_server...");
                await Task.Run(() => Run(adb, $"shell \"{startServerCmd}\""));
                if (!await EnsureRunningAsync(adb, "rknn_server", startServerCmd))
                {
                    onStatus("rknn_server failed to stay up after retrying.");
                    return false;
                }

                // yolov5s (not yolov5n) — confirmed this session to survive reliably now that
                // img_trans_app is OOM-protected and drop_caches is run first; noticeably better
                // detection confidence for ~250-300ms extra per cycle (still well under a second).
                string startDetectorCmd =
                    $"cd {RemoteBase}/yolov5 && LD_LIBRARY_PATH={RemoteBase}/mini VIV_VX_ENABLE_SHADER=0 VIV_VX_ENABLE_TP_TENSOR_SOFTMAX=1 VIV_VX_ENABLE_CACHE_GRAPH_BINARY=0 " +
                    $"nohup ./rknn_yolov5_loop model/yolov5s.rknn {RemoteBase}/yolov5/live_in.jpg {RemoteBase}/yolov5/live_out.jpg > {RemoteBase}/loop_proc.log 2>&1 & sleep 1";
                onStatus("starting detector (model load, a few seconds)...");
                await Task.Run(() => Run(adb, $"shell \"{startDetectorCmd}\""));
                if (!await EnsureRunningAsync(adb, "rknn_yolov5_loop", startDetectorCmd))
                {
                    onStatus("detector process failed to stay up after retrying.");
                    return false;
                }

                // Model init measured 5-10s for yolov5n, but yolov5s has run 20-28s on a real cold
                // boot — give it generous room rather than false-alarming while it's still fine.
                var deadline = DateTime.UtcNow.AddSeconds(60);
                while (DateTime.UtcNow < deadline)
                {
                    await Task.Delay(1000);
                    string log = await Task.Run(() => Run(adb, $"shell \"cat {RemoteBase}/loop_proc.log\""));
                    if (log.Contains("READY")) { onStatus("NPU stack up and ready — AI Detect is good to go."); return true; }
                }
                onStatus("timed out waiting for the detector to report READY — check manually.");
                return false;
            }
            catch (Exception ex)
            {
                onStatus("bring-up failed: " + ex.Message);
                return false;
            }
        }

        /// <summary>Confirms <paramref name="processName"/> shows up in `ps`; if not, re-issues
        /// <paramref name="launchShellCmd"/> once and checks again. Guards against the backgrounded
        /// process occasionally not surviving its own shell session's teardown (see comment above).</summary>
        static async Task<bool> EnsureRunningAsync(string adb, string processName, string launchShellCmd, int maxAttempts = 2)
        {
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                await Task.Delay(500);
                string ps = await Task.Run(() => Run(adb, $"shell \"ps | grep {processName} | grep -v grep\""));
                if (!string.IsNullOrWhiteSpace(ps)) return true;
                if (attempt < maxAttempts - 1)
                    await Task.Run(() => Run(adb, $"shell \"{launchShellCmd}\""));
            }
            return false;
        }

        static string? ResolveRuntimeDir()
        {
            string p = Path.Combine(AppContext.BaseDirectory, "npu_runtime");
            return Directory.Exists(p) ? p : null;
        }

        static string Run(string adb, string args)
        {
            var psi = new ProcessStartInfo(adb, args)
            {
                RedirectStandardOutput = true, RedirectStandardError = true,
                UseShellExecute = false, CreateNoWindow = true
            };
            using var p = Process.Start(psi) ?? throw new InvalidOperationException("could not start adb");
            string o = p.StandardOutput.ReadToEnd();
            string e = p.StandardError.ReadToEnd();
            p.WaitForExit(20000);
            return o + e;
        }
    }
}

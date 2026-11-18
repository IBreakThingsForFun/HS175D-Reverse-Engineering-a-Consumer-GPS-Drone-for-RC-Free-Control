using System.Diagnostics;
using System.Globalization;

namespace HS175D
{
    /// <summary>
    /// Grabs a frame from the currently-playing mpv video, pushes it to the drone's already-running
    /// on-device NPU detector (<c>rknn_yolov5_loop</c>, watching <c>/tmp/npu/yolov5/live_in.jpg</c> —
    /// see docs-hub/pages/npu.html), and pulls back the plaintext results sidecar
    /// (<c>live_out.txt</c>, one "label x1 y1 x2 y2 confidence" line per detection) instead of the
    /// whole annotated image, so the caller can draw boxes itself (see <see cref="DetectionOverlay"/>).
    ///
    /// Assumes the on-device stack (kernel module, rknn_server, rknn_yolov5_loop) is already running —
    /// this class only does capture/push/pull/parse, it never brings the NPU stack up itself.
    /// </summary>
    public sealed class NpuDetector
    {
        const string RemoteInPath = "/tmp/npu/yolov5/live_in.jpg";
        const string RemoteOutTxt = "/tmp/npu/yolov5/live_out.txt";

        readonly string _adb;
        bool _busy;

        public NpuDetector() => _adb = DroneConsole.ResolveAdb();

        public bool Busy => _busy;

        /// <summary>One capture → push → detect → pull cycle. Never throws — returns an empty list on any failure.</summary>
        public async Task<List<(string Label, Rectangle Box, float Confidence)>> CaptureAndDetectAsync(MpvClient mpv, string ip)
        {
            var results = new List<(string, Rectangle, float)>();
            if (_busy) return results;
            _busy = true;
            try
            {
                string tmpDir = Path.Combine(Path.GetTempPath(), "HS175D_npu");
                Directory.CreateDirectory(tmpDir);
                string localJpg = Path.Combine(tmpDir, "frame.jpg");
                string localTxt = Path.Combine(tmpDir, "result.txt");

                File.Delete(localJpg);
                mpv.Command("screenshot-to-file", localJpg, "video");

                // mpv's screenshot command returns before the file is necessarily flushed —
                // poll for it to exist with a stable size rather than assume it's ready.
                if (!await WaitForStableFileAsync(localJpg, TimeSpan.FromMilliseconds(300)))
                    return results;

                await Task.Run(() => RunAdb($"connect {ip}:5555"));   // idempotent
                int pushRc = await Task.Run(() => RunAdb($"push \"{localJpg}\" {RemoteInPath}.tmp"));
                if (pushRc != 0) return results;
                await Task.Run(() => RunAdb($"shell mv {RemoteInPath}.tmp {RemoteInPath}"));   // atomic on-device rename

                await Task.Delay(450);   // measured on-device processing time for this model

                File.Delete(localTxt);
                int pullRc = await Task.Run(() => RunAdb($"pull {RemoteOutTxt} \"{localTxt}\""));
                if (pullRc != 0 || !File.Exists(localTxt)) return results;

                foreach (var line in File.ReadAllLines(localTxt))
                {
                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length != 6) continue;
                    if (!int.TryParse(parts[1], out int x1)) continue;
                    if (!int.TryParse(parts[2], out int y1)) continue;
                    if (!int.TryParse(parts[3], out int x2)) continue;
                    if (!int.TryParse(parts[4], out int y2)) continue;
                    if (!float.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out float conf)) continue;
                    results.Add((parts[0], Rectangle.FromLTRB(x1, y1, x2, y2), conf));
                }
            }
            catch { /* best-effort; caller just sees an empty/partial result */ }
            finally { _busy = false; }
            return results;
        }

        static async Task<bool> WaitForStableFileAsync(string path, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;
            long lastSize = -1;
            while (DateTime.UtcNow < deadline)
            {
                if (File.Exists(path))
                {
                    long size = new FileInfo(path).Length;
                    if (size > 0 && size == lastSize) return true;
                    lastSize = size;
                }
                await Task.Delay(30);
            }
            return false;
        }

        int RunAdb(string args)
        {
            var psi = new ProcessStartInfo(_adb, args)
            {
                RedirectStandardOutput = true, RedirectStandardError = true,
                UseShellExecute = false, CreateNoWindow = true
            };
            using var p = Process.Start(psi) ?? throw new InvalidOperationException("could not start adb");
            p.StandardOutput.ReadToEnd();
            p.StandardError.ReadToEnd();
            p.WaitForExit(10000);
            return p.ExitCode;
        }
    }
}

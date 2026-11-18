using System.Runtime.InteropServices;

namespace HS175D
{
    /// <summary>
    /// Minimal hand-rolled P/Invoke wrapper over libmpv (the FFmpeg-based mpv engine), used as a
    /// low-latency alternative to LibVLC for the drone's RTSP feed. mpv recovers from network hiccups
    /// far faster than VLC on live RTSP, which is the whole reason this exists (see VideoPanel).
    ///
    /// The native <c>libmpv-2.dll</c> ships via the <c>Endpne.LibMPV.Windows</c> NuGet package and is
    /// copied to <c>libmpv\win-x64\</c> / <c>libmpv\win-arm64\</c> next to the exe; the resolver below
    /// loads the arch-correct one. Video is embedded straight into a WinForms control by handing mpv
    /// that control's HWND as the <c>wid</c> option (mpv renders into it, no separate window).
    /// </summary>
    public sealed class MpvClient : IDisposable
    {
        const string Lib = "libmpv-2";
        const int MPV_FORMAT_INT64 = 4;

        [DllImport(Lib)] static extern IntPtr mpv_create();
        [DllImport(Lib)] static extern int mpv_initialize(IntPtr ctx);
        [DllImport(Lib)] static extern void mpv_terminate_destroy(IntPtr ctx);
        [DllImport(Lib)] static extern int mpv_set_option(IntPtr ctx, byte[] name, int format, ref long data);
        [DllImport(Lib)] static extern int mpv_set_option_string(IntPtr ctx, byte[] name, byte[] data);
        [DllImport(Lib)] static extern int mpv_set_property_string(IntPtr ctx, byte[] name, byte[] data);
        [DllImport(Lib)] static extern IntPtr mpv_get_property_string(IntPtr ctx, byte[] name);
        [DllImport(Lib)] static extern void mpv_free(IntPtr data);
        [DllImport(Lib)] static extern int mpv_command(IntPtr ctx, IntPtr[] args);

        static MpvClient()
        {
            // libmpv-2.dll lives in a per-arch subfolder, not next to the exe, so the default P/Invoke
            // search won't find it — resolve it explicitly the first time "libmpv-2" is requested.
            NativeLibrary.SetDllImportResolver(typeof(MpvClient).Assembly, (name, asm, path) =>
            {
                if (!name.StartsWith("libmpv", StringComparison.OrdinalIgnoreCase)) return IntPtr.Zero;
                string rid = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "win-arm64" : "win-x64";
                string dll = Path.Combine(AppContext.BaseDirectory, "libmpv", rid, "libmpv-2.dll");
                return File.Exists(dll) && NativeLibrary.TryLoad(dll, out var h) ? h : IntPtr.Zero;
            });
        }

        IntPtr _ctx;

        /// <summary>
        /// Create the player, embed it into <paramref name="hwnd"/>, apply the low-latency RTSP profile,
        /// then initialize. Options that affect embedding/decoding must be set BEFORE mpv_initialize().
        /// </summary>
        public MpvClient(IntPtr hwnd)
        {
            _ctx = mpv_create();
            if (_ctx == IntPtr.Zero) throw new InvalidOperationException("mpv_create failed (libmpv-2.dll missing or wrong arch?)");

            long wid = hwnd.ToInt64();
            mpv_set_option(_ctx, Utf8("wid"), MPV_FORMAT_INT64, ref wid);   // render into our panel

            // ── Low-latency live-RTSP setup ────────────────────────────────────────────────
            // profile=low-latency is mpv's built-in preset (tiny buffers, +nobuffer demux, latency
            // hacks) — the base win over VLC. The rest shave glass-to-glass delay further so the feed
            // tracks the phone apps: kill audio sync, don't pace frames, don't buffer for reorder, and
            // keep the shortest GPU present queue. These are the levers when "mpv works but still lags".
            SetOption("profile", "low-latency");
            SetOption("rtsp-transport", "tcp");        // matches the drone's TCP-interleaved RTP
            SetOption("hwdec", "auto-safe");           // hardware decode when the GPU/driver allows
            SetOption("cache", "no");                  // no read-ahead cache — show frames as they arrive
            SetOption("audio", "no");                  // drone feed has no audio; drop the path + its A/V sync
            SetOption("untimed", "yes");               // present each frame the instant it decodes (no pacing)
            SetOption("video-sync", "desync");         // don't lock video to a clock — lowest latency for live
            SetOption("framedrop", "vo");              // after a hiccup, drop late frames to catch up, not lag
            SetOption("vd-lavc-threads", "1");         // frame-threaded decode buffers frames; 1 = no reorder delay
            SetOption("vd-lavc-o", "flags=low_delay"); // tell the H.264/265 decoder to emit frames ASAP
            SetOption("swapchain-depth", "1");         // shortest GPU present queue (1) vs the default 3
            // Hold the last decoded frame on screen instead of clearing to black when the current
            // stream ends/errors (e.g. the RTSP session getting torn down) — makes our proactive
            // reconnect (VideoPanel's FlushWatchdog_Tick) look like a brief freeze instead of a
            // flash, since the old frame stays up until the new stream's first frame replaces it.
            SetOption("keep-open", "yes");
            // Purely a render target: silence terminal, disable input/OSD.
            SetOption("terminal", "no");
            SetOption("osc", "no");
            SetOption("input-default-bindings", "no");
            SetOption("input-vo-keyboard", "no");

            int rc = mpv_initialize(_ctx);
            if (rc < 0) { mpv_terminate_destroy(_ctx); _ctx = IntPtr.Zero; throw new InvalidOperationException($"mpv_initialize failed (rc={rc})"); }
        }

        public void SetOption(string name, string value)
        {
            if (_ctx != IntPtr.Zero) mpv_set_option_string(_ctx, Utf8(name), Utf8(value));
        }

        public void SetProperty(string name, string value)
        {
            if (_ctx != IntPtr.Zero) mpv_set_property_string(_ctx, Utf8(name), Utf8(value));
        }

        /// <summary>Read an mpv property as a string (null if unavailable). Caller-agnostic; libmpv owns the buffer.</summary>
        public string? GetProperty(string name)
        {
            if (_ctx == IntPtr.Zero) return null;
            IntPtr p = mpv_get_property_string(_ctx, Utf8(name));
            if (p == IntPtr.Zero) return null;
            try { return Marshal.PtrToStringUTF8(p); }
            finally { mpv_free(p); }
        }

        /// <summary>Read a numeric mpv property (NaN if unavailable/unparsable). Used by the latency watchdog.</summary>
        public double GetDouble(string name) =>
            double.TryParse(GetProperty(name), System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out double d) ? d : double.NaN;

        /// <summary>Flush demuxer + decoder buffers and resume from fresh data — jumps back to the live edge
        /// without a reconnect (the low-latency "don't try to catch up, skip to now" reset).</summary>
        public void DropBuffers() => Command("drop-buffers");

        /// <summary>Start (or switch) playback of an RTSP/file URL.</summary>
        public void Load(string url) => Command("loadfile", url);

        /// <summary>Stop playback and drop the current file (leaves the player alive for a re-Load).</summary>
        public void Stop() => Command("stop");

        /// <summary>Run an mpv command as a null-terminated UTF-8 argv array (e.g. Command("loadfile", url)).</summary>
        public void Command(params string[] args)
        {
            if (_ctx == IntPtr.Zero) return;
            var ptrs = new IntPtr[args.Length + 1];
            try
            {
                for (int i = 0; i < args.Length; i++) ptrs[i] = Utf8Ptr(args[i]);
                ptrs[args.Length] = IntPtr.Zero;   // argv terminator
                mpv_command(_ctx, ptrs);
            }
            finally
            {
                for (int i = 0; i < args.Length; i++) if (ptrs[i] != IntPtr.Zero) Marshal.FreeHGlobal(ptrs[i]);
            }
        }

        // libmpv is all UTF-8; C# marshals as UTF-16 by default, so convert explicitly.
        static byte[] Utf8(string s) => System.Text.Encoding.UTF8.GetBytes(s + "\0");
        static IntPtr Utf8Ptr(string s)
        {
            byte[] b = Utf8(s);
            IntPtr p = Marshal.AllocHGlobal(b.Length);
            Marshal.Copy(b, 0, p, b.Length);
            return p;
        }

        public void Dispose()
        {
            if (_ctx != IntPtr.Zero) { try { mpv_terminate_destroy(_ctx); } catch { } _ctx = IntPtr.Zero; }
        }
    }
}

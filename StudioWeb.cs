using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace HS175D
{
    /// <summary>
    /// A SEPARATE, LAN-bound, token-authed HTTP server (port 8732) that serves the browser cockpit
    /// (HUD + video + flight control + map) to any device — Quest 2, phone, tablet. Kept distinct from the
    /// localhost-only <see cref="DebugBus"/> so exposing flight control to the LAN is a deliberate, gated,
    /// authenticated act and never weakens the debug surface.
    ///
    /// Safety: a token guards every request; a deadman watchdog re-centres the sticks if the browser stops
    /// heart-beating; the local arm checkbox stays authoritative; and the <see cref="ControlArbiter"/> lets a
    /// human (local sliders / Xbox / browser) preempt the AI.
    /// </summary>
    public static class StudioWeb
    {
        static HttpListener? _listener;
        static Form1? _form;
        static Action<string>? _log;
        static volatile bool _run;
        static int _port;
        static string _token = "";
        static bool _lanBound;

        // deadman / browser-control lease
        static volatile bool _browserControlling;
        static long _lastHeartbeatMs;
        static System.Threading.Timer? _deadman;
        const long DeadmanMs = 600;

        public static bool Running => _run;
        public static int Port => _port;
        public static string Token => _token;
        public static bool LanBound => _lanBound;

        // ---- lifecycle ------------------------------------------------------

        public static bool Start(Form1 form, int port, Action<string>? log)
        {
            if (_run) return true;
            _form = form; _port = port; _log = log;
            _token = Guid.NewGuid().ToString("N")[..12];

            // Prefer binding all interfaces (so other devices reach us). That needs a URL-ACL on Windows;
            // if denied, fall back to localhost so at least same-machine testing works, and tell the user how.
            if (TryListen($"http://+:{port}/")) _lanBound = true;
            else
            {
                _log?.Invoke($"StudioWeb: LAN bind denied. Run once as admin:  netsh http add urlacl url=http://+:{port}/ user=Everyone");
                if (!TryListen($"http://localhost:{port}/")) { _log?.Invoke("StudioWeb: could not bind localhost either — aborting."); return false; }
                _lanBound = false;
            }

            _run = true;
            new Thread(Loop) { IsBackground = true, Name = "StudioWeb" }.Start();
            _deadman = new System.Threading.Timer(_ => DeadmanCheck(), null, 200, 200);

            // Best-effort start the video bridge (no-op if mediamtx.exe absent).
            try { MediaMtxHelper.Start(form.DroneIp, log); } catch { }

            _log?.Invoke($"StudioWeb on :{port}  token={_token}  (LAN={_lanBound})");
            return true;
        }

        static bool TryListen(string prefix)
        {
            try
            {
                var l = new HttpListener();
                l.Prefixes.Add(prefix);
                l.Start();
                _listener = l;
                return true;
            }
            catch { return false; }
        }

        public static void Stop()
        {
            _run = false;
            _browserControlling = false;
            try { _deadman?.Dispose(); } catch { }
            try { _listener?.Stop(); _listener?.Close(); } catch { }
            _listener = null;
            try { MediaMtxHelper.Stop(); } catch { }
        }

        public static List<string> Urls()
        {
            var list = new List<string>();
            if (_lanBound)
                foreach (var ip in LocalIPv4())
                    list.Add($"http://{ip}:{_port}/?t={_token}");
            list.Add($"http://localhost:{_port}/?t={_token}");
            return list;
        }

        static IEnumerable<string> LocalIPv4()
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                    if (ua.Address.AddressFamily == AddressFamily.InterNetwork)
                        yield return ua.Address.ToString();
            }
        }

        // ---- HTTP loop ------------------------------------------------------

        static void Loop()
        {
            while (_run)
            {
                HttpListenerContext ctx;
                try { ctx = _listener!.GetContext(); }
                catch { if (_run) continue; else break; }
                ThreadPool.QueueUserWorkItem(_ => { try { Handle(ctx); } catch { try { ctx.Response.Abort(); } catch { } } });
            }
        }

        static void Handle(HttpListenerContext ctx)
        {
            string path = (ctx.Request.Url?.AbsolutePath ?? "/").TrimEnd('/');
            if (path == "") path = "/index.html";

            // Static assets are non-sensitive and load without a token (the page reads its own ?t= for the API).
            if (!path.StartsWith("/api/")) { ServeStatic(ctx, path); return; }

            // Everything under /api/ — including telemetry and all control — requires the access token.
            if (!TokenOk(ctx)) { Json(ctx, 401, new { error = "unauthorized — use the tokenized URL from the Studio Web dialog" }); return; }

            switch (path)
            {
                case "/api/snapshot": Json(ctx, 200, _form!.WebSnapshot()); return;
                case "/api/config":   Json(ctx, 200, Config()); return;
                case "/api/stream":   ServeSse(ctx); return;
                case "/api/heartbeat": _lastHeartbeatMs = Environment.TickCount64; Json(ctx, 200, new { ok = true }); return;
                case "/api/sticks":   ApiSticks(ctx); return;
                case "/api/command":  ApiCommand(ctx); return;
                case "/api/arm":      ApiArm(ctx); return;
                case "/api/control/claim":   _browserControlling = true; _lastHeartbeatMs = Environment.TickCount64; _log?.Invoke("StudioWeb: browser claimed control"); Json(ctx, 200, new { controlling = true }); return;
                case "/api/control/release": _browserControlling = false; _form!.CenterSticksExternal(); _log?.Invoke("StudioWeb: browser released control"); Json(ctx, 200, new { controlling = false }); return;
                case "/api/gps":      ApiGps(ctx); return;
                default: Json(ctx, 404, new { error = "unknown endpoint " + path }); return;
            }
        }

        // ---- control endpoints ---------------------------------------------

        static void ApiSticks(HttpListenerContext ctx)
        {
            var b = Body(ctx);
            if (!_browserControlling) { Json(ctx, 409, new { error = "claim control first" }); return; }
            _lastHeartbeatMs = Environment.TickCount64;     // sticks count as a heartbeat
            byte thr = ByteOf(b, "throttle", 0), yaw = ByteOf(b, "yaw", 128), pit = ByteOf(b, "pitch", 128), rol = ByteOf(b, "roll", 128);
            _form!.ApplyExternalSticks(StickSource.Browser, thr, yaw, pit, rol);
            Json(ctx, 200, new { ok = true, owner = ControlArbiter.Owner.ToString() });
        }

        static void ApiCommand(HttpListenerContext ctx)
        {
            var b = Body(ctx);
            string name = StrOf(b, "name");
            string res = _form!.WebDiscreteCommand(name);
            Json(ctx, 200, new { command = res });
        }

        static void ApiArm(HttpListenerContext ctx)
        {
            var b = Body(ctx);
            bool on = BoolOf(b, "on", true);
            bool armed = _form!.WebArm(on);
            Json(ctx, 200, new { armed });
        }

        static void ApiGps(HttpListenerContext ctx)
        {
            var b = Body(ctx);
            bool orbit = StrOf(b, "mode").Equals("orbit", StringComparison.OrdinalIgnoreCase);
            double lat = DblOf(b, "lat"), lon = DblOf(b, "lon");
            int alt = (int)DblOf(b, "alt"), speed = (int)DblOf(b, "speed"), radius = (int)DblOf(b, "radius");
            byte cmdId = (byte)(IntOf(b, "cmdId") ?? 0x20);
            string res = _form!.WebSendGps(orbit, lat, lon, alt, speed, radius, cmdId);
            Json(ctx, 200, new { result = res });
        }

        // ---- deadman --------------------------------------------------------

        static void DeadmanCheck()
        {
            if (!_browserControlling) return;
            if (Environment.TickCount64 - _lastHeartbeatMs > DeadmanMs)
            {
                _browserControlling = false;
                try { _form?.CenterSticksExternal(); } catch { }
                _log?.Invoke("StudioWeb: DEADMAN — browser heartbeat lost, sticks centred");
            }
        }

        // ---- SSE ------------------------------------------------------------

        static void ServeSse(HttpListenerContext ctx)
        {
            var r = ctx.Response;
            r.StatusCode = 200;
            r.ContentType = "text/event-stream";
            r.Headers["Cache-Control"] = "no-cache";
            r.Headers["X-Accel-Buffering"] = "no";
            r.SendChunked = true;
            var enc = new UTF8Encoding(false);
            try
            {
                while (_run)
                {
                    string json = JsonSerializer.Serialize(_form!.WebSnapshot());
                    var bytes = enc.GetBytes("data: " + json + "\n\n");
                    r.OutputStream.Write(bytes, 0, bytes.Length);
                    r.OutputStream.Flush();
                    Thread.Sleep(100);   // ~10 Hz
                }
            }
            catch { /* client disconnected */ }
            finally { try { r.Close(); } catch { } }
        }

        // ---- static files ---------------------------------------------------

        static void ServeStatic(HttpListenerContext ctx, string path)
        {
            string root = Path.Combine(AppContext.BaseDirectory, "webroot");
            string rel = path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            string full = Path.GetFullPath(Path.Combine(root, rel));
            if (!full.StartsWith(Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase) || !File.Exists(full))
            { Text(ctx, 404, "not found: " + path); return; }
            try
            {
                var bytes = File.ReadAllBytes(full);
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = ContentType(full);
                ctx.Response.ContentLength64 = bytes.Length;
                ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
            }
            catch (Exception ex) { Text(ctx, 500, ex.Message); }
            finally { try { ctx.Response.Close(); } catch { } }
        }

        static string ContentType(string f) => Path.GetExtension(f).ToLowerInvariant() switch
        {
            ".html" => "text/html; charset=utf-8",
            ".js" => "text/javascript; charset=utf-8",
            ".css" => "text/css; charset=utf-8",
            ".json" => "application/json",
            ".png" => "image/png",
            ".svg" => "image/svg+xml",
            _ => "application/octet-stream"
        };

        static object Config() => new
        {
            token = _token,
            videoReady = MediaMtxHelper.Running,
            videoPort = MediaMtxHelper.WebRtcPort,
            videoPath = MediaMtxHelper.PathName,    // WHEP at http://<host>:videoPort/videoPath/whep
            lanBound = _lanBound
        };

        // ---- helpers --------------------------------------------------------

        static bool TokenOk(HttpListenerContext ctx)
        {
            string? t = ctx.Request.QueryString["t"] ?? ctx.Request.Headers["X-Studio-Token"];
            return !string.IsNullOrEmpty(_token) && t == _token;
        }

        static JsonElement Body(HttpListenerContext ctx)
        {
            try
            {
                using var sr = new StreamReader(ctx.Request.InputStream, ctx.Request.ContentEncoding);
                var s = sr.ReadToEnd();
                return JsonDocument.Parse(string.IsNullOrWhiteSpace(s) ? "{}" : s).RootElement;
            }
            catch { return JsonDocument.Parse("{}").RootElement; }
        }

        static byte ByteOf(JsonElement a, string k, byte def)
            => a.ValueKind == JsonValueKind.Object && a.TryGetProperty(k, out var v) && v.TryGetInt32(out var n)
               ? (byte)Math.Clamp(n, 0, 255) : def;
        static string StrOf(JsonElement a, string k)
            => a.ValueKind == JsonValueKind.Object && a.TryGetProperty(k, out var v) ? (v.GetString() ?? "") : "";
        static bool BoolOf(JsonElement a, string k, bool def)
            => a.ValueKind == JsonValueKind.Object && a.TryGetProperty(k, out var v) && (v.ValueKind == JsonValueKind.True || v.ValueKind == JsonValueKind.False) ? v.GetBoolean() : def;
        static double DblOf(JsonElement a, string k)
            => a.ValueKind == JsonValueKind.Object && a.TryGetProperty(k, out var v) && v.TryGetDouble(out var d) ? d : 0;
        static int? IntOf(JsonElement a, string k)
            => a.ValueKind == JsonValueKind.Object && a.TryGetProperty(k, out var v) && v.TryGetInt32(out var n) ? n : null;

        static void Json(HttpListenerContext ctx, int code, object obj) => Write(ctx, code, "application/json", JsonSerializer.Serialize(obj));
        static void Text(HttpListenerContext ctx, int code, string body) => Write(ctx, code, "text/plain; charset=utf-8", body);
        static void Write(HttpListenerContext ctx, int code, string ctype, string body)
        {
            var b = Encoding.UTF8.GetBytes(body);
            try
            {
                ctx.Response.StatusCode = code;
                ctx.Response.ContentType = ctype;
                ctx.Response.ContentLength64 = b.Length;
                ctx.Response.OutputStream.Write(b, 0, b.Length);
            }
            catch { }
            finally { try { ctx.Response.Close(); } catch { } }
        }
    }
}

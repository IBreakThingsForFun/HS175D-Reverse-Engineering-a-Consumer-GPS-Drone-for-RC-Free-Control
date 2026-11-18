using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;

namespace HS175D
{
    /// <summary>
    /// Localhost JSON control surface so an external tool (curl) can read state and drive EVERY window
    /// of the app without screen-clicking. Two layers:
    ///   1. Named commands — windows register semantic handlers via <see cref="Register"/>.
    ///   2. Generic UI automation — built-in ui.* commands reflect over Application.OpenForms to list,
    ///      read, set and click any control by Name, marshalled onto the UI thread.
    /// HTTP:  GET /            → help (command list)
    ///        GET /log?n=200   → recent global log lines
    ///        GET /c/&lt;cmd&gt;?a=b  → run &lt;cmd&gt; with the query as its args object (convenience)
    ///        POST /           → body {"cmd":"name","args":{...}} → {"ok":true,"result":...}
    /// Bound to localhost only (no admin / URL-ACL needed).
    /// </summary>
    public static class DebugBus
    {
        sealed record Cmd(string Name, string Help, Func<JsonElement, object?> Handler);

        static readonly ConcurrentDictionary<string, Cmd> _cmds = new(StringComparer.OrdinalIgnoreCase);
        static readonly object _logLock = new();
        static readonly Queue<string> _log = new();
        static readonly JsonSerializerOptions _json = new() { WriteIndented = true };

        static HttpListener? _listener;
        static System.Windows.Forms.Form? _ui;   // marshalling context (the main form)
        static Action<string>? _appLog;
        static volatile bool _run;
        public static int Port { get; private set; }

        // ---- public API ------------------------------------------------------

        public static void Register(string name, string help,
                                    System.Windows.Forms.Control? owner,
                                    Func<JsonElement, object?> handler)
        {
            // Wrap so the handler always runs on its owner's UI thread.
            Func<JsonElement, object?> h = owner == null ? handler : a => OnUi(owner, () => handler(a));
            _cmds[name] = new Cmd(name, help, h);
        }

        public static void Unregister(string name) => _cmds.TryRemove(name, out _);

        /// <summary>
        /// Invoke a registered command in-process — the same handler path the HTTP layer uses, including
        /// the owner-UI-thread marshalling. Lets one window drive another's command (e.g. the Ramp tab
        /// hand throttle to Form1's single control loop) without a network round-trip. Throws if unknown.
        /// </summary>
        public static object? Dispatch(string cmd, IDictionary<string, string>? args = null)
        {
            if (!_cmds.TryGetValue(cmd, out var handler))
                throw new ArgumentException($"unknown cmd '{cmd}'");
            var a = args == null || args.Count == 0
                    ? EmptyObj()
                    : JsonDocument.Parse(JsonSerializer.Serialize(args)).RootElement;
            return handler.Handler(a);
        }

        /// <summary>Global log sink — every window's Log() should also call this so /log is unified.</summary>
        public static void Log(string line)
        {
            lock (_logLock) { _log.Enqueue(line); while (_log.Count > 1000) _log.Dequeue(); }
        }

        public static void Start(System.Windows.Forms.Form main, int port, Action<string>? appLog = null)
        {
            if (_run) return;
            _ui = main; Port = port; _appLog = appLog;
            RegisterBuiltins();
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{port}/");
            try { _listener.Start(); }
            catch (Exception ex) { _appLog?.Invoke($"DebugBus failed on {port}: {ex.Message}"); return; }
            _run = true;
            new Thread(Loop) { IsBackground = true, Name = "DebugBus" }.Start();
            _appLog?.Invoke($"DebugBus on http://localhost:{port}/  — GET / for commands");
        }

        public static void Stop()
        {
            _run = false;
            try { _listener?.Stop(); _listener?.Close(); } catch { }
        }

        // ---- arg helpers (for handlers) -------------------------------------

        public static string? Str(JsonElement a, string k)
            => a.ValueKind == JsonValueKind.Object && a.TryGetProperty(k, out var v)
               ? (v.ValueKind == JsonValueKind.String ? v.GetString() : v.ToString())
               : null;
        public static int? Int(JsonElement a, string k) => int.TryParse(Str(a, k), out var n) ? n : null;
        public static int? IntHex(JsonElement a, string k)
        {
            var s = Str(a, k)?.Replace("0x", "", StringComparison.OrdinalIgnoreCase);
            return int.TryParse(s, System.Globalization.NumberStyles.HexNumber, null, out var n) ? n : null;
        }
        public static bool? Bool(JsonElement a, string k)
        {
            var s = Str(a, k)?.Trim().ToLowerInvariant();
            return s is null ? null : s is "1" or "true" or "on" or "yes";
        }

        // ---- HTTP loop -------------------------------------------------------

        static void Loop()
        {
            while (_run)
            {
                HttpListenerContext ctx;
                try { ctx = _listener!.GetContext(); } catch { break; }
                try
                {
                    string path = (ctx.Request.Url?.AbsolutePath ?? "/").TrimEnd('/');
                    if (path == "") { Respond(ctx, 200, HelpText()); continue; }
                    if (path.Equals("/log", StringComparison.OrdinalIgnoreCase)) { Respond(ctx, 200, LogText(ctx)); continue; }

                    string cmd; JsonElement args;
                    if (path.StartsWith("/c/", StringComparison.OrdinalIgnoreCase))
                    {
                        cmd = path[3..];
                        args = QueryToJson(ctx.Request.Url?.Query);
                    }
                    else // POST / with a JSON body {"cmd","args"}
                    {
                        using var sr = new StreamReader(ctx.Request.InputStream, ctx.Request.ContentEncoding);
                        var body = sr.ReadToEnd();
                        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
                        cmd = doc.RootElement.TryGetProperty("cmd", out var c) ? c.GetString() ?? "" : "";
                        args = doc.RootElement.TryGetProperty("args", out var ar)
                               ? JsonDocument.Parse(ar.GetRawText()).RootElement : EmptyObj();
                    }

                    if (!_cmds.TryGetValue(cmd, out var handler))
                    { Respond(ctx, 404, Wrap(false, cmd, null, $"unknown cmd '{cmd}' — GET / for the list")); continue; }

                    object? result; string? err = null;
                    try { result = handler.Handler(args); }
                    catch (Exception ex) { result = null; err = ex.Message; }
                    Respond(ctx, err == null ? 200 : 500, Wrap(err == null, cmd, result, err));
                }
                catch { try { Respond(ctx, 500, Wrap(false, "", null, "bad request")); } catch { } }
            }
        }

        static string Wrap(bool ok, string cmd, object? result, string? error)
            => JsonSerializer.Serialize(new { ok, cmd, result, error }, _json);

        static void Respond(HttpListenerContext ctx, int code, string body)
        {
            var b = Encoding.UTF8.GetBytes(body);
            ctx.Response.StatusCode = code;
            ctx.Response.ContentType = body.StartsWith('{') || body.StartsWith('[') ? "application/json" : "text/plain";
            ctx.Response.ContentLength64 = b.Length;
            try { ctx.Response.OutputStream.Write(b, 0, b.Length); } catch { }
            try { ctx.Response.Close(); } catch { }
        }

        static string LogText(HttpListenerContext ctx)
        {
            int n = int.TryParse(QueryToJson(ctx.Request.Url?.Query) is var q && q.TryGetProperty("n", out var nv) ? nv.GetString() : null, out var v) ? v : 200;
            lock (_logLock)
            {
                var arr = _log.ToArray();
                return string.Join("\n", arr[Math.Max(0, arr.Length - n)..]);
            }
        }

        static string HelpText()
        {
            var sb = new StringBuilder();
            sb.AppendLine("HS175D DebugBus — JSON control surface\n");
            sb.AppendLine("GET  /log?n=200            recent log lines");
            sb.AppendLine("GET  /c/<cmd>?k=v           run a command (query = args)");
            sb.AppendLine("POST /  {\"cmd\":..,\"args\":{..}}\n");
            sb.AppendLine("Commands:");
            foreach (var c in _cmds.Values.OrderBy(c => c.Name))
                sb.AppendLine($"  {c.Name,-22} {c.Help}");
            return sb.ToString();
        }

        static JsonElement QueryToJson(string? query)
        {
            var map = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(query))
                foreach (var part in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
                {
                    var kv = part.Split('=', 2);
                    map[Uri.UnescapeDataString(kv[0])] = kv.Length > 1 ? Uri.UnescapeDataString(kv[1]) : "";
                }
            return JsonDocument.Parse(JsonSerializer.Serialize(map)).RootElement;
        }

        static JsonElement EmptyObj() => JsonDocument.Parse("{}").RootElement;

        // ---- generic UI automation (built-in commands) ----------------------

        static object? OnUi(System.Windows.Forms.Control c, Func<object?> f)
            => c.InvokeRequired ? c.Invoke(f) : f();

        static void RegisterBuiltins()
        {
            // All ui.* run on the main form's UI thread, then reflect over OpenForms.
            _cmds["ui.windows"] = new Cmd("ui.windows", "list open windows", _ => OnUi(_ui!, () =>
                System.Windows.Forms.Application.OpenForms.Cast<System.Windows.Forms.Form>()
                    .Select(f => new { name = f.Name, title = f.Text, type = f.GetType().Name, visible = f.Visible })
                    .ToArray()));

            _cmds["ui.tree"] = new Cmd("ui.tree", "dump a window's controls: args {window}", a => OnUi(_ui!, () =>
            {
                var form = FindForm(Str(a, "window"));
                if (form == null) return new { error = "window not found", open = OpenFormNames() };
                return Tree(form);
            }));

            _cmds["ui.get"] = new Cmd("ui.get", "read a control: args {window,control}", a => OnUi(_ui!, () =>
            {
                var c = Find(a);
                return c == null ? new { error = "control not found" } : Describe(c);
            }));

            _cmds["ui.set"] = new Cmd("ui.set", "set a control value: args {window,control,value}", a => OnUi(_ui!, () =>
            {
                var c = Find(a);
                if (c == null) return new { error = "control not found" };
                string val = Str(a, "value") ?? "";
                switch (c)
                {
                    case System.Windows.Forms.CheckBox cb: cb.Checked = val is "1" or "true" or "on" or "yes"; break;
                    case System.Windows.Forms.RadioButton rb: rb.Checked = true; break;
                    case System.Windows.Forms.TrackBar tb:
                        if (int.TryParse(val, out var iv)) tb.Value = Math.Clamp(iv, tb.Minimum, tb.Maximum); break;
                    case System.Windows.Forms.ComboBox combo: combo.Text = val; break;
                    default: c.Text = val; break;
                }
                return Describe(c);
            }));

            _cmds["ui.click"] = new Cmd("ui.click", "click/toggle a control: args {window,control[,value]}", a => OnUi(_ui!, () =>
            {
                var c = Find(a);
                if (c == null) return new { error = "control not found" };
                var force = Bool(a, "value");
                switch (c)
                {
                    case System.Windows.Forms.CheckBox cb: cb.Checked = force ?? !cb.Checked; break;
                    case System.Windows.Forms.RadioButton rb: rb.Checked = true; break;
                    case System.Windows.Forms.IButtonControl b: b.PerformClick(); break;
                    default:
                        var m = c.GetType().GetMethod("PerformClick", Type.EmptyTypes);
                        if (m != null) m.Invoke(c, null);
                        else return new { error = "not clickable", type = c.GetType().Name };
                        break;
                }
                return Describe(c);
            }));
        }

        static System.Windows.Forms.Control? Find(JsonElement a)
        {
            var form = FindForm(Str(a, "window"));
            return form == null ? null : FindControl(form, Str(a, "control"));
        }

        static System.Windows.Forms.Form? FindForm(string? id)
        {
            var forms = System.Windows.Forms.Application.OpenForms.Cast<System.Windows.Forms.Form>().ToList();
            if (string.IsNullOrEmpty(id)) return forms.FirstOrDefault();
            return forms.FirstOrDefault(f => f.Name.Equals(id, StringComparison.OrdinalIgnoreCase))
                ?? forms.FirstOrDefault(f => f.GetType().Name.Equals(id, StringComparison.OrdinalIgnoreCase))
                ?? forms.FirstOrDefault(f => (f.Text ?? "").Contains(id, StringComparison.OrdinalIgnoreCase));
        }

        static System.Windows.Forms.Control? FindControl(System.Windows.Forms.Control root, string? name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            // "#N" = address by the index shown in ui.tree (for programmatic windows whose controls have no Name)
            if (name.StartsWith('#') && int.TryParse(name[1..], out var idx))
            {
                var list = Walk(root).ToList();
                return idx >= 0 && idx < list.Count ? list[idx] : null;
            }
            string trimmed = name.TrimStart('_');
            foreach (System.Windows.Forms.Control c in Walk(root))
            {
                if (c.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) return c;
                if (c.Name.TrimStart('_').Equals(trimmed, StringComparison.OrdinalIgnoreCase)) return c;
            }
            // fall back to match by visible Text (e.g. a button caption)
            foreach (System.Windows.Forms.Control c in Walk(root))
                if (!string.IsNullOrEmpty(c.Text) && c.Text.Equals(name, StringComparison.OrdinalIgnoreCase)) return c;
            return null;
        }

        static IEnumerable<System.Windows.Forms.Control> Walk(System.Windows.Forms.Control root)
        {
            foreach (System.Windows.Forms.Control c in root.Controls)
            {
                yield return c;
                foreach (var d in Walk(c)) yield return d;
            }
        }

        static object Describe(System.Windows.Forms.Control c) => new
        {
            name = c.Name,
            type = c.GetType().Name,
            text = c.Text,
            value = c switch
            {
                System.Windows.Forms.CheckBox cb => (object)cb.Checked,
                System.Windows.Forms.TrackBar tb => tb.Value,
                System.Windows.Forms.ComboBox cmb => cmb.Text,
                _ => null!
            },
            enabled = c.Enabled,
            visible = c.Visible
        };

        static object Tree(System.Windows.Forms.Control root)
        {
            int i = 0;
            return new
            {
                window = string.IsNullOrEmpty(root.Name) ? root.GetType().Name : root.Name,
                // id = the #index to address each control with (these windows mostly have no Name)
                controls = Walk(root).Select(c => new
                {
                    id = i++,
                    name = c.Name,
                    type = c.GetType().Name,
                    text = Trunc(c.Text),
                    value = c switch
                    {
                        System.Windows.Forms.CheckBox cb => (object)cb.Checked,
                        System.Windows.Forms.TrackBar tb => tb.Value,
                        _ => null!
                    },
                    enabled = c.Enabled
                }).ToArray()
            };
        }

        static string? Trunc(string? s) => s != null && s.Length > 80 ? s[..80] + "…" : s;
        static string[] OpenFormNames() => System.Windows.Forms.Application.OpenForms.Cast<System.Windows.Forms.Form>().Select(f => f.Name).ToArray();
    }
}

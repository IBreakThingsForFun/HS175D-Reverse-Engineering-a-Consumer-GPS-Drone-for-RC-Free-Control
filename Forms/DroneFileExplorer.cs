using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;

namespace HS175D
{
    /// <summary>One filesystem entry as parsed from a Unix <c>ls -l</c> row.</summary>
    internal sealed record FsEntry(string Name, bool IsDir, bool IsLink, long Size, string Perms, string LinkTarget);

    /// <summary>
    /// A browsable filesystem channel to the drone. Both back doors expose the WHOLE filesystem
    /// and both emit Unix <c>ls -l</c>–format listings, so the explorer treats them identically.
    /// </summary>
    internal interface IFsBackend
    {
        string Name { get; }
        /// <summary>List <paramref name="path"/>; returns the canonical path + its entries.</summary>
        Task<(string path, List<FsEntry> entries)> ListAsync(string path);
        Task DownloadAsync(string remotePath, string localPath);
    }

    /// <summary>
    /// Explorer-style view of the drone filesystem — switchable between adb (root shell, :5555)
    /// and FTP (HY819/1663819, :21), the two whole-FS back doors (docs/DEVICE_ACCESS.md). Address
    /// bar + Up/Refresh + a details list (folders first); double-click a folder to enter, Open to
    /// view a file (pulled to a temp copy → OS default app), Download… to save it anywhere.
    /// Self-contained Form so Form1 / the Designer layout stay untouched.
    /// </summary>
    public sealed class DroneFileExplorer : Form
    {
        readonly IFsBackend[] _backends;
        IFsBackend _backend;
        string _cwd = "/";

        readonly ComboBox _cboChannel = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        readonly TextBox  _txtPath = new() { Text = "/" };
        readonly Button   _btnUp   = new() { Text = "↑ Up" };
        readonly Button   _btnGo   = new() { Text = "Go" };
        readonly Button   _btnRefresh = new() { Text = "⟳" };
        readonly ListView _list = new()
        {
            View = View.Details, FullRowSelect = true, MultiSelect = false,
            HideSelection = false, Font = new Font("Consolas", 9F)
        };
        readonly Button _btnOpen = new() { Text = "Open" };
        readonly Button _btnDownload = new() { Text = "Download…" };
        readonly Label  _status = new() { AutoSize = true, ForeColor = Color.DimGray };
        readonly ToolTip _tip = new() { AutoPopDelay = 30000, InitialDelay = 350, ReshowDelay = 80, ShowAlways = true };

        public DroneFileExplorer(string droneIp)
        {
            string ip = string.IsNullOrWhiteSpace(droneIp) ? "192.168.100.1" : droneIp.Trim();
            _backends = new IFsBackend[] { new AdbBackend(ip, DroneConsole.ResolveAdb()), new FtpBackend(ip) };
            _backend  = _backends[0];

            Text = $"Drone file explorer — {ip}";
            ClientSize = new Size(840, 560);
            MinimumSize = new Size(560, 360);
            Font = new Font("Segoe UI", 9F);

            // ---- channel + address bar ----
            _cboChannel.SetBounds(8, 9, 150, 23);
            foreach (var b in _backends) _cboChannel.Items.Add(b.Name);
            _cboChannel.SelectedIndex = 0;
            _btnUp.SetBounds(164, 8, 52, 26);
            _txtPath.SetBounds(222, 10, 502, 23);
            _txtPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _btnGo.SetBounds(730, 8, 48, 26);
            _btnGo.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _btnRefresh.SetBounds(782, 8, 40, 26);
            _btnRefresh.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _tip.SetToolTip(_cboChannel, "Filesystem channel — adb (root shell, :5555) or FTP (HY819/1663819, :21). Both see the whole filesystem.");
            _tip.SetToolTip(_btnUp, "Go up one directory.");
            _tip.SetToolTip(_txtPath, "Current path — type one and press Go/Enter to jump there.");
            _tip.SetToolTip(_btnGo, "Navigate to the typed path.");
            _tip.SetToolTip(_btnRefresh, "Reload the current directory listing.");

            // ---- listing ----
            _list.SetBounds(8, 42, 824, 470);
            _list.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _list.Columns.Add("Name", 360);
            _list.Columns.Add("Size", 90, HorizontalAlignment.Right);
            _list.Columns.Add("Type", 70);
            _list.Columns.Add("Perms / →", 270);

            // ---- actions ----
            _btnOpen.SetBounds(8, 520, 90, 28);
            _btnOpen.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            _btnDownload.SetBounds(104, 520, 110, 28);
            _btnDownload.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            _status.Location = new Point(228, 527);
            _status.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            _tip.SetToolTip(_btnOpen, "Pull the selected file to a temp copy and open it in the OS default app.");
            _tip.SetToolTip(_btnDownload, "Download the selected file to a location you choose.");

            Controls.AddRange(new Control[]
            {
                _cboChannel, _btnUp, _txtPath, _btnGo, _btnRefresh, _list, _btnOpen, _btnDownload, _status
            });

            _cboChannel.SelectedIndexChanged += async (s, e) =>
            {
                _backend = _backends[_cboChannel.SelectedIndex];
                await NavigateTo("/");
            };
            _btnUp.Click       += async (s, e) => await NavigateTo(_cwd + "/..");
            _btnGo.Click       += async (s, e) => await NavigateTo(_txtPath.Text);
            _btnRefresh.Click  += async (s, e) => await NavigateTo(_cwd);
            _txtPath.KeyDown   += async (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await NavigateTo(_txtPath.Text); } };
            _list.DoubleClick  += async (s, e) => await ActivateSelection();
            _btnOpen.Click     += async (s, e) => await OpenSelected();
            _btnDownload.Click += async (s, e) => await DownloadSelected();

            Theme.Apply(this);
            _list.BackColor = Theme.Bg2; _list.ForeColor = Theme.Text; _list.BorderStyle = BorderStyle.FixedSingle;

            Shown += async (s, e) => await NavigateTo("/");
        }

        // ===================== navigation =====================
        async Task NavigateTo(string path)
        {
            Busy(true); SetStatus($"{_backend.Name}: ls {path} …", Theme.Muted);
            try
            {
                var (canon, entries) = await _backend.ListAsync(path);
                _cwd = canon; _txtPath.Text = canon;
                Populate(entries);
                SetStatus($"{_backend.Name}  {canon}  —  {entries.Count} items", Theme.Good);
            }
            catch (Exception ex) { SetStatus($"{_backend.Name}: {ex.Message}", Theme.Warn); }
            finally { Busy(false); }
        }

        void Populate(List<FsEntry> entries)
        {
            _list.BeginUpdate();
            _list.Items.Clear();
            foreach (var en in entries
                         .OrderByDescending(x => x.IsDir)
                         .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
            {
                var it = new ListViewItem(en.Name) { Tag = en };
                it.SubItems.Add(en.IsDir ? "" : HumanSize(en.Size));
                it.SubItems.Add(en.IsLink ? "link" : en.IsDir ? "folder" : "file");
                it.SubItems.Add(en.IsLink ? "→ " + en.LinkTarget : en.Perms);
                if (en.IsDir || en.IsLink) it.ForeColor = Theme.Accent;
                _list.Items.Add(it);
            }
            _list.EndUpdate();
        }

        async Task ActivateSelection()
        {
            if (_list.SelectedItems.Count == 0) return;
            var en = (FsEntry)_list.SelectedItems[0].Tag!;
            if (en.IsDir || en.IsLink) await NavigateTo(PosixPath.Join(_cwd, en.Name));  // links: try to enter; backend reports if not a dir
            else await OpenSelected();
        }

        // ===================== open / download =====================
        async Task OpenSelected()
        {
            var en = Selected();
            if (en is null || en.IsDir) { SetStatus("Select a file to open.", Theme.Warn); return; }
            string remote = PosixPath.Join(_cwd, en.Name);
            string local = Path.Combine(Path.GetTempPath(), "drone_" + SafeName(en.Name));
            Busy(true); SetStatus($"fetch {remote} …", Theme.Muted);
            try
            {
                await _backend.DownloadAsync(remote, local);
                SetStatus($"opened {en.Name}", Theme.Good);
                Process.Start(new ProcessStartInfo(local) { UseShellExecute = true });
            }
            catch (Exception ex) { SetStatus("open failed: " + ex.Message, Theme.Danger); }
            finally { Busy(false); }
        }

        async Task DownloadSelected()
        {
            var en = Selected();
            if (en is null || en.IsDir) { SetStatus("Select a file to download.", Theme.Warn); return; }
            string remote = PosixPath.Join(_cwd, en.Name);
            using var sfd = new SaveFileDialog { FileName = en.Name, Title = $"Download {remote}" };
            if (sfd.ShowDialog(this) != DialogResult.OK) return;
            Busy(true); SetStatus($"fetch {remote} …", Theme.Muted);
            try
            {
                await _backend.DownloadAsync(remote, sfd.FileName);
                SetStatus($"saved → {sfd.FileName}", Theme.Good);
            }
            catch (Exception ex) { SetStatus("download failed: " + ex.Message, Theme.Danger); }
            finally { Busy(false); }
        }

        // ===================== helpers =====================
        FsEntry? Selected() => _list.SelectedItems.Count > 0 ? (FsEntry)_list.SelectedItems[0].Tag! : null;

        static string SafeName(string n)
        {
            foreach (char c in Path.GetInvalidFileNameChars()) n = n.Replace(c, '_');
            return n.Length == 0 ? "file" : n;
        }

        static string HumanSize(long bytes)
        {
            if (bytes < 1024) return bytes + " B";
            string[] u = { "KB", "MB", "GB", "TB" };
            double v = bytes; int i = -1;
            do { v /= 1024; i++; } while (v >= 1024 && i < u.Length - 1);
            return $"{v:0.#} {u[i]}";
        }

        void Busy(bool b)
        {
            _cboChannel.Enabled = _btnUp.Enabled = _btnGo.Enabled = _btnRefresh.Enabled =
                _btnOpen.Enabled = _btnDownload.Enabled = _txtPath.Enabled = !b;
            Cursor = b ? Cursors.WaitCursor : Cursors.Default;
        }

        void SetStatus(string text, Color c) { _status.ForeColor = c; _status.Text = text; }
    }

    // ============================================================
    //  Shared Unix-ls parsing + POSIX path math
    // ============================================================
    internal static class LsParser
    {
        // busybox `ls -l` / FTP LIST row: perms links owner group size  mon day time/year  name[ -> target]
        static readonly Regex Row = new(
            @"^(?<perm>[-dlcbsp][rwxsStT-]{9})\s+\d+\s+\S+\s+\S+\s+(?<size>\d+)\s+\S+\s+\S+\s+\S+\s+(?<name>.+)$",
            RegexOptions.Compiled);

        // adb's `ls` colorizes output (ESC[1;34m…ESC[0m on dir names); strip so names/paths stay clean.
        static readonly Regex Ansi = new(@"\x1b\[[0-9;]*m", RegexOptions.Compiled);

        public static List<FsEntry> Parse(IEnumerable<string> lines)
        {
            var list = new List<FsEntry>();
            foreach (var raw in lines)
            {
                var line = Ansi.Replace(raw.TrimEnd('\r'), "");
                if (line.StartsWith("total ")) continue;
                var m = Row.Match(line);
                if (!m.Success) continue;

                string perm = m.Groups["perm"].Value;
                string name = m.Groups["name"].Value.Trim();
                bool isLink = perm[0] == 'l';
                string target = "";
                if (isLink)
                {
                    int arrow = name.IndexOf(" -> ", StringComparison.Ordinal);
                    if (arrow >= 0) { target = name[(arrow + 4)..]; name = name[..arrow]; }
                }
                if (name is "." or "..") continue;

                bool isDir = perm[0] == 'd';
                long.TryParse(m.Groups["size"].Value, out long size);
                list.Add(new FsEntry(name, isDir, isLink, size, perm, target));
            }
            return list;
        }
    }

    internal static class PosixPath
    {
        public static string Join(string dir, string name)
        {
            string d = dir.TrimEnd('/');
            return (d.Length == 0 ? "" : d) + "/" + name;
        }

        /// <summary>Resolve "." and ".." segments client-side (for backends that don't canonicalize server-side).</summary>
        public static string Canonical(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return "/";
            path = path.Trim().Replace('\\', '/');
            var stack = new List<string>();
            foreach (var seg in path.Split('/', StringSplitOptions.RemoveEmptyEntries))
            {
                if (seg == ".") continue;
                if (seg == "..") { if (stack.Count > 0) stack.RemoveAt(stack.Count - 1); }
                else stack.Add(seg);
            }
            return "/" + string.Join('/', stack);
        }
    }

    // ============================================================
    //  Backends
    // ============================================================
    internal sealed class AdbBackend : IFsBackend
    {
        readonly string _ip, _adb;
        public AdbBackend(string ip, string adb) { _ip = ip; _adb = adb; }
        public string Name => "ADB (root shell)";

        public Task<(string, List<FsEntry>)> ListAsync(string path) => Task.Run(() =>
        {
            string p = string.IsNullOrWhiteSpace(path) ? "/" : path.Trim().Replace('\\', '/');
            Run($"connect {_ip}:5555", 8000);   // idempotent
            // cd resolves symlinks + ".." server-side; pwd hands back the canonical path; sentinel = bad dir.
            string raw = Run($"-s {_ip}:5555 shell \"cd '{p}' 2>/dev/null && pwd && ls -la || echo __NOTADIR__\"", 20000)
                         .Replace("\r", "");
            if (raw.Contains("__NOTADIR__")) throw new IOException($"not a directory / no access: {p}");
            var lines = raw.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0) throw new IOException("empty response (device offline?)");
            return (lines[0].Trim(), LsParser.Parse(lines.Skip(1)));
        });

        public Task DownloadAsync(string remote, string local) =>
            Task.Run(() => Run($"-s {_ip}:5555 pull \"{remote}\" \"{local}\"", 180000));

        string Run(string args, int timeoutMs)
        {
            var psi = new ProcessStartInfo(_adb, args)
            {
                RedirectStandardOutput = true, RedirectStandardError = true,
                UseShellExecute = false, CreateNoWindow = true
            };
            using var p = Process.Start(psi) ?? throw new InvalidOperationException("could not start adb");
            string o = p.StandardOutput.ReadToEnd();
            string e = p.StandardError.ReadToEnd();
            p.WaitForExit(timeoutMs);
            if (o.Length == 0 && e.Contains("offline")) throw new IOException("adb device offline");
            return o + (e.Length > 0 ? "\n[stderr] " + e : "");
        }
    }

    internal sealed class FtpBackend : IFsBackend
    {
        const string User = "HY819", Pass = "1663819";
        readonly string _ip;
        public FtpBackend(string ip) { _ip = ip; }
        public string Name => "FTP";

#pragma warning disable SYSLIB0014   // FtpWebRequest is the only built-in FTP client; matches DroneConsole's choice (no external dep)
        public Task<(string, List<FsEntry>)> ListAsync(string path) => Task.Run(() =>
        {
            string p = PosixPath.Canonical(path);
            var req = (FtpWebRequest)WebRequest.Create($"ftp://{_ip}{p}");
            req.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
            req.Credentials = new NetworkCredential(User, Pass);
            req.UsePassive = true; req.KeepAlive = false;
            using var resp = (FtpWebResponse)req.GetResponse();
            using var sr = new StreamReader(resp.GetResponseStream()!);
            var lines = new List<string>();
            string? l;
            while ((l = sr.ReadLine()) != null) lines.Add(l);
            return (p, LsParser.Parse(lines));
        });

        public Task DownloadAsync(string remote, string local) => Task.Run(() =>
        {
            var req = (FtpWebRequest)WebRequest.Create($"ftp://{_ip}{PosixPath.Canonical(remote)}");
            req.Method = WebRequestMethods.Ftp.DownloadFile;
            req.Credentials = new NetworkCredential(User, Pass);
            req.UseBinary = true; req.UsePassive = true; req.KeepAlive = false;
            using var resp = (FtpWebResponse)req.GetResponse();
            using var s = resp.GetResponseStream();
            using var fs = File.Create(local);
            s!.CopyTo(fs);
        });
#pragma warning restore SYSLIB0014
    }
}

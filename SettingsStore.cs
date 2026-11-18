using System.Text.Json;

namespace HS175D
{
    /// <summary>
    /// Persists a panel's slider/checkbox/combo settings to disk, restores them on the next launch,
    /// and remembers each control's construction-time value so a group can be reset to defaults.
    ///
    /// Every value is stored as an int - TrackBar.Value, CheckBox.Checked as 0/1, ComboBox.SelectedIndex
    /// - which keeps the file readable by hand and lets one code path cover every control type. That's
    /// deliberate: the alternative (a typed settings class) would need a new field, a new default and a
    /// new load/save line for every control added later, which is exactly the kind of bookkeeping that
    /// silently rots. Here, registering a control is the only step.
    ///
    /// Registration captures the default at the moment it's called, so it MUST happen after a control's
    /// initial value is set and BEFORE Load() replaces it with the stored one.
    ///
    /// Applying a stored value fires the control's own ValueChanged, so everything downstream (the
    /// volatile mirror fields, the live RPC pushes, the value labels) updates through exactly the same
    /// path as a user drag. Nothing needs a separate "apply loaded settings" pass.
    /// </summary>
    public sealed class SettingsStore
    {
        readonly record struct Entry(string Group, Func<int> Get, Action<int> Set, int Default);

        readonly Dictionary<string, Entry> _entries = new();
        readonly string _path;
        bool _dirty;

        public SettingsStore(string fileName)
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HS175D");
            Directory.CreateDirectory(dir);
            _path = Path.Combine(dir, fileName);
        }

        public string Path_ => _path;

        public void Register(string group, string key, TrackBar t)
        {
            // Clamped on the way in: a settings file from a build with a wider range would otherwise
            // throw when its value lands outside the current Minimum/Maximum.
            _entries[key] = new Entry(group, () => t.Value, v => t.Value = Math.Clamp(v, t.Minimum, t.Maximum), t.Value);
            t.ValueChanged += (s, e) => _dirty = true;
        }

        public void Register(string group, string key, CheckBox c)
        {
            _entries[key] = new Entry(group, () => c.Checked ? 1 : 0, v => c.Checked = v != 0, c.Checked ? 1 : 0);
            c.CheckedChanged += (s, e) => _dirty = true;
        }

        public void Register(string group, string key, ComboBox c)
        {
            _entries[key] = new Entry(group, () => c.SelectedIndex,
                v => { if (v >= 0 && v < c.Items.Count) c.SelectedIndex = v; }, c.SelectedIndex);
            c.SelectedIndexChanged += (s, e) => _dirty = true;
        }

        /// <summary>Applies whatever was saved last run. Unknown or missing keys simply keep their
        /// default, so a settings file written by an older build never blocks startup.</summary>
        public void Load()
        {
            try
            {
                if (!File.Exists(_path)) return;
                var stored = JsonSerializer.Deserialize<Dictionary<string, int>>(File.ReadAllText(_path));
                if (stored == null) return;
                foreach (var (key, value) in stored)
                    if (_entries.TryGetValue(key, out var e))
                        try { e.Set(value); } catch { /* one bad key must not abandon the rest */ }
            }
            catch { /* a corrupt or unreadable settings file must never stop the app opening */ }
            _dirty = false;
        }

        public void Save()
        {
            try
            {
                var map = _entries.ToDictionary(kv => kv.Key, kv => kv.Value.Get());
                File.WriteAllText(_path, JsonSerializer.Serialize(map, new JsonSerializerOptions { WriteIndented = true }));
                _dirty = false;
            }
            catch { /* best-effort - losing a settings write is never worth an exception at shutdown */ }
        }

        public void SaveIfDirty() { if (_dirty) Save(); }

        /// <summary>Restores one group of controls to the values they were constructed with. Goes
        /// through each control's setter, so the same ValueChanged path that a user drag takes also
        /// runs here and everything downstream follows.</summary>
        public void ResetGroup(string group)
        {
            foreach (var e in _entries.Values)
                if (e.Group == group)
                    try { e.Set(e.Default); } catch { }
            _dirty = true;
        }
    }
}

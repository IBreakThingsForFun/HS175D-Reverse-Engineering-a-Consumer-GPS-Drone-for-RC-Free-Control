using System.Collections.Concurrent;

namespace HS175D
{
    /// <summary>
    /// Live catalog of distinct packet "types" seen on each protocol, so a frame shape we have
    /// never seen before raises an alert instead of scrolling past unnoticed. Fed from
    /// <c>Form1.PacketTap</c> (every tx/rx frame) plus any explicit transport (ADB/FTP/UDP-inject).
    ///
    /// Hot path is a lock + dictionary lookup only; the SQLite catalog (<c>packet_types</c>) is
    /// touched just on novelty and on a periodic flush, so high-rate telemetry doesn't hammer disk.
    /// Signatures persist across runs (loaded at <see cref="Init"/>), so "new" means genuinely new.
    /// </summary>
    public static class PacketCatalog
    {
        public sealed class PacketType
        {
            public string Transport = "";
            public string Direction = "";   // tx | rx
            public string Sig = "";          // hex of the leading bytes — the discriminator
            public string FirstSeen = "";
            public string LastSeen = "";
            public long Count;
            public string? SampleHex;
            public int SampleLen;
            public string? Note;
            public bool NewThisSession;
            public string Key => Transport + "|" + Direction + "|" + Sig;
        }

        const int SigBytes = 2;     // leading bytes that identify a frame type (tweak if needed)
        const int SampleMax = 48;   // bytes of sample kept per type

        static readonly object _lock = new();
        static readonly ConcurrentDictionary<string, PacketType> _types = new();
        static System.Threading.Timer? _flush;
        static volatile bool _dirty;
        static bool _init;

        /// <summary>Raised on the first sighting of a brand-new packet type (any thread).</summary>
        public static event Action<PacketType>? OnNew;

        public static void Init()
        {
            lock (_lock)
            {
                if (_init) return;
                _init = true;
                foreach (var (transport, dir, sig, first, last, count, hex, len, note) in CommLog.LoadPacketTypes())
                {
                    var pt = new PacketType
                    {
                        Transport = transport, Direction = dir, Sig = sig,
                        FirstSeen = first, LastSeen = last, Count = count,
                        SampleHex = hex, SampleLen = len, Note = note, NewThisSession = false
                    };
                    _types[pt.Key] = pt;
                }
                _flush = new System.Threading.Timer(_ => Flush(), null, 8000, 8000);
            }
        }

        /// <summary>Record a frame. Cheap on known types; alerts + persists on first sighting.</summary>
        public static void Observe(string transport, PacketDir dir, byte[]? bytes)
        {
            try
            {
                if (!_init) Init();
                if (bytes == null || bytes.Length == 0) return;
                string d = dir == PacketDir.Tx ? "tx" : "rx";
                string sig = Signature(bytes);
                string key = transport + "|" + d + "|" + sig;
                string now = DateTime.UtcNow.ToString("o");

                bool isNew = false;
                PacketType pt;
                lock (_lock)
                {
                    if (!_types.TryGetValue(key, out pt!))
                    {
                        pt = new PacketType
                        {
                            Transport = transport, Direction = d, Sig = sig,
                            FirstSeen = now, LastSeen = now, Count = 0,
                            SampleHex = Convert.ToHexString(bytes, 0, Math.Min(bytes.Length, SampleMax)),
                            SampleLen = bytes.Length, NewThisSession = true
                        };
                        _types[key] = pt;
                        isNew = true;
                    }
                    pt.Count++;
                    pt.LastSeen = now;
                }
                _dirty = true;

                if (isNew)
                {
                    CommLog.UpsertPacketTypes(new[] { (pt.Transport, pt.Direction, pt.Sig, pt.FirstSeen, pt.LastSeen, pt.Count, pt.SampleHex, pt.SampleLen) });
                    CommLog.Event("CATALOG", $"NEW {transport}/{d} type {sig}", text: $"len={pt.SampleLen} sample={pt.SampleHex}");
                    try { OnNew?.Invoke(pt); } catch { }
                }
            }
            catch { }
        }

        /// <summary>Persist current counts/last-seen for every known type.</summary>
        public static void Flush()
        {
            if (!_dirty) return;
            PacketType[] arr;
            lock (_lock) { arr = _types.Values.ToArray(); _dirty = false; }
            CommLog.UpsertPacketTypes(arr.Select(p => (p.Transport, p.Direction, p.Sig, p.FirstSeen, p.LastSeen, p.Count, p.SampleHex, p.SampleLen)));
        }

        /// <summary>Snapshot for the viewer / bus, newest-active first.</summary>
        public static List<PacketType> Snapshot(string? transport = null)
        {
            var all = _types.Values.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(transport))
                all = all.Where(p => p.Transport.Equals(transport, StringComparison.OrdinalIgnoreCase));
            return all.OrderByDescending(p => p.LastSeen).ToList();
        }

        public static void SetNote(PacketType pt, string? note)
        {
            pt.Note = note;
            CommLog.SetPacketTypeNote(pt.Transport, pt.Direction, pt.Sig, note);
        }

        static string Signature(byte[] b) => Convert.ToHexString(b, 0, Math.Min(SigBytes, b.Length));
    }
}

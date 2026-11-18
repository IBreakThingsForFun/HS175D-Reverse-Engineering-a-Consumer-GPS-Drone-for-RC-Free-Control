using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace HS175D
{
    /// <summary>
    /// Persistent SQLite record of every transport exchange (ADB / FTP / UDP / telnet / CMD …).
    /// Each row is one chat-style message; a request and its reply share a <c>corr</c> id.
    /// The canonical record is JSON (column <c>json</c>) so ingest and output use one standard
    /// shape; indexable fields are also broken out into columns. Thread-safe via a process-wide
    /// lock, and hardened so a logging failure can never break a live transport.
    ///
    /// Standard JSON record:
    ///   { ts, transport, direction(tx|rx|event), name, target, len, hex, text, ok, error, corr }
    ///
    /// DB: &lt;app dir&gt;/comm_log.sqlite. Read/ingest it via the DebugBus log.* commands.
    /// </summary>
    public static class CommLog
    {
        static readonly object _lock = new();
        static readonly JsonSerializerOptions _jsonOpts = new() { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never };
        static string _dbPath = "";
        static bool _init;

        public static string DbPath => _dbPath;

        // ---- lifecycle ------------------------------------------------------

        public static void Init(string? dir = null)
        {
            lock (_lock)
            {
                if (_init) return;
                dir ??= AppContext.BaseDirectory;
                _dbPath = Path.Combine(dir, "comm_log.sqlite");
                using var con = Open();
                using var cmd = con.CreateCommand();
                cmd.CommandText = @"
PRAGMA journal_mode=WAL;
CREATE TABLE IF NOT EXISTS comm_log (
  id        INTEGER PRIMARY KEY AUTOINCREMENT,
  ts        TEXT    NOT NULL,
  transport TEXT    NOT NULL,
  direction TEXT    NOT NULL,
  name      TEXT,
  target    TEXT,
  length    INTEGER,
  hex       TEXT,
  text      TEXT,
  ok        INTEGER,
  error     TEXT,
  corr      TEXT,
  json      TEXT    NOT NULL
);
CREATE INDEX IF NOT EXISTS ix_comm_ts        ON comm_log(ts);
CREATE INDEX IF NOT EXISTS ix_comm_transport ON comm_log(transport);
CREATE INDEX IF NOT EXISTS ix_comm_corr      ON comm_log(corr);
CREATE TABLE IF NOT EXISTS packet_types (
  transport  TEXT    NOT NULL,
  direction  TEXT    NOT NULL,
  sig        TEXT    NOT NULL,
  first_seen TEXT    NOT NULL,
  last_seen  TEXT    NOT NULL,
  count      INTEGER NOT NULL,
  sample_hex TEXT,
  sample_len INTEGER,
  note       TEXT,
  PRIMARY KEY (transport, direction, sig)
);";
                cmd.ExecuteNonQuery();
                _init = true;
            }
        }

        static SqliteConnection Open()
        {
            var con = new SqliteConnection($"Data Source={_dbPath}");
            con.Open();
            using (var pragma = con.CreateCommand()) { pragma.CommandText = "PRAGMA busy_timeout=4000;"; pragma.ExecuteNonQuery(); }
            return con;
        }

        // ---- write API ------------------------------------------------------

        static string NewCorr() => Guid.NewGuid().ToString("N")[..8];

        /// <summary>Record an outbound message; returns the corr id to pass to the matching <see cref="Rx"/>.</summary>
        public static string Tx(string transport, string name, string? target = null,
                                byte[]? bytes = null, string? text = null)
        {
            string corr = NewCorr();
            Insert(transport, "tx", name, target, bytes, text, null, null, corr);
            return corr;
        }

        /// <summary>Record an inbound reply (or an error) for a prior <see cref="Tx"/>.</summary>
        public static void Rx(string transport, string name, string? target = null,
                              byte[]? bytes = null, string? text = null,
                              bool ok = true, string? error = null, string? corr = null)
            => Insert(transport, "rx", name, target, bytes, text, ok ? 1 : 0, error, corr ?? NewCorr());

        /// <summary>Record a single request+reply pair (synchronous transports).</summary>
        public static void Exchange(string transport, string name, string? target,
                                    string? requestText = null, byte[]? requestBytes = null,
                                    string? responseText = null, byte[]? responseBytes = null,
                                    bool ok = true, string? error = null)
        {
            string corr = NewCorr();
            Insert(transport, "tx", name, target, requestBytes, requestText, null, null, corr);
            Insert(transport, "rx", name, target, responseBytes, responseText, ok ? 1 : 0, error, corr);
        }

        /// <summary>Record a one-off event (telemetry note, fire-and-forget send, etc.).</summary>
        public static void Event(string transport, string name, string? text = null, byte[]? bytes = null, string? target = null)
            => Insert(transport, "event", name, target, bytes, text, null, null, NewCorr());

        /// <summary>Ingest an external record in the standard JSON shape; returns the new row id (-1 on failure).</summary>
        public static long Add(System.Text.Json.JsonElement a)
        {
            string transport = Str(a, "transport") ?? "EXT";
            string direction = Str(a, "direction") ?? "event";
            string? name     = Str(a, "name");
            string? target   = Str(a, "target");
            string? error    = Str(a, "error");
            string? text     = Str(a, "text");
            string? hex      = Str(a, "hex");
            int? ok = a.ValueKind == JsonValueKind.Object && a.TryGetProperty("ok", out var okv)
                      ? (okv.ValueKind == JsonValueKind.True ? 1 : okv.ValueKind == JsonValueKind.False ? 0 : (int?)null)
                      : null;

            byte[]? bytes = null;
            if (!string.IsNullOrWhiteSpace(hex))
            {
                try { bytes = Convert.FromHexString(hex.Replace(" ", "").Replace("0x", "", StringComparison.OrdinalIgnoreCase)); }
                catch { /* keep hex as text fallback below */ if (text == null) text = hex; }
            }
            return Insert(transport, direction, name, target, bytes, text, ok, error, NewCorr());
        }

        // ---- core insert ----------------------------------------------------

        static long Insert(string transport, string direction, string? name, string? target,
                           byte[]? bytes, string? text, int? ok, string? error, string corr)
        {
            try
            {
                if (!_init) Init();
                string? hex = bytes != null ? Convert.ToHexString(bytes) : null;
                int? len = bytes?.Length ?? text?.Length;
                string ts = DateTime.UtcNow.ToString("o");

                var rec = new
                {
                    ts, transport, direction, name, target, len, hex, text,
                    ok = ok == null ? (bool?)null : ok != 0,
                    error, corr
                };
                string json = JsonSerializer.Serialize(rec, _jsonOpts);

                long id;
                lock (_lock)
                {
                    using var con = Open();
                    using var cmd = con.CreateCommand();
                    cmd.CommandText = @"INSERT INTO comm_log
                        (ts,transport,direction,name,target,length,hex,text,ok,error,corr,json)
                        VALUES ($ts,$tr,$dir,$name,$tgt,$len,$hex,$text,$ok,$err,$corr,$json);
                        SELECT last_insert_rowid();";
                    cmd.Parameters.AddWithValue("$ts", ts);
                    cmd.Parameters.AddWithValue("$tr", transport);
                    cmd.Parameters.AddWithValue("$dir", direction);
                    cmd.Parameters.AddWithValue("$name", (object?)name ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$tgt", (object?)target ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$len", (object?)len ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$hex", (object?)hex ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$text", (object?)text ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$ok", (object?)ok ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$err", (object?)error ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$corr", (object?)corr ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$json", json);
                    id = Convert.ToInt64(cmd.ExecuteScalar() ?? 0L);
                }

                Mirror(transport, direction, name, len, ok, error, hex != null);
                return id;
            }
            catch { return -1; }   // logging must never break a transport
        }

        // Echo a one-line summary into the live DebugBus /log so persisted records also show in the chat.
        static void Mirror(string transport, string direction, string? name, int? len, int? ok, string? error, bool binary)
        {
            try
            {
                string info = len != null ? (binary ? $"{len}B" : $"{len}c") : "";
                string mark = ok == 0 ? " ERR" : "";
                string tail = error != null ? " " + error : "";
                DebugBus.Log($"[{transport}/{direction}] {name} {info}{mark}{tail}".TrimEnd());
            }
            catch { }
        }

        // ---- read / export API ---------------------------------------------

        /// <summary>Recent records (chronological) as a list of dictionaries — DebugBus serializes to JSON.</summary>
        public static List<Dictionary<string, object?>> Query(string? transport, int n, string? contains)
        {
            var rows = new List<Dictionary<string, object?>>();
            try
            {
                if (!_init) Init();
                lock (_lock)
                {
                    using var con = Open();
                    using var cmd = con.CreateCommand();
                    var where = new List<string>();
                    if (!string.IsNullOrWhiteSpace(transport)) { where.Add("transport = $tr"); cmd.Parameters.AddWithValue("$tr", transport); }
                    if (!string.IsNullOrWhiteSpace(contains)) { where.Add("(name LIKE $q OR text LIKE $q)"); cmd.Parameters.AddWithValue("$q", "%" + contains + "%"); }
                    string w = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";
                    cmd.CommandText = $"SELECT id,ts,transport,direction,name,target,length,hex,text,ok,error,corr FROM comm_log {w} ORDER BY id DESC LIMIT $n";
                    cmd.Parameters.AddWithValue("$n", Math.Clamp(n, 1, 5000));
                    using var r = cmd.ExecuteReader();
                    while (r.Read())
                        rows.Add(new Dictionary<string, object?>
                        {
                            ["id"] = r.GetInt64(0),
                            ["ts"] = r.GetString(1),
                            ["transport"] = r.GetString(2),
                            ["direction"] = r.GetString(3),
                            ["name"] = r.IsDBNull(4) ? null : r.GetString(4),
                            ["target"] = r.IsDBNull(5) ? null : r.GetString(5),
                            ["len"] = r.IsDBNull(6) ? (long?)null : r.GetInt64(6),
                            ["hex"] = r.IsDBNull(7) ? null : r.GetString(7),
                            ["text"] = r.IsDBNull(8) ? null : r.GetString(8),
                            ["ok"] = r.IsDBNull(9) ? (bool?)null : r.GetInt32(9) != 0,
                            ["error"] = r.IsDBNull(10) ? null : r.GetString(10),
                            ["corr"] = r.IsDBNull(11) ? null : r.GetString(11),
                        });
                }
                rows.Reverse();   // oldest-first for a natural chat read
            }
            catch { }
            return rows;
        }

        /// <summary>Dump matching records to a JSON file; returns a small summary object.</summary>
        public static object Export(string? path, string? transport, int n)
        {
            var rows = Query(transport, n, null);
            path ??= Path.Combine(AppContext.BaseDirectory, $"comm_log_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            File.WriteAllText(path, JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true }));
            return new { exported = rows.Count, path };
        }

        /// <summary>Row counts per transport + total + db path.</summary>
        public static object Stats()
        {
            var byTransport = new Dictionary<string, long>();
            long total = 0;
            try
            {
                if (!_init) Init();
                lock (_lock)
                {
                    using var con = Open();
                    using var cmd = con.CreateCommand();
                    cmd.CommandText = "SELECT transport, COUNT(*) FROM comm_log GROUP BY transport";
                    using var r = cmd.ExecuteReader();
                    while (r.Read()) { long c = r.GetInt64(1); byTransport[r.GetString(0)] = c; total += c; }
                }
            }
            catch { }
            return new { total, byTransport, db = _dbPath };
        }

        // ---- packet-type catalog persistence (used by PacketCatalog) --------

        /// <summary>Load every known packet signature so novelty detection survives restarts.</summary>
        public static List<(string transport, string dir, string sig, string first, string last, long count, string? hex, int len, string? note)> LoadPacketTypes()
        {
            var list = new List<(string, string, string, string, string, long, string?, int, string?)>();
            try
            {
                if (!_init) Init();
                lock (_lock)
                {
                    using var con = Open();
                    using var cmd = con.CreateCommand();
                    cmd.CommandText = "SELECT transport,direction,sig,first_seen,last_seen,count,sample_hex,sample_len,note FROM packet_types";
                    using var r = cmd.ExecuteReader();
                    while (r.Read())
                        list.Add((r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4),
                                  r.GetInt64(5), r.IsDBNull(6) ? null : r.GetString(6), r.IsDBNull(7) ? 0 : r.GetInt32(7),
                                  r.IsDBNull(8) ? null : r.GetString(8)));
                }
            }
            catch { }
            return list;
        }

        /// <summary>Upsert one or many packet-type rows (counts/last-seen) in a single transaction.</summary>
        public static void UpsertPacketTypes(IEnumerable<(string transport, string dir, string sig, string first, string last, long count, string? hex, int len)> rows)
        {
            try
            {
                if (!_init) Init();
                lock (_lock)
                {
                    using var con = Open();
                    using var tx = con.BeginTransaction();
                    using var cmd = con.CreateCommand();
                    cmd.Transaction = tx;
                    cmd.CommandText = @"INSERT INTO packet_types (transport,direction,sig,first_seen,last_seen,count,sample_hex,sample_len)
                        VALUES ($tr,$dir,$sig,$first,$last,$cnt,$hex,$len)
                        ON CONFLICT(transport,direction,sig) DO UPDATE SET
                          last_seen=excluded.last_seen, count=excluded.count,
                          sample_hex=COALESCE(packet_types.sample_hex,excluded.sample_hex),
                          sample_len=excluded.sample_len;";
                    var pTr = cmd.Parameters.Add("$tr", SqliteType.Text);
                    var pDir = cmd.Parameters.Add("$dir", SqliteType.Text);
                    var pSig = cmd.Parameters.Add("$sig", SqliteType.Text);
                    var pFirst = cmd.Parameters.Add("$first", SqliteType.Text);
                    var pLast = cmd.Parameters.Add("$last", SqliteType.Text);
                    var pCnt = cmd.Parameters.Add("$cnt", SqliteType.Integer);
                    var pHex = cmd.Parameters.Add("$hex", SqliteType.Text);
                    var pLen = cmd.Parameters.Add("$len", SqliteType.Integer);
                    foreach (var row in rows)
                    {
                        pTr.Value = row.transport; pDir.Value = row.dir; pSig.Value = row.sig;
                        pFirst.Value = row.first; pLast.Value = row.last; pCnt.Value = row.count;
                        pHex.Value = (object?)row.hex ?? DBNull.Value; pLen.Value = row.len;
                        cmd.ExecuteNonQuery();
                    }
                    tx.Commit();
                }
            }
            catch { }
        }

        public static void SetPacketTypeNote(string transport, string dir, string sig, string? note)
        {
            try
            {
                if (!_init) Init();
                lock (_lock)
                {
                    using var con = Open();
                    using var cmd = con.CreateCommand();
                    cmd.CommandText = "UPDATE packet_types SET note=$note WHERE transport=$tr AND direction=$dir AND sig=$sig";
                    cmd.Parameters.AddWithValue("$note", (object?)note ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("$tr", transport);
                    cmd.Parameters.AddWithValue("$dir", dir);
                    cmd.Parameters.AddWithValue("$sig", sig);
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }

        // ---- small helpers --------------------------------------------------

        static string? Str(JsonElement a, string k)
            => a.ValueKind == JsonValueKind.Object && a.TryGetProperty(k, out var v)
               ? (v.ValueKind == JsonValueKind.String ? v.GetString() : v.ValueKind == JsonValueKind.Null ? null : v.ToString())
               : null;
    }
}

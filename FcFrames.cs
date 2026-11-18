using System.Text.RegularExpressions;

namespace HS175D
{
    /// <summary>
    /// Parser + differ for the SoC↔flight-controller (ttyS1) frame format, reverse-engineered from
    /// the raw UART captures (docs/20_20.log). Framing:  58 | type | len | payload[len] | checksum(1).
    /// Known telemetry types: 0x8a (len 12, "SMLGM401" model id), 0x8b (len 14, status), 0x8c (len 13,
    /// sensor + uptime). Resync is structural (sync 0x58 + known type/len + the next frame also starts
    /// 0x58), and is now backed by the checksum: csum = type ^ len ^ XOR(payload) (sync byte excluded),
    /// verified against live 0x8b frames — bad-checksum frames from the lossy `cat` tap are dropped.
    /// Field decode: on 0x8b, payload[13] is the positioning MODE (0x0e GPS / 0x02 optical / 0x00 init).
    /// </summary>
    public static class FcFrames
    {
        static readonly Dictionary<byte, int> Len = new() { [0x8a] = 12, [0x8b] = 14, [0x8c] = 13 };

        // 0x8b field decode: positioning mode lives at payload[13] (the optical/GPS gate we're chasing).
        public const int ModePos = 13;
        public static string ModeName(byte b) => b switch
        {
            0x0e => "GPS",
            0x02 => "optical",
            0x00 => "init",
            _ => "0x" + b.ToString("x2"),
        };

        // XOR checksum over type + len + payload (sync 0x58 excluded). Matches the trailing byte.
        public static byte Checksum(byte type, byte len, byte[] payload)
        {
            byte c = (byte)(type ^ len);
            foreach (byte p in payload) c ^= p;
            return c;
        }

        public readonly record struct Frame(byte Type, byte[] Payload);

        public static List<Frame> Parse(string hexText)
        {
            var b = ToBytes(hexText);
            var frames = new List<Frame>();
            int i = 0;
            while (i + 3 < b.Count)
            {
                if (b[i] != 0x58) { i++; continue; }
                byte type = b[i + 1];
                if (!Len.TryGetValue(type, out int len) || b[i + 2] != len) { i++; continue; }
                int total = 3 + len + 1;                       // hdr + type + len + payload + checksum
                if (i + total > b.Count) break;
                if (i + total < b.Count && b[i + total] != 0x58) { i++; continue; }  // next frame must resync
                var payload = new byte[len];
                for (int k = 0; k < len; k++) payload[k] = b[i + 3 + k];
                if (b[i + 3 + len] != Checksum(type, (byte)len, payload)) { i++; continue; }  // reject bad checksum
                frames.Add(new Frame(type, payload));
                i += total;
            }
            return frames;
        }

        /// <summary>Per frame-type, per byte position: report positions that hold a small, stable set of
        /// values in each capture but whose sets don't overlap — i.e. a flag that changed with the mode.</summary>
        public static List<string> Diff(string aHex, string bHex)
        {
            var a = Parse(aHex);
            var b = Parse(bHex);
            var outp = new List<string>();

            foreach (byte type in Len.Keys.OrderBy(x => x))
            {
                var fa = a.Where(f => f.Type == type).ToList();
                var fb = b.Where(f => f.Type == type).ToList();
                if (fa.Count == 0 || fb.Count == 0)
                {
                    if (fa.Count != fb.Count) outp.Add($"frame 0x{type:x2}: only one capture has it (GPS={fa.Count} OPT={fb.Count})");
                    continue;
                }
                for (int pos = 0; pos < Len[type]; pos++)
                {
                    var sa = fa.Select(f => f.Payload[pos]).Distinct().ToList();
                    var sb = fb.Select(f => f.Payload[pos]).Distinct().ToList();
                    if (sa.Count <= 2 && sb.Count <= 2 && !sa.Intersect(sb).Any())
                        outp.Add($"0x{type:x2} byte[{pos,2}]:  GPS={Fmt(sa)}  OPT={Fmt(sb)}");
                }
            }
            if (outp.Count == 0)
                outp.Add("no stable field differs — mode may not be telemetered on this link, or captures too lossy (grab again, hold longer).");
            return outp;
        }

        static string Fmt(List<byte> v) => "{" + string.Join(",", v.Select(x => x.ToString("x2"))) + "}";

        // Pull every standalone 2-hex-digit token out of od/hexdump text (ignores offsets, ascii, labels).
        static List<byte> ToBytes(string text)
        {
            var list = new List<byte>(text.Length / 3);
            foreach (Match m in Regex.Matches(text, "(?<![0-9a-fA-F])[0-9a-fA-F]{2}(?![0-9a-fA-F])"))
                list.Add(Convert.ToByte(m.Value, 16));
            return list;
        }
    }
}

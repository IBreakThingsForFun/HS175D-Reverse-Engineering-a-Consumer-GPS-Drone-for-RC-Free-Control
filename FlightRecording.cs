using System.Globalization;

namespace HS175D
{
    /// <summary>One recorded control-stick sample: elapsed seconds since recording start, and the RAW
    /// -1..1 stick fractions AirSimPanel.ControlLoop reads every tick - pre-expo, exactly what a
    /// human's stick drag produced. Replaying these through the SAME ControlLoop pipeline (expo,
    /// smoothing, idle-hold) reproduces the original flight's actual behaviour, not an autopilot's
    /// re-interpretation of it as a velocity setpoint (compare Orbit Scan/Line Pass, which write
    /// velocity fractions directly and skip expo via _autopilotDirect).</summary>
    public readonly record struct FlightSample(double TSec, float Pitch, float Roll, float Yaw, float Throttle);

    /// <summary>
    /// A recorded flight: the starting pose (so playback can teleport back to the exact same spot
    /// before every replay, the same reproducibility Line Pass's Save Loc teleport already provides)
    /// plus the full stick timeline. Saved/loaded as a small CSV - one start-pose row, a header, then
    /// one row per sample - so it's readable/editable by hand if needed, consistent with every other
    /// export format in this project.
    /// </summary>
    public sealed class FlightRecording
    {
        public double StartX, StartY, StartZ, StartYawDeg;
        public readonly List<FlightSample> Samples = new();

        public double DurationSec => Samples.Count > 0 ? Samples[^1].TSec : 0;

        public void Save(string path)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("# HS175D flight recording");
            sb.Append("StartPose,").Append(Inv(StartX)).Append(',').Append(Inv(StartY)).Append(',')
              .Append(Inv(StartZ)).Append(',').Append(Inv(StartYawDeg)).AppendLine();
            sb.AppendLine("TSec,Pitch,Roll,Yaw,Throttle");
            foreach (var s in Samples)
                sb.Append(Inv(s.TSec)).Append(',').Append(Inv(s.Pitch)).Append(',').Append(Inv(s.Roll)).Append(',')
                  .Append(Inv(s.Yaw)).Append(',').Append(Inv(s.Throttle)).AppendLine();
            File.WriteAllText(path, sb.ToString());
        }

        public static FlightRecording Load(string path)
        {
            var rec = new FlightRecording();
            foreach (var line in File.ReadAllLines(path))
            {
                if (line.Length == 0 || line[0] == '#') continue;
                var p = line.Split(',');
                if (p[0] == "StartPose" && p.Length >= 5)
                {
                    rec.StartX = D(p[1]); rec.StartY = D(p[2]); rec.StartZ = D(p[3]); rec.StartYawDeg = D(p[4]);
                    continue;
                }
                if (p[0] == "TSec") continue;   // header row
                if (p.Length < 5 || !double.TryParse(p[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var t)) continue;
                rec.Samples.Add(new FlightSample(t, F(p[1]), F(p[2]), F(p[3]), F(p[4])));
            }
            return rec;

            static double D(string s) => double.Parse(s, CultureInfo.InvariantCulture);
            static float F(string s) => float.Parse(s, CultureInfo.InvariantCulture);
        }

        static string Inv(double d) => d.ToString("R", CultureInfo.InvariantCulture);
    }
}

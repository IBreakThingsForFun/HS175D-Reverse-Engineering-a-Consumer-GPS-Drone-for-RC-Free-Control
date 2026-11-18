using System.Text.Json;

namespace HS175D
{
    /// <summary>
    /// Tiny persisted app preferences (currently just the last-used Drone IP), stored as
    /// JSON next to the exe. Mirrors the best-effort load/save convention in <see cref="Theme"/>
    /// (<c>theme.json</c>) so the two behave the same. Never throws — settings are a convenience.
    /// </summary>
    internal static class AppSettings
    {
        private static string SettingsPath => Path.Combine(AppContext.BaseDirectory, "app-settings.json");

        /// <summary>Last Drone IP the user entered (null until loaded/set).</summary>
        public static string? DroneIp { get; set; }

        /// <summary>A saved AirSim spot (NED metres + heading) to jump back to across sessions. All null
        /// until the user saves one. See AirSimPanel's Save/Go-to-Location buttons.</summary>
        public static double? SavedX { get; set; }
        public static double? SavedY { get; set; }
        public static double? SavedZ { get; set; }
        public static double? SavedYawDeg { get; set; }
        public static bool HasSavedLocation => SavedX.HasValue && SavedY.HasValue && SavedZ.HasValue;

        // AirSim tab's LINE GRAPH tunables (LineGraph.cs/LineGraphLift.cs) - null means "use the
        // slider's own built-in default", so a fresh install behaves exactly as before this existed.
        public static int? LineMergeRadiusPx { get; set; }
        public static int? LineMinLengthPx { get; set; }
        public static int? LineCollinearTolDeg { get; set; }
        public static int? LineParallelAngleTolDeg { get; set; }
        public static int? LineParallelOffsetTolPx { get; set; }
        public static int? LineParallelMaxGapPx { get; set; }
        public static int? LineMaxLiftLenM { get; set; }
        public static int? LineMinLiftLenM { get; set; }
        public static int? LineVerticalTolDeg { get; set; }
        public static int? LineMaxWallHeightM { get; set; }
        public static int? LineCaptureMinTimeMs { get; set; }
        public static int? LineCaptureMinDistCm { get; set; }
        public static int? LineCaptureMinRotDeg { get; set; }
        // LSD's OWN detection-sensitivity parameters (CvLineDetector.Detect), previously hardcoded to
        // OpenCV's stock defaults and never exposed - see CvLineDetector.cs's own doc comment.
        public static int? LineDetectScale { get; set; }
        public static int? LineDetectAngleDeg { get; set; }
        public static int? LineDetectDensity { get; set; }
        public static int? LineDetectMinLenPx { get; set; }

        // AirSim tab's WORLD GRID overlay (DrawWorldGrid in AirSimPanel.cs).
        public static int? WorldGridSpacingM { get; set; }
        public static int? WorldGridRadiusM { get; set; }

        public static void Load()
        {
            try
            {
                if (!File.Exists(SettingsPath)) return;
                var s = JsonSerializer.Deserialize<Data>(File.ReadAllText(SettingsPath));
                if (s is null) return;
                DroneIp = s.DroneIp;
                SavedX = s.SavedX; SavedY = s.SavedY; SavedZ = s.SavedZ; SavedYawDeg = s.SavedYawDeg;
                LineMergeRadiusPx = s.LineMergeRadiusPx;
                LineMinLengthPx = s.LineMinLengthPx;
                LineCollinearTolDeg = s.LineCollinearTolDeg;
                LineParallelAngleTolDeg = s.LineParallelAngleTolDeg;
                LineParallelOffsetTolPx = s.LineParallelOffsetTolPx;
                LineParallelMaxGapPx = s.LineParallelMaxGapPx;
                LineMaxLiftLenM = s.LineMaxLiftLenM;
                LineMinLiftLenM = s.LineMinLiftLenM;
                LineVerticalTolDeg = s.LineVerticalTolDeg;
                LineMaxWallHeightM = s.LineMaxWallHeightM;
                LineCaptureMinTimeMs = s.LineCaptureMinTimeMs;
                LineCaptureMinDistCm = s.LineCaptureMinDistCm;
                LineCaptureMinRotDeg = s.LineCaptureMinRotDeg;
                LineDetectScale = s.LineDetectScale;
                LineDetectAngleDeg = s.LineDetectAngleDeg;
                LineDetectDensity = s.LineDetectDensity;
                LineDetectMinLenPx = s.LineDetectMinLenPx;
                WorldGridSpacingM = s.WorldGridSpacingM;
                WorldGridRadiusM = s.WorldGridRadiusM;
            }
            catch { /* best-effort */ }
        }

        public static void Save()
        {
            try
            {
                File.WriteAllText(SettingsPath,
                    JsonSerializer.Serialize(new Data
                    {
                        DroneIp = DroneIp,
                        SavedX = SavedX, SavedY = SavedY, SavedZ = SavedZ, SavedYawDeg = SavedYawDeg,
                        LineMergeRadiusPx = LineMergeRadiusPx,
                        LineMinLengthPx = LineMinLengthPx,
                        LineCollinearTolDeg = LineCollinearTolDeg,
                        LineParallelAngleTolDeg = LineParallelAngleTolDeg,
                        LineParallelOffsetTolPx = LineParallelOffsetTolPx,
                        LineParallelMaxGapPx = LineParallelMaxGapPx,
                        LineMaxLiftLenM = LineMaxLiftLenM,
                        LineMinLiftLenM = LineMinLiftLenM,
                        LineVerticalTolDeg = LineVerticalTolDeg,
                        LineMaxWallHeightM = LineMaxWallHeightM,
                        LineCaptureMinTimeMs = LineCaptureMinTimeMs,
                        LineCaptureMinDistCm = LineCaptureMinDistCm,
                        LineCaptureMinRotDeg = LineCaptureMinRotDeg,
                        LineDetectScale = LineDetectScale,
                        LineDetectAngleDeg = LineDetectAngleDeg,
                        LineDetectDensity = LineDetectDensity,
                        LineDetectMinLenPx = LineDetectMinLenPx,
                        WorldGridSpacingM = WorldGridSpacingM,
                        WorldGridRadiusM = WorldGridRadiusM
                    }, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { /* best-effort */ }
        }

        private sealed class Data
        {
            public string? DroneIp { get; set; }
            public double? SavedX { get; set; }
            public double? SavedY { get; set; }
            public double? SavedZ { get; set; }
            public double? SavedYawDeg { get; set; }
            public int? LineMergeRadiusPx { get; set; }
            public int? LineMinLengthPx { get; set; }
            public int? LineCollinearTolDeg { get; set; }
            public int? LineParallelAngleTolDeg { get; set; }
            public int? LineParallelOffsetTolPx { get; set; }
            public int? LineParallelMaxGapPx { get; set; }
            public int? LineMaxLiftLenM { get; set; }
            public int? LineMinLiftLenM { get; set; }
            public int? LineVerticalTolDeg { get; set; }
            public int? LineMaxWallHeightM { get; set; }
            public int? LineCaptureMinTimeMs { get; set; }
            public int? LineCaptureMinDistCm { get; set; }
            public int? LineCaptureMinRotDeg { get; set; }
            public int? LineDetectScale { get; set; }
            public int? LineDetectAngleDeg { get; set; }
            public int? LineDetectDensity { get; set; }
            public int? LineDetectMinLenPx { get; set; }
            public int? WorldGridSpacingM { get; set; }
            public int? WorldGridRadiusM { get; set; }
        }
    }
}

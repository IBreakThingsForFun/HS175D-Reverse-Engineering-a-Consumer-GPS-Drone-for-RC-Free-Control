using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace HS175D
{
    /// <summary>
    /// Experimental: classical (non-learned) straight-line-segment detection via OpenCvSharp's LSD
    /// binding - the concrete first step of the Tier-2 building-wireframe roadmap in
    /// WorldObjectRegistry.cs (see the "airsim-world-model-goal" notes: line/edge detection -> vertex
    /// graph -> wireframe, lifted to 3D via single-view metrology). Runs entirely separately from the
    /// YOLO detector (YoloOnnxDetector.cs) - this finds generic straight edges (rooflines, wall
    /// corners, fences), not object classes.
    /// </summary>
    public static class CvLineDetector
    {
        /// <summary>One straight segment in the source frame's own pixel coordinates.</summary>
        public readonly record struct LineSeg(float X1, float Y1, float X2, float Y2)
        {
            public float LengthPx => MathF.Sqrt((X2 - X1) * (X2 - X1) + (Y2 - Y1) * (Y2 - Y1));
        }

        // Diagnostics mirroring YoloOnnxDetector's (LastError/LastKeptCount/etc.) - Detect() never
        // throws, so without these a native-load failure or an unexpected Mat conversion error would
        // just look like "0 lines found", indistinguishable from a genuinely edge-free frame.
        public static string? LastError { get; private set; }
        public static int LastRawCount { get; private set; }
        public static long LastRunMs { get; private set; }

        /// <summary>Runs OpenCV's LSD (Line Segment Detector) against one frame. Grayscale conversion
        /// happens internally - LSD operates on single-channel intensity, same as the reference
        /// implementation. minLengthPx filters out short noisy fragments (fence texture, grass) before
        /// they ever reach a caller, since a wireframe pass only cares about edges long enough to be
        /// structural. Never throws - returns empty on any failure so a bad/disposed frame can't take
        /// down whatever loop is polling this every tick; check LastError to tell that apart from
        /// "ran fine, found nothing above threshold".
        ///
        /// scale/angleThresholdDeg/densityThreshold are LSD's OWN detection-sensitivity parameters
        /// (previously hardcoded to LineSegmentDetector.Create()'s stock defaults, scale=0.8,
        /// ang_th=22.5, density_th=0.7 - i.e. never exposed at all): scale is how much LSD downsamples
        /// the image before detecting (lower = faster/smoother but loses fine edges; 1.0 = full
        /// resolution, most detail). angleThresholdDeg is how much adjacent pixel gradients are allowed
        /// to vary and still be grouped into the same line region (higher = more permissive merging,
        /// can bridge across real corners; lower = stricter, can fragment one straight edge into
        /// several). densityThreshold is the minimum fraction of aligned pixels a candidate region needs
        /// to be kept (higher = fewer, more solid lines; lower = more lines, more noise). These are
        /// upstream of every graph-cleanup slider in LineGraph.Build - no amount of merge/collinear/
        /// parallel tuning there can recover an edge LSD itself never found, or filter out fragments it
        /// over-detected, as cleanly as tuning detection sensitivity at the source.</summary>
        public static List<LineSeg> Detect(Bitmap frame, float minLengthPx = 20f, double scale = 0.8,
            double angleThresholdDeg = 22.5, double densityThreshold = 0.7)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            LastError = null;
            try
            {
                using var mat = BitmapConverter.ToMat(frame);
                using var gray = new Mat();
                Cv2.CvtColor(mat, gray, ColorConversionCodes.BGR2GRAY);

                using var lsd = LineSegmentDetector.Create(scale: scale, angTh: angleThresholdDeg, densityTh: densityThreshold);
                lsd.Detect(gray, out Vec4f[] lines, out _, out _, out _);
                LastRawCount = lines.Length;

                var result = new List<LineSeg>(lines.Length);
                foreach (var l in lines)
                {
                    var seg = new LineSeg(l.Item0, l.Item1, l.Item2, l.Item3);
                    if (seg.LengthPx >= minLengthPx) result.Add(seg);
                }
                return result;
            }
            catch (Exception ex) { LastError = ex.Message; return new List<LineSeg>(); }
            finally { LastRunMs = sw.ElapsedMilliseconds; }
        }
    }
}

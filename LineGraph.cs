namespace HS175D
{
    /// <summary>
    /// Turns CvLineDetector's raw LSD segments into a cleaned vertex/edge graph - the prerequisite for
    /// the Tier-2 building-wireframe roadmap (see airsim-world-model-goal notes): LSD returns a soup of
    /// disconnected segments (both sides of every thick stroke, fence/grass texture, unrelated noise),
    /// with no notion that two segments meeting at a roof peak share a point. This clusters endpoints
    /// that land near each other into one shared vertex, dedupes the resulting edges, and prunes
    /// anything too short to be structural - the output is what a later 2D-to-3D lift or shape-matching
    /// pass should actually work from, not the raw detector output.
    /// </summary>
    public sealed class LineGraph
    {
        public List<PointF> Vertices { get; } = new();
        public List<(int A, int B)> Edges { get; } = new();

        /// <summary>mergeRadiusPx: endpoints within this distance are treated as the same point (LSD
        /// rarely lands two segments on the exact same pixel, so this needs real slack - too small and
        /// nothing merges, too large and distinct corners collapse together). minEdgeLengthPx: dropped
        /// after merging, since a merge can shrink a short segment to near-zero length (two nearby
        /// texture fragments merging into a degenerate point-like edge). collinearToleranceDeg: after
        /// clustering, LSD still frequently breaks one real straight edge (a roofline, a curb) into
        /// several short collinear pieces meeting at "vertices" that aren't real corners at all - any
        /// vertex where exactly two edges meet and point within this many degrees of dead straight
        /// through it gets fused away, chaining until the whole run collapses to one edge.
        /// parallelAngleToleranceDeg/parallelOffsetTolerancePx: edges lying on nearly the same infinite
        /// line (same orientation within the angle tolerance, same perpendicular offset within the
        /// distance tolerance) collapse into one - catches both edges of one thick painted stroke, and
        /// a fan of short near-parallel segments (roof shingle rows) that MergeCollinear can't touch
        /// since they don't share endpoints at all. parallelMaxGapPx: angle+offset match alone isn't
        /// enough - two unrelated edges (a fence and a distant roofline) can coincidentally share a
        /// near-identical slope/intercept; this also requires the candidate to fall within this many
        /// pixels of the group's existing span ALONG that line, so distant coincidental matches don't
        /// bridge into one long fake edge spanning the whole frame.</summary>
        public static LineGraph Build(IReadOnlyList<CvLineDetector.LineSeg> segs, float mergeRadiusPx = 10f,
            float minEdgeLengthPx = 15f, float collinearToleranceDeg = 15f,
            float parallelAngleToleranceDeg = 8f, float parallelOffsetTolerancePx = 12f, float parallelMaxGapPx = 40f)
        {
            var graph = new LineGraph();

            int VertexFor(PointF p)
            {
                float bestD2 = mergeRadiusPx * mergeRadiusPx;
                int best = -1;
                for (int i = 0; i < graph.Vertices.Count; i++)
                {
                    var v = graph.Vertices[i];
                    float dx = v.X - p.X, dy = v.Y - p.Y;
                    float d2 = dx * dx + dy * dy;
                    if (d2 <= bestD2) { bestD2 = d2; best = i; }
                }
                if (best >= 0) return best;
                graph.Vertices.Add(p);
                return graph.Vertices.Count - 1;
            }

            var seenEdges = new HashSet<(int, int)>();
            foreach (var s in segs)
            {
                int a = VertexFor(new PointF(s.X1, s.Y1));
                int b = VertexFor(new PointF(s.X2, s.Y2));
                if (a == b) continue;   // both endpoints merged into the same vertex - degenerate
                var key = a < b ? (a, b) : (b, a);
                if (!seenEdges.Add(key)) continue;   // duplicate of an edge already recorded
                graph.Edges.Add(key);
            }

            graph.Edges.RemoveAll(e =>
            {
                var p1 = graph.Vertices[e.A]; var p2 = graph.Vertices[e.B];
                float dx = p2.X - p1.X, dy = p2.Y - p1.Y;
                return MathF.Sqrt(dx * dx + dy * dy) < minEdgeLengthPx;
            });

            MergeCollinear(graph, collinearToleranceDeg);
            CompactOrphans(graph);
            MergeParallel(graph, parallelAngleToleranceDeg, parallelOffsetTolerancePx, parallelMaxGapPx);
            CompactOrphans(graph);
            return graph;
        }

        // Groups edges by the infinite line they lie on (orientation + perpendicular offset from the
        // origin, both mod-180 since a line has no distinguishable direction) AND by proximity along
        // that line (maxGapPx), then collapses each group of 2+ into one edge spanning the group's
        // combined extent along its own averaged direction. Unlike MergeCollinear this needs no shared
        // vertex - a shingle-row fan or a double-stroke duplicate usually doesn't touch endpoint-to-
        // endpoint at all, only sits close and parallel.
        static void MergeParallel(LineGraph graph, float angleToleranceDeg, float offsetTolerancePx, float maxGapPx)
        {
            if (graph.Edges.Count == 0) return;

            var groupEdges = new List<List<int>>();
            var groupAngle = new List<float>();
            var groupOffset = new List<float>();
            var groupMinT = new List<float>();   // extent-so-far along the group's own direction, from the origin
            var groupMaxT = new List<float>();

            for (int ei = 0; ei < graph.Edges.Count; ei++)
            {
                var (a, b) = graph.Edges[ei];
                var p1 = graph.Vertices[a]; var p2 = graph.Vertices[b];
                float theta = MathF.Atan2(p2.Y - p1.Y, p2.X - p1.X);
                if (theta < 0) theta += MathF.PI;   // fold into [0, PI) - orientation, not direction
                float offset = p1.X * MathF.Sin(theta) - p1.Y * MathF.Cos(theta);   // signed perpendicular distance from origin

                int found = -1;
                for (int gi = 0; gi < groupEdges.Count; gi++)
                {
                    float da = MathF.Abs(theta - groupAngle[gi]) * 180f / MathF.PI;
                    da = MathF.Min(da, 180f - da);   // wraparound near the 0/180 seam
                    if (da > angleToleranceDeg) continue;
                    if (MathF.Abs(offset - groupOffset[gi]) > offsetTolerancePx) continue;

                    // Same infinite line isn't enough on its own - project onto the GROUP's own angle
                    // (not this candidate's slightly-different one) and require actual proximity, not
                    // just a coincidental slope/intercept match from clear across the frame.
                    float gcos = MathF.Cos(groupAngle[gi]), gsin = MathF.Sin(groupAngle[gi]);
                    float ct1 = p1.X * gcos + p1.Y * gsin, ct2 = p2.X * gcos + p2.Y * gsin;
                    float cMin = MathF.Min(ct1, ct2), cMax = MathF.Max(ct1, ct2);
                    if (cMin > groupMaxT[gi] + maxGapPx || cMax < groupMinT[gi] - maxGapPx) continue;

                    found = gi;
                    groupMinT[gi] = MathF.Min(groupMinT[gi], cMin);
                    groupMaxT[gi] = MathF.Max(groupMaxT[gi], cMax);
                    break;
                }
                if (found < 0)
                {
                    float cos = MathF.Cos(theta), sin = MathF.Sin(theta);
                    float t1 = p1.X * cos + p1.Y * sin, t2 = p2.X * cos + p2.Y * sin;
                    groupEdges.Add(new List<int> { ei });
                    groupAngle.Add(theta);
                    groupOffset.Add(offset);
                    groupMinT.Add(MathF.Min(t1, t2));
                    groupMaxT.Add(MathF.Max(t1, t2));
                }
                else groupEdges[found].Add(ei);
            }

            var newVerts = new List<PointF>();
            var newEdges = new List<(int, int)>();

            // Tight exact-match dedup (not the fuzzy clustering VertexFor does) - a real shared corner
            // between two edges that end up in DIFFERENT groups (different orientation) must stay one
            // vertex, not fork into two coincident copies.
            int GetOrAddVertex(PointF p)
            {
                for (int i = 0; i < newVerts.Count; i++)
                {
                    float dx = newVerts[i].X - p.X, dy = newVerts[i].Y - p.Y;
                    if (dx * dx + dy * dy < 0.25f) return i;
                }
                newVerts.Add(p);
                return newVerts.Count - 1;
            }

            for (int gi = 0; gi < groupEdges.Count; gi++)
            {
                var group = groupEdges[gi];
                if (group.Count == 1)
                {
                    var (a, b) = graph.Edges[group[0]];
                    newEdges.Add((GetOrAddVertex(graph.Vertices[a]), GetOrAddVertex(graph.Vertices[b])));
                    continue;
                }

                var pts = new List<PointF>();
                foreach (var ei in group) { var (a, b) = graph.Edges[ei]; pts.Add(graph.Vertices[a]); pts.Add(graph.Vertices[b]); }
                float cx = pts.Average(p => p.X), cy = pts.Average(p => p.Y);
                float theta = groupAngle[gi];
                float dirx = MathF.Cos(theta), diry = MathF.Sin(theta);
                float minT = float.MaxValue, maxT = float.MinValue;
                foreach (var p in pts)
                {
                    float t = (p.X - cx) * dirx + (p.Y - cy) * diry;
                    if (t < minT) minT = t;
                    if (t > maxT) maxT = t;
                }
                var e1 = new PointF(cx + dirx * minT, cy + diry * minT);
                var e2 = new PointF(cx + dirx * maxT, cy + diry * maxT);
                newEdges.Add((GetOrAddVertex(e1), GetOrAddVertex(e2)));
            }

            graph.Vertices.Clear(); graph.Vertices.AddRange(newVerts);
            graph.Edges.Clear(); graph.Edges.AddRange(newEdges);
        }

        // Repeatedly fuses a degree-2 vertex's two edges into one when they point nearly opposite
        // directions from it (i.e. a straight line passes through, not a real corner) - a 90-degree
        // building corner has a ~0 dot product between its two outgoing edges and is correctly left
        // alone; a straight run broken into several LSD segments has each interior joint reporting
        // close to -1. Restarts the scan after every fuse rather than trying to patch adjacency
        // in place - counts here are small (tens to low hundreds of vertices per frame) so the
        // simplicity is worth more than the extra passes.
        static void MergeCollinear(LineGraph graph, float toleranceDeg)
        {
            float cosThreshold = MathF.Cos((180f - toleranceDeg) * MathF.PI / 180f);
            bool changed = true;
            while (changed)
            {
                changed = false;
                var adj = new List<int>[graph.Vertices.Count];
                for (int i = 0; i < adj.Length; i++) adj[i] = new();
                foreach (var (a, b) in graph.Edges) { adj[a].Add(b); adj[b].Add(a); }

                for (int v = 0; v < graph.Vertices.Count; v++)
                {
                    if (adj[v].Count != 2) continue;
                    int n1 = adj[v][0], n2 = adj[v][1];
                    if (n1 == n2) continue;

                    var p = graph.Vertices[v]; var p1 = graph.Vertices[n1]; var p2 = graph.Vertices[n2];
                    float d1x = p1.X - p.X, d1y = p1.Y - p.Y, len1 = MathF.Sqrt(d1x * d1x + d1y * d1y);
                    float d2x = p2.X - p.X, d2y = p2.Y - p.Y, len2 = MathF.Sqrt(d2x * d2x + d2y * d2y);
                    if (len1 < 1e-3f || len2 < 1e-3f) continue;
                    float cos = (d1x * d2x + d1y * d2y) / (len1 * len2);
                    if (cos > cosThreshold) continue;   // not straight-enough through v - a real corner

                    graph.Edges.RemoveAll(e => (e.A == v && (e.B == n1 || e.B == n2)) || (e.B == v && (e.A == n1 || e.A == n2)));
                    if (!graph.Edges.Contains((n1, n2)) && !graph.Edges.Contains((n2, n1)))
                        graph.Edges.Add(n1 < n2 ? (n1, n2) : (n2, n1));
                    changed = true;
                    break;   // adjacency is stale now - restart the outer scan
                }
            }
        }

        // Shared by Build's length-prune and MergeCollinear's vertex-dissolving - drop vertices no
        // surviving edge references, so a consumer walking Vertices doesn't have to separately check
        // "is this one actually connected to anything."
        static void CompactOrphans(LineGraph graph)
        {
            var used = new HashSet<int>();
            foreach (var e in graph.Edges) { used.Add(e.A); used.Add(e.B); }
            if (used.Count == graph.Vertices.Count) return;

            var remap = new Dictionary<int, int>();
            var newVerts = new List<PointF>();
            foreach (var i in used.OrderBy(x => x)) { remap[i] = newVerts.Count; newVerts.Add(graph.Vertices[i]); }
            for (int i = 0; i < graph.Edges.Count; i++)
                graph.Edges[i] = (remap[graph.Edges[i].A], remap[graph.Edges[i].B]);
            graph.Vertices.Clear();
            graph.Vertices.AddRange(newVerts);
        }
    }
}

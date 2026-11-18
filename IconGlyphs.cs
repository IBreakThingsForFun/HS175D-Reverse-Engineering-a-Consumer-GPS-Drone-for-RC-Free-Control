using System.Drawing.Drawing2D;

namespace HS175D
{
    /// <summary>Small flat-colour vector pictograms drawn with plain GDI+ primitives, used by
    /// IconButton. Emoji text/font rendering was tried three ways (GDI+ button text, WPF's
    /// FormattedText/DrawingVisual, and RichEdit via RichTextBox) and all three come out monochrome -
    /// Segoe UI Emoji on this Windows build stores its glyphs as COLR/CPAL vector colour layers, which
    /// only DirectWrite's colour-glyph path (TranslateColorGlyphRun) renders; none of GDI, GDI+, WPF's
    /// text stack, or classic RichEdit invoke it. Hand-drawn icons sidestep the whole problem and are
    /// fully theme-colourable besides.</summary>
    static class IconGlyphs
    {
        public static void Draw(string glyph, Graphics g, RectangleF r)
        {
            var old = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            switch (glyph)
            {
                case "plug": Plug(g, r); break;
                case "bolt": Bolt(g, r); break;
                case "takeoff": Plane(g, r, up: true); break;
                case "land": Plane(g, r, up: false); break;
                case "hover": Hover(g, r); break;
                case "joystick": Joystick(g, r); break;
                case "cloud": Cloud(g, r); break;
                case "orbit": Orbit(g, r); break;
                case "tree": Tree(g, r); break;
                case "dish": Dish(g, r); break;
                case "pin": Pin(g, r); break;
                case "rocket": Rocket(g, r); break;
                case "cube": Cube(g, r); break;
                case "wand": Wand(g, r); break;
                case "world": World(g, r); break;
                case "flag": Flag(g, r); break;
                case "target": Target(g, r); break;
                case "crosshair": Crosshair(g, r); break;
                case "map": Map(g, r); break;
                case "axes3d": Axes3D(g, r); break;
                case "route": Route(g, r); break;
                case "wireframe": Wireframe(g, r); break;
                case "grid": GridIcon(g, r); break;
            }
            g.SmoothingMode = old;
        }

        static void Plug(Graphics g, RectangleF r)
        {
            using var body = new SolidBrush(Color.DeepSkyBlue);
            using var prong = new SolidBrush(Color.Gainsboro);
            float w = r.Width, h = r.Height;
            var bodyRect = new RectangleF(r.X + w * 0.15f, r.Y + h * 0.35f, w * 0.7f, h * 0.55f);
            g.FillRoundedRect(body, bodyRect, w * 0.12f);
            g.FillRectangle(prong, r.X + w * 0.3f, r.Y + h * 0.05f, w * 0.12f, h * 0.35f);
            g.FillRectangle(prong, r.X + w * 0.58f, r.Y + h * 0.05f, w * 0.12f, h * 0.35f);
        }

        static void Bolt(Graphics g, RectangleF r)
        {
            using var b = new SolidBrush(Color.Gold);
            float w = r.Width, h = r.Height;
            PointF[] pts =
            {
                new(r.X + w * 0.58f, r.Y),
                new(r.X + w * 0.15f, r.Y + h * 0.58f),
                new(r.X + w * 0.45f, r.Y + h * 0.58f),
                new(r.X + w * 0.38f, r.Y + h),
                new(r.X + w * 0.85f, r.Y + h * 0.38f),
                new(r.X + w * 0.52f, r.Y + h * 0.38f),
            };
            g.FillPolygon(b, pts);
        }

        static void Plane(Graphics g, RectangleF r, bool up)
        {
            using var pen = new Pen(up ? Color.LimeGreen : Color.OrangeRed, Math.Max(2f, r.Width * 0.14f)) { StartCap = LineCap.Round, EndCap = LineCap.ArrowAnchor };
            float w = r.Width, h = r.Height;
            if (up) g.DrawLine(pen, r.X + w * 0.15f, r.Y + h * 0.85f, r.X + w * 0.85f, r.Y + h * 0.15f);
            else g.DrawLine(pen, r.X + w * 0.15f, r.Y + h * 0.15f, r.X + w * 0.85f, r.Y + h * 0.85f);
            using var basePen = new Pen(Color.Gainsboro, Math.Max(1.5f, r.Width * 0.08f));
            g.DrawLine(basePen, r.X + w * 0.05f, r.Y + h * 0.95f, r.X + w * 0.55f, r.Y + h * 0.95f);
        }

        static void Hover(Graphics g, RectangleF r)
        {
            using var ring = new Pen(Color.CornflowerBlue, Math.Max(2f, r.Width * 0.12f));
            g.DrawEllipse(ring, r.X + r.Width * 0.08f, r.Y + r.Height * 0.08f, r.Width * 0.84f, r.Height * 0.84f);
            using var dot = new SolidBrush(Color.CornflowerBlue);
            float d = r.Width * 0.22f;
            g.FillEllipse(dot, r.X + (r.Width - d) / 2, r.Y + (r.Height - d) / 2, d, d);
        }

        static void Joystick(Graphics g, RectangleF r)
        {
            using var baseBrush = new SolidBrush(Color.MediumPurple);
            float w = r.Width, h = r.Height;
            g.FillEllipse(baseBrush, r.X + w * 0.15f, r.Y + h * 0.72f, w * 0.7f, h * 0.22f);
            using var stick = new Pen(Color.MediumPurple, Math.Max(2f, w * 0.14f)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLine(stick, r.X + w * 0.5f, r.Y + h * 0.8f, r.X + w * 0.5f, r.Y + h * 0.22f);
            using var knob = new SolidBrush(Color.Plum);
            float d = w * 0.4f;
            g.FillEllipse(knob, r.X + (w - d) / 2, r.Y, d, d);
        }

        static void Cloud(Graphics g, RectangleF r)
        {
            using var b = new SolidBrush(Color.LightSkyBlue);
            float w = r.Width, h = r.Height;
            g.FillEllipse(b, r.X + w * 0.05f, r.Y + h * 0.4f, w * 0.45f, h * 0.4f);
            g.FillEllipse(b, r.X + w * 0.32f, r.Y + h * 0.2f, w * 0.5f, h * 0.5f);
            g.FillEllipse(b, r.X + w * 0.55f, r.Y + h * 0.35f, w * 0.42f, h * 0.42f);
            g.FillRectangle(b, r.X + w * 0.12f, r.Y + h * 0.55f, w * 0.76f, h * 0.28f);
        }

        static void Orbit(Graphics g, RectangleF r)
        {
            using var core = new SolidBrush(Color.Turquoise);
            float w = r.Width, h = r.Height, d = w * 0.28f;
            g.FillEllipse(core, r.X + (w - d) / 2, r.Y + (h - d) / 2, d, d);
            using var ring = new Pen(Color.Turquoise, Math.Max(1.5f, w * 0.08f));
            var orbitRect = new RectangleF(r.X + w * 0.05f, r.Y + h * 0.25f, w * 0.9f, h * 0.5f);
            g.DrawEllipse(ring, orbitRect);
            using var moon = new SolidBrush(Color.PaleTurquoise);
            float md = w * 0.16f;
            g.FillEllipse(moon, orbitRect.Right - md / 2, orbitRect.Y + orbitRect.Height / 2 - md / 2, md, md);
        }

        static void Tree(Graphics g, RectangleF r)
        {
            using var trunk = new SolidBrush(Color.SaddleBrown);
            float w = r.Width, h = r.Height;
            g.FillRectangle(trunk, r.X + w * 0.42f, r.Y + h * 0.62f, w * 0.16f, h * 0.35f);
            using var foliage = new SolidBrush(Color.ForestGreen);
            g.FillEllipse(foliage, r.X + w * 0.1f, r.Y, w * 0.8f, h * 0.7f);
        }

        static void Dish(Graphics g, RectangleF r)
        {
            using var pen = new Pen(Color.Silver, Math.Max(2f, r.Width * 0.1f)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            float w = r.Width, h = r.Height;
            g.DrawArc(pen, r.X + w * 0.05f, r.Y + h * 0.15f, w * 0.75f, h * 0.6f, 200, 160);
            g.DrawLine(pen, r.X + w * 0.42f, r.Y + h * 0.45f, r.X + w * 0.85f, r.Y + h * 0.1f);
            using var dot = new SolidBrush(Color.Crimson);
            float d = w * 0.16f;
            g.FillEllipse(dot, r.X + w * 0.8f, r.Y + h * 0.02f, d, d);
            g.FillRectangle(pen.Brush, r.X + w * 0.35f, r.Y + h * 0.7f, w * 0.1f, h * 0.28f);
        }

        static void Pin(Graphics g, RectangleF r)
        {
            using var b = new SolidBrush(Color.Crimson);
            float w = r.Width, h = r.Height;
            PointF[] tear =
            {
                new(r.X + w * 0.5f, r.Y + h * 0.98f),
                new(r.X + w * 0.15f, r.Y + h * 0.42f),
                new(r.X + w * 0.85f, r.Y + h * 0.42f),
            };
            g.FillPolygon(b, tear);
            g.FillEllipse(b, r.X + w * 0.1f, r.Y, w * 0.8f, w * 0.8f);
            using var hole = new SolidBrush(Color.White);
            float d = w * 0.28f;
            g.FillEllipse(hole, r.X + (w - d) / 2, r.Y + w * 0.4f - d / 2, d, d);
        }

        static void Rocket(Graphics g, RectangleF r)
        {
            float w = r.Width, h = r.Height;
            using var body = new SolidBrush(Color.OrangeRed);
            PointF[] hull =
            {
                new(r.X + w * 0.5f, r.Y),
                new(r.X + w * 0.75f, r.Y + h * 0.6f),
                new(r.X + w * 0.25f, r.Y + h * 0.6f),
            };
            g.FillPolygon(body, hull);
            g.FillRectangle(body, r.X + w * 0.32f, r.Y + h * 0.45f, w * 0.36f, h * 0.35f);
            using var fin = new SolidBrush(Color.Crimson);
            g.FillPolygon(fin, new[] { new PointF(r.X + w * 0.32f, r.Y + h * 0.6f), new PointF(r.X + w * 0.15f, r.Y + h * 0.95f), new PointF(r.X + w * 0.32f, r.Y + h * 0.8f) });
            g.FillPolygon(fin, new[] { new PointF(r.X + w * 0.68f, r.Y + h * 0.6f), new PointF(r.X + w * 0.85f, r.Y + h * 0.95f), new PointF(r.X + w * 0.68f, r.Y + h * 0.8f) });
            using var window = new SolidBrush(Color.DodgerBlue);
            float d = w * 0.2f;
            g.FillEllipse(window, r.X + (w - d) / 2, r.Y + h * 0.28f, d, d);
        }

        static void Cube(Graphics g, RectangleF r)
        {
            float w = r.Width, h = r.Height;
            var top = new SolidBrush(Color.LightSteelBlue);
            var left = new SolidBrush(Color.SteelBlue);
            var right = new SolidBrush(Color.RoyalBlue);
            PointF c = new(r.X + w * 0.5f, r.Y + h * 0.22f);
            g.FillPolygon(top, new[] { c, new PointF(r.X + w * 0.08f, r.Y + h * 0.42f), new PointF(r.X + w * 0.5f, r.Y + h * 0.62f), new PointF(r.X + w * 0.92f, r.Y + h * 0.42f) });
            g.FillPolygon(left, new[] { new PointF(r.X + w * 0.08f, r.Y + h * 0.42f), new PointF(r.X + w * 0.5f, r.Y + h * 0.62f), new PointF(r.X + w * 0.5f, r.Y + h * 0.98f), new PointF(r.X + w * 0.08f, r.Y + h * 0.78f) });
            g.FillPolygon(right, new[] { new PointF(r.X + w * 0.92f, r.Y + h * 0.42f), new PointF(r.X + w * 0.5f, r.Y + h * 0.62f), new PointF(r.X + w * 0.5f, r.Y + h * 0.98f), new PointF(r.X + w * 0.92f, r.Y + h * 0.78f) });
            top.Dispose(); left.Dispose(); right.Dispose();
        }

        static void Wand(Graphics g, RectangleF r)
        {
            float w = r.Width, h = r.Height;
            using var pen = new Pen(Color.MediumOrchid, Math.Max(2f, w * 0.12f)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLine(pen, r.X + w * 0.15f, r.Y + h * 0.95f, r.X + w * 0.75f, r.Y + h * 0.35f);
            using var star = new SolidBrush(Color.Gold);
            DrawStar(g, star, r.X + w * 0.72f, r.Y + h * 0.22f, w * 0.26f);
            using var sparkle = new SolidBrush(Color.Gold);
            DrawStar(g, sparkle, r.X + w * 0.15f, r.Y + h * 0.12f, w * 0.14f);
        }

        static void DrawStar(Graphics g, Brush b, float cx, float cy, float size)
        {
            var pts = new PointF[8];
            for (int i = 0; i < 8; i++)
            {
                double ang = Math.PI / 4 * i - Math.PI / 2;
                float rad = (i % 2 == 0) ? size / 2 : size / 5;
                pts[i] = new PointF(cx + (float)(rad * Math.Cos(ang)), cy + (float)(rad * Math.Sin(ang)));
            }
            g.FillPolygon(b, pts);
        }

        static void World(Graphics g, RectangleF r)
        {
            float w = r.Width, h = r.Height;
            using var ocean = new SolidBrush(Color.DodgerBlue);
            var disc = new RectangleF(r.X + w * 0.06f, r.Y + h * 0.06f, w * 0.88f, h * 0.88f);
            g.FillEllipse(ocean, disc);
            using var land = new SolidBrush(Color.SeaGreen);
            g.FillEllipse(land, disc.X + disc.Width * 0.1f, disc.Y + disc.Height * 0.15f, disc.Width * 0.35f, disc.Height * 0.3f);
            g.FillEllipse(land, disc.X + disc.Width * 0.5f, disc.Y + disc.Height * 0.5f, disc.Width * 0.4f, disc.Height * 0.35f);
            using var pen = new Pen(Color.FromArgb(120, Color.White), 1f);
            g.DrawEllipse(pen, disc);
        }

        static void Flag(Graphics g, RectangleF r)
        {
            float w = r.Width, h = r.Height;
            using var pole = new Pen(Color.Gainsboro, Math.Max(1.5f, w * 0.08f));
            g.DrawLine(pole, r.X + w * 0.15f, r.Y, r.X + w * 0.15f, r.Y + h);
            int cols = 3, rows = 2;
            float fw = w * 0.7f, fh = h * 0.55f, cw = fw / cols, ch = fh / rows;
            for (int cRow = 0; cRow < rows; cRow++)
                for (int cCol = 0; cCol < cols; cCol++)
                {
                    bool blackSq = (cRow + cCol) % 2 == 0;
                    using var sq = new SolidBrush(blackSq ? Color.Black : Color.White);
                    g.FillRectangle(sq, r.X + w * 0.2f + cCol * cw, r.Y + h * 0.05f + cRow * ch, cw, ch);
                }
        }

        static void Target(Graphics g, RectangleF r)
        {
            float w = r.Width, h = r.Height;
            var colors = new[] { Color.Crimson, Color.White, Color.Crimson };
            float d = Math.Min(w, h);
            for (int i = 0; i < 3; i++)
            {
                float s = d * (1f - i * 0.32f);
                using var b = new SolidBrush(colors[i]);
                g.FillEllipse(b, r.X + (w - s) / 2, r.Y + (h - s) / 2, s, s);
            }
        }

        static void Crosshair(Graphics g, RectangleF r)
        {
            float w = r.Width, h = r.Height;
            using var pen = new Pen(Color.Tomato, Math.Max(1.5f, w * 0.09f));
            var ring = new RectangleF(r.X + w * 0.12f, r.Y + h * 0.12f, w * 0.76f, h * 0.76f);
            g.DrawEllipse(pen, ring);
            g.DrawLine(pen, r.X + w * 0.5f, r.Y, r.X + w * 0.5f, r.Y + h * 0.18f);
            g.DrawLine(pen, r.X + w * 0.5f, r.Y + h * 0.82f, r.X + w * 0.5f, r.Y + h);
            g.DrawLine(pen, r.X, r.Y + h * 0.5f, r.X + w * 0.18f, r.Y + h * 0.5f);
            g.DrawLine(pen, r.X + w * 0.82f, r.Y + h * 0.5f, r.X + w, r.Y + h * 0.5f);
            using var dot = new SolidBrush(Color.Tomato);
            float d = w * 0.12f;
            g.FillEllipse(dot, r.X + (w - d) / 2, r.Y + (h - d) / 2, d, d);
        }

        static void Map(Graphics g, RectangleF r)
        {
            float w = r.Width, h = r.Height;
            using var paper = new SolidBrush(Color.Khaki);
            g.FillRectangle(paper, r.X + w * 0.06f, r.Y + h * 0.08f, w * 0.88f, h * 0.84f);
            using var fold = new Pen(Color.DarkKhaki, 1f);
            g.DrawLine(fold, r.X + w * 0.38f, r.Y + h * 0.08f, r.X + w * 0.38f, r.Y + h * 0.92f);
            g.DrawLine(fold, r.X + w * 0.68f, r.Y + h * 0.08f, r.X + w * 0.68f, r.Y + h * 0.92f);
            using var road = new Pen(Color.SteelBlue, Math.Max(1.5f, w * 0.07f)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            var path = new GraphicsPath();
            path.AddBezier(r.X + w * 0.15f, r.Y + h * 0.8f, r.X + w * 0.35f, r.Y + h * 0.4f, r.X + w * 0.55f, r.Y + h * 0.75f, r.X + w * 0.85f, r.Y + h * 0.25f);
            g.DrawPath(road, path);
            using var pin = new SolidBrush(Color.Crimson);
            float d = w * 0.14f;
            g.FillEllipse(pin, r.X + w * 0.8f, r.Y + h * 0.15f, d, d);
        }

        static void Axes3D(Graphics g, RectangleF r)
        {
            float w = r.Width, h = r.Height;
            PointF origin = new(r.X + w * 0.32f, r.Y + h * 0.68f);
            void Arrow(PointF to, Color c)
            {
                using var pen = new Pen(c, Math.Max(1.5f, w * 0.1f)) { EndCap = LineCap.ArrowAnchor, StartCap = LineCap.Round };
                g.DrawLine(pen, origin, to);
            }
            Arrow(new PointF(r.X + w * 0.95f, r.Y + h * 0.68f), Color.Tomato);           // X
            Arrow(new PointF(r.X + w * 0.32f, r.Y), Color.MediumSeaGreen);                // Y
            Arrow(new PointF(r.X + w * 0.02f, r.Y + h * 0.98f), Color.DodgerBlue);         // Z
        }

        static void Route(Graphics g, RectangleF r)
        {
            float w = r.Width, h = r.Height;
            using var pen = new Pen(Color.MediumSeaGreen, Math.Max(1.5f, w * 0.06f)) { DashStyle = DashStyle.Dot };
            g.DrawLine(pen, r.X + w * 0.12f, r.Y + h * 0.85f, r.X + w * 0.85f, r.Y + h * 0.18f);
            using var dot = new SolidBrush(Color.MediumSeaGreen);
            float d = w * 0.16f;
            g.FillEllipse(dot, r.X + w * 0.08f, r.Y + h * 0.78f, d, d);
            g.FillEllipse(dot, r.X + w * 0.42f, r.Y + h * 0.44f, d, d);
            using var pin = new SolidBrush(Color.Crimson);
            PointF[] tear =
            {
                new(r.X + w * 0.85f, r.Y + h * 0.02f),
                new(r.X + w * 0.62f, r.Y + h * 0.32f),
                new(r.X + w * 0.98f, r.Y + h * 0.32f),
            };
            g.FillPolygon(pin, tear);
        }

        static void Wireframe(Graphics g, RectangleF r)
        {
            float w = r.Width, h = r.Height;
            using var pen = new Pen(Color.Orange, Math.Max(1.5f, w * 0.07f));
            g.DrawLine(pen, r.X + w * 0.08f, r.Y + h * 0.85f, r.X + w * 0.55f, r.Y + h * 0.15f);
            g.DrawLine(pen, r.X + w * 0.55f, r.Y + h * 0.15f, r.X + w * 0.92f, r.Y + h * 0.60f);
            g.DrawLine(pen, r.X + w * 0.15f, r.Y + h * 0.35f, r.X + w * 0.85f, r.Y + h * 0.25f);
        }

        // A simple perspective floor-grid glyph (converging lines, like a horizon-ward ground plane) -
        // distinct from Wireframe's freeform roofline shape, reads as "grid on the ground."
        static void GridIcon(Graphics g, RectangleF r)
        {
            float w = r.Width, h = r.Height;
            using var pen = new Pen(Color.DeepSkyBlue, Math.Max(1.2f, w * 0.05f));
            float vanishX = r.X + w * 0.5f, vanishY = r.Y + h * 0.15f;
            for (int i = 0; i <= 3; i++)
            {
                float bx = r.X + w * (0.05f + i * 0.3f);
                g.DrawLine(pen, bx, r.Y + h * 0.95f, vanishX + (bx - vanishX) * 0.25f, vanishY + h * 0.5f);
            }
            for (int i = 1; i <= 3; i++)
            {
                float t = i / 3.5f;
                float y = vanishY + h * 0.5f + (r.Y + h * 0.95f - (vanishY + h * 0.5f)) * t;
                float spread = 0.05f + (0.95f - 0.05f - 0.1f) * t;
                g.DrawLine(pen, r.X + w * 0.5f - w * spread, y, r.X + w * 0.5f + w * spread, y);
            }
        }
    }

    static class GraphicsExtensions
    {
        public static void FillRoundedRect(this Graphics g, Brush b, RectangleF rect, float radius)
        {
            using var path = new GraphicsPath();
            float d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            g.FillPath(b, path);
        }
    }
}

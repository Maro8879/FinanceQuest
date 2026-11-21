using System.Collections.Generic;
using UnityEngine;

namespace FinanceQuest
{
    // Rodzaj trofeum: każdy kurs ma figurkę w kształcie swojego tematu (dobieraną po ilustracji kursu).
    public enum TrophyKind { Cup, Start, Master, Piggy, Candles, Phone, Sprout, Sapling, Tree }

    // Trofea. Projekt w polu 40 x 46 jednostek (oś Y w dół).
    // Figurki: złota figurka na cokole w kolorze kursu. Niezdobyte: szara sylwetka tej samej figurki z kłódką,
    // więc od razu widać, o co się walczy. Cup = klasyczny puchar dla kursów bez własnej figurki.
    public static class TrophyArt
    {
        public const float BoxW = 40f, BoxH = 46f;
        const float PixelsPerUnit = 3f;

        static readonly Color LockedBody = Palette.Hex("CFC9BD");
        static readonly Color LockedRim = Palette.Hex("BDB6A8");

        public static Sprite Earned(Color body, Color rim, Color star)
        {
            return Get("trophy_" + ColorUtility.ToHtmlStringRGB(body) + "_" + ColorUtility.ToHtmlStringRGB(star), body, rim, star, false);
        }

        public static Sprite Locked()
        {
            return Get("trophy_locked", LockedBody, LockedRim, Color.white, true);
        }

        // Figurka pasująca do kursu – po jego ilustracji na karcie.
        public static TrophyKind ForCourse(CourseData c)
        {
            switch (c.Art)
            {
                case CourseArt.Portfolio: return TrophyKind.Piggy;
                case CourseArt.Candles:
                case CourseArt.Magnifier: return TrophyKind.Candles;
                case CourseArt.Platform:
                case CourseArt.Laptop: return TrophyKind.Phone;
                case CourseArt.Sprout: return TrophyKind.Sprout;
                case CourseArt.Sapling: return TrophyKind.Sapling;
                case CourseArt.Tree: return TrophyKind.Tree;
                default: return TrophyKind.Cup;
            }
        }

        // Zdobyte trofeum danego rodzaju; baseColor = kolor cokołu (kolor kursu).
        public static Sprite Earned(TrophyKind kind, Color baseColor)
        {
            if (kind == TrophyKind.Cup) return Earned(baseColor, Palette.Darker(baseColor, 0.78f), Palette.Accent);
            string key = "trophy2_" + kind + "_" + ColorUtility.ToHtmlStringRGB(baseColor);
            return DrawnPictures.Get(key, () => DrawFigure(key, kind, baseColor, false));
        }

        public static Sprite Locked(TrophyKind kind)
        {
            if (kind == TrophyKind.Cup) return Locked();
            string key = "trophy2_" + kind + "_locked";
            return DrawnPictures.Get(key, () => DrawFigure(key, kind, LockedBody, true));
        }

        static Sprite Get(string key, Color body, Color rim, Color emblem, bool locked)
        {
            return DrawnPictures.Get(key, () => Draw(key, body, rim, emblem, locked));
        }

        static Sprite Draw(string key, Color body, Color rim, Color emblem, bool locked)
        {
            var c = new ColorCanvas(BoxW, BoxH, PixelsPerUnit);

            // Uszy pucharu – krzywe jak w projekcie, narysowane odcinkami.
            Curve(c, V(7, 8), V(2, 8), V(2.5f, 13), 1.5f, rim);
            Curve(c, V(2.5f, 13), V(3.5f, 19), V(10, 20), 1.5f, rim);
            Curve(c, V(33, 8), V(38, 8), V(37.5f, 13), 1.5f, rim);
            Curve(c, V(37.5f, 13), V(36.5f, 19), V(30, 20), 1.5f, rim);

            // Czasza: prostokąt u góry, zaokrąglony dół.
            var bowl = new List<Vector2> { V(7, 4), V(33, 4), V(33, 12) };
            AddQuad(bowl, V(33, 12), V(33, 26), V(20, 28));
            AddQuad(bowl, V(20, 28), V(7, 26), V(7, 12));
            var pts = bowl.ToArray();
            c.Fill(p => SdfCanvas.Polygon(p, pts), body, 7, 4, 33, 28);

            Rect(c, 7, 4, 26, 4, 2, rim);        // brzeg
            Rect(c, 17.5f, 27, 5, 7, 0, rim);    // nóżka
            Rect(c, 11, 34, 18, 5, 2, body);     // podstawa
            Rect(c, 8, 39, 24, 5, 2.5f, rim);

            if (!locked)
            {
                var star = new Vector2[10];
                for (int i = 0; i < 10; i++)
                {
                    float a = -Mathf.PI / 2f + i * Mathf.PI / 5f, r = i % 2 == 0 ? 6.2f : 2.7f;
                    star[i] = V(20 + Mathf.Cos(a) * r, 15.5f + Mathf.Sin(a) * r);
                }
                c.Fill(p => SdfCanvas.Polygon(p, star), emblem, 13, 9, 27, 22);
            }
            else
            {
                var lockC = new Color(1f, 1f, 1f, 0.85f);
                Rect(c, 14.5f, 13, 11, 8, 2, lockC);
                // Kabłąk kłódki = górna połówka pierścienia.
                var o = V(20, 12.5f);
                c.Fill(p => Mathf.Max(Mathf.Abs((p - o).magnitude - 3.2f) - 1f, p.y - o.y), lockC, 15, 8, 25, 13.5f);
            }

            return Sprite.Create(c.ToTexture("fq_" + key), new UnityEngine.Rect(0, 0, c.Width, c.Height),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }

        // ---------- figurki ----------

        static readonly Color G = Palette.Accent, GD = Palette.Hex("C9A200"), GL = Palette.Hex("FFD84D");

        static Sprite DrawFigure(string key, TrophyKind kind, Color baseColor, bool locked)
        {
            var c = new ColorCanvas(BoxW, BoxH, PixelsPerUnit);
            // Niezdobyte: ta sama figurka w szarościach (bez złota), na wierzchu kłódka.
            Color g = locked ? LockedBody : G, gd = locked ? LockedRim : GD, gl = locked ? LockedBody : GL;
            Color rim = locked ? LockedRim : Palette.Darker(baseColor, 0.78f);

            if (kind != TrophyKind.Master) Pedestal(c, baseColor, rim, !locked);
            switch (kind)
            {
                case TrophyKind.Start: Flag(c, g, gd, gl); break;
                case TrophyKind.Master: Master(c, g, gd, gl, locked); Rect(c, 6, 38, 28, 7, 2, rim); Rect(c, 9, 33, 22, 6, 2, baseColor); break;
                case TrophyKind.Piggy: Piggy(c, g, gd, gl); break;
                case TrophyKind.Candles: Candles(c, g, gd, gl); break;
                case TrophyKind.Phone: Phone(c, g, gd, gl, locked ? LockedRim : Palette.Darker(baseColor, 0.55f)); break;
                case TrophyKind.Sprout: Sprout(c, g, gd, gl); break;
                case TrophyKind.Sapling: Sapling(c, g, gd, gl); break;
                case TrophyKind.Tree: Tree(c, g, gd, gl); break;
            }

            if (locked)
            {
                var w = new Color(1f, 1f, 1f, 0.92f);
                Rect(c, 15, 22, 10, 8, 1.5f, w);
                Line(c, 17, 22.5f, 17, 19, 0.8f, w); Line(c, 17, 19, 23, 19, 0.8f, w); Line(c, 23, 19, 23, 22.5f, 0.8f, w);
                Circle(c, V(20, 26), 1.2f, LockedRim);
            }
            return Sprite.Create(c.ToTexture("fq_" + key), new UnityEngine.Rect(0, 0, c.Width, c.Height),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }

        // Dwustopniowy cokół w kolorze kursu (z jaśniejszą górną krawędzią).
        static void Pedestal(ColorCanvas c, Color col, Color rim, bool shine)
        {
            Rect(c, 6, 38, 28, 7, 2, rim);
            Rect(c, 9, 33, 22, 6, 2, col);
            if (shine) Rect(c, 9, 33, 22, 2, 1, Color.Lerp(col, Color.white, 0.15f));
        }

        static void Flag(ColorCanvas c, Color g, Color gd, Color gl)
        {
            // Maszt dokładnie na środku cokołu (x = 20), płachta powiewa w prawo.
            Line(c, 20, 33, 20, 4, 1f, gd);
            Poly(c, new[] { V(21, 5), V(36, 10), V(21, 16) }, g);
            Circle(c, V(20, 4), 2f, gl);
        }

        // Skarbonka od przodu: okrągłe ciało, dwa uszy, duży ryjek, szczelina na monety.
        static void Piggy(ColorCanvas c, Color g, Color gd, Color gl)
        {
            var tail = new Vector2[15];
            tail[0] = V(6, 20);
            for (int k = 0; k < 14; k++)
            {
                float a = (-20f - k * 25f) * Mathf.Deg2Rad, r = 2f * (1f - k / 18f);
                tail[k + 1] = V(4f + Mathf.Cos(a) * r, 19.8f + Mathf.Sin(a) * r);
            }
            for (int k = 1; k < tail.Length; k++) Line(c, tail[k - 1].x, tail[k - 1].y, tail[k].x, tail[k].y, 0.55f, gd);

            Ellipse(c, V(20, 20), 14, 11, g);
            Poly(c, new[] { V(10, 12), V(11, 5.5f), V(16, 10) }, g);
            Poly(c, new[] { V(24, 10), V(29, 5.5f), V(30, 12) }, g);
            Poly(c, new[] { V(11.2f, 11), V(11.8f, 7.5f), V(14.5f, 9.8f) }, gd);
            Poly(c, new[] { V(25.5f, 9.8f), V(28.2f, 7.5f), V(28.8f, 11) }, gd);
            Rect(c, 15.5f, 20, 9, 6, 3, gl);
            Circle(c, V(18.5f, 23), 0.9f, gd); Circle(c, V(21.5f, 23), 0.9f, gd);
            Circle(c, V(15, 17), 1.2f, gd); Circle(c, V(25, 17), 1.2f, gd);
            Rect(c, 16, 10.5f, 8, 1.8f, 0.9f, gd);
            Rect(c, 12, 28, 5, 5, 2, g); Rect(c, 23, 28, 5, 5, 2, g);
        }

        // Świece wyśrodkowane nad cokołem, stoją na złotej listwie.
        static void Candles(ColorCanvas c, Color g, Color gd, Color gl)
        {
            float[] xs = { 7, 14, 21, 28 };
            float[,] d = { { 20, 8, 15 }, { 13, 13, 8 }, { 17, 9, 12 }, { 6, 15, 3 } };   // góra korpusu, wysokość, góra knota
            for (int i = 0; i < 4; i++)
            {
                Line(c, xs[i] + 2.5f, d[i, 2], xs[i] + 2.5f, 31, 0.5f, gd);
                Rect(c, xs[i], d[i, 0], 5, d[i, 1], 1.2f, i % 2 == 0 ? g : gl);
            }
            Rect(c, 7, 30.5f, 26, 2.5f, 1, gd);
        }

        static void Phone(ColorCanvas c, Color g, Color gd, Color gl, Color screen)
        {
            Rect(c, 11, 3, 18, 30, 3.5f, g);
            Rect(c, 13, 7, 14, 20, 1.5f, screen);
            Line(c, 14.5f, 22, 18, 18, 0.8f, gl); Line(c, 18, 18, 21, 20, 0.8f, gl); Line(c, 21, 20, 25.5f, 12, 0.8f, gl);
            Rect(c, 17, 29, 6, 1.6f, 0.8f, gd);
        }

        static void Sprout(ColorCanvas c, Color g, Color gd, Color gl)
        {
            Line(c, 20, 33, 20, 18, 1f, gd);
            Leaf(c, V(20, 22), 13, -160, 5, g);
            Leaf(c, V(20, 19), 14, -25, 5.5f, gl);
        }

        static void Sapling(ColorCanvas c, Color g, Color gd, Color gl)
        {
            Line(c, 20, 33, 20, 14, 1.2f, gd);
            Line(c, 20, 24, 15, 19, 0.8f, gd);
            Circle(c, V(20, 10), 7, g); Circle(c, V(14, 14), 5.5f, g); Circle(c, V(26, 14), 5.5f, g); Circle(c, V(20, 17), 5.5f, g);
            Circle(c, V(17, 9), 2.6f, gl);
        }

        static void Tree(ColorCanvas c, Color g, Color gd, Color gl)
        {
            Poly(c, new[] { V(17.5f, 33), V(22.5f, 33), V(21.5f, 18), V(18.5f, 18) }, gd);
            Circle(c, V(20, 8), 8, g); Circle(c, V(11, 13), 6.5f, g); Circle(c, V(29, 13), 6.5f, g);
            Circle(c, V(15, 18), 6, g); Circle(c, V(25, 18), 6, g); Circle(c, V(20, 15), 7, g);
            Circle(c, V(16, 8), 3.4f, gl); Circle(c, V(26, 12), 2.4f, gl);
        }

        // Mistrz: jedyny „prawdziwy” puchar – z gwiazdą i iskierkami.
        static void Master(ColorCanvas c, Color g, Color gd, Color gl, bool locked)
        {
            Line(c, 10, 9, 5, 10, 0.9f, gd); Line(c, 5, 10, 6, 17, 0.9f, gd); Line(c, 6, 17, 12, 20, 0.9f, gd);
            Line(c, 30, 9, 35, 10, 0.9f, gd); Line(c, 35, 10, 34, 17, 0.9f, gd); Line(c, 34, 17, 28, 20, 0.9f, gd);
            Poly(c, new[] { V(9, 6), V(31, 6), V(29, 18), V(24, 25), V(16, 25), V(11, 18) }, g);
            Rect(c, 9, 5, 22, 3, 1.5f, gl);
            Rect(c, 18, 25, 4, 5, 1, gd);
            Rect(c, 13, 30, 14, 3, 1.5f, g);
            var star = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float a = -Mathf.PI / 2f + i * Mathf.PI / 5f, r = i % 2 == 0 ? 5f : 2.2f;
                star[i] = V(20 + Mathf.Cos(a) * r, 14 + Mathf.Sin(a) * r);
            }
            if (!locked) Poly(c, star, Color.white);   // na zablokowanym gwiazdę zastępuje kłódka
            Sparkle(c, 5, 27, 4, gl); Sparkle(c, 35, 24, 3.2f, gl); Sparkle(c, 33, 33, 2.2f, gl);
        }

        static void Sparkle(ColorCanvas c, float x, float y, float r, Color col)
        {
            float q = r * 0.3f;
            Poly(c, new[] { V(x, y - r), V(x + q, y - q), V(x + r, y), V(x + q, y + q), V(x, y + r), V(x - q, y + q), V(x - r, y), V(x - q, y - q) }, col);
        }

        // ---------- kształty ----------

        static void Line(ColorCanvas c, float x0, float y0, float x1, float y1, float r, Color col)
        {
            Vector2 a = V(x0, y0), b = V(x1, y1);
            c.Fill(p => SdfCanvas.Capsule(p, a, b, r), col,
                Mathf.Min(x0, x1) - r, Mathf.Min(y0, y1) - r, Mathf.Max(x0, x1) + r, Mathf.Max(y0, y1) + r);
        }

        static void Circle(ColorCanvas c, Vector2 o, float r, Color col)
        {
            c.Fill(p => SdfCanvas.Circle(p, o, r), col, o.x - r, o.y - r, o.x + r, o.y + r);
        }

        static void Ellipse(ColorCanvas c, Vector2 o, float rx, float ry, Color col)
        {
            float m = Mathf.Min(rx, ry);
            c.Fill(p => (V((p.x - o.x) / rx, (p.y - o.y) / ry).magnitude - 1f) * m, col, o.x - rx, o.y - ry, o.x + rx, o.y + ry);
        }

        static void Poly(ColorCanvas c, Vector2[] pts, Color col)
        {
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            foreach (var q in pts) { x0 = Mathf.Min(x0, q.x); y0 = Mathf.Min(y0, q.y); x1 = Mathf.Max(x1, q.x); y1 = Mathf.Max(y1, q.y); }
            c.Fill(p => SdfCanvas.Polygon(p, pts), col, x0, y0, x1, y1);
        }

        static void Leaf(ColorCanvas c, Vector2 b, float length, float deg, float width, Color col)
        {
            float a = deg * Mathf.Deg2Rad;
            var u = V(Mathf.Cos(a), Mathf.Sin(a));
            var n = V(-u.y, u.x);
            const int Steps = 16;
            // Podstawa i czubek tylko raz – powtórzony punkt (odcinek zerowej długości) psuje liczenie odległości od wielokąta.
            var pts = new Vector2[Steps * 2];
            for (int i = 0; i <= Steps; i++)
            {
                float t = i / (float)Steps, w = Mathf.Sin(Mathf.PI * t) * width;
                pts[i] = b + u * (length * t) + n * w;
                if (i > 0 && i < Steps) pts[Steps * 2 - i] = b + u * (length * t) - n * w;
            }
            Poly(c, pts, col);
        }

        static Vector2 V(float x, float y) { return new Vector2(x, y); }

        static Vector2 Quad(Vector2 a, Vector2 b, Vector2 c, float t)
        {
            float u = 1f - t;
            return a * (u * u) + b * (2f * u * t) + c * (t * t);
        }

        static void AddQuad(List<Vector2> list, Vector2 a, Vector2 b, Vector2 c)
        {
            for (int i = 1; i <= 8; i++) list.Add(Quad(a, b, c, i / 8f));
        }

        static void Curve(ColorCanvas c, Vector2 a, Vector2 b, Vector2 d, float r, Color col)
        {
            Vector2 prev = a;
            for (int i = 1; i <= 8; i++)
            {
                Vector2 cur = Quad(a, b, d, i / 8f), p0 = prev;
                c.Fill(p => SdfCanvas.Capsule(p, p0, cur, r), col,
                    Mathf.Min(p0.x, cur.x) - r, Mathf.Min(p0.y, cur.y) - r, Mathf.Max(p0.x, cur.x) + r, Mathf.Max(p0.y, cur.y) + r);
                prev = cur;
            }
        }

        static void Rect(ColorCanvas c, float x, float y, float w, float h, float r, Color col)
        {
            var center = V(x + w * 0.5f, y + h * 0.5f);
            var size = V(w, h);
            c.Fill(p => SdfCanvas.RoundRect(p, center, size, r), col, x, y, x + w, y + h);
        }
    }
}

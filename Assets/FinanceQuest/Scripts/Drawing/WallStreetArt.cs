using UnityEngine;

namespace FinanceQuest
{
    // Tło „Myśli na dziś”: nocna Wall Street w perspektywie z dołu – gmach giełdy z kolumnadą
    // (ogólny styl, bez napisów i logo), za nim wieżowce typowe dla tej ulicy, na balkonie 3 flagi USA.
    // Rysunek w polu 358 x 230 jednostek (oś Y w dół), jedna tekstura = jedno wywołanie rysowania.
    public static class WallStreetArt
    {
        public const float W = 358f, H = 230f;
        const float PPU = 2.5f;                       // pikseli na jednostkę – ostro na telefonie, a liczy się szybko
        const float VPX = 179f, VPY = -620f, G = 230f; // punkt zbiegu pionów (widok z dołu) i linia ziemi

        // Kolory wersji nocnej.
        static readonly Color Sky = Palette.Hex("0B2F37");
        static readonly Color Glass = Palette.Hex("123F49"), GlassHi = Palette.Hex("1B5260"), Mullion = A(Palette.Hex("24606C"), 0.8f);
        static readonly Color White = Palette.Hex("2A5E68"), Ribbon = Palette.Hex("174650");
        static readonly Color StoneT = Palette.Hex("24555E"), StoneTSh = Palette.Hex("1C4952"), WinT = Palette.Hex("173F48");
        static readonly Color Deco = Palette.Hex("1E4F58"), DecoSh = Palette.Hex("18434C"), Copper = Palette.Hex("3F7F74"), CopperSh = Palette.Hex("2F6A60");
        static readonly Color StoneF = Palette.Hex("D9CBAA"), ShadeF = Palette.Hex("B8A77F"), DarkF = Palette.Hex("3C3420");
        static readonly Color Lit = Palette.Accent;
        static readonly Color Red = Palette.Hex("B83A33"), FlagWhite = Palette.Hex("F4F1EA"), Navy = Palette.Hex("24406E");
        static readonly Color Pole = Palette.Hex("7B8284"), Finial = Palette.Hex("D8B45A"), Bracket = Palette.Hex("6E7577"), Bolt = Palette.Hex("A9B0B2");

        // Duży rysunek (ok. 0,8 s liczenia) – w grze wczytywany z PNG po „Bake Drawn Pictures”.
        public static Sprite Night { get { return DrawnPictures.Get("wallstreet_night", DrawNight); } }

        static Sprite DrawNight()
        {
            {
                var c = new ColorCanvas(W, H, PPU);
                c.Clear(Sky);
                DecoTower(c, 214, 300, 70);
                Masonry(c, 36, 126, 64, 2);
                Masonry(c, 160, 198, 92, 5);
                GlassTower(c, -80, 66, 20);
                WhiteTower(c, 290, 440, 26);
                Facade(c);
                Ledge(c);
                foreach (float x in new[] { 124f, 172f, 220f })
                {
                    UsFlag(c, x, 195f, 26f, 28f, x * 0.02f);
                    Rect(c, x - 3, 193, 6, 5, 1, Bracket);
                    Circle(c, V(x, 195), 1.4f, Bolt);
                }
                return Sprite.Create(c.ToTexture("fq_wallstreet_night"), new UnityEngine.Rect(0, 0, c.Width, c.Height),
                    new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            }
        }

        // ---------- perspektywa ----------

        // (x przy ziemi, y ekranowe) -> ekran: piony zbiegają się do punktu nad gmachem.
        static Vector2 M(float X, float y)
        {
            float t = (G - y) / (G - VPY);
            return V(X + (VPX - X) * t, y);
        }

        static void Box(ColorCanvas c, float X0, float X1, float y0, float y1, Color col)
        {
            Poly(c, new[] { M(X0, y1), M(X1, y1), M(X1, y0), M(X0, y0) }, col);
        }

        static void PLine(ColorCanvas c, float X0, float y0, float X1, float y1, float r, Color col)
        {
            Line(c, M(X0, y0), M(X1, y1), r, col);
        }

        // ---------- wieżowce ----------

        static void GlassTower(ColorCanvas c, float X0, float X1, float top)
        {
            Box(c, X0, X1, top, G, Glass);
            Box(c, X0 + (X1 - X0) * .55f, X0 + (X1 - X0) * .72f, top, G, GlassHi);   // pas odbicia
            for (float X = X0; X <= X1; X += 8) PLine(c, X, top, X, G, 0.4f, Mullion);
            for (float y = top; y <= G; y += 7) PLine(c, X0, y, X1, y, 0.4f, Mullion);
            Box(c, X0, X1, top - 3, top, Mullion);
        }

        static void WhiteTower(ColorCanvas c, float X0, float X1, float top)
        {
            Box(c, X0, X1, top, G, White);
            for (float y = top + 6; y < G; y += 11) Box(c, X0 + 3, X1 - 3, y, y + 5, Ribbon);   // poziome pasy okien
            for (float X = X0 + 14; X < X1 - 4; X += 14) PLine(c, X, top, X, G, 0.6f, White);
        }

        // Przedwojenny kamienny wieżowiec z uskokami i rzędami okien (część świeci).
        static void Masonry(ColorCanvas c, float X0, float X1, float top, int seed)
        {
            float w = X1 - X0;
            var tiers = new[]
            {
                new Vector4(X0, X1, top + 40, G),
                new Vector4(X0 + w * .12f, X1 - w * .12f, top + 16, top + 40),
                new Vector4(X0 + w * .26f, X1 - w * .26f, top, top + 16),
            };
            foreach (var t in tiers)
            {
                Box(c, t.x, t.y, t.z, t.w, StoneT);
                Box(c, t.x - 1.5f, t.y + 1.5f, t.z, t.z + 3, StoneTSh);   // gzyms
                int col = 0;
                for (float X = t.x + 4; X + 4 < t.y; X += 7, col++)
                {
                    int row = 0;
                    for (float y = t.z + 6; y + 6 < t.w; y += 9, row++)
                    {
                        bool on = (col * 5 + row * 3 + seed) % 6 == 0 && y > 96;   // pod cytatem okna nie świecą
                        Box(c, X, X + 3.6f, y, y + 5.5f, on ? Lit : WinT);
                    }
                }
            }
        }

        // Wieża art déco: uskoki, pionowe pilastry, miedziana piramida z iglicą.
        static void DecoTower(ColorCanvas c, float X0, float X1, float top)
        {
            float w = X1 - X0;
            var tiers = new[]
            {
                new Vector4(X0, X1, top + 56, G),
                new Vector4(X0 + w * .13f, X1 - w * .13f, top + 36, top + 56),
                new Vector4(X0 + w * .26f, X1 - w * .26f, top + 22, top + 36),
            };
            foreach (var t in tiers)
            {
                Box(c, t.x, t.y, t.z, t.w, Deco);
                for (float X = t.x + 3; X < t.y - 2; X += 6) Box(c, X, X + 2.2f, t.z + 3, t.w, DecoSh);
                Box(c, t.x - 1, t.y + 1, t.z, t.z + 2.5f, DecoSh);
            }
            float a = X0 + w * .26f, b = X1 - w * .26f, m = (a + b) * 0.5f;
            Poly(c, new[] { M(a, top + 22), M(m, top + 22), M(m, top - 8) }, Copper);
            Poly(c, new[] { M(m, top + 22), M(b, top + 22), M(m, top - 8) }, CopperSh);
            PLine(c, m, top - 8, m, top - 30, 0.8f, CopperSh);
            Circle(c, M(m, top - 30), 1.8f, Lit);
        }

        // ---------- gmach giełdy ----------

        static void Facade(ColorCanvas c)
        {
            Poly(c, new[] { V(62, 230), V(296, 230), V(288, 146), V(70, 146) }, StoneF);
            Poly(c, new[] { V(92, 212), V(266, 212), V(262, 152), V(96, 152) }, DarkF);          // wnęka za kolumnami
            for (int i = 0; i < 6; i++)
            {
                float bx = 96 + i * 32, tx = 99 + i * 31;
                Poly(c, new[] { V(bx, 212), V(bx + 16, 212), V(tx + 14, 152), V(tx, 152) }, StoneF);
                Poly(c, new[] { V(bx + 5, 210), V(bx + 6.5f, 210), V(tx + 5.6f, 154), V(tx + 4.4f, 154) }, ShadeF);   // żłobkowanie
                Poly(c, new[] { V(bx + 10, 210), V(bx + 11.5f, 210), V(tx + 10.2f, 154), V(tx + 9, 154) }, ShadeF);
                Poly(c, new[] { V(tx - 3, 152), V(tx + 17, 152), V(tx + 16, 146), V(tx - 2, 146) }, ShadeF);           // głowica
            }
            Poly(c, new[] { V(66, 146), V(292, 146), V(288, 136), V(70, 136) }, StoneF);         // belkowanie
            Poly(c, new[] { V(66, 140), V(292, 140), V(291, 138), V(67, 138) }, ShadeF);
            for (int k = 0; k < 30; k++) Rect(c, 72 + k * 7.2f, 141.5f, 3, 2.5f, 0, ShadeF);     // ząbki gzymsu
            Poly(c, new[] { V(72, 136), V(286, 136), V(179, 108) }, StoneF);                     // tympanon
            Poly(c, new[] { V(90, 133), V(268, 133), V(179, 114) }, ShadeF);
            float[] fig = { 5, 8, 11, 14, 17, 14, 11, 8, 5 };                                     // grupa rzeźb
            for (int k = 0; k < fig.Length; k++) Rect(c, 147 + k * 7.5f, 132 - fig[k], 4.5f, fig[k], 2, A(StoneF, 0.85f));
            Poly(c, new[] { V(62, 230), V(296, 230), V(296, 214), V(62, 214) }, ShadeF);          // cokół
            for (int k = 0; k < 7; k++)
            {
                float x = 76 + k * 32;
                Rect(c, x, 219, 14, 11, 2, Lit);           // podświetlone wejścia
                Rect(c, x - 2, 216, 18, 2.5f, 0, StoneF);
            }
        }

        // Kamienny balkon z tralkami nad wejściem – na nim uchwyty flag.
        static void Ledge(ColorCanvas c)
        {
            Poly(c, new[] { V(76, 200), V(282, 200), V(282, 193), V(76, 193) }, StoneF);
            Poly(c, new[] { V(76, 200), V(282, 200), V(282, 202.5f), V(76, 202.5f) }, ShadeF);
            for (int k = 0; k < 26; k++) Rect(c, 80 + k * 7.7f, 203, 3.6f, 8, 1.5f, StoneF);
            Poly(c, new[] { V(74, 211), V(284, 211), V(284, 214), V(74, 214) }, StoneF);
        }

        // Flaga USA na ukośnym drzewcu: 7 pasów, kanton z gwiazdkami, lekkie falowanie i cienie fałd.
        static void UsFlag(ColorCanvas c, float x, float y, float length, float drop, float phase)
        {
            float ang = -38f * Mathf.Deg2Rad;
            var root = V(x, y);
            var tip = V(x + Mathf.Cos(ang) * length, y + Mathf.Sin(ang) * length);
            Line(c, root, tip, 0.9f, Pole);
            Circle(c, tip, 2f, Finial);

            var A0 = root + (tip - root) * 0.35f;
            var B0 = root + (tip - root) * 0.95f;
            System.Func<float, float, Vector2> P = (u, v) =>
            {
                var h = A0 + (B0 - A0) * u;
                float wave = Mathf.Sin(v * Mathf.PI * 1.6f + phase + u * 0.8f) * 1.8f * v;
                return V(h.x + wave - v * 2.5f, h.y + v * drop);
            };
            const int stripes = 7;
            for (int k = 0; k < stripes; k++) Band(c, P, k / (float)stripes, (k + 1) / (float)stripes, 0, 1, k % 2 == 0 ? Red : FlagWhite);
            Band(c, P, 3f / stripes, 1, 0, 0.42f, Navy);
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    Circle(c, P(3f / stripes + (4f / stripes) * (i + 0.5f) / 3f, 0.42f * (j + 0.5f) / 3f), 0.7f, Color.white);
            for (int k = 1; k < 4; k++) Band(c, P, 0, 1, k * 0.25f - 0.04f, k * 0.25f, new Color(0, 0, 0, 0.08f));
        }

        // Pas materiału flagi między u0..u1 (w poprzek) i v0..v1 (wzdłuż zwisu) – krawędzie próbkowane, żeby falowały.
        static void Band(ColorCanvas c, System.Func<float, float, Vector2> P, float u0, float u1, float v0, float v1, Color col)
        {
            const int n = 8;
            var pts = new Vector2[(n + 1) * 2];
            for (int k = 0; k <= n; k++)
            {
                float v = v0 + (v1 - v0) * k / n;
                pts[k] = P(u0, v);
                pts[pts.Length - 1 - k] = P(u1, v);
            }
            Poly(c, pts, col);
        }

        // ---------- kształty (wszystko w jednostkach 358 x 230) ----------

        static Vector2 V(float x, float y) { return new Vector2(x, y); }
        static Color A(Color c, float a) { c.a = a; return c; }

        static void Poly(ColorCanvas c, Vector2[] pts, Color col)
        {
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            foreach (var q in pts) { x0 = Mathf.Min(x0, q.x); y0 = Mathf.Min(y0, q.y); x1 = Mathf.Max(x1, q.x); y1 = Mathf.Max(y1, q.y); }
            c.Fill(p => SdfCanvas.Polygon(p, pts), col, x0, y0, x1, y1);
        }

        static void Rect(ColorCanvas c, float x, float y, float w, float h, float r, Color col)
        {
            var center = V(x + w * 0.5f, y + h * 0.5f);
            var size = V(w, h);
            c.Fill(p => SdfCanvas.RoundRect(p, center, size, r), col, x, y, x + w, y + h);
        }

        static void Circle(ColorCanvas c, Vector2 o, float r, Color col)
        {
            c.Fill(p => SdfCanvas.Circle(p, o, r), col, o.x - r, o.y - r, o.x + r, o.y + r);
        }

        static void Line(ColorCanvas c, Vector2 a, Vector2 b, float r, Color col)
        {
            c.Fill(p => SdfCanvas.Capsule(p, a, b, r), col,
                Mathf.Min(a.x, b.x) - r, Mathf.Min(a.y, b.y) - r, Mathf.Max(a.x, b.x) + r, Mathf.Max(a.y, b.y) + r);
        }
    }
}

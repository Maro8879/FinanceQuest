using UnityEngine;

namespace FinanceQuest
{
    // Tło „Myśli na dziś”: Wall Street (wersja dzienna i nocna – przełącza je „Tryb ciemny”) w perspektywie z dołu – gmach giełdy z kolumnadą
    // (ogólny styl, bez napisów i logo), za nim wieżowce typowe dla tej ulicy, na balkonie 3 flagi USA.
    // Rysunek w polu 358 x 230 jednostek (oś Y w dół), jedna tekstura = jedno wywołanie rysowania.
    public static class WallStreetArt
    {
        public const float W = 358f, H = 230f;
        const float PPU = 2.5f;                       // pikseli na jednostkę – ostro na telefonie, a liczy się szybko
        const float VPX = 179f, VPY = -620f, G = 230f; // punkt zbiegu pionów (widok z dołu) i linia ziemi

        // Kolory rysunku – ustawia je SetScheme przed rysowaniem (rysujemy zawsze na głównym wątku). Tylko wersja dzienna.
        static Color Sky, Glass, GlassHi, Mullion, White, Ribbon, StoneT, StoneTSh, WinT, Deco, DecoSh, Copper, CopperSh, Lit, LitDoor;

        static void SetScheme()
        {
            {
                // Dzień: jasne, stonowane barwy pod paletę aplikacji (wariant C).
                Sky = Palette.Hex("DCE7E6");
                Glass = Palette.Hex("9DB5B8"); GlassHi = Palette.Hex("BCD0D2"); Mullion = A(Palette.Hex("AFC3C5"), 0.8f);
                White = Palette.Hex("F2EFE8"); Ribbon = Palette.Hex("D6D1C6");
                StoneT = Palette.Hex("D9D0BF"); StoneTSh = Palette.Hex("C5BBA8"); WinT = Palette.Hex("A6B6B5");
                Deco = Palette.Hex("D3C8B3"); DecoSh = Palette.Hex("BFB39C"); Copper = Palette.Hex("7FAE9F"); CopperSh = Palette.Hex("6B998B");
                Lit = Palette.Hex("B7CBCB"); LitDoor = Palette.Hex("57503F");   // w dzień okna odbijają niebo, wejścia są ciemne
            }
        }

        static readonly Color StoneF = Palette.Hex("D9CBAA"), ShadeF = Palette.Hex("B8A77F"), DarkF = Palette.Hex("3C3420");
        internal static readonly Color Red = Palette.Hex("B83A33"), FlagWhite = Palette.Hex("F4F1EA"), Navy = Palette.Hex("24406E");

        // Flagi: maszty są w obrazku, a sam materiał rysuje osobno WavingFlags (żeby mógł powiewać).
        internal static readonly float[] FlagX = { 124f, 172f, 220f };
        internal const float FlagY = 195f, PoleLength = 26f, FlagDrop = 28f, PoleAngle = -38f;
        static readonly Color Pole = Palette.Hex("7B8284"), Finial = Palette.Hex("D8B45A"), Bracket = Palette.Hex("6E7577"), Bolt = Palette.Hex("A9B0B2");

        // Duży rysunek (ok. 0,8 s liczenia) – w grze wczytywany z PNG po „Bake Drawn Pictures”.
        public static Sprite Day { get { return DrawnPictures.Get("wallstreet_day_poles", () => Draw()); } }

        static Sprite Draw()
        {
            {
                SetScheme();
                var c = new ColorCanvas(W, H, PPU);
                c.Clear(Sky);
                DecoTower(c, 214, 300, 70);
                Masonry(c, 36, 126, 64, 2);
                Masonry(c, 160, 198, 92, 5);
                GlassTower(c, -80, 66, 20);
                WhiteTower(c, 290, 440, 26);
                Facade(c);
                Ledge(c);
                foreach (float x in FlagX)
                {
                    FlagPole(c, x, FlagY, PoleLength);
                    Rect(c, x - 3, 193, 6, 5, 1, Bracket);
                    Circle(c, V(x, 195), 1.4f, Bolt);
                }
                return Sprite.Create(c.ToTexture("fq_wallstreet_day"), new UnityEngine.Rect(0, 0, c.Width, c.Height),
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
                Rect(c, x, 219, 14, 11, 2, LitDoor);           // podświetlone wejścia
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
        // Maszt z kulką na końcu (materiał flagi dorysowuje WavingFlags).
        static void FlagPole(ColorCanvas c, float x, float y, float length)
        {
            float ang = PoleAngle * Mathf.Deg2Rad;
            var root = V(x, y);
            var tip = V(x + Mathf.Cos(ang) * length, y + Mathf.Sin(ang) * length);
            Line(c, root, tip, 0.9f, Pole);
            Circle(c, tip, 2f, Finial);
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

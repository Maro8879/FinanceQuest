using System.Collections.Generic;
using UnityEngine;

namespace FinanceQuest
{
    public enum CourseArt { Portfolio, Magnifier, Laptop, Wallet, Cards, Candles, Platform, Sprout, Sapling, Tree }

    // Ilustracje na kartach kursów (wariant B). Projektowane w polu 132 x 86 jednostek (oś Y w dół),
    // tak żeby nic nie wychodziło poza kartę. Kolory zależą od koloru karty (ciemny odcień),
    // więc w pamięci podręcznej kluczem jest ilustracja + kolor.
    public static class Illustrations
    {
        public const float BoxW = 132f, BoxH = 86f;
        const float PixelsPerUnit = 3f;   // ostro na telefonach z gęstym ekranem

        // Wspólne kolory: półprzezroczysta biel w trzech mocach + żółć marki.
        static readonly Color W2 = new Color(1f, 1f, 1f, 0.32f);
        static readonly Color W3 = new Color(1f, 1f, 1f, 0.58f);
        static readonly Color W4 = new Color(1f, 1f, 1f, 0.85f);
        static readonly Color Y = Palette.Accent;
        static readonly Color YD = Palette.Hex("C9A200");
        static readonly Color YL = Palette.Hex("FFD84D");

        public static Sprite Get(CourseArt art, Color cardColor)
        {
            string key = "art_" + art + "_" + ColorUtility.ToHtmlStringRGB(cardColor);
            return DrawnPictures.Get(key, () =>
            {
                var c = new ColorCanvas(BoxW, BoxH, PixelsPerUnit);
                var dark = Palette.Darker(cardColor, 0.7f);
                switch (art)
                {
                    case CourseArt.Portfolio: Portfolio(c); break;
                    case CourseArt.Magnifier: Magnifier(c, dark); break;
                    case CourseArt.Laptop: Laptop(c, dark); break;
                    case CourseArt.Wallet: Wallet(c, dark); break;
                    case CourseArt.Cards: Cards(c, dark); break;
                    case CourseArt.Candles: Candles(c); break;
                    case CourseArt.Platform: Platform(c, dark); break;
                    case CourseArt.Sprout: Sprout(c); break;
                    case CourseArt.Sapling: Sapling(c); break;
                    case CourseArt.Tree: Tree(c); break;
                }
                // Przycięta do samego rysunku – na karcie dopasowujemy ją do pola z zachowaniem proporcji.
                var tex = c.ToTexture("fq_" + key, true);
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f),
                    100f, 0, SpriteMeshType.FullRect);
            });
        }

        // ---------- ilustracje ----------

        // Podstawy inwestowania: wykres kołowy portfela + legenda.
        static void Portfolio(ColorCanvas c)
        {
            var o = new Vector2(50, 44);
            Circle(c, o, 34, W2);
            float split = Mathf.Atan2(12, 32) * Mathf.Rad2Deg;   // ok. 21°
            Sector(c, o, 34, -90f, split, Y);
            Sector(c, o, 34, split, Mathf.Atan2(28, -20) * Mathf.Rad2Deg, W3);
            c.Cut(p => SdfCanvas.Circle(p, o, 13), o.x - 14, o.y - 14, o.x + 14, o.y + 14);   // „pączek” zamiast pełnego koła
            RectTL(c, 96, 20, 30, 8, 4, Y);
            RectTL(c, 96, 36, 24, 8, 4, W3);
            RectTL(c, 96, 52, 28, 8, 4, W2);
        }

        // Inwestowanie krótkoterminowe: lupa nad świecami.
        static void Magnifier(ColorCanvas c, Color dark)
        {
            Line(c, 14, 44, 14, 74, 1f, W3); RectTL(c, 10, 50, 8, 16, 2, W3);
            Line(c, 30, 36, 30, 66, 1f, W3); StrokeRectTL(c, 26, 42, 8, 14, 2, 2f, W3);
            Line(c, 46, 26, 46, 58, 1f, W3); RectTL(c, 42, 30, 8, 20, 2, W3);

            var lens = new Vector2(82, 38);
            Circle(c, lens, 26, dark);
            Line(c, 72, 26, 72, 52, 1.25f, Y); RectTL(c, 67, 32, 10, 14, 2, Y);
            Line(c, 90, 20, 90, 46, 1.25f, Y); RectTL(c, 85, 24, 10, 16, 2, Y);
            Ring(c, lens, 26, 6, W4);
            Line(c, 101, 57, 122, 78, 4.5f, W4);   // rączka
        }

        // Platforma: laptop z wykresem liniowym (wypełnione pole + żółta linia + kropka na końcu).
        static void Laptop(ColorCanvas c, Color dark)
        {
            RectTL(c, 20, 4, 92, 60, 7, W3);
            RectTL(c, 26, 10, 80, 48, 3, dark);
            var line = new[] { V(31, 49), V(45, 40), V(56, 44), V(71, 28), V(82, 33), V(99.5f, 17) };
            var area = new Vector2[line.Length + 2];
            for (int i = 0; i < line.Length; i++) area[i] = line[i];
            area[line.Length] = V(99.5f, 53);
            area[line.Length + 1] = V(31, 53);
            Poly(c, area, W2);
            for (int i = 1; i < line.Length; i++) Line(c, line[i - 1].x, line[i - 1].y, line[i].x, line[i].y, 1.5f, Y);
            Circle(c, line[line.Length - 1], 3.5f, Y);
            Poly(c, new[] { V(8, 66), V(124, 66), V(116, 78), V(16, 78) }, W3);   // klawiatura
            RectTL(c, 56, 68, 20, 4, 2, dark);
        }

        // Budżet domowy: portfel z banknotem i stosik monet.
        static void Wallet(ColorCanvas c, Color dark)
        {
            RectTL(c, 8, 26, 84, 54, 10, W3);
            RotRectTL(c, 16, 12, 62, 18, 4, new Color(0.914f, 0.953f, 0.902f, 0.85f), -8f, V(47, 21));
            RectTL(c, 66, 42, 32, 24, 8, dark);
            Circle(c, V(78, 54), 4.5f, Y);
            for (int i = 0; i < 4; i++) CoinTL(c, 100, 80 - i * 7 - 7, 26, i % 2 == 0 ? Y : YL);
        }

        // Karty i kredyty: dwie karty płatnicze, jedna na drugiej.
        static void Cards(ColorCanvas c, Color dark)
        {
            var p1 = V(60, 44);
            RotRectTL(c, 6, 12, 84, 54, 8, W2, -10f, p1);
            RotRectTL(c, 6, 24, 84, 10, 0, dark, -10f, p1);   // pasek magnetyczny

            var p2 = V(80, 54);
            RotRectTL(c, 38, 26, 84, 54, 8, W3, 5f, p2);
            RotRectTL(c, 48, 40, 16, 12, 3, Y, 5f, p2);       // chip
            RotLine(c, 48, 68, 72, 68, 1.5f, dark, 5f, p2);
            RotLine(c, 78, 68, 94, 68, 1.5f, dark, 5f, p2);
            RotCircle(c, V(104, 46), 7, new Color(dark.r, dark.g, dark.b, 0.6f), 5f, p2);
            RotCircle(c, V(112, 46), 7, new Color(dark.r, dark.g, dark.b, 0.35f), 5f, p2);
        }

        // Inwestowanie krótkoterminowe: sam wykres świecowy – rozedrgany, ale z trendem w górę.
        // Świece na zmianę żółte i białe, żeby wykres był czytelny także na ciemnej karcie.
        static void Candles(ColorCanvas c)
        {
            // x, góra korpusu, wysokość korpusu, góra knota, dół knota
            float[,] d =
            {
                { 6, 44, 22, 36, 72 }, { 23, 30, 30, 22, 66 }, { 40, 48, 16, 40, 70 }, { 57, 22, 34, 12, 62 },
                { 74, 36, 22, 28, 66 }, { 91, 14, 30, 6, 52 }, { 108, 26, 18, 18, 50 },
            };
            for (int i = 0; i < d.GetLength(0); i++)
            {
                float x = d[i, 0] + 6f;
                Line(c, x, d[i, 3], x, d[i, 4], 1f, W3);
                RectTL(c, d[i, 0], d[i, 1], 12, d[i, 2], 3, i % 2 == 0 ? Y : W3);
            }
        }

        // Obsługa platformy: telefon z aplikacją stoi przed monitorem – jedna scena zamiast dwóch osobnych rzeczy.
        static void Platform(ColorCanvas c, Color dark)
        {
            RectTL(c, 6, 8, 92, 62, 8, W2);
            RectTL(c, 12, 14, 80, 48, 4, dark);
            Polyline(c, new[] { V(18, 52), V(32, 42), V(44, 46), V(60, 28), V(74, 34), V(86, 22) }, 1.5f, W3);
            RectTL(c, 44, 70, 16, 8, 2, W2);                   // nóżka monitora
            RectTL(c, 80, 26, 42, 60, 8, W4);                  // telefon
            RectTL(c, 84, 34, 34, 40, 3, dark);
            Polyline(c, new[] { V(88, 64), V(96, 56), V(104, 60), V(114, 44) }, 1.25f, Y);
            RectTL(c, 88, 77, 26, 5, 2, W3);
        }

        // Seria „Horyzont”: roślina rośnie razem z horyzontem (kiełek w doniczce → drzewko w doniczce → drzewo w ziemi).
        // Liście i korony są złote (pełny kolor, bez przezroczystości), więc nakładające się koła nie robią szwów.
        static void Pot(ColorCanvas c)
        {
            Poly(c, new[] { V(44, 66), V(88, 66), V(82, 84), V(50, 84) }, W3);
            RectTL(c, 40, 59, 52, 9, 3, W4);
        }

        static void Sprout(ColorCanvas c)
        {
            Pot(c);
            Line(c, 66, 60, 66, 40, 0.8f, W4);
            Leaf(c, V(66, 46), 22, -160, 8, Y);
            Leaf(c, V(66, 42), 24, -25, 9, YL);
        }

        static void Sapling(ColorCanvas c)
        {
            Pot(c);
            Line(c, 66, 60, 66, 30, 1f, W4);
            Line(c, 66, 46, 56, 38, 0.75f, W4);
            // Korona większa niż doniczka – drzewko ma być wyraźnie „pomiędzy” kiełkiem a drzewem.
            Circle(c, V(66, 22), 19, Y);
            Circle(c, V(50, 31), 14, Y);
            Circle(c, V(82, 31), 14, Y);
            Circle(c, V(66, 38), 13, Y);
            Circle(c, V(58, 17), 7, YL);    // odblask
        }

        static void Tree(ColorCanvas c)
        {
            RectTL(c, 8, 80, 116, 5, 2, W2);                   // ziemia
            Poly(c, new[] { V(60, 82), V(72, 82), V(69, 44), V(63, 44) }, W4);
            Line(c, 67, 58, 54, 46, 1.2f, W4);
            Line(c, 66, 54, 80, 44, 1.2f, W4);
            Circle(c, V(66, 22), 20, Y);
            Circle(c, V(46, 32), 16, Y);
            Circle(c, V(86, 32), 16, Y);
            Circle(c, V(56, 42), 14, Y);
            Circle(c, V(76, 42), 14, Y);
            Circle(c, V(66, 36), 16, Y);
            Circle(c, V(56, 20), 9, YL);    // odblaski
            Circle(c, V(82, 28), 6, YL);
        }

        // Liść w kształcie soczewki: od podstawy w kierunku kąta (stopnie, 0° = w prawo, oś Y w dół), najszerszy w połowie.
        static void Leaf(ColorCanvas c, Vector2 b, float length, float deg, float width, Color col)
        {
            float a = deg * Mathf.Deg2Rad;
            var u = V(Mathf.Cos(a), Mathf.Sin(a));
            var n = V(-u.y, u.x);
            const int Steps = 20;
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

        // ---------- pomocnicze kształty (wszystko w jednostkach 132 x 86) ----------

        static Vector2 V(float x, float y) { return new Vector2(x, y); }

        static void Circle(ColorCanvas c, Vector2 o, float r, Color col)
        {
            c.Fill(p => SdfCanvas.Circle(p, o, r), col, o.x - r, o.y - r, o.x + r, o.y + r);
        }

        // Pierścień: kontur koła o grubości t (środek grubości na promieniu r).
        static void Ring(ColorCanvas c, Vector2 o, float r, float t, Color col)
        {
            float R = r + t;
            c.Fill(p => Mathf.Abs((p - o).magnitude - r) - t * 0.5f, col, o.x - R, o.y - R, o.x + R, o.y + R);
        }

        static void Polyline(ColorCanvas c, Vector2[] pts, float r, Color col)
        {
            for (int i = 1; i < pts.Length; i++) Line(c, pts[i - 1].x, pts[i - 1].y, pts[i].x, pts[i].y, r, col);
        }

        static void Line(ColorCanvas c, float x0, float y0, float x1, float y1, float r, Color col)
        {
            Vector2 a = V(x0, y0), b = V(x1, y1);
            c.Fill(p => SdfCanvas.Capsule(p, a, b, r), col,
                Mathf.Min(x0, x1) - r, Mathf.Min(y0, y1) - r, Mathf.Max(x0, x1) + r, Mathf.Max(y0, y1) + r);
        }

        // Prostokąt podany jak w SVG: lewy górny róg + wymiary.
        static void RectTL(ColorCanvas c, float x, float y, float w, float h, float r, Color col)
        {
            var center = V(x + w * 0.5f, y + h * 0.5f);
            var size = V(w, h);
            c.Fill(p => SdfCanvas.RoundRect(p, center, size, r), col, x, y, x + w, y + h);
        }

        static void StrokeRectTL(ColorCanvas c, float x, float y, float w, float h, float r, float t, Color col)
        {
            var center = V(x + w * 0.5f, y + h * 0.5f);
            var size = V(w - t, h - t);
            c.Fill(p => Mathf.Abs(SdfCanvas.RoundRect(p, center, size, Mathf.Max(0f, r - t * 0.5f))) - t * 0.5f, col, x, y, x + w, y + h);
        }

        static void CoinTL(ColorCanvas c, float x, float y, float w, Color face)
        {
            RectTL(c, x, y, w, 8, 4, YD);
            RectTL(c, x + 1.2f, y + 1.2f, w - 2.4f, 5.6f, 2.8f, face);
        }

        static void Poly(ColorCanvas c, Vector2[] pts, Color col)
        {
            float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
            foreach (var q in pts) { x0 = Mathf.Min(x0, q.x); y0 = Mathf.Min(y0, q.y); x1 = Mathf.Max(x1, q.x); y1 = Mathf.Max(y1, q.y); }
            c.Fill(p => SdfCanvas.Polygon(p, pts), col, x0, y0, x1, y1);
        }

        // Wycinek koła jako wielokąt (kąty w stopniach, 0° = w prawo, rośnie zgodnie ze wskazówkami zegara).
        static void Sector(ColorCanvas c, Vector2 o, float r, float fromDeg, float toDeg, Color col)
        {
            int steps = Mathf.Max(2, Mathf.CeilToInt((toDeg - fromDeg) / 6f));
            var pts = new Vector2[steps + 2];
            pts[0] = o;
            for (int i = 0; i <= steps; i++)
            {
                float ang = Mathf.Lerp(fromDeg, toDeg, i / (float)steps) * Mathf.Deg2Rad;
                pts[i + 1] = o + V(Mathf.Cos(ang), Mathf.Sin(ang)) * r;
            }
            Poly(c, pts, col);
        }

        // --- obrócone kształty: obracamy punkt próbkowania zamiast samego kształtu ---

        static Vector2 Rotate(Vector2 v, float deg)
        {
            float a = deg * Mathf.Deg2Rad, cs = Mathf.Cos(a), sn = Mathf.Sin(a);
            return V(v.x * cs - v.y * sn, v.x * sn + v.y * cs);
        }

        static void RotRectTL(ColorCanvas c, float x, float y, float w, float h, float r, Color col, float deg, Vector2 pivot)
        {
            var center = pivot + Rotate(V(x + w * 0.5f, y + h * 0.5f) - pivot, deg);
            var size = V(w, h);
            float ext = 0.5f * Mathf.Sqrt(w * w + h * h);
            c.Fill(p => SdfCanvas.RoundRect(center + Rotate(p - center, -deg), center, size, r), col,
                center.x - ext, center.y - ext, center.x + ext, center.y + ext);
        }

        static void RotLine(ColorCanvas c, float x0, float y0, float x1, float y1, float r, Color col, float deg, Vector2 pivot)
        {
            var a = pivot + Rotate(V(x0, y0) - pivot, deg);
            var b = pivot + Rotate(V(x1, y1) - pivot, deg);
            Line(c, a.x, a.y, b.x, b.y, r, col);
        }

        static void RotCircle(ColorCanvas c, Vector2 o, float r, Color col, float deg, Vector2 pivot)
        {
            Circle(c, pivot + Rotate(o - pivot, deg), r, col);
        }
    }
}

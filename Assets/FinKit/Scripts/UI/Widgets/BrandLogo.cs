using UnityEngine;

namespace FinanceQuest
{
    // Znak INVESTOMM: schodki z trzech kolumn w izometrii (1, 2 i 3 kostki) na petrolowym kwadracie.
    // Każda kolumna to jedna bryła (góra + dwie ściany), a nie osobne kostki – wtedy nie ma cienkich szwów
    // na łączeniach. Rysowane raz i trzymane w pamięci podręcznej.
    public static class BrandLogo
    {
        const float Size = 64f, PixelsPerUnit = 3f;   // 192 px – ostre także na gęstych ekranach

        static readonly Color Light = Palette.Hex("FFDA4D");   // góra (najjaśniejsza)
        static readonly Color Mid = Palette.Hex("F2C200");     // ściana lewa
        static readonly Color Dark = Palette.Hex("A98500");    // ściana prawa (w cieniu)

        public static Sprite Icon { get { return DrawnPictures.Get("brand_investomm", Make); } }

        static Sprite Make()
        {
            var c = new ColorCanvas(Size, Size, PixelsPerUnit);
            var mid = new Vector2(Size / 2f, Size / 2f);
            c.Fill(p => SdfCanvas.RoundRect(p, mid, new Vector2(Size, Size), 16f), Palette.Brand, 0, 0, Size, Size);

            // Osie izometrii (oś Y w dół): X – w prawo i w głąb, Y – w lewo i w głąb.
            // Wysokość kostki = a / cos30°, dzięki temu ściany wyglądają na kwadratowe.
            const float a = 10f;
            float e = a / Mathf.Cos(30f * Mathf.Deg2Rad);
            var X = new Vector2(a, -a / 2f);
            var Y = new Vector2(-a, -a / 2f);

            // Obrys całych schodków: x od -a do 3a, y od -(2a + 3e) do 0 (punkt zaczepienia w (0,0)).
            float minX = -a, maxX = 3f * a, minY = -(2f * a + 3f * e), maxY = 0f;
            float s = 42f / Mathf.Max(maxX - minX, maxY - minY);
            var o = mid - new Vector2((minX + maxX) / 2f, (minY + maxY) / 2f) * s;

            // Od najdalszej kolumny do najbliższej – bliższa przykrywa dalszą.
            for (int i = 2; i >= 0; i--)
            {
                var b = o + X * (i * s);
                var up = new Vector2(0f, -(i + 1) * e * s);
                var x = X * s; var y = Y * s;
                Face(c, Light, b + up, b + x + up, b + x + y + up, b + y + up);
                Face(c, Mid, b, b + y, b + y + up, b + up);
                Face(c, Dark, b, b + x, b + x + up, b + up);
            }

            return Sprite.Create(c.ToTexture("fq_brand_investomm"), new Rect(0, 0, c.Width, c.Height),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }

        static void Face(ColorCanvas c, Color col, params Vector2[] v)
        {
            float x0 = v[0].x, x1 = v[0].x, y0 = v[0].y, y1 = v[0].y;
            foreach (var p in v) { x0 = Mathf.Min(x0, p.x); x1 = Mathf.Max(x1, p.x); y0 = Mathf.Min(y0, p.y); y1 = Mathf.Max(y1, p.y); }
            // +0.15 jednostki: ściany lekko na siebie zachodzą, więc między nimi nie prześwituje tło.
            c.Fill(p => SdfCanvas.Polygon(p, v) - 0.15f, col, x0, y0, x1, y1);
        }
    }
}

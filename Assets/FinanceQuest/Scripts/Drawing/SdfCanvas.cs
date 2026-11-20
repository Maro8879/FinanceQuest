using UnityEngine;

namespace FinanceQuest
{
    // Malutki rasteryzator kształtów z funkcji odległości (SDF).
    // Dzięki temu grafiki mają gładkie krawędzie w każdej rozdzielczości, a nie trzymamy ich jako plików.
    // Na razie rysuje jednym kolorem (maska alfa) – ikony barwimy potem kolorem Image.
    public sealed class SdfCanvas
    {
        public delegate float Sdf(Vector2 p);

        public readonly int Width, Height;
        readonly float scale;          // ile pikseli na jednostkę rysunku
        readonly float[] alpha;

        // units = szerokość rysunku w jednostkach (ikony projektujemy na siatce 24x24 jak w SVG).
        public SdfCanvas(int width, int height, float unitsWide)
        {
            Width = width; Height = height;
            scale = width / unitsWide;
            alpha = new float[width * height];
        }

        public void Add(Sdf f) { Paint(f, false, 1f); }
        public void Cut(Sdf f) { Paint(f, true, 1f); }
        // Słabsze krycie – do drugoplanowych detali (siatka, linia trendu), choć cała grafika jest jednym kolorem.
        public void Add(Sdf f, float strength) { Paint(f, false, strength); }

        void Paint(Sdf f, bool cut, float strength)
        {
            for (int y = 0; y < Height; y++)
            {
                // Rysujemy z osią Y w dół (jak w SVG), a tekstura ma Y w górę – stąd odwrócenie.
                float py = (Height - y - 0.5f) / scale;
                int row = y * Width;
                for (int x = 0; x < Width; x++)
                {
                    float d = f(new Vector2((x + 0.5f) / scale, py)) * scale;
                    // Pokrycie liczone z odległości w pikselach daje wygładzoną krawędź szerokości ~1 px.
                    float c = Mathf.Clamp01(0.5f - d) * strength;
                    if (c <= 0f) continue;
                    int i = row + x;
                    alpha[i] = cut ? alpha[i] * (1f - c) : alpha[i] + c * (1f - alpha[i]);
                }
            }
        }

        public Texture2D ToTexture(string name)
        {
            var tex = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            tex.name = name;
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[alpha.Length];
            // Kolor zawsze biały (także w przezroczystych pikselach), żeby filtrowanie nie robiło ciemnych obwódek.
            for (int i = 0; i < px.Length; i++)
                px[i] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha[i] * 255f));
            tex.SetPixels32(px);
            // Bez mipmap; kopię w RAM zwalniamy, chyba że narzędzie Bake chce zapisać PNG.
            tex.Apply(false, !DrawnPictures.KeepReadable);
            return tex;
        }

        // ---------- kształty ----------

        public static float Circle(Vector2 p, Vector2 c, float r)
        {
            return (p - c).magnitude - r;
        }

        public static float RoundRect(Vector2 p, Vector2 center, Vector2 size, float r)
        {
            float qx = Mathf.Abs(p.x - center.x) - size.x * 0.5f + r;
            float qy = Mathf.Abs(p.y - center.y) - size.y * 0.5f + r;
            float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
            return outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }

        // Odcinek z grubością – do kresek, ptaszka itp.
        public static float Capsule(Vector2 p, Vector2 a, Vector2 b, float r)
        {
            Vector2 pa = p - a, ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude - r;
        }

        // Dowolny wielokąt (także wklęsły, np. gwiazda).
        public static float Polygon(Vector2 p, Vector2[] v)
        {
            float d = Vector2.Dot(p - v[0], p - v[0]);
            float s = 1f;
            for (int i = 0, j = v.Length - 1; i < v.Length; j = i, i++)
            {
                Vector2 e = v[j] - v[i];
                Vector2 w = p - v[i];
                Vector2 b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Vector2.Dot(e, e));
                d = Mathf.Min(d, Vector2.Dot(b, b));
                bool c1 = p.y >= v[i].y, c2 = p.y < v[j].y, c3 = e.x * w.y > e.y * w.x;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
            }
            return s * Mathf.Sqrt(d);
        }
    }
}

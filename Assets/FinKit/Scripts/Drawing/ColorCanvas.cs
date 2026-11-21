using UnityEngine;

namespace FinanceQuest
{
    // Kolorowa wersja rasteryzatora SDF – do ilustracji z kilkoma kolorami w jednej teksturze
    // (jedna tekstura = jeden obrazek = jedno wywołanie rysowania na karcie).
    // Każdy kształt maluje tylko w swoim prostokącie otaczającym, więc nawet duże ilustracje liczą się szybko.
    public sealed class ColorCanvas
    {
        public readonly int Width, Height;
        readonly float scale;                 // pikseli na jednostkę rysunku
        readonly float[] r, g, b, a;          // kolor przemnożony przez alfę – poprawne nakładanie półprzezroczystych warstw

        public ColorCanvas(float unitsWide, float unitsHigh, float pixelsPerUnit)
        {
            scale = pixelsPerUnit;
            Width = Mathf.CeilToInt(unitsWide * scale);
            Height = Mathf.CeilToInt(unitsHigh * scale);
            int n = Width * Height;
            r = new float[n]; g = new float[n]; b = new float[n]; a = new float[n];
        }

        // Wypełnia całe płótno jednym kolorem (szybciej niż kształt na całą powierzchnię – np. niebo).
        public void Clear(Color c)
        {
            for (int i = 0; i < a.Length; i++) { r[i] = c.r * c.a; g[i] = c.g * c.a; b[i] = c.b * c.a; a[i] = c.a; }
        }

        // Prostokąt otaczający podajemy w jednostkach rysunku (oś Y w dół, jak w SVG).
        public void Fill(SdfCanvas.Sdf f, Color c, float x0, float y0, float x1, float y1) { Paint(f, c, x0, y0, x1, y1, false); }
        public void Cut(SdfCanvas.Sdf f, float x0, float y0, float x1, float y1) { Paint(f, Color.clear, x0, y0, x1, y1, true); }

        void Paint(SdfCanvas.Sdf f, Color c, float x0, float y0, float x1, float y1, bool cut)
        {
            int px0 = Mathf.Max(0, Mathf.FloorToInt(x0 * scale) - 2);
            int px1 = Mathf.Min(Width - 1, Mathf.CeilToInt(x1 * scale) + 2);
            int row0 = Mathf.Max(0, Mathf.FloorToInt(Height - y1 * scale) - 2);
            int row1 = Mathf.Min(Height - 1, Mathf.CeilToInt(Height - y0 * scale) + 2);
            for (int row = row0; row <= row1; row++)
            {
                float py = (Height - row - 0.5f) / scale;
                for (int x = px0; x <= px1; x++)
                {
                    float d = f(new Vector2((x + 0.5f) / scale, py)) * scale;
                    float cov = Mathf.Clamp01(0.5f - d);
                    if (cov <= 0f) continue;
                    int i = row * Width + x;
                    if (cut)
                    {
                        float k = 1f - cov;
                        r[i] *= k; g[i] *= k; b[i] *= k; a[i] *= k;
                    }
                    else
                    {
                        float s = cov * c.a, k = 1f - s;   // „over”: nowa warstwa na wierzchu
                        r[i] = c.r * s + r[i] * k;
                        g[i] = c.g * s + g[i] * k;
                        b[i] = c.b * s + b[i] * k;
                        a[i] = s + a[i] * k;
                    }
                }
            }
        }

        public Texture2D ToTexture(string name) { return ToTexture(name, false); }

        // crop = przycinamy przezroczyste brzegi (zostawiając 2 px), żeby różne ilustracje dało się
        // dopasować do tego samego pola i wszystkie wyglądały na podobnie duże.
        public Texture2D ToTexture(string name, bool crop)
        {
            int cx0 = 0, cy0 = 0, cw = Width, ch = Height;
            if (crop)
            {
                int minX = Width, minY = Height, maxX = -1, maxY = -1;
                for (int y = 0; y < Height; y++)
                    for (int x = 0; x < Width; x++)
                        if (a[y * Width + x] > 0.02f)
                        {
                            if (x < minX) minX = x; if (x > maxX) maxX = x;
                            if (y < minY) minY = y; if (y > maxY) maxY = y;
                        }
                if (maxX >= 0)
                {
                    cx0 = Mathf.Max(0, minX - 2); cy0 = Mathf.Max(0, minY - 2);
                    cw = Mathf.Min(Width - 1, maxX + 2) - cx0 + 1; ch = Mathf.Min(Height - 1, maxY + 2) - cy0 + 1;
                }
            }
            var px = new Color32[cw * ch];
            for (int y = 0; y < ch; y++)
                for (int x = 0; x < cw; x++)
                {
                    int i = (y + cy0) * Width + x + cx0;
                    float al = a[i];
                    if (al <= 0.0001f) continue;
                    px[y * cw + x] = new Color32(To8(r[i] / al), To8(g[i] / al), To8(b[i] / al), To8(al));
                }
            return Finish(name, px, cw, ch);
        }

        Texture2D Finish(string name, Color32[] px, int Width, int Height)
        {
            // Przezroczyste piksele przy krawędziach dostają kolor sąsiada – inaczej filtrowanie robi ciemne obwódki.
            var src = (Color32[])px.Clone();
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    int i = y * Width + x;
                    if (src[i].a != 0) continue;
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = x + dx, ny = y + dy;
                            if (nx < 0 || ny < 0 || nx >= Width || ny >= Height) continue;
                            var n = src[ny * Width + nx];
                            if (n.a != 0) { px[i] = new Color32(n.r, n.g, n.b, 0); dy = 2; break; }
                        }
                }

            var tex = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            tex.name = name;
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.SetPixels32(px);
            tex.Apply(false, !DrawnPictures.KeepReadable);   // czytelna tylko przy wypiekaniu do PNG
            return tex;
        }

        static byte To8(float v) { return (byte)Mathf.RoundToInt(Mathf.Clamp01(v) * 255f); }
    }
}

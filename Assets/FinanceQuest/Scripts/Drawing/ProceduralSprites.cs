using System.Collections.Generic;
using UnityEngine;

namespace FinanceQuest
{
    // Sprite'y rysowane w kodzie. Każdy powstaje raz i siedzi w pamięci podręcznej,
    // żeby przebudowa ekranu nie rysowała ich od nowa.
    public static class ProceduralSprites
    {
        // Promień narożnika w teksturze „zaokrąglonego prostokąta”. Image skaluje go przez pixelsPerUnitMultiplier.
        public const float RoundTexRadius = 48f;
        const int IconSize = 96;

        static Sprite rounded, band;
        static readonly Dictionary<IconId, Sprite> icons = new Dictionary<IconId, Sprite>();

        // Jeden 9-slice na wszystkie karty, przyciski i paski – mniej tekstur = lepszy batching.
        public static Sprite Rounded
        {
            get
            {
                if (rounded != null) return rounded;
                int size = (int)RoundTexRadius * 2 + 5;
                float half = size * 0.5f;
                var c = new SdfCanvas(size, size, size);
                c.Add(p => SdfCanvas.RoundRect(p, new Vector2(half, half), new Vector2(size - 2, size - 2), RoundTexRadius));
                float b = Mathf.Floor(half);
                rounded = Sprite.Create(c.ToTexture("fq_rounded"), new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                    100f, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
                return rounded;
            }
        }

        // Miękki pionowy pas do przelatującego połysku na złotych kartach.
        public static Sprite ShineBand
        {
            get
            {
                if (band != null) return band;
                const int w = 64, h = 4;
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                tex.wrapMode = TextureWrapMode.Clamp;
                var px = new Color32[w * h];
                for (int x = 0; x < w; x++)
                {
                    float t = (x + 0.5f) / w * 2f - 1f;
                    byte a = (byte)Mathf.RoundToInt(Mathf.Exp(-t * t * 4f) * 255f);
                    for (int y = 0; y < h; y++) px[y * w + x] = new Color32(255, 255, 255, a);
                }
                tex.SetPixels32(px);
                tex.Apply(false, true);
                band = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
                return band;
            }
        }

        public static Sprite Icon(IconId id)
        {
            Sprite s;
            if (icons.TryGetValue(id, out s)) return s;
            var c = new SdfCanvas(IconSize, IconSize, 24f);
            DrawIcon(c, id);
            s = Sprite.Create(c.ToTexture("fq_icon_" + id), new Rect(0, 0, IconSize, IconSize), new Vector2(0.5f, 0.5f), 100f);
            icons[id] = s;
            return s;
        }

        static Vector2 V(float x, float y) { return new Vector2(x, y); }

        // Ikony projektowane na siatce 24x24 (oś Y w dół), rysowane na biało – kolor nadaje Image.
        static void DrawIcon(SdfCanvas c, IconId id)
        {
            switch (id)
            {
                case IconId.Home:
                {
                    var house = new[] { V(12, 3.5f), V(21, 11), V(19.5f, 11), V(19.5f, 20.5f), V(4.5f, 20.5f), V(4.5f, 11), V(3, 11) };
                    c.Add(p => SdfCanvas.Polygon(p, house) - 0.6f);
                    c.Cut(p => SdfCanvas.RoundRect(p, V(12, 17.5f), V(4.4f, 7f), 1.2f));
                    break;
                }
                case IconId.Lock:
                    c.Add(p => SdfCanvas.RoundRect(p, V(12, 15.5f), V(15, 10.5f), 3f));
                    // Kabłąk = górna połowa pierścienia + dwie nóżki.
                    c.Add(p => Mathf.Max(Mathf.Abs((p - V(12, 8.5f)).magnitude - 4f) - 1.4f, p.y - 8.5f));
                    c.Add(p => SdfCanvas.Capsule(p, V(8, 8.5f), V(8, 11), 1.4f));
                    c.Add(p => SdfCanvas.Capsule(p, V(16, 8.5f), V(16, 11), 1.4f));
                    c.Cut(p => SdfCanvas.Circle(p, V(12, 15), 1.6f));
                    break;
                case IconId.Check:
                    c.Add(p => SdfCanvas.Capsule(p, V(5, 12.5f), V(10, 17.5f), 1.9f));
                    c.Add(p => SdfCanvas.Capsule(p, V(10, 17.5f), V(19, 7), 1.9f));
                    break;
                case IconId.Book:
                {
                    var left = new[] { V(2.5f, 5), V(11.2f, 6.2f), V(11.2f, 20), V(2.5f, 18.8f) };
                    var right = new[] { V(12.8f, 6.2f), V(21.5f, 5), V(21.5f, 18.8f), V(12.8f, 20) };
                    c.Add(p => SdfCanvas.Polygon(p, left) - 0.5f);
                    c.Add(p => SdfCanvas.Polygon(p, right) - 0.5f);
                    break;
                }
                case IconId.Gamepad:
                    c.Add(p => SdfCanvas.RoundRect(p, V(12, 12.5f), V(21, 12), 6f));
                    c.Cut(p => SdfCanvas.Capsule(p, V(7, 10), V(7, 15), 0.9f));
                    c.Cut(p => SdfCanvas.Capsule(p, V(4.5f, 12.5f), V(9.5f, 12.5f), 0.9f));
                    c.Cut(p => SdfCanvas.Circle(p, V(16, 11), 1.4f));
                    c.Cut(p => SdfCanvas.Circle(p, V(18.6f, 13.8f), 1.4f));
                    break;
                case IconId.Task:
                    c.Add(p => SdfCanvas.RoundRect(p, V(12, 12.5f), V(16, 19), 3f));
                    c.Cut(p => SdfCanvas.Capsule(p, V(8, 9), V(16, 9), 1f));
                    c.Cut(p => SdfCanvas.Capsule(p, V(8, 13), V(16, 13), 1f));
                    c.Cut(p => SdfCanvas.Capsule(p, V(8, 17), V(13, 17), 1f));
                    break;
                case IconId.Star:
                {
                    var pts = new Vector2[10];
                    for (int i = 0; i < 10; i++)
                    {
                        float a = -Mathf.PI / 2f + i * Mathf.PI / 5f;
                        float r = i % 2 == 0 ? 10.5f : 4.6f;
                        pts[i] = V(12 + Mathf.Cos(a) * r, 13 + Mathf.Sin(a) * r);
                    }
                    c.Add(p => SdfCanvas.Polygon(p, pts) - 0.6f);
                    break;
                }
                case IconId.Moon:
                    c.Add(p => SdfCanvas.Circle(p, V(12, 12), 8.5f));
                    c.Cut(p => SdfCanvas.Circle(p, V(16.5f, 8), 7.5f));
                    break;
                case IconId.Chart:
                    c.Add(p => SdfCanvas.Capsule(p, V(5, 19), V(5, 13), 1.8f));
                    c.Add(p => SdfCanvas.Capsule(p, V(10, 19), V(10, 6), 1.8f));
                    c.Add(p => SdfCanvas.Capsule(p, V(15, 19), V(15, 10), 1.8f));
                    c.Add(p => SdfCanvas.Capsule(p, V(20, 19), V(20, 4), 1.8f));
                    break;
                case IconId.Phone:
                    c.Add(p => SdfCanvas.RoundRect(p, V(12, 12), V(13, 21), 3.2f));
                    c.Cut(p => SdfCanvas.RoundRect(p, V(12, 11), V(9, 13), 1.2f));
                    c.Add(p => SdfCanvas.Capsule(p, V(8.8f, 14), V(11, 11.5f), 0.8f));
                    c.Add(p => SdfCanvas.Capsule(p, V(11, 11.5f), V(12.6f, 13), 0.8f));
                    c.Add(p => SdfCanvas.Capsule(p, V(12.6f, 13), V(15.2f, 8.8f), 0.8f));
                    break;
                case IconId.Wallet:
                    c.Add(p => SdfCanvas.RoundRect(p, V(12, 13.5f), V(20, 14), 3f));
                    c.Add(p => SdfCanvas.RoundRect(p, V(11, 6.5f), V(14, 4), 1.5f));
                    c.Cut(p => SdfCanvas.RoundRect(p, V(19, 13.5f), V(7, 5.4f), 2f));
                    c.Add(p => SdfCanvas.Circle(p, V(18.6f, 13.5f), 1.2f));
                    break;
                case IconId.Card:
                    c.Add(p => SdfCanvas.RoundRect(p, V(12, 12), V(21, 15), 3f));
                    c.Cut(p => SdfCanvas.RoundRect(p, V(12, 9.5f), V(23, 3), 0f));
                    c.Cut(p => SdfCanvas.Capsule(p, V(6, 15.5f), V(10, 15.5f), 0.9f));
                    break;
                case IconId.Spark:
                {
                    var s = new[] { V(12, 0.5f), V(14.4f, 9.6f), V(23.5f, 12), V(14.4f, 14.4f), V(12, 23.5f), V(9.6f, 14.4f), V(0.5f, 12), V(9.6f, 9.6f) };
                    c.Add(p => SdfCanvas.Polygon(p, s));
                    break;
                }
            }
        }
    }
}

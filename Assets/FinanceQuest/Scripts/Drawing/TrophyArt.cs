using System.Collections.Generic;
using UnityEngine;

namespace FinanceQuest
{
    // Puchar na półkę trofeów. Projekt w polu 40 x 46 jednostek (oś Y w dół).
    // Zdobyty: kolor kursu + gwiazdka; niezdobyty: szara sylwetka z kłódką.
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

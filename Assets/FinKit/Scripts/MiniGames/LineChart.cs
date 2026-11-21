using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FinanceQuest
{
    // Prosty wykres liniowy rysowany jedną siatką (bez tekstur i bibliotek) – tani dla telefonu.
    // Linia główna (ciągła), opcjonalna „duch” (przerywana, np. „gdybyś trzymał”) i przerywana linia odniesienia (wpłata).
    public class LineChart : MaskableGraphic
    {
        public List<double> Main = new List<double>();
        public List<double> Ghost;                 // null = nie rysujemy
        public double Baseline;                    // np. 1000 € – linia wpłaty
        public double Min, Max;                    // zakres osi Y (stały przez całą grę, żeby wykres nie „skakał”)
        public int Steps = 12;                     // ile odcinków na osi X
        public float Thickness = 3.5f;
        public Color MainColor = Color.black, GhostColor = Color.gray, BaseColor = Color.gray;

        // Pozycja punktu w układzie tego prostokąta – do kropki na końcu linii.
        public Vector2 Point(int i, double v)
        {
            var r = rectTransform.rect;
            float x = r.xMin + r.width * i / Mathf.Max(1, Steps);
            float t = (float)((v - Min) / System.Math.Max(1e-6, Max - Min));
            return new Vector2(x, r.yMin + r.height * Mathf.Clamp01(t));
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            // Linia wpłaty: cienka, przerywana, przez całą szerokość.
            var b0 = Point(0, Baseline); var b1 = new Vector2(r.xMax, b0.y);
            Dashed(vh, b0, b1, 1.5f, BaseColor, 6f, 5f);

            if (Ghost != null)
                for (int i = 1; i < Ghost.Count; i++)
                    Dashed(vh, Point(i - 1, Ghost[i - 1]), Point(i, Ghost[i]), Thickness * 0.7f, GhostColor, 7f, 5f);

            for (int i = 1; i < Main.Count; i++)
                Segment(vh, Point(i - 1, Main[i - 1]), Point(i, Main[i]), Thickness, MainColor, true);
        }

        void Dashed(VertexHelper vh, Vector2 a, Vector2 b, float w, Color c, float dash, float gap)
        {
            float len = Vector2.Distance(a, b);
            if (len < 0.01f) return;
            Vector2 d = (b - a) / len;
            for (float s = 0f; s < len; s += dash + gap)
                Segment(vh, a + d * s, a + d * Mathf.Min(len, s + dash), w, c, false);
        }

        // Odcinek jako prostokąt; „cap” wydłuża go o pół grubości, żeby załamania linii nie miały szczelin.
        static void Segment(VertexHelper vh, Vector2 a, Vector2 b, float w, Color c, bool cap)
        {
            Vector2 d = b - a;
            if (d.sqrMagnitude < 1e-6f) return;
            d.Normalize();
            if (cap) { a -= d * w * 0.5f; b += d * w * 0.5f; }
            Vector2 n = new Vector2(-d.y, d.x) * (w * 0.5f);
            int i = vh.currentVertCount;
            UIVertex v = UIVertex.simpleVert;
            v.color = c;
            v.position = a - n; vh.AddVert(v);
            v.position = a + n; vh.AddVert(v);
            v.position = b + n; vh.AddVert(v);
            v.position = b - n; vh.AddVert(v);
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i, i + 2, i + 3);
        }
    }
}

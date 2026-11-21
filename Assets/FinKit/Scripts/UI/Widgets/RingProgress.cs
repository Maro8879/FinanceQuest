using UnityEngine;
using UnityEngine.UI;

namespace FinanceQuest
{
    // Okrągły wskaźnik postępu: szary pierścień + żółty łuk od góry zgodnie z zegarem, z zaokrąglonymi końcami.
    // Rysowany siatką (bez tekstury) z cienkim zanikającym brzegiem, więc jest ostry w każdym rozmiarze.
    public class RingProgress : MaskableGraphic
    {
        public float Value;                    // 0..1
        public float Thickness = 7f;
        public Color TrackColor = Color.gray;
        const int Segments = 72;
        const float Feather = 0.9f;

        public static RingProgress Create(Transform parent, float x, float y, float size, float value, Color track, Color fill, float thickness)
        {
            var rt = Ui.Rect("Ring", parent);
            Ui.Place(rt, x, y, size, size);
            rt.gameObject.AddComponent<CanvasRenderer>();
            var r = rt.gameObject.AddComponent<RingProgress>();
            r.raycastTarget = false;
            r.Value = Mathf.Clamp01(value);
            r.TrackColor = track;
            r.color = fill;
            r.Thickness = thickness;
            return r;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var rect = rectTransform.rect;
            var c = rect.center;
            float radius = Mathf.Min(rect.width, rect.height) * 0.5f - Thickness * 0.5f - Feather;
            Arc(vh, c, radius, 0f, 1f, TrackColor, false);
            if (Value > 0.001f) Arc(vh, c, radius, 0f, Value, color, true);
        }

        // Łuk od „godziny 12” zgodnie z zegarem: from..to w ułamkach pełnego koła.
        void Arc(VertexHelper vh, Vector2 c, float radius, float from, float to, Color col, bool caps)
        {
            Color32 solid = col, clear = col;
            clear.a = 0;
            float half = Thickness * 0.5f;
            int steps = Mathf.Max(2, Mathf.CeilToInt(Segments * (to - from)));
            int start = vh.currentVertCount;
            for (int i = 0; i <= steps; i++)
            {
                float t = from + (to - from) * i / steps;
                float a = Mathf.PI * 0.5f - t * Mathf.PI * 2f;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                vh.AddVert(c + dir * (radius + half + Feather), clear, Vector2.zero);
                vh.AddVert(c + dir * (radius + half), solid, Vector2.zero);
                vh.AddVert(c + dir * (radius - half), solid, Vector2.zero);
                vh.AddVert(c + dir * (radius - half - Feather), clear, Vector2.zero);
                if (i == 0) continue;
                int p = start + (i - 1) * 4, q = start + i * 4;
                for (int s = 0; s < 3; s++)
                {
                    vh.AddTriangle(p + s, q + s, q + s + 1);
                    vh.AddTriangle(p + s, q + s + 1, p + s + 1);
                }
            }
            if (!caps) return;
            // Zaokrąglone końce łuku: półkola na początku i na końcu.
            Cap(vh, c, radius, from, half, solid, clear, true);
            Cap(vh, c, radius, to, half, solid, clear, false);
        }

        void Cap(VertexHelper vh, Vector2 c, float radius, float t, float half, Color32 solid, Color32 clear, bool atStart)
        {
            float a = Mathf.PI * 0.5f - t * Mathf.PI * 2f;
            var center = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
            // Kierunek „na zewnątrz” łuku na tym końcu (wstecz dla początku, naprzód dla końca).
            var tangent = new Vector2(Mathf.Sin(a), -Mathf.Cos(a)) * (atStart ? -1f : 1f);
            var normal = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            const int n = 10;
            int mid = vh.currentVertCount;
            vh.AddVert(center, solid, Vector2.zero);
            for (int i = 0; i <= n; i++)
            {
                float k = Mathf.PI * i / n;   // od normalnej przez styczną do -normalnej
                var d = normal * Mathf.Cos(k) + tangent * Mathf.Sin(k);
                vh.AddVert(center + d * half, solid, Vector2.zero);
                vh.AddVert(center + d * (half + Feather), clear, Vector2.zero);
                if (i == 0) continue;
                int p = mid + 1 + (i - 1) * 2, q = mid + 1 + i * 2;
                vh.AddTriangle(mid, p, q);
                vh.AddTriangle(p, p + 1, q + 1);
                vh.AddTriangle(p, q + 1, q);
            }
        }
    }
}

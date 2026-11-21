using UnityEngine;
using UnityEngine.UI;

namespace FinanceQuest
{
    // Tło kart rozdziałów: jedna łagodna linia wykresu z delikatnym wypełnieniem pod spodem.
    // Rysowana siatką (bez tekstury), więc jest ostra przy każdym rozmiarze karty i nic nie kosztuje w pamięci.
    // Krawędzie linii mają cienki, zanikający brzeg – zamiast „schodków”.
    public class WaveLine : MaskableGraphic
    {
        public float Phase;                 // każdy rozdział ma inaczej ułożoną falę
        const float Step = 6f, HalfWidth = 1.5f, Feather = 0.9f, FillShare = 0.36f;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            float w = r.width, h = r.height;
            if (w <= 1f || h <= 1f) return;

            int n = Mathf.CeilToInt(w / Step) + 1;
            var pts = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                float x = Mathf.Min(w, i * Step);
                // Liczone od góry karty: lekko w górę w prawo, z jedną łagodną falą.
                // Trzymamy się dolnej części karty, żeby linia nie wchodziła pod napisy.
                float fromTop = h * 0.9f - 10f * Mathf.Sin(x / w * Mathf.PI * 1.2f + Phase) - x / w * h * 0.22f;
                pts[i] = new Vector2(r.xMin + x, r.yMax - fromTop);
            }

            Color32 line = color, clear = color, fill = color;
            clear.a = 0;
            fill.a = (byte)(color.a * 255f * FillShare);

            // Wypełnienie od linii do dołu karty.
            for (int i = 0; i < n; i++)
            {
                vh.AddVert(pts[i], fill, Vector2.zero);
                vh.AddVert(new Vector2(pts[i].x, r.yMin), fill, Vector2.zero);
                if (i > 0) { int k = i * 2; vh.AddTriangle(k - 2, k, k + 1); vh.AddTriangle(k - 2, k + 1, k - 1); }
            }

            // Linia: rdzeń + zanikający brzeg z obu stron.
            int start = vh.currentVertCount;
            for (int i = 0; i < n; i++)
            {
                var d = pts[Mathf.Min(n - 1, i + 1)] - pts[Mathf.Max(0, i - 1)];
                var nrm = new Vector2(-d.y, d.x).normalized;
                vh.AddVert(pts[i] + nrm * (HalfWidth + Feather), clear, Vector2.zero);
                vh.AddVert(pts[i] + nrm * HalfWidth, line, Vector2.zero);
                vh.AddVert(pts[i] - nrm * HalfWidth, line, Vector2.zero);
                vh.AddVert(pts[i] - nrm * (HalfWidth + Feather), clear, Vector2.zero);
                if (i == 0) continue;
                int a = start + (i - 1) * 4, b = start + i * 4;
                for (int s = 0; s < 3; s++)
                {
                    vh.AddTriangle(a + s, b + s, b + s + 1);
                    vh.AddTriangle(a + s, b + s + 1, a + s + 1);
                }
            }
        }
    }
}

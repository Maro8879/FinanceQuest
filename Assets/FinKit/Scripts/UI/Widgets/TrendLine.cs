using UnityEngine;
using UnityEngine.UI;

namespace FinanceQuest
{
    // Tło kart rozdziałów: łamana linia „giełdowa” – trend wzrostowy z lekkimi spadkami po drodze.
    // Wychodzi spod dolnej krawędzi mniej więcej w połowie karty i rośnie w prawo; lewa strona zostaje pusta, bo tam są napisy.
    // Pole pod linią jest lekko jaśniejsze (ten sam kolor, mniej przezroczysty), żeby wykres był czytelny.
    // Rysowana siatką (bez tekstury), więc jest ostra przy każdym rozmiarze karty i nic nie kosztuje w pamięci.
    public class TrendLine : MaskableGraphic
    {
        public int Seed;                    // każdy rozdział ma inaczej ułożoną linię
        const int Points = 9;
        const float HalfWidth = 1.4f, Feather = 0.9f, FillShare = 0.4f;   // wypełnienie = 40% przezroczystości linii

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            float w = r.width, h = r.height;
            if (w <= 1f || h <= 1f) return;

            // Punkty: pierwszy pod dolną krawędzią karty (maska go przytnie – linia „wychodzi” spod karty),
            // ostatni lekko za prawą krawędzią. Co drugi krok to mały spadek, pozostałe to wyraźne wzrosty,
            // więc całość rośnie, ale nie jest prostą kreską. Wysokość liczona od dołu karty.
            var pts = new Vector2[Points];
            uint s = 7919u * (uint)(Seed + 1) + 12345u;
            float v = -0.3f;
            for (int i = 0; i < Points; i++)
            {
                if (i > 0)
                {
                    bool dip = i % 2 == 0 && i < Points - 1;
                    v += dip ? -(0.04f + Rand(ref s) * 0.07f) : 0.2f + Rand(ref s) * 0.1f;
                }
                // Odstępy maleją w prawo: na początku długi spokojny odcinek, potem gęstsze wahania.
                float t = 1f - Mathf.Pow(1f - i / (float)(Points - 1), 1.35f);
                float x = w * 0.42f + (w * 0.58f + 6f) * t;
                // +15% wysokości: punkty przegięcia siedzą wyżej, a pierwszy i tak jest pod kartą (v < 0).
                pts[i] = new Vector2(r.xMin + x, r.yMin + h * (0.15f + 0.7f * Mathf.Min(v, 1f)));
            }

            Color32 fill = color;
            fill.a = (byte)(color.a * 255f * FillShare);
            // Wypełnienie od linii do dołu karty (i trochę niżej – maska przytnie).
            for (int i = 0; i < Points; i++)
            {
                vh.AddVert(pts[i], fill, Vector2.zero);
                vh.AddVert(new Vector2(pts[i].x, r.yMin - 10f), fill, Vector2.zero);
                if (i > 0) { int k = i * 2; vh.AddTriangle(k - 2, k, k + 1); vh.AddTriangle(k - 2, k + 1, k - 1); }
            }
            int start = vh.currentVertCount;

            Color32 line = color, clear = color;
            clear.a = 0;

            // Linia: rdzeń + zanikający brzeg z obu stron (zamiast „schodków”).
            // Na załamaniach normalna jest uśredniona i wydłużona (miter), żeby linia nie chudła na rogach.
            for (int i = 0; i < Points; i++)
            {
                Vector2 dirIn = i > 0 ? (pts[i] - pts[i - 1]).normalized : (pts[1] - pts[0]).normalized;
                Vector2 dirOut = i < Points - 1 ? (pts[i + 1] - pts[i]).normalized : dirIn;
                Vector2 segN = new Vector2(-dirIn.y, dirIn.x);
                Vector2 nrm = new Vector2(-(dirIn.y + dirOut.y), dirIn.x + dirOut.x).normalized;
                float miter = 1f / Mathf.Max(0.5f, Vector2.Dot(nrm, segN));
                vh.AddVert(pts[i] + nrm * (HalfWidth + Feather) * miter, clear, Vector2.zero);
                vh.AddVert(pts[i] + nrm * HalfWidth * miter, line, Vector2.zero);
                vh.AddVert(pts[i] - nrm * HalfWidth * miter, line, Vector2.zero);
                vh.AddVert(pts[i] - nrm * (HalfWidth + Feather) * miter, clear, Vector2.zero);
                if (i == 0) continue;
                int a = start + (i - 1) * 4, b = start + i * 4;
                for (int k = 0; k < 3; k++)
                {
                    vh.AddTriangle(a + k, b + k, b + k + 1);
                    vh.AddTriangle(a + k, b + k + 1, a + k + 1);
                }
            }
        }

        // Prosty generator liczb (0..1) o stałym ziarnie – linia wygląda tak samo na każdym urządzeniu.
        static float Rand(ref uint s)
        {
            s = s * 1664525u + 1013904223u;
            return (s >> 8) / 16777216f;
        }
    }
}

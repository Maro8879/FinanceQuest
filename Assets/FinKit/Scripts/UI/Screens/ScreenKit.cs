using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FinanceQuest
{
    // Wspólne części ekranów: przewijana lista, bloki tytułów, paski.
    public static class ScreenKit
    {
        public const float Margin = 16f;
        public const float Lip = 5f;
        public const float CardRadius = 20f;

        // Przewijana lista pod nagłówkiem. Zwraca Content, do którego wkładamy elementy (współrzędne od góry).
        public static RectTransform Scroll(RectTransform screen, float width, float top)
        {
            var view = Ui.Rect("Scroll", screen);
            view.anchorMin = Vector2.zero;
            view.anchorMax = Vector2.one;
            view.offsetMin = Vector2.zero;
            view.offsetMax = new Vector2(0f, -top);

            // Przezroczyste tło łapie przeciąganie w pustych miejscach listy.
            var hit = view.gameObject.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);
            view.gameObject.AddComponent<RectMask2D>();

            var content = Ui.Rect("Content", view);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 100f);

            var sr = view.gameObject.AddComponent<ScrollRect>();
            sr.viewport = view;
            sr.content = content;
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Elastic;
            sr.decelerationRate = 0.135f;
            sr.scrollSensitivity = 20f;
            return content;
        }

        public static void SetContentHeight(RectTransform content, float h)
        {
            content.sizeDelta = new Vector2(0f, h);
        }

        // Wiersz „tytuł + podtytuł”; zwraca wysokość, żeby karta mogła się dopasować.
        public static float TitleBlock(Transform parent, float x, float y, float w, string title, float titleSize,
            string subtitle, Color titleColor, Color subColor)
        {
            var t = Ui.Text("Title", parent, title, titleSize, titleColor);
            float th = Ui.TextHeight(t, w);
            Ui.Place(t.rectTransform, x, y, w, th);
            float used = th;
            if (!string.IsNullOrEmpty(subtitle))
            {
                var s = Ui.Text("Subtitle", parent, subtitle, 12.5f, subColor, FontWeight.SemiBold);
                Ui.Place(s.rectTransform, x, y + th + 1, w, 18);
                used += 19;
            }
            return used;
        }

        // Pasek z procentem po prawej.
        public static void BarRow(Transform parent, float x, float y, float w, float value, Color track, Color pctColor, string label)
        {
            // Procent stoi tuż za paskiem (wyrównany do lewej), więc odstęp jest zawsze taki sam.
            const float labelW = 40f, gap = 6f;
            ProgressBar.Create(parent, x, y, w - labelW - gap, 16f, track, value);
            // Capline = środek między linią bazową a wysokością wielkich liter. Cyfry nie mają ogonków,
            // więc tak wyrównane stoją dokładnie na wysokości środka paska (Midline brał pod uwagę ogonki i podnosił je).
            var p = Ui.Text("Pct", parent, label, 13f, pctColor, FontWeight.ExtraBold, TextAlignmentOptions.CaplineLeft);
            Ui.Place(p.rectTransform, x + w - labelW, y, labelW, 16f);
        }

        // Licznik gwiazdek „★ 5/12” w jednej linii (np. za podpisem karty). Zwraca szerokość; nic nie rysuje, gdy max = 0.
        public static float StarCount(Transform parent, float x, float y, int got, int max, Color starColor, Color textColor)
        {
            if (max <= 0) return 0f;
            const float star = 13f, h = 18f;
            var icon = Ui.Icon("StarIcon", parent, IconId.Star, starColor, star);
            Ui.Place(icon.rectTransform, x, y + (h - star) * 0.5f - 1f, star, star);
            var t = Ui.Text("StarCount", parent, got + "/" + max, 12.5f, textColor, FontWeight.ExtraBold, TextAlignmentOptions.MidlineLeft);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            float tw = t.GetPreferredValues(t.text, 1000f, 0f).x;
            Ui.Place(t.rectTransform, x + star + 3f, y, tw + 2f, h);
            return star + 3f + tw;
        }

        public static string Pct(float v) { return Mathf.RoundToInt(v * 100f) + "%"; }
    }
}

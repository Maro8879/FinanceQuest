using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FinanceQuest
{
    // Wspólne części ekranów: nagłówek i przewijana lista.
    public static class ScreenKit
    {
        public const float HeaderHeight = 68f;
        public const float Margin = 16f;
        public const float Lip = 5f;
        public const float CardRadius = 20f;

        // Nagłówek: „Finance Quest” po lewej, tytuł / hasło na środku, przycisk ikony po prawej.
        public static void Header(RectTransform screen, float width, string center, bool isTagline,
            IconId rightIcon, Color rightIconColor, Action onRight)
        {
            var bar = Ui.Rect("Header", screen);
            Ui.Place(bar, 0, 0, width, HeaderHeight);

            var logo = Ui.Text("Logo", bar, "Finance\nQuest", 15f, Palette.Brand, true, TextAlignmentOptions.MidlineLeft);
            logo.lineSpacing = -12f;
            Ui.Place(logo.rectTransform, Margin, 8, 90, 52);

            const float btn = 40f;
            float sideW = 92f;
            var title = Ui.Text("Title", bar, center,
                isTagline ? 12f : 17f, isTagline ? Palette.TextMuted : Palette.Text, true, TextAlignmentOptions.Center);
            Ui.Place(title.rectTransform, Margin + sideW - 6, 8, width - 2 * (Margin + sideW - 6), 52);
            title.enableAutoSizing = !isTagline;   // długi tytuł kursu ma się zmieścić w dwóch liniach
            title.fontSizeMin = 13f;
            title.fontSizeMax = 17f;

            var b = LipCard.Create(bar, "RightButton", width - Margin - btn, 12, btn, btn + 3,
                Palette.CardFill, Palette.CardEdge, Palette.CardLip, 12f, 3f, 2f);
            var ic = Ui.Icon("Icon", b.Content, rightIcon, rightIconColor, 20f);
            Ui.Center(ic.rectTransform, 20, 20);
            b.SetClickable(onRight);
        }

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
                var s = Ui.Text("Subtitle", parent, subtitle, 12.5f, subColor);
                Ui.Place(s.rectTransform, x, y + th + 1, w, 18);
                used += 19;
            }
            return used;
        }

        // Pasek z procentem po prawej.
        public static void BarRow(Transform parent, float x, float y, float w, float value, Color track, Color pctColor, string label)
        {
            const float labelW = 44f;
            ProgressBar.Create(parent, x, y, w - labelW - 8f, 16f, track, value);
            var p = Ui.Text("Pct", parent, label, 13f, pctColor, true, TextAlignmentOptions.MidlineRight);
            Ui.Place(p.rectTransform, x + w - labelW, y - 2, labelW, 20);
        }

        public static string Pct(float v) { return Mathf.RoundToInt(v * 100f) + "%"; }
    }
}

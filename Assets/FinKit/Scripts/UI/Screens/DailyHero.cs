using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FinanceQuest
{
    // Karta „Myśl na dziś” nad trofeami: Wall Street w tle (dzień albo noc – według „Trybu ciemnego”),
    // cytat na półprzezroczystym panelu: w nocy ciemnym z białym tekstem, w dzień jasnym z ciemnym tekstem.
    public static class DailyHero
    {
        const float Radius = 22f;
        static readonly Color PanelNight = new Color(8f / 255f, 32f / 255f, 38f / 255f, 0.78f);
        static readonly Color PanelDay = new Color(1f, 1f, 1f, 0.84f);

        // Zwraca wysokość karty.
        public static float Build(RectTransform parent, float x, float y, float w)
        {
            // Wysokość z proporcji rysunku, żeby obraz się nie rozciągał.
            float h = Mathf.Round(w * WallStreetArt.H / WallStreetArt.W);
            var root = Ui.Rect("DailyThought", parent);
            Ui.Place(root, x, y, w, h);

            // Zaokrąglona maska, w niej rysunek.
            var mask = Ui.Rounded("Mask", root, Color.white, Radius);
            Ui.Stretch(mask.rectTransform);
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var art = Ui.Rect("Art", mask.transform).gameObject.AddComponent<Image>();
            bool night = Settings.DarkMode;
            art.sprite = night ? WallStreetArt.Night : WallStreetArt.Day;
            art.raycastTarget = false;
            Ui.Stretch(art.rectTransform);

            // Panel z cytatem – półprzezroczysty, żeby wysokie budynki mogły sięgać pod tekst.
            const float inset = 10f, pad = 12f;
            var panel = Ui.Rounded("Panel", root, night ? PanelNight : PanelDay, 14f);
            var label = Ui.Text("Label", panel.transform, "MYŚL NA DZIŚ", 11.5f, night ? new Color(1f, 1f, 1f, 0.67f) : Palette.Brand,
                FontWeight.ExtraBold, TextAlignmentOptions.TopLeft);
            label.characterSpacing = 6f;
            Ui.Place(label.rectTransform, pad, pad, w - 2 * inset - 2 * pad, 14);

            float tw = w - 2 * inset - 2 * pad;
            var quote = Ui.Text("Quote", panel.transform, "„" + DailyThoughts.Today() + "”", 18f, night ? Color.white : Palette.Text,
                FontWeight.ExtraBold, TextAlignmentOptions.TopLeft);
            float th = Ui.TextHeight(quote, tw);
            Ui.Place(quote.rectTransform, pad, pad + 19, tw, th);
            Ui.Place(panel.rectTransform, inset, inset, w - 2 * inset, pad + 19 + th + pad);
            return h;
        }
    }
}

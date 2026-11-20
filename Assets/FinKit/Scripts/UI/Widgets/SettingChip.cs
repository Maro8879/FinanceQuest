using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FinanceQuest
{
    // Przycisk-przełącznik w kokpicie. Ikona i podpis zależą od stanu (np. księżyc ↔ słońce,
    // dzwonek ↔ przekreślony dzwonek). Przełącza się w miejscu, bez przebudowy ekranu.
    public class SettingChip
    {
        LipCard card;
        Image icon;
        TextMeshProUGUI label;
        bool on, tintWhenOn;
        Func<bool, IconId> iconFor;
        Func<bool, string> textFor;

        // tintWhenOn: czy włączony stan ma petrolowe tło (dla języka nie – to wybór, a nie włącz/wyłącz).
        public static SettingChip Create(Transform parent, float x, float y, float w, float h,
            Func<bool, IconId> iconFor, Func<bool, string> textFor, bool tintWhenOn, bool on, Action<bool> onToggle)
        {
            var c = new SettingChip { iconFor = iconFor, textFor = textFor, tintWhenOn = tintWhenOn };
            c.card = LipCard.Create(parent, "Chip", x, y, w, h, Palette.CardFill, Palette.CardEdge, Palette.CardLip, 12f, 3f, 2f);
            c.icon = Ui.Icon("Icon", c.card.Content, iconFor(on), Palette.TextMuted, 18f);
            var irt = c.icon.rectTransform;
            irt.anchorMin = irt.anchorMax = new Vector2(0.5f, 1f);
            irt.pivot = new Vector2(0.5f, 1f);
            irt.anchoredPosition = new Vector2(0f, -9f);
            c.label = Ui.Text("Label", c.card.Content, textFor(on), 11.5f, Palette.TextMuted, FontWeight.SemiBold, TextAlignmentOptions.Capline);
            c.label.textWrappingMode = TextWrappingModes.NoWrap;
            c.label.enableAutoSizing = true;
            c.label.fontSizeMin = 9f;
            c.label.fontSizeMax = 11.5f;
            var lrt = c.label.rectTransform;
            lrt.anchorMin = new Vector2(0f, 0f);
            lrt.anchorMax = new Vector2(1f, 0f);
            lrt.pivot = new Vector2(0.5f, 0f);
            lrt.offsetMin = new Vector2(4f, 8f);
            lrt.offsetMax = new Vector2(-4f, 22f);

            c.Apply(on);
            c.card.SetClickable(() =>
            {
                c.Apply(!c.on);
                if (onToggle != null) onToggle(c.on);
            });
            return c;
        }

        void Apply(bool value)
        {
            on = value;
            bool tinted = on && tintWhenOn;
            card.Fill.color = tinted ? Palette.Brand : Palette.CardFill;
            card.Edge.color = tinted ? Palette.Brand : Palette.CardEdge;
            card.Lip.color = tinted ? Palette.Darker(Palette.Brand) : Palette.CardLip;
            Color fg = tinted ? Color.white : Palette.TextMuted;
            icon.sprite = ProceduralSprites.Icon(iconFor(on));
            icon.color = fg;
            label.text = textFor(on);
            label.color = fg;
        }
    }
}

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FinanceQuest
{
    // Wiersz ustawień: ikona (zależna od stanu), nazwa i po prawej przełącznik albo wartość (np. język).
    // Cały wiersz jest klikalny i przełącza się w miejscu, bez przebudowy ekranu.
    public class SettingRow
    {
        const float H = 60f, TrackW = 46f, TrackH = 28f, Knob = 22f;

        LipCard card;
        Image icon, track;
        RectTransform knob;
        TextMeshProUGUI value;
        bool on;
        Func<bool, IconId> iconFor;
        Func<bool, string> valueFor;   // null = przełącznik zamiast tekstu

        public static float Height { get { return H + ScreenKit.Lip; } }

        public static SettingRow Create(Transform parent, float x, float y, float w, string title,
            Func<bool, IconId> iconFor, Func<bool, string> valueFor, bool on, Action<bool> onToggle)
        {
            var r = new SettingRow { iconFor = iconFor, valueFor = valueFor };
            r.card = LipCard.Create(parent, "Setting_" + title, x, y, w, H + ScreenKit.Lip,
                Palette.CardFill, Palette.CardEdge, Palette.CardLip, 16f, ScreenKit.Lip, 2f);
            var root = r.card.Content;

            r.icon = Ui.Icon("Icon", root, iconFor(on), Palette.TextMuted, 22f);
            Ui.Place(r.icon.rectTransform, 16, (H - 22f) * 0.5f, 22, 22);

            var t = Ui.Text("Title", root, title, 15f, Palette.Text, FontWeight.SemiBold, TextAlignmentOptions.CaplineLeft);
            Ui.Place(t.rectTransform, 50, (H - 20f) * 0.5f, w - 50 - 90, 20);

            if (valueFor == null)
            {
                // Przełącznik: tor + biała gałka (pozycja = stan).
                r.track = Ui.Rounded("Track", root, Palette.CardEdge, TrackH * 0.5f);
                Ui.Place(r.track.rectTransform, w - 16 - TrackW, (H - TrackH) * 0.5f, TrackW, TrackH);
                var k = Ui.Rounded("Knob", r.track.transform, Color.white, Knob * 0.5f);
                r.knob = k.rectTransform;
                r.knob.anchorMin = r.knob.anchorMax = new Vector2(0f, 0.5f);
                r.knob.pivot = new Vector2(0f, 0.5f);
                r.knob.sizeDelta = new Vector2(Knob, Knob);
            }
            else
            {
                r.value = Ui.Text("Value", root, "", 14f, Palette.TextMuted, FontWeight.SemiBold, TextAlignmentOptions.CaplineRight);
                Ui.Place(r.value.rectTransform, w - 16 - 120, (H - 20f) * 0.5f, 120, 20);
            }

            r.Apply(on);
            r.card.SetClickable(() =>
            {
                r.Apply(!r.on);
                if (onToggle != null) onToggle(r.on);
            });
            return r;
        }

        void Apply(bool v)
        {
            on = v;
            icon.sprite = ProceduralSprites.Icon(iconFor(on));
            if (valueFor == null)
            {
                track.color = on ? Palette.Brand : Palette.CardEdge;
                float pad = (TrackH - Knob) * 0.5f;
                knob.anchoredPosition = new Vector2(on ? TrackW - Knob - pad : pad, 0f);
            }
            else value.text = valueFor(on);
        }
    }
}

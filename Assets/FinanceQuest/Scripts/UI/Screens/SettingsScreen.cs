using UnityEngine;

namespace FinanceQuest
{
    // Ekran ustawień – otwierany zębatką w menu. Ikony pokazują stan
    // (ciemny → słońce, wyłączone dźwięki/przypomnienia → przekreślone).
    public static class SettingsScreen
    {
        public static void Build(RectTransform screen, FinanceQuestApp app)
        {
            float W = app.ScreenWidth;
            app.Header.Set("Ustawienia", false, IconId.Back, Palette.TextLocked, app.Back);

            var content = ScreenKit.Scroll(screen, W, 0f);
            float m = ScreenKit.Margin, cw = W - 2 * m, y = 6f, step = SettingRow.Height + 12f;

            SettingRow.Create(content, m, y, cw, "Tryb ciemny", on => on ? IconId.Sun : IconId.Moon, null,
                Settings.DarkMode, v => Settings.DarkMode = v);
            y += step;
            SettingRow.Create(content, m, y, cw, "Dźwięki", on => on ? IconId.Sound : IconId.SoundOff, null,
                Settings.Sound, v => Settings.Sound = v);
            y += step;
            SettingRow.Create(content, m, y, cw, "Przypomnienia", on => on ? IconId.Bell : IconId.BellOff, null,
                Settings.Reminders, v => Settings.Reminders = v);
            y += step;
            // Język: wybór z dwóch – po prawej nazwa języka zamiast przełącznika.
            SettingRow.Create(content, m, y, cw, "Język", on => IconId.Language, on => on ? "English" : "Polski",
                Settings.English, v => Settings.English = v);
            y += step;

            ScreenKit.SetContentHeight(content, y + 24f);
        }
    }
}

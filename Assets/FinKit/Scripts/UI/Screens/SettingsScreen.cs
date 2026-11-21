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
            app.Header.Set("Settings", false, IconId.Back, Palette.TextLocked, app.Back);

            var content = ScreenKit.Scroll(screen, W, 0f);
            float m = ScreenKit.Margin, cw = W - 2 * m, y = 6f, step = SettingRow.Height + 12f;

            SettingRow.Create(content, m, y, cw, "Dark mode", on => on ? IconId.Sun : IconId.Moon, null,
                Settings.DarkMode, v => Settings.DarkMode = v);
            y += step;
            SettingRow.Create(content, m, y, cw, "Sounds", on => on ? IconId.Sound : IconId.SoundOff, null,
                Settings.Sound, v => Settings.Sound = v);
            y += step;
            SettingRow.Create(content, m, y, cw, "Vibration", on => on ? IconId.Vibrate : IconId.VibrateOff, null,
                Settings.Vibration, v => Settings.Vibration = v);
            y += step;
            SettingRow.Create(content, m, y, cw, "Reminders", on => on ? IconId.Bell : IconId.BellOff, null,
                Settings.Reminders, v => Settings.Reminders = v);
            y += step;
            // Bez wyboru języka – aplikacja ma jedną wersję językową.

            // Ankieta od nowa – po zakończeniu menu pokaże nowe polecane kursy.
            var redo = LipCard.Create(content, "RedoSurvey", m, y, cw, SettingRow.Height, Palette.CardFill, Palette.CardEdge, Palette.CardLip,
                16f, ScreenKit.Lip, 2f);
            var rt = Ui.Text("Label", redo.Content, "Retake the survey", 16f, Palette.Text, FontWeight.ExtraBold, TMPro.TextAlignmentOptions.MidlineLeft);
            Ui.Stretch(rt.rectTransform, 18f, 0f, 18f, 0f);
            redo.SetClickable(() => app.Push(r => SurveyScreen.Build(r, app)));
            y += step;

            // Reset postępu: pierwsze stuknięcie tylko pyta, drugie (w ciągu 3 s) naprawdę kasuje – łatwo o przypadkowy dotyk.
            var reset = LipCard.Create(content, "ResetProgress", m, y, cw, SettingRow.Height, Palette.CardFill, Palette.CardEdge, Palette.CardLip,
                16f, ScreenKit.Lip, 2f);
            const string ask = "Reset progress in all courses";
            var rl = Ui.Text("Label", reset.Content, ask, 16f, Palette.Brick, FontWeight.ExtraBold, TMPro.TextAlignmentOptions.MidlineLeft);
            Ui.Stretch(rl.rectTransform, 18f, 0f, 18f, 0f);
            int armed = 0;   // licznik prób – stare odliczanie nie cofa napisu, jeśli w międzyczasie coś kliknięto
            reset.SetClickable(() =>
            {
                if (rl.text == ask)
                {
                    rl.text = "Sure? Tap again";
                    int my = ++armed;
                    FinanceQuestApp.RunLater(3f, () => { if (rl != null && my == armed && rl.text != ask) rl.text = ask; });
                }
                else if (rl.text != "Progress reset")
                {
                    app.ResetProgress();
                    armed++;
                    rl.text = "Progress reset";
                    rl.color = Palette.TextMuted;
                    FinanceQuestApp.RunLater(2f, () => { if (rl != null) { rl.text = ask; rl.color = Palette.Brick; } });
                }
            });
            y += step;

            ScreenKit.SetContentHeight(content, y + 24f);
        }
    }
}

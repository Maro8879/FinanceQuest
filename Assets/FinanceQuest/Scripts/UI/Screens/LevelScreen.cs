using TMPro;
using UnityEngine;

namespace FinanceQuest
{
    // Tymczasowy ekran levelu (etap 1). Przycisk „Ukończ (test)” pozwala już teraz sprawdzić
    // odblokowywanie, złoto i paski postępu, zanim powstaną prawdziwe lekcje i zadania.
    public static class LevelScreen
    {
        public static void Build(RectTransform screen, FinanceQuestApp app, CourseData course, int chapterIndex, int levelIndex)
        {
            float W = app.ScreenWidth;
            var ch = course.Chapters[chapterIndex];
            var level = ch.Levels[levelIndex];
            app.Header.Set(ch.Title, false, IconId.Home, Palette.TextLocked, app.Home);

            float m = ScreenKit.Margin, cw = W - 2 * m, y = 8f;

            var card = LipCard.Create(screen, "Info", m, y, cw, 150, Palette.CardFill, Palette.CardEdge, Palette.CardLip,
                ScreenKit.CardRadius, ScreenKit.Lip, 2f);
            var ic = Ui.Icon("Icon", card.Content, ContentText.KindIcon(level.Kind), ch.Color, 40f);
            Ui.Place(ic.rectTransform, 18, 18, 40, 40);
            var t = Ui.Text("Title", card.Content, "Level " + level.Number + " · " + ContentText.KindLabel(level.Kind),
                20f, Palette.Text);
            Ui.Place(t.rectTransform, 72, 22, cw - 90, 30);
            var d = Ui.Text("Desc", card.Content, "Tu pojawi się treść levelu (tekst, mini gra lub zadanie).", 14f,
                Palette.TextMuted, FontWeight.Regular);
            Ui.Place(d.rectTransform, 18, 74, cw - 36, 60);

            y += 150 + 20f;
            var done = LipCard.Create(screen, "Complete", m, y, cw, 54, Palette.Accent, Palette.Accent, Palette.AccentLip,
                16f, 5f, 0f);
            var dl = Ui.Text("Label", done.Content, "UKOŃCZ (TEST)", 16f, Palette.OnAccent, FontWeight.ExtraBold, TextAlignmentOptions.Capline);
            Ui.Stretch(dl.rectTransform);
            done.SetClickable(() => { app.Progress.Complete(level); app.Back(); });

            y += 54 + 14f;
            var back = LipCard.Create(screen, "Back", m, y, cw, 54, Palette.CardFill, Palette.CardEdge, Palette.CardLip,
                16f, 5f, 2f);
            var bl = Ui.Text("Label", back.Content, "WRÓĆ", 16f, Palette.TextMuted, FontWeight.ExtraBold, TextAlignmentOptions.Capline);
            Ui.Stretch(bl.rectTransform);
            back.SetClickable(app.Back);
        }
    }
}

using UnityEngine;

namespace FinanceQuest
{
    // Ekran 1: kokpit (trofea + ustawienia) i lista kursów z paskiem postępu każdego kursu.
    public static class MenuScreen
    {
        public static void Build(RectTransform screen, FinanceQuestApp app)
        {
            float W = app.ScreenWidth;
            // Zębatka w prawym górnym rogu otwiera ekran ustawień.
            app.Header.Set("an easy and fun way to learn finance", true, IconId.Gear, Palette.TextMuted,
                () => app.Push(r => SettingsScreen.Build(r, app)));

            var content = ScreenKit.Scroll(screen, W, 0f);
            var last = app.LastCourse;
            if (last != null) app.SwipeForward = () => app.Push(r => CourseScreen.Build(r, app, last), true);
            float y = 6f, cw = W - 2 * ScreenKit.Margin;

            // Wstęp: „Myśl na dziś” na tle nocnej Wall Street – dużo przestrzeni, nic nie przytłacza.
            y += DailyHero.Build(content, ScreenKit.Margin, y, cw) + 24f;

            // Kokpit nad kursami: półki z trofeami.
            y += Cockpit.Build(content, ScreenKit.Margin, y, cw, app) + 18f;

            var label = Ui.Text("CoursesLabel", content, "KURSY", 12.5f, Palette.TextMuted, FontWeight.ExtraBold, TMPro.TextAlignmentOptions.CaplineLeft);
            label.characterSpacing = 4f;
            Ui.Place(label.rectTransform, ScreenKit.Margin + 4, y, cw, 16);
            y += 16f + 10f;

            foreach (var course in app.Courses)
            {
                y += CourseCard(content, ScreenKit.Margin, y, cw, course, app) + 14f;
            }
            ScreenKit.SetContentHeight(content, y + 24f);
        }

        // Cała karta w kolorze kursu, po prawej ilustracja tematyczna (mieści się w karcie z marginesem).
        static float CourseCard(RectTransform parent, float x, float y, float w, CourseData course, FinanceQuestApp app)
        {
            const float pad = 16f, artMargin = 10f, gap = 8f;
            float artW = Illustrations.BoxW, artH = Illustrations.BoxH;
            float textW = w - pad - artW - artMargin - gap;
            float value = app.Progress.CourseProgress(course);

            // Najpierw karta o tymczasowej wysokości – wysokość znamy dopiero po zmierzeniu tytułu.
            var card = LipCard.Create(parent, "Course_" + course.Id, x, y, w, 100,
                course.Color, course.Color, Palette.Darker(course.Color), ScreenKit.CardRadius, ScreenKit.Lip, 0f);

            string sub = ContentText.Plural(course.Chapters.Count, "rozdział", "rozdziały", "rozdziałów") + " · " +
                         ContentText.Plural(course.LevelCount, "level", "levele", "leveli");
            float th = ScreenKit.TitleBlock(card.Content, pad, pad, textW, course.Title, 17f, sub, Color.white, Palette.WhiteSoft);
            float barY = pad + th + 9f;
            ScreenKit.BarRow(card.Content, pad, barY, textW, value, Palette.TrackOnColor, Color.white, ScreenKit.Pct(value));

            // Karta musi pomieścić ilustrację z marginesem 8 u góry i u dołu.
            float face = Mathf.Max(barY + 16f + pad, artH + 16f);
            ((RectTransform)card.transform).sizeDelta = new Vector2(w, face + ScreenKit.Lip);
            card.AddIllustration(Illustrations.Get(course.Art, course.Color), artW, artH, artMargin);

            card.SetClickable(() => app.Push(r => CourseScreen.Build(r, app, course)));
            return face + ScreenKit.Lip;
        }
    }
}

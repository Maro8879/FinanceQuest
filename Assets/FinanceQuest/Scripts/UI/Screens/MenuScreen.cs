using UnityEngine;

namespace FinanceQuest
{
    // Ekran 1: lista kursów z paskiem postępu każdego kursu.
    public static class MenuScreen
    {
        public static void Build(RectTransform screen, FinanceQuestApp app)
        {
            float W = app.ScreenWidth;
            ScreenKit.Header(screen, W, "an easy and fun way\nto learn finance", true, IconId.Moon,
                Palette.Hex("8B8BD6"), () => Debug.Log("Tryb ciemny dojdzie w kolejnym etapie."));

            var content = ScreenKit.Scroll(screen, W, ScreenKit.HeaderHeight);
            float y = 6f, cw = W - 2 * ScreenKit.Margin;

            foreach (var course in app.Courses)
            {
                y += CourseCard(content, ScreenKit.Margin, y, cw, course, app) + 14f;
            }
            ScreenKit.SetContentHeight(content, y + 24f);
        }

        static float CourseCard(RectTransform parent, float x, float y, float w, CourseData course, FinanceQuestApp app)
        {
            const float pad = 14f, tile = 54f, gap = 13f;
            float textX = pad + tile + gap, textW = w - textX - pad;
            float value = app.Progress.CourseProgress(course);

            // Najpierw karta o tymczasowej wysokości – wysokość znamy dopiero po zmierzeniu tytułu.
            var card = LipCard.Create(parent, "Course_" + course.Id, x, y, w, 100,
                Palette.CardFill, Palette.CardEdge, Palette.CardLip, ScreenKit.CardRadius, ScreenKit.Lip, 2f);

            string sub = ContentText.Plural(course.Chapters.Count, "rozdział", "rozdziały", "rozdziałów") + " · " +
                         ContentText.Plural(course.LevelCount, "level", "levele", "leveli");
            float th = ScreenKit.TitleBlock(card.Content, textX, pad, textW, course.Title, 16f, sub, Palette.Text, Palette.TextMuted);
            float barY = pad + th + 7f;
            ScreenKit.BarRow(card.Content, textX, barY, textW, value, Palette.Track,
                value > 0f ? Palette.Orange : Palette.TextLocked, ScreenKit.Pct(value));

            float face = Mathf.Max(barY + 16f + pad, tile + 2 * pad);
            ((RectTransform)card.transform).sizeDelta = new Vector2(w, face + ScreenKit.Lip);

            // Kolorowy kafelek z ikoną kursu (też z wargą, ale nieklikalny osobno).
            var t = LipCard.Create(card.Content, "Tile", pad, (face - tile) * 0.5f - 2f, tile, tile + 4f,
                course.Color, course.Color, Palette.Darker(course.Color), 16f, 4f, 0f);
            var ic = Ui.Icon("Icon", t.Content, course.Icon, Color.white, 30f);
            Ui.Center(ic.rectTransform, 30, 30);

            card.SetClickable(() => app.Push(r => CourseScreen.Build(r, app, course)));
            return face + ScreenKit.Lip;
        }
    }
}

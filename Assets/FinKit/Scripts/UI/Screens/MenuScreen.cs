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

            // „Kontynuuj”: następny level w kursie, który właśnie robisz (znika, gdy wszystko ukończone).
            float cont = ContinueCard.Build(content, ScreenKit.Margin, y, cw, app);
            if (cont > 0f) y += cont + 22f;

            // Zaraz po ankiecie: krótki komunikat, ile kursów polecamy (pokazuje się raz).
            var recommended = Recommendations.Get();
            if (Recommendations.JustFinished && recommended.Count > 0)
            {
                Recommendations.JustFinished = false;
                y += RecommendNote(content, ScreenKit.Margin, y, cw, recommended.Count) + 16f;
            }

            var label = Ui.Text("CoursesLabel", content, "KURSY", 12.5f, Palette.TextMuted, FontWeight.ExtraBold, TMPro.TextAlignmentOptions.CaplineLeft);
            label.characterSpacing = 4f;
            Ui.Place(label.rectTransform, ScreenKit.Margin + 4, y, cw, 16);
            y += 16f + 10f;

            // Polecane kursy na górze (w kolejności z ankiety), potem reszta.
            var ordered = new System.Collections.Generic.List<CourseData>();
            foreach (var id in recommended) foreach (var c in app.Courses) if (c.Id == id) ordered.Add(c);
            foreach (var c in app.Courses) if (!ordered.Contains(c)) ordered.Add(c);
            foreach (var course in ordered)
            {
                bool rec = recommended.Contains(course.Id);
                y += CourseCard(content, ScreenKit.Margin, y + (rec ? 4f : 0f), cw, course, app, rec) + 14f + (rec ? 8f : 0f);
            }
            ScreenKit.SetContentHeight(content, y + 24f);
        }

        // Cała karta w kolorze kursu, po prawej ilustracja tematyczna (mieści się w karcie z marginesem).
        // Ciemny pasek „Mamy dla Ciebie N kursów” – wynik ankiety (wariant C).
        static float RecommendNote(RectTransform parent, float x, float y, float w, int count)
        {
            const float pad = 14f, icon = 26f, h = 62f;
            var bg = Ui.Rounded("RecommendNote", parent, Palette.Text, 16f);
            Ui.Place(bg.rectTransform, x, y, w, h);
            var ic = Ui.Icon("Spark", bg.transform, IconId.Spark, Palette.Accent, icon);
            Ui.Place(ic.rectTransform, pad, (h - icon) * 0.5f, icon, icon);
            float tx = pad + icon + 12f, tw = w - tx - pad;
            var t1 = Ui.Text("Title", bg.transform, "Mamy dla Ciebie " + ContentText.Plural(count, "kurs", "kursy", "kursów"), 14.5f, Color.white);
            Ui.Place(t1.rectTransform, tx, 12f, tw, 20f);
            var t2 = Ui.Text("Sub", bg.transform, count == 1 ? "Oznaczyliśmy go jako „Polecany”" : "Oznaczyliśmy je jako „Polecany”", 12f,
                new Color(1f, 1f, 1f, 0.7f), FontWeight.SemiBold);
            Ui.Place(t2.rectTransform, tx, 33f, tw, 18f);
            return h;
        }

        // Żółta plakietka „★ POLECANY DLA CIEBIE”; zwraca jej wysokość.
        static float RecommendedTag(Transform parent, float x, float y)
        {
            const float bh = 22f, star = 12f;
            var tag = Ui.Text("Label", parent, "POLECANY DLA CIEBIE", 11f, Palette.OnAccent, FontWeight.ExtraBold, TMPro.TextAlignmentOptions.CaplineLeft);
            tag.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            float bw = tag.GetPreferredValues("POLECANY DLA CIEBIE", 1000f, 0f).x + star + 22f;
            var pill = Ui.Rounded("RecommendedTag", parent, Palette.Accent, 8f);
            Ui.Place(pill.rectTransform, x, y, bw, bh);
            var si = Ui.Icon("Star", pill.transform, IconId.Star, Palette.OnAccent, star);
            Ui.Place(si.rectTransform, 8f, (bh - star) * 0.5f, star, star);
            tag.transform.SetParent(pill.transform, false);
            Ui.Place(tag.rectTransform, 8f + star + 4f, 0f, bw - star - 14f, bh);
            return bh;
        }

        static float CourseCard(RectTransform parent, float x, float y, float w, CourseData course, FinanceQuestApp app, bool recommended)
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
            // Polecany kurs: plakietka „★ POLECANY DLA CIEBIE” nad tytułem (po lewej – nie wchodzi na grafikę), tytuł niżej o jej wysokość.
            float top = pad;
            if (recommended) top += RecommendedTag(card.Content, pad, pad - 2f) + 8f;
            float th = ScreenKit.TitleBlock(card.Content, pad, top, textW, course.Title, 17f, sub, Color.white, Palette.WhiteSoft);
            float barY = top + th + 9f;
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

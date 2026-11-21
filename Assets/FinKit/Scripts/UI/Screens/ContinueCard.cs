using TMPro;
using UnityEngine;

namespace FinanceQuest
{
    // Karta „Kontynuuj” w menu (pod cytatem): następny level w kursie, który właśnie robisz.
    // Jedno kliknięcie przenosi prosto do levelu, a „wstecz” prowadzi przez rozdział i kurs.
    public static class ContinueCard
    {
        const float Pad = 16f, Radius = 22f, LipH = 6f, Border = 2f, ButtonH = 50f;

        // Zwraca wysokość karty (0, gdy wszystko ukończone).
        public static float Build(RectTransform parent, float x, float y, float w, FinanceQuestApp app)
        {
            var course = PickCourse(app);
            if (course == null) return 0f;
            int chIndex = -1, lvIndex = -1;
            for (int c = 0; c < course.Chapters.Count && chIndex < 0; c++)
                if (app.Progress.ChapterState(course, c) == ItemState.Current) chIndex = c;
            if (chIndex < 0) return 0f;
            var ch = course.Chapters[chIndex];
            for (int l = 0; l < ch.Levels.Count && lvIndex < 0; l++)
                if (app.Progress.LevelState(course, chIndex, l) == ItemState.Current) lvIndex = l;
            if (lvIndex < 0) return 0f;
            var level = ch.Levels[lvIndex];
            bool started = app.Progress.CourseProgress(course) > 0f;
            float inner = w - 2 * Pad;

            var card = LipCard.Create(parent, "Continue", x, y, w, 100, Palette.CardFill, Palette.Accent, Palette.AccentLip, Radius, LipH, Border);
            var root = card.Content;
            float cy = Pad - 2f;

            // Nagłówek karty + plakietka z nazwą kursu w jego kolorze.
            var label = Ui.Text("Label", root, started ? "KONTYNUUJ" : "ZACZNIJ", 11.5f, Palette.TextMuted, FontWeight.ExtraBold, TextAlignmentOptions.CaplineLeft);
            label.characterSpacing = 6f;
            Ui.Place(label.rectTransform, Pad, cy, inner * 0.5f, 20f);
            var tag = Ui.Text("Tag", root, course.ShortName, 11f, Color.white, FontWeight.ExtraBold, TextAlignmentOptions.Capline);
            tag.textWrappingMode = TextWrappingModes.NoWrap;
            float tagW = Mathf.Min(inner * 0.5f, tag.GetPreferredValues(course.ShortName, 10000f, 0f).x + 16f);
            var pill = Ui.Rounded("TagBg", root, course.Color, 8f);
            Ui.Place(pill.rectTransform, w - Pad - tagW, cy, tagW, 20f);
            tag.transform.SetParent(pill.transform, false);
            Ui.Stretch(tag.rectTransform);
            cy += 26f;

            // Tytuł levelu i gdzie on jest.
            var title = Ui.Text("Title", root, string.IsNullOrEmpty(level.Title) ? "Level " + level.Number : level.Title, 19f, Palette.Text);
            float th = Ui.TextHeight(title, inner);
            Ui.Place(title.rectTransform, Pad, cy, inner, th);
            cy += th + 2f;
            var where = Ui.Text("Where", root, "Rozdział " + (chIndex + 1) + " · " + ch.Title + " · level " + level.Number + " z " + ch.Levels.Count,
                12.5f, Palette.TextMuted, FontWeight.SemiBold);
            float wh = Ui.TextHeight(where, inner);
            Ui.Place(where.rectTransform, Pad, cy, inner, wh);
            cy += wh + 12f;

            // Kreski postępu rozdziału: zrobione w kolorze rozdziału, aktualny żółty, reszta szara.
            int n = ch.Levels.Count;
            const float dashGap = 6f, dashH = 8f;
            float dw = (inner - dashGap * (n - 1)) / n;
            for (int i = 0; i < n; i++)
            {
                var st = app.Progress.LevelState(course, chIndex, i);
                var dash = Ui.Rounded("Dash", root, st == ItemState.Done ? ch.Color : st == ItemState.Current ? Palette.Accent : Palette.Track, dashH * 0.5f);
                Ui.Place(dash.rectTransform, Pad + i * (dw + dashGap), cy, dw, dashH);
            }
            cy += dashH + 14f;

            // Duży żółty przycisk.
            var btn = LipCard.Create(root, "Go", Pad, cy, inner, ButtonH, Palette.Accent, Palette.Accent, Palette.AccentLip, 14f, 5f, 0f);
            var bl = Ui.Text("Label", btn.Content, started ? "KONTYNUUJ" : "ZACZNIJ", 16f, Palette.OnAccent, FontWeight.ExtraBold, TextAlignmentOptions.Capline);
            Ui.Stretch(bl.rectTransform);
            int ci = chIndex, li = lvIndex;
            btn.SetClickable(() =>
            {
                var lv = course.Chapters[ci].Levels[li];
                if (LevelScreen.HasContent(lv))
                    app.PushPath(
                        r => CourseScreen.Build(r, app, course),
                        r => ChapterScreen.Build(r, app, course, ci),
                        r => LevelScreen.Build(r, app, course, ci, li));
                else
                {
                    // Level bez treści: zaliczamy od razu i pokazujemy rozdział z odblokowaniem następnego.
                    app.CompleteLevel(course, lv);
                    app.PushPath(
                        r => CourseScreen.Build(r, app, course),
                        r => ChapterScreen.Build(r, app, course, ci));
                }
            });
            cy += ButtonH + Pad - 2f;

            float h = cy + 2 * Border + LipH;
            ((RectTransform)card.transform).sizeDelta = new Vector2(w, h);
            return h;
        }

        // Kurs do kontynuowania: ostatnio otwierany (jeśli nieukończony), potem pierwszy zaczęty, a na koniec pierwszy nieukończony.
        static CourseData PickCourse(FinanceQuestApp app)
        {
            var last = app.LastCourse;
            if (last != null && !app.Progress.IsCourseComplete(last)) return last;
            foreach (var c in app.Courses)
                if (app.Progress.CourseProgress(c) > 0f && !app.Progress.IsCourseComplete(c)) return c;
            // Nic jeszcze nie zaczęte – pierwszy kurs polecony w ankiecie.
            foreach (var id in Recommendations.Get())
                foreach (var c in app.Courses)
                    if (c.Id == id && !app.Progress.IsCourseComplete(c)) return c;
            foreach (var c in app.Courses)
                if (!app.Progress.IsCourseComplete(c)) return c;
            return null;
        }
    }
}

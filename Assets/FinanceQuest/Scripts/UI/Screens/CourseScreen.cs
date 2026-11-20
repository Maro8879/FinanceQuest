using TMPro;
using UnityEngine;

namespace FinanceQuest
{
    // Ekran 2: postęp kursu + rozdziały (kolor rozdziału = otwarty, ptaszek = ukończony, szare = zablokowane).
    public static class CourseScreen
    {
        public static void Build(RectTransform screen, FinanceQuestApp app, CourseData course)
        {
            float W = app.ScreenWidth;
            app.Header.Set(course.Title, false, IconId.Home, Palette.TextLocked, app.Home);

            var content = ScreenKit.Scroll(screen, W, 0f);
            app.LastCourse = course;
            // Swipe w lewo = aktualny rozdział (pierwszy otwarty, nieukończony).
            for (int i = 0; i < course.Chapters.Count; i++)
                if (app.Progress.ChapterState(course, i) == ItemState.Current)
                {
                    int idx = i;
                    app.SwipeForward = () => app.Push(r => ChapterScreen.Build(r, app, course, idx), true);
                    break;
                }
            float m = ScreenKit.Margin, cw = W - 2 * m;

            // Postęp całego kursu.
            float value = app.Progress.CourseProgress(course);
            var label = Ui.Text("CourseLabel", content, "Kurs", 13f, Palette.TextMuted, FontWeight.SemiBold, TextAlignmentOptions.CaplineLeft);
            Ui.Place(label.rectTransform, m, 4, 40, 16);
            ScreenKit.BarRow(content, m + 42, 4, cw - 42, value, Palette.Track, Palette.Text, ScreenKit.Pct(value));

            float y = 36f;
            for (int i = 0; i < course.Chapters.Count; i++)
                y += ChapterCard(content, m, y, cw, app, course, i) + 14f;
            ScreenKit.SetContentHeight(content, y + 24f);
        }

        static float ChapterCard(RectTransform parent, float x, float y, float w, FinanceQuestApp app, CourseData course, int index)
        {
            var ch = course.Chapters[index];
            var state = app.Progress.ChapterState(course, index);
            const float pad = 16f, iconSpace = 40f;

            Color fill, edge, lip, title, sub;
            string subText;
            switch (state)
            {
                case ItemState.Done:
                    fill = ch.Color; edge = ch.Color; lip = Palette.Darker(ch.Color);
                    title = Color.white; sub = Palette.WhiteSoft; subText = "Ukończony";
                    break;
                case ItemState.Current:
                    fill = ch.Color; edge = ch.Color; lip = Palette.Darker(ch.Color);
                    title = Color.white; sub = Palette.WhiteSoft;
                    subText = app.Progress.DoneCount(ch) + " / " + ContentText.Plural(ch.Levels.Count, "level", "levele", "leveli");
                    break;
                default:
                    fill = Palette.LockedFill; edge = Palette.LockedEdge; lip = Palette.LockedLip;
                    title = Palette.TextLocked; sub = Palette.TextLocked;
                    subText = "0 / " + ContentText.Plural(ch.Levels.Count, "level", "levele", "leveli");
                    break;
            }

            var card = LipCard.Create(parent, "Chapter_" + index, x, y, w, 100, fill, edge, lip,
                ScreenKit.CardRadius, ScreenKit.Lip, state == ItemState.Locked ? 2f : 0f);
            float th = ScreenKit.TitleBlock(card.Content, pad, pad, w - 2 * pad - iconSpace, ch.Title, 17f, subText, title, sub);
            float face = pad + th + pad;

            if (state == ItemState.Current)
            {
                float v = app.Progress.ChapterProgress(ch);
                ScreenKit.BarRow(card.Content, pad, pad + th + 10f, w - 2 * pad, v, Palette.TrackOnColor, Color.white, ScreenKit.Pct(v));
                face += 26f;
            }
            ((RectTransform)card.transform).sizeDelta = new Vector2(w, face + ScreenKit.Lip);

            // Wykres świecowy w tle: biały na kolorowych kartach, ledwie widoczny na zablokowanych.
            card.AddBackgroundChart(ScreenKit.CardRadius,
                state == ItemState.Locked ? new Color(0.64f, 0.62f, 0.58f, 0.22f) : new Color(1f, 1f, 1f, 0.16f));
            if (state == ItemState.Current) card.AddFocusRing(Palette.Accent, ScreenKit.CardRadius, 3f, 3f);

            if (state != ItemState.Current)
            {
                var icon = Ui.Icon("StateIcon", card.Content,
                    state == ItemState.Done ? IconId.Check : IconId.Lock,
                    state == ItemState.Done ? Color.white : Palette.LockedIcon, state == ItemState.Done ? 30f : 26f);
                var rt = icon.rectTransform;
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0.5f);
                rt.anchoredPosition = new Vector2(-pad, 0f);
            }

            if (state != ItemState.Locked)
                card.SetClickable(() => app.Push(r => ChapterScreen.Build(r, app, course, index)));
            return face + ScreenKit.Lip;
        }
    }
}

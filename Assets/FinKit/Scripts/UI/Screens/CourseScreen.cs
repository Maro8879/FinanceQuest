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

            // Postęp kursu: pierścień z procentem, ile rozdziałów zostało do pucharu i sam puchar (szary, dopóki niezdobyty).
            float value = app.Progress.CourseProgress(course);
            const float ring = 64f;
            RingProgress.Create(content, m + 2f, 4f, ring, value, Palette.Track, Palette.Accent, 7f);
            var pct = Ui.Text("Pct", content, ScreenKit.Pct(value), 15f, Palette.Text, FontWeight.ExtraBold, TextAlignmentOptions.Center);
            Ui.Place(pct.rectTransform, m + 2f, 4f, ring, ring);

            int total = course.Chapters.Count, done = 0;
            foreach (var c in course.Chapters) if (app.Progress.IsComplete(c)) done++;
            bool won = done == total && total > 0;
            const float trophyW = 44f, trophyH = 44f * TrophyArt.BoxH / TrophyArt.BoxW;
            float tx = m + ring + 16f, tw = cw - ring - 16f - trophyW - 10f;
            var line1 = Ui.Text("Chapters", content, done + " z " + total + " rozdziałów", 15f, Palette.Text, FontWeight.ExtraBold);
            Ui.Place(line1.rectTransform, tx, 4f + ring * 0.5f - 20f, tw, 20f);
            int left = total - done;
            var line2 = Ui.Text("ToTrophy", content, won ? "Puchar zdobyty!" : "Jeszcze " + left + " do pucharu", 12.5f, Palette.TextMuted, FontWeight.SemiBold);
            Ui.Place(line2.rectTransform, tx, 4f + ring * 0.5f + 2f, tw, 18f);

            var cup = Ui.Rect("Trophy", content).gameObject.AddComponent<UnityEngine.UI.Image>();
            var kind = TrophyArt.ForCourse(course);
            cup.sprite = won ? TrophyArt.Earned(kind, course.Color) : TrophyArt.Locked(kind);
            cup.raycastTarget = false;
            Ui.Place(cup.rectTransform, m + cw - trophyW - 2f, 4f + (ring - trophyH) * 0.5f, trophyW, trophyH);

            float y = 4f + ring + 18f;
            for (int i = 0; i < course.Chapters.Count; i++)
                y += Chapter(content, m, y, cw, app, course, i) + 14f;
            ScreenKit.SetContentHeight(content, y + 24f);
        }

        // Karta rozdziału; jeśli rozdział właśnie się otworzył – z animacją odblokowania (tylko raz).
        static float Chapter(RectTransform parent, float x, float y, float w, FinanceQuestApp app, CourseData course, int index)
        {
            var ch = course.Chapters[index];
            if (ch.Id != app.UnlockedChapter || app.Progress.ChapterState(course, index) != ItemState.Current)
                return ChapterCard(parent, x, y, w, app, course, index, null, 0f);

            app.UnlockedChapter = null;
            var box = UnlockReveal.Box(parent, "Unlock_" + index, x, y);
            float fh = ChapterCard(box, 0f, 0f, w, app, course, index, null, 0f);
            var fin = (RectTransform)box.GetChild(box.childCount - 1);
            // Zablokowana wersja dostaje tę samą wysokość, żeby fala odsłaniała kartę bez skoku.
            ChapterCard(box, 0f, 0f, w, app, course, index, ItemState.Locked, fh - ScreenKit.Lip);
            var lck = (RectTransform)box.GetChild(box.childCount - 1);
            UnlockReveal.Play(box, lck, fin, w, fh, app.Refreshing ? 0.12f : 0.6f);
            return fh;
        }

        static float ChapterCard(RectTransform parent, float x, float y, float w, FinanceQuestApp app, CourseData course, int index,
            ItemState? force, float forceFace)
        {
            var ch = course.Chapters[index];
            var state = force ?? app.Progress.ChapterState(course, index);
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
            if (forceFace > 0f) face = forceFace;
            ((RectTransform)card.transform).sizeDelta = new Vector2(w, face + ScreenKit.Lip);

            // Rosnące słupki w prawym dolnym rogu (białe, delikatne). Na zablokowanych kartach bez nich.
            if (state != ItemState.Locked)
                card.AddBackgroundBars(ScreenKit.CardRadius, new Color(1f, 1f, 1f, 0.06f), index);

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

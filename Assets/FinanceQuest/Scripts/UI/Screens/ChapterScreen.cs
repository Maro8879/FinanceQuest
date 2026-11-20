using TMPro;
using UnityEngine;

namespace FinanceQuest
{
    // Ekran 3: nagłówek rozdziału z postępem + levele ułożone lekkim zygzakiem (jak na szkicu).
    public static class ChapterScreen
    {
        // Przesunięcia kolejnych kart – daje wrażenie „ścieżki” bez rysowania drogi.
        static readonly float[] Offsets = { 0f, 14f, 28f, 14f };
        const float Zig = 28f;

        public static void Build(RectTransform screen, FinanceQuestApp app, CourseData course, int chapterIndex)
        {
            float W = app.ScreenWidth;
            var ch = course.Chapters[chapterIndex];
            // W rozdziale przycisk cofa do listy rozdziałów kursu (zamiast skakać do menu).
            app.Header.Set(course.Title, false, IconId.Back, Palette.TextLocked, app.Back);

            var content = ScreenKit.Scroll(screen, W, 0f);
            // Swipe w lewo = aktualny level w tym rozdziale.
            for (int i = 0; i < ch.Levels.Count; i++)
                if (app.Progress.LevelState(course, chapterIndex, i) == ItemState.Current)
                {
                    int idx = i;
                    app.SwipeForward = () => app.Push(r => LevelScreen.Build(r, app, course, chapterIndex, idx), true);
                    break;
                }
            float m = ScreenKit.Margin, cw = W - 2 * m;

            // Karta rozdziału (nieklikalna) – kolor rozdziału, tytuł i ile leveli zrobione.
            const float pad = 16f;
            var head = LipCard.Create(content, "ChapterHeader", m, 4, cw, 100, ch.Color, ch.Color, Palette.Darker(ch.Color),
                ScreenKit.CardRadius, ScreenKit.Lip, 0f);
            var label = Ui.Text("Label", head.Content, "ROZDZIAŁ " + (chapterIndex + 1), 12.5f, Palette.WhiteSoft, FontWeight.SemiBold);
            Ui.Place(label.rectTransform, pad, pad - 2, cw - 2 * pad, 18);
            float th = ScreenKit.TitleBlock(head.Content, pad, pad + 17, cw - 2 * pad, ch.Title, 19f, null, Color.white, Color.white);
            float barY = pad + 17 + th + 10;
            ScreenKit.BarRow(head.Content, pad, barY, cw - 2 * pad, app.Progress.ChapterProgress(ch), Palette.TrackOnColor,
                Color.white, app.Progress.DoneCount(ch) + "/" + ch.Levels.Count);
            float headFace = barY + 16 + pad;
            ((RectTransform)head.transform).sizeDelta = new Vector2(cw, headFace + ScreenKit.Lip);
            head.AddBackgroundChart(ScreenKit.CardRadius, new Color(1f, 1f, 1f, 0.16f));

            float y = 4 + headFace + ScreenKit.Lip + 14f;
            for (int i = 0; i < ch.Levels.Count; i++)
            {
                float off = Offsets[i % Offsets.Length];
                y += LevelCard(content, m + off, y, cw - Zig, app, course, chapterIndex, i) + 12f;
            }
            ScreenKit.SetContentHeight(content, y + 24f);
        }

        static float LevelCard(RectTransform parent, float x, float y, float w, FinanceQuestApp app,
            CourseData course, int chapterIndex, int levelIndex)
        {
            var ch = course.Chapters[chapterIndex];
            var level = ch.Levels[levelIndex];
            var state = app.Progress.LevelState(course, chapterIndex, levelIndex);
            const float face = 64f, pad = 14f, icon = 28f;

            Color fill, edge, lip, iconC, title, sub;
            switch (state)
            {
                case ItemState.Done:
                    fill = ch.Color; edge = ch.Color; lip = Palette.Darker(ch.Color);
                    iconC = Color.white; title = Color.white; sub = Palette.WhiteSoft;
                    break;
                case ItemState.Current:
                    fill = Palette.CardFill; edge = Palette.Accent; lip = Palette.AccentLip;
                    iconC = Palette.AccentDark; title = Palette.Text; sub = Palette.TextMuted;
                    break;
                default:
                    fill = Palette.LockedFill; edge = Palette.LockedEdge; lip = Palette.LockedLip;
                    iconC = Palette.LockedIcon; title = Palette.TextLocked; sub = Palette.TextLocked;
                    break;
            }

            var card = LipCard.Create(parent, "Level_" + level.Number, x, y, w, face + ScreenKit.Lip, fill, edge, lip,
                ScreenKit.CardRadius, ScreenKit.Lip, 2f);

            var ic = Ui.Icon("KindIcon", card.Content, ContentText.KindIcon(level.Kind), iconC, icon);
            Ui.Place(ic.rectTransform, pad, (face - icon) * 0.5f, icon, icon);

            float textX = pad + icon + 14f;
            var t = Ui.Text("Title", card.Content, "Level " + level.Number, 17f, title, FontWeight.ExtraBold, TextAlignmentOptions.TopLeft);
            Ui.Place(t.rectTransform, textX, 11, w - textX - 100, 24);
            var s = Ui.Text("Kind", card.Content, ContentText.KindLabel(level.Kind), 12.5f, sub, FontWeight.SemiBold, TextAlignmentOptions.TopLeft);
            Ui.Place(s.rectTransform, textX, 34, w - textX - 100, 18);

            System.Action open = () => app.Push(r => LevelScreen.Build(r, app, course, chapterIndex, levelIndex));

            if (state == ItemState.Current)
            {
                // Żółty START – wyraźnie pokazuje, gdzie jest użytkownik.
                const float bw = 78f, bh = 38f;
                var start = LipCard.Create(card.Content, "Start", w - pad - bw, (face - bh) * 0.5f, bw, bh,
                    Palette.Accent, Palette.Accent, Palette.AccentLip, 12f, 4f, 0f);
                var st = Ui.Text("Label", start.Content, "START", 14f, Palette.OnAccent, FontWeight.ExtraBold, TextAlignmentOptions.Capline);
                Ui.Stretch(st.rectTransform);
                start.SetClickable(open);
            }
            else
            {
                var si = Ui.Icon("StateIcon", card.Content, state == ItemState.Done ? IconId.Check : IconId.Lock,
                    state == ItemState.Done ? Color.white : Palette.LockedIcon, state == ItemState.Done ? 24f : 22f);
                var rt = si.rectTransform;
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0.5f);
                rt.anchoredPosition = new Vector2(-pad - 4f, 0f);
            }

            if (state != ItemState.Locked) card.SetClickable(open);
            return face + ScreenKit.Lip;
        }
    }
}

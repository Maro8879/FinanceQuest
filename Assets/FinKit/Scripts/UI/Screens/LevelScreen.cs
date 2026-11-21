using TMPro;
using UnityEngine;

namespace FinanceQuest
{
    // Ekran levelu. Lekcja ze stronami pokazuje strony z blokami (LessonPages), a po nich – jeśli są – zadania.
    // Test rozdziału i level „Zadania” to ekran zadań (QuizScreen). Mini gry mają na razie tymczasowy ekran z przyciskiem „Ukończ (test)”.
    public static class LevelScreen
    {
        // Czy level ma już treść do przejścia (strony lekcji albo zadania). Pusty level zalicza się samym kliknięciem.
        public static bool HasContent(LevelData level)
        {
            var def = level.Def;
            if (def == null) return false;
            bool pages = level.Kind == LevelKind.Lesson && def.pages != null && def.pages.Count > 0;
            return pages || (def.tasks != null && def.tasks.Count > 0);
        }

        public static void Build(RectTransform screen, FinanceQuestApp app, CourseData course, int chapterIndex, int levelIndex)
        {
            var ch = course.Chapters[chapterIndex];
            var level = ch.Levels[levelIndex];
            app.BlockSwipeBack = true;   // z levelu wychodzi się tylko przyciskiem – swipe łatwo zrobić przypadkiem
            app.Header.Set(string.IsNullOrEmpty(level.Title) ? ch.Title : level.Title, false, IconId.Back, Palette.TextLocked, app.Back);

            var def = level.Def;
            bool hasTasks = def != null && def.tasks != null && def.tasks.Count > 0;
            // Zaliczenie levelu; z zadań przychodzi też wynik w gwiazdkach (zapamiętujemy najlepszy).
            // save: wynik zapisujemy od razu po teście (bez czekania na UKOŃCZ). SetStars trzyma najlepszy wynik –
            // gorszy go nie nadpisze. Level zaliczony = co najmniej 1 gwiazdka (0 = niezaliczony, nic się nie zmienia).
            System.Action<int> save = stars =>
            {
                if (stars == 0) return;
                if (stars > 0) app.Progress.SetStars(level, stars);
                // Już zaliczony level nie „odblokowuje się” ponownie (bez animacji odblokowania przy powrocie).
                if (!app.Progress.IsDone(level)) app.CompleteLevel(course, level);
            };
            // Po zaliczeniu od razu następny level tego rozdziału (wjeżdża z boku). Gdy to był ostatni level
            // albo następny nie ma jeszcze treści – powrót do rozdziału, jak wcześniej.
            System.Action<int> finish = stars =>
            {
                save(stars);
                int next = levelIndex + 1;
                if (next < ch.Levels.Count && HasContent(ch.Levels[next])
                    && app.Progress.LevelState(course, chapterIndex, next) != ItemState.Locked)
                {
                    app.UnlockedLevel = null;   // wchodzimy prosto do niego – animacja odblokowania nie jest potrzebna
                    app.ReplaceTop(r => Build(r, app, course, chapterIndex, next));
                }
                else app.Back();
            };
            int best = app.Progress.Stars(level);
            if (level.Kind == LevelKind.Lesson && def != null && def.pages != null && def.pages.Count > 0)
            {
                LessonPages.Build(screen, app, level, () =>
                {
                    if (hasTasks)
                    {
                        // Zadania na końcu lekcji – podmieniamy treść tego samego ekranu, więc „wstecz” wraca od razu do rozdziału.
                        for (int i = screen.childCount - 1; i >= 0; i--) Object.Destroy(screen.GetChild(i).gameObject);
                        app.SwipeForward = null;
                        QuizScreen.Build(screen, app, def.tasks, false, best, finish, save);
                    }
                    else finish(-1);
                });
                return;
            }
            if (hasTasks && level.Kind != LevelKind.MiniGame)
            {
                QuizScreen.Build(screen, app, def.tasks, level.Kind == LevelKind.Test, best, finish, save);
                return;
            }
            Placeholder(screen, app, course, ch, level, "Tu pojawi się mini gra.");
        }

        // Tymczasowy ekran: pozwala oznaczyć level jako ukończony, zanim powstaną zadania i mini gry.
        static void Placeholder(RectTransform screen, FinanceQuestApp app, CourseData course, ChapterData ch, LevelData level, string info)
        {
            float W = app.ScreenWidth;
            float m = ScreenKit.Margin, cw = W - 2 * m, y = 8f;

            var card = LipCard.Create(screen, "Info", m, y, cw, 150, Palette.CardFill, Palette.CardEdge, Palette.CardLip,
                ScreenKit.CardRadius, ScreenKit.Lip, 2f);
            var ic = Ui.Icon("Icon", card.Content, ContentText.KindIcon(level.Kind), ch.Color, 40f);
            Ui.Place(ic.rectTransform, 18, 18, 40, 40);
            var t = Ui.Text("Title", card.Content, "Level " + level.Number + " · " + ContentText.KindLabel(level.Kind),
                20f, Palette.Text);
            Ui.Place(t.rectTransform, 72, 22, cw - 90, 30);
            var d = Ui.Text("Desc", card.Content, info, 14f, Palette.TextMuted, FontWeight.Regular);
            Ui.Place(d.rectTransform, 18, 74, cw - 36, 60);

            y += 150 + 20f;
            var done = LipCard.Create(screen, "Complete", m, y, cw, 54, Palette.Accent, Palette.Accent, Palette.AccentLip,
                16f, 5f, 0f);
            var dl = Ui.Text("Label", done.Content, "UKOŃCZ (TEST)", 16f, Palette.OnAccent, FontWeight.ExtraBold, TextAlignmentOptions.Capline);
            Ui.Stretch(dl.rectTransform);
            done.SetClickable(() => { app.CompleteLevel(course, level); app.Back(); });

            y += 54 + 14f;
            var back = LipCard.Create(screen, "Back", m, y, cw, 54, Palette.CardFill, Palette.CardEdge, Palette.CardLip,
                16f, 5f, 2f);
            var bl = Ui.Text("Label", back.Content, "WRÓĆ", 16f, Palette.TextMuted, FontWeight.ExtraBold, TextAlignmentOptions.Capline);
            Ui.Stretch(bl.rectTransform);
            back.SetClickable(app.Back);
        }
    }
}

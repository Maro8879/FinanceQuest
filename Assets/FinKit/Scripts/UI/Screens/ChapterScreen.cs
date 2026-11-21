using TMPro;
using UnityEngine;

namespace FinanceQuest
{
    // Ekran 3: nagłówek rozdziału z postępem + levele jako kafelki w dwóch kolumnach.
    // Aktualny level jest żółty ze START. Siatka jest stała – kafelki nie zmieniają miejsc ani rozmiaru po postępie.
    public static class ChapterScreen
    {
        const float TileH = 140f, CurrentH = 96f, Gap = 14f;

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
                    app.SwipeForward = () => Open(app, course, chapterIndex, idx, true);
                    break;
                }
            float m = ScreenKit.Margin, cw = W - 2 * m;

            // Karta rozdziału (nieklikalna) – kolor rozdziału, tytuł i ile leveli zrobione.
            const float pad = 16f;
            var head = LipCard.Create(content, "ChapterHeader", m, 4, cw, 100, ch.Color, ch.Color, Palette.Darker(ch.Color),
                ScreenKit.CardRadius, ScreenKit.Lip, 0f);
            var label = Ui.Text("Label", head.Content, "CHAPTER " + (chapterIndex + 1), 12.5f, Palette.WhiteSoft, FontWeight.SemiBold);
            Ui.Place(label.rectTransform, pad, pad - 2, cw - 2 * pad, 18);
            // Gwiazdki rozdziału w prawym górnym rogu, w linii z „ROZDZIAŁ N”.
            int sg, sm;
            app.Progress.StarTotals(ch, out sg, out sm);
            ScreenKit.StarCountRight(head.Content, cw - pad, pad - 2, sg, sm, Palette.Accent, Color.white);
            float th = ScreenKit.TitleBlock(head.Content, pad, pad + 17, cw - 2 * pad, ch.Title, 19f, null, Color.white, Color.white);
            float barY = pad + 17 + th + 10;
            // Sam pasek na całą szerokość – bez licznika leveli obok (ile leveli zostało, widać po kafelkach).
            ProgressBar.Create(head.Content, pad, barY, cw - 2 * pad, 16f, Palette.TrackOnColor, app.Progress.ChapterProgress(ch));
            float headFace = barY + 16 + pad;
            ((RectTransform)head.transform).sizeDelta = new Vector2(cw, headFace + ScreenKit.Lip);
            head.AddBackgroundBars(ScreenKit.CardRadius, new Color(1f, 1f, 1f, 0.06f), chapterIndex);

            float y = 4 + headFace + ScreenKit.Lip + 14f;
            float half = (cw - Gap) * 0.5f;
            // Stała siatka: każdy level ma zawsze to samo miejsce (dwie kolumny, równe kafelki) –
            // po przejściu levelu zmienia się tylko jego wygląd, a nie układ całego rozdziału.
            for (int i = 0; i < ch.Levels.Count; i++)
            {
                int col = i % 2, row = i / 2;
                Tile(content, col == 0 ? m : m + half + Gap, y + row * (TileH + ScreenKit.Lip + Gap), half, TileH, app, course, chapterIndex, i);
            }
            y += (ch.Levels.Count + 1) / 2 * (TileH + ScreenKit.Lip + Gap);
            ScreenKit.SetContentHeight(content, y + 10f);
        }

        // Otwiera level. Level bez treści (jeszcze nienapisany) zaliczamy od razu kliknięciem – bez osobnego ekranu –
        // i odświeżamy rozdział, żeby było widać odblokowanie następnego.
        static void Open(FinanceQuestApp app, CourseData course, int chapterIndex, int levelIndex, bool swipe)
        {
            var level = course.Chapters[chapterIndex].Levels[levelIndex];
            if (LevelScreen.HasContent(level))
            {
                app.Push(r => LevelScreen.Build(r, app, course, chapterIndex, levelIndex), swipe);
                return;
            }
            if (app.Progress.IsDone(level)) return;   // już zaliczony – nie ma czego robić
            app.CompleteLevel(course, level);
            app.Refresh();
        }

        // Kafelek levelu; jeśli ten level właśnie się odblokował – z animacją odblokowania (tylko raz).
        static float Tile(RectTransform parent, float x, float y, float w, float h, FinanceQuestApp app,
            CourseData course, int chapterIndex, int levelIndex)
        {
            var level = course.Chapters[chapterIndex].Levels[levelIndex];
            if (level.Id != app.UnlockedLevel || app.Progress.LevelState(course, chapterIndex, levelIndex) != ItemState.Current)
                return LevelTile(parent, x, y, w, h, app, course, chapterIndex, levelIndex, null);

            app.UnlockedLevel = null;
            var box = UnlockReveal.Box(parent, "Unlock_" + level.Number, x, y);
            float fh = LevelTile(box, 0f, 0f, w, h, app, course, chapterIndex, levelIndex, null);
            var fin = (RectTransform)box.GetChild(box.childCount - 1);
            // Zablokowana wersja o tym samym rozmiarze – leży pod spodem, dopóki fala jej nie zakryje.
            LevelTile(box, 0f, 0f, w, fh - ScreenKit.Lip, app, course, chapterIndex, levelIndex, ItemState.Locked);
            var lck = (RectTransform)box.GetChild(box.childCount - 1);
            UnlockReveal.Play(box, lck, fin, w, fh, app.Refreshing ? 0.12f : 0.6f);
            return fh;
        }

        // Kafelek levelu: duży numer w lewym górnym rogu, tytuł przy dolnej krawędzi.
        // Ukończony – w kolorze rozdziału z ptaszkiem; aktualny – żółty ze START; zablokowany – szary z kłódką.
        static float LevelTile(RectTransform parent, float x, float y, float w, float h, FinanceQuestApp app,
            CourseData course, int chapterIndex, int levelIndex, ItemState? force)
        {
            var ch = course.Chapters[chapterIndex];
            var level = ch.Levels[levelIndex];
            var state = force ?? app.Progress.LevelState(course, chapterIndex, levelIndex);
            const float pad = 12f;

            Color fill, lip, text;
            switch (state)
            {
                case ItemState.Done: fill = ch.Color; lip = Palette.Darker(ch.Color); text = Color.white; break;
                case ItemState.Current: fill = Palette.Accent; lip = Palette.AccentLip; text = Palette.OnAccent; break;
                default: fill = Palette.LockedFill; lip = Palette.LockedLip; text = Palette.TextLocked; break;
            }

            // Tytuł liczymy przed kartą – aktualny kafelek dopasowuje wysokość, żeby numer nie nachodził na tytuł.
            float tw = w - 2 * pad;
            string titleText = string.IsNullOrEmpty(level.Title) ? "Level " + level.Number : level.Title;
            const float numberH = 40f, sh = 24f, sw = 68f;
            if (h <= 0f)
            {
                var probe = Ui.Text("Probe", parent, titleText, 14.5f, text, FontWeight.ExtraBold);
                float ph = Mathf.Min(Ui.TextHeight(probe, tw), 56f);
                Object.DestroyImmediate(probe.gameObject);
                h = Mathf.Max(CurrentH, pad + numberH + 4f + ph + 8f + sh + pad);
            }

            var card = LipCard.Create(parent, "Level_" + level.Number, x, y, w, h + ScreenKit.Lip, fill, fill, lip,
                18f, ScreenKit.Lip, 0f);

            var num = Ui.Text("Number", card.Content, level.Number.ToString(), 34f, text, FontWeight.ExtraBold, TextAlignmentOptions.TopLeft);
            num.textWrappingMode = TextWrappingModes.NoWrap;
            Ui.Place(num.rectTransform, pad, pad - 2f, 80f, numberH);

            // Level z gwiazdkami (test, zadania, mini gra): od początku trzy puste gwiazdki – widać, że jest o co grać.
            // Po ukończeniu zamiast ptaszka najlepszy wynik.
            int stars = state == ItemState.Done ? app.Progress.Stars(level) : -1;
            if (stars >= 0 || ProgressStore.GivesStars(level))
            {
                Color empty = state == ItemState.Done ? new Color(1f, 1f, 1f, 0.3f)
                    : state == ItemState.Current ? new Color(Palette.OnAccent.r, Palette.OnAccent.g, Palette.OnAccent.b, 0.22f)
                    : new Color(Palette.LockedIcon.r, Palette.LockedIcon.g, Palette.LockedIcon.b, 0.55f);
                StarRow(card.Content, w - pad, pad, Mathf.Max(0, stars), empty);
            }
            else if (state != ItemState.Current)
            {
                var si = Ui.Icon("StateIcon", card.Content, state == ItemState.Done ? IconId.Check : IconId.Lock,
                    state == ItemState.Done ? Color.white : Palette.LockedIcon, state == ItemState.Done ? 22f : 18f);
                Ui.Place(si.rectTransform, w - pad - 22f, pad, 22f, 22f);
            }

            // Tytuł przy dole kafelka (do 3 linii; dłuższy dostaje „…”).
            var t = Ui.Text("Title", card.Content, titleText, 14.5f, text, FontWeight.ExtraBold, TextAlignmentOptions.BottomLeft);
            t.overflowMode = TextOverflowModes.Ellipsis;
            t.maxVisibleLines = 3;
            // Tytuł nie może wejść na numer: tyle miejsca, ile zostaje pod numerem (i nad START w aktualnym).
            float room = h - pad - numberH - 4f - (state == ItemState.Current ? 8f + sh : 0f) - pad;
            float th = Mathf.Min(Ui.TextHeight(t, tw), Mathf.Min(56f, room));

            System.Action open = () => Open(app, course, chapterIndex, levelIndex, false);
            if (state == ItemState.Current)
            {
                // START jako ciemna plakietka pod tytułem; kliknąć można cały kafelek.
                Ui.Place(t.rectTransform, pad, h - pad - sh - 8f - th, tw, th);
                var pill = Ui.Rounded("StartPill", card.Content, Palette.Text, 8f);
                Ui.Place(pill.rectTransform, pad, h - pad - sh, sw, sh);
                var st = Ui.Text("Label", pill.transform, "START", 12f, Palette.Accent, FontWeight.ExtraBold, TextAlignmentOptions.Capline);
                Ui.Stretch(st.rectTransform);
            }
            else Ui.Place(t.rectTransform, pad, h - pad - th, tw, th);

            if (state != ItemState.Locked) card.SetClickable(open);
            return h + ScreenKit.Lip;
        }

        // Trzy gwiazdki wyrównane do prawej (right = prawa krawędź); niezdobyte w kolorze „empty” (zależnym od kafelka).
        static void StarRow(RectTransform parent, float right, float y, int stars, Color empty)
        {
            const float size = 17f, gap = 2f;
            float x = right - 3 * size - 2 * gap;
            for (int i = 0; i < 3; i++)
            {
                var s = Ui.Icon("Star", parent, IconId.Star, i < stars ? Palette.Accent : empty, size);
                Ui.Place(s.rectTransform, x + i * (size + gap), y, size, size);
            }
        }
    }
}

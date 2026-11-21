using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FinanceQuest
{
    // Ekran zadań: test rozdziału, level „Zadania” i zadania na końcu lekcji.
    // Jedno zadanie na raz: polecenie → odpowiedź → SPRAWDŹ → kolorowy pasek z wyjaśnieniem albo ciekawostką → DALEJ.
    // Na końcu wynik w gwiazdkach: 0 gwiazdek = trzeba spróbować ponownie, 1–2 = można zaliczyć albo poprawić, 3 = komplet.
    // Najlepszy wynik zapisuje LevelScreen (ProgressStore). Typy jak w edytorze treści: choice, gap, tiles, pairs, zones.
    // Wszystko działa na stuknięcia (bez przeciągania) – pewniej na telefonie i nie gryzie się z przewijaniem.
    // Ekran budujemy od nowa przy każdej zmianie (jak ankieta) – to kilkanaście kart, więc jest szybko i prosto.
    public class QuizScreen
    {
        const float ButtonH = 58f, ChipGap = 8f, ChipPadX = 14f, ChipPadY = 11f, ChipText = 15f;

        static readonly Color Good = Palette.Hex("3F8F5F"), GoodFill = Palette.Hex("E7F3EB"), GoodLip = Palette.Hex("2F6F49");
        static readonly Color Bad = Palette.Hex("C8553D"), BadFill = Palette.Hex("FBE7E2"), BadLip = Palette.Hex("9E3F2C");
        static readonly Color SelFill = Palette.Hex("FFF6D1");
        static readonly Color ZoneFill = Palette.Hex("EFECE5");
        // Kolory połączonych par – z palety rozdziałów, żeby pasowały do reszty aplikacji.
        static readonly Color[] PairColors =
        {
            Palette.Hex("0E4A55"), Palette.Hex("9C7A2E"), Palette.Hex("8E4A36"),
            Palette.Hex("4A5D73"), Palette.Hex("3F6B5C"), Palette.Hex("6F7A7C"),
        };

        enum Look { Normal, Selected, Good, Bad, Dim }

        readonly FinanceQuestApp app;
        readonly RectTransform screen;
        readonly List<TaskDef> tasks;
        readonly bool isTest;
        int best;                  // dotychczasowy najlepszy wynik w gwiazdkach (-1 = jeszcze nie zaliczony)
        readonly Action<int> onFinish;      // dostaje zdobyte gwiazdki
        readonly System.Random rng = new System.Random();
        readonly float W, m, cw;

        int index, score;
        bool answered, lastCorrect, finished;

        // Stan bieżącego zadania (zerowany w StartTask).
        List<int> order = new List<int>();          // kolejność wyświetlania: odpowiedzi / propozycji / kafelków / prawych kart / elementów
        readonly HashSet<int> picked = new HashSet<int>();   // choice: zaznaczone odpowiedzi
        List<string> words = new List<string>();    // gap (wybór): propozycje; tiles: wszystkie kafelki
        int gapPick = -1;                           // gap (wybór): wybrana propozycja
        string typed = "";                          // gap (wpisywanie): wpisany tekst
        readonly List<int> placed = new List<int>();  // tiles: ułożone kafelki po kolei
        int[] pairOf = new int[0];                  // pairs: lewa karta i → prawa karta (indeks pary) albo -1
        int pairSel = -1;                           // pairs: zaznaczona lewa karta
        int[] zoneOf = new int[0];                  // zones: element i → strefa albo -1
        int zoneSel = -1;                           // zones: zaznaczony element

        RectTransform bar;
        TMP_InputField input;

        // Animacja przy najbliższej przebudowie ekranu: Slide = następne zadanie / wynik, Answer = po SPRAWDŹ.
        // Zwykłe stuknięcia (zaznaczanie odpowiedzi) przebudowują ekran bez animacji.
        enum Fx { None, Slide, Answer }
        Fx fx = Fx.None;
        ProgressBar progress;          // pasek postępu bieżącego widoku (null na ekranie wyniku)
        float progressValue, shownProgress;   // docelowa wartość i to, co pasek pokazywał poprzednio

        // onResult: wywoływane raz na podejście, gdy pokazuje się wynik – zapis najlepszego wyniku od razu, nawet jeśli
        // ktoś potem wyjdzie przyciskiem WRÓĆ zamiast UKOŃCZ.
        public static void Build(RectTransform screen, FinanceQuestApp app, List<TaskDef> tasks, bool isTest, int bestStars, Action<int> onFinish,
            Action<int> onResult = null)
        {
            new QuizScreen(screen, app, tasks, isTest, bestStars, onFinish) { onResult = onResult };
        }

        Action<int> onResult;
        bool resultSaved;

        QuizScreen(RectTransform screen, FinanceQuestApp app, List<TaskDef> tasks, bool isTest, int bestStars, Action<int> onFinish)
        {
            this.screen = screen;
            this.app = app;
            this.tasks = tasks;
            this.isTest = isTest;
            this.best = bestStars;
            this.onFinish = onFinish;
            W = app.ScreenWidth;
            m = ScreenKit.Margin;
            cw = W - 2 * m;
            StartTask(0);
        }

        // ---------------- przebieg ----------------

        void StartTask(int i)
        {
            index = i;
            answered = false;
            var t = tasks[i];
            picked.Clear();
            placed.Clear();
            gapPick = pairSel = zoneSel = -1;
            typed = "";
            words = new List<string>();
            order = new List<int>();

            switch (t.type)
            {
                case "choice":
                    order = Shuffled(t.options.Count, false);
                    break;
                case "gap":
                    if (t.mode != "type")
                    {
                        if (t.answers.Count > 0) words.Add(t.answers[0]);
                        words.AddRange(t.wrong);
                        order = Shuffled(words.Count, false);
                    }
                    break;
                case "tiles":
                    words.AddRange(SplitWords(t.answer));
                    words.AddRange(t.extra);
                    order = Shuffled(words.Count, true);
                    break;
                case "pairs":
                    order = Shuffled(t.pairs.Count, true);
                    pairOf = Filled(t.pairs.Count);
                    break;
                case "zones":
                    order = Shuffled(t.items.Count, false);
                    zoneOf = Filled(t.items.Count);
                    break;
            }
            Show(true);
        }

        void Check()
        {
            if (answered || !Ready()) return;
            if (input != null) typed = input.text;
            lastCorrect = Evaluate(tasks[index]);
            if (lastCorrect) score++;
            answered = true;
            Haptics.Tap();
            fx = Fx.Answer;
            Show(false);
        }

        void Next()
        {
            fx = Fx.Slide;
            if (index < tasks.Count - 1) StartTask(index + 1);
            else { finished = true; Show(true); }
        }

        void Restart()
        {
            resultSaved = false;
            score = 0;
            finished = false;
            fx = Fx.Slide;
            StartTask(0);
        }

        // ---------------- budowa ekranu ----------------

        void Show(bool toTop)
        {
            Vector2 keep = Vector2.zero;
            var old = screen.Find("Scroll/Content") as RectTransform;
            if (old != null && !toTop) keep = old.anchoredPosition;
            var anim = fx;
            fx = Fx.None;
            // Przy animacji stary widok nie znika od razu: przenosimy go na osobną warstwę, która płynnie odjeżdża / blednie.
            RectTransform oldLayer = null;
            if (anim != Fx.None && screen.childCount > 0)
            {
                oldLayer = Ui.Rect("Old", screen);
                Ui.Stretch(oldLayer);
                for (int i = screen.childCount - 1; i >= 0; i--)
                {
                    var ch = screen.GetChild(i);
                    if (ch != oldLayer) ch.SetParent(oldLayer, false);
                }
            }
            else
                for (int i = screen.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(screen.GetChild(i).gameObject);
            input = null;
            bar = null;
            progress = null;
            app.SwipeForward = null;

            var content = ScreenKit.Scroll(screen, W, 0f);
            float y = finished ? BuildSummary(content) : BuildTask(content);
            if (finished && !resultSaved)
            {
                // Najpierw podsumowanie (porównuje z dotychczasowym rekordem), potem zapis i nowy rekord na resztę sesji.
                resultSaved = true;
                int st = Stars();
                if (onResult != null) onResult(st);
                if (st > best) best = st;
            }
            ScreenKit.SetContentHeight(content, y + 16f);
            content.anchoredPosition = keep;

            float barH = finished ? BuildSummaryBar() : BuildBar();
            ((RectTransform)content.parent).offsetMin = new Vector2(0f, barH);
            // Swipe w lewo = DALEJ, ale dopiero po sprawdzeniu odpowiedzi.
            if (!finished && answered) app.SwipeForward = Next;
            // Na podsumowaniu bez przycisku powrotu w nagłówku – wychodzi się przez UKOŃCZ (albo próbuje ponownie).
            app.Header.Set(app.Header.CurrentTitle, false, IconId.Back, Palette.TextLocked, finished ? null : (Action)app.Back);

            if (anim != Fx.None) Animate(anim, oldLayer, content, barH);

            // Pasek postępu dojeżdża płynnie od poprzedniej wartości (zamiast skoczyć).
            if (progress != null)
            {
                float from = shownProgress, to = progressValue;
                var pb = progress;
                if (anim != Fx.None && Mathf.Abs(to - from) > 0.001f)
                {
                    var fxr = Fx_(screen);
                    fxr.Add(0.1f, 0.45f, t => pb.SetValue(Mathf.Lerp(from, to, QuizFx.EaseOut(t))), null);
                }
                shownProgress = to;
            }
        }

        // ---------------- animacje ----------------

        // GetComponent w Unity zwraca „udawany null”, którego ?? nie łapie – dlatego zwykłe sprawdzenie.
        static QuizFx Fx_(RectTransform screen)
        {
            var f = screen.GetComponent<QuizFx>();
            if (f == null) f = screen.gameObject.AddComponent<QuizFx>();
            return f;
        }

        void Animate(Fx anim, RectTransform oldLayer, RectTransform content, float barH)
        {
            var scrollRoot = (RectTransform)content.parent;
            var fxr = Fx_(screen);
            if (oldLayer != null)
            {
                oldLayer.SetAsLastSibling();   // stary widok na wierzchu, ale nieklikalny – nowy już działa pod spodem
                var g = oldLayer.gameObject.AddComponent<CanvasGroup>();
                g.blocksRaycasts = false;
                g.interactable = false;
                // Stary widok tylko blednie w miejscu – przycisk na dole płynnie zmienia napis, nic nie jeździ.
                fxr.Add(0f, anim == Fx.Slide ? 0.15f : 0.16f, t => g.alpha = 1f - t,
                    () => UnityEngine.Object.Destroy(oldLayer.gameObject));
            }

            if (anim == Fx.Slide)
            {
                // Kaskada jak w reszcie aplikacji: elementy zadania wskakują po kolei od góry (ten sam wiersz = razem).
                // Pasek postępu stoi w miejscu (tylko się napełnia), dolny pasek z przyciskiem też się nie rusza.
                var tops = new List<float>();
                for (int i = 0; i < content.childCount; i++)
                {
                    float top = -((RectTransform)content.GetChild(i)).anchoredPosition.y;
                    if (!tops.Contains(top)) tops.Add(top);
                }
                tops.Sort();
                for (int i = 0; i < content.childCount; i++)
                {
                    var rt = (RectTransform)content.GetChild(i);
                    if (progress != null && rt == progress.transform) continue;
                    int rank = Mathf.Min(tops.IndexOf(-rt.anchoredPosition.y), 8);
                    var g = rt.GetComponent<CanvasGroup>();
                    if (g == null) g = rt.gameObject.AddComponent<CanvasGroup>();
                    var basePos = rt.anchoredPosition;
                    fxr.Add(0.08f + rank * 0.045f, 0.3f, t =>
                    {
                        float e = QuizFx.BackOut(t);
                        g.alpha = Mathf.Clamp01(t * 1.6f);
                        rt.anchoredPosition = basePos - new Vector2(0f, 16f * (1f - e));
                    }, null);
                }
                return;
            }

            // Po SPRAWDŹ: przycisk zostaje w tym samym miejscu (zmienia się tylko napis i kolor – stary blednie na wierzchu),
            // a karta z wynikiem wysuwa się zza niego do góry. Karta leży pod przyciskiem, więc „wychodzi” spod niego.
            var card = bar != null ? bar.Find("Feedback") as RectTransform : null;
            if (card != null)
            {
                var cg = card.GetComponent<CanvasGroup>();
                if (cg == null) cg = card.gameObject.AddComponent<CanvasGroup>();
                var basePos = card.anchoredPosition;
                var size = card.sizeDelta;
                bool pop = lastCorrect;   // dobra odpowiedź: dodatkowo lekkie „wyskoczenie” (rośnie z 90%)
                fxr.Add(0.05f, 0.34f, t =>
                {
                    float e = QuizFx.BackOut(t);
                    cg.alpha = Mathf.Clamp01(t * 2f);
                    float k = pop ? 0.9f + 0.1f * e : 1f;
                    card.localScale = new Vector3(k, k, 1f);
                    // Punkt zaczepienia karty jest w lewym górnym rogu – korekta, żeby rosła od środka.
                    var grow = new Vector2(size.x * (1f - k) * 0.5f, -size.y * (1f - k) * 0.5f);
                    card.anchoredPosition = basePos + grow - new Vector2(0f, 28f * (1f - e));
                }, null);
            }
            if (lastCorrect) return;
            if (scrollRoot != null)
            {
                // Źle: zadanie krótko „kręci głową” na boki (drganie wygasa).
                var basePos = scrollRoot.anchoredPosition;
                fxr.Add(0.05f, 0.4f, t =>
                {
                    float x = Mathf.Sin(t * Mathf.PI * 6f) * 10f * (1f - t);
                    scrollRoot.anchoredPosition = basePos + new Vector2(x, 0f);
                }, () => { if (scrollRoot != null) scrollRoot.anchoredPosition = basePos; });
            }
        }

        float BuildTask(RectTransform c)
        {
            var t = tasks[index];
            float y = 6f;
            progressValue = (index + (answered ? 1f : 0f)) / Mathf.Max(1, tasks.Count);
            progress = ProgressBar.Create(c, m, y, cw, 12f, Palette.Track, progressValue);
            y += 12f + 22f;

            if (index == 0 && best >= 0 && best < 3) y += BestNote(c, y);

            var kick = Ui.Text("Kicker", c, (isTest ? "PYTANIE " : "ZADANIE ") + (index + 1) + " Z " + tasks.Count, 11.5f, Palette.TextMuted);
            kick.characterSpacing = 6f;
            Ui.Place(kick.rectTransform, m, y, cw, 16f);
            y += 22f;

            var prompt = Ui.Text("Prompt", c, ContentBlocks.Rich(t.prompt), 21f, Palette.Text);
            prompt.richText = true;
            float ph = Ui.TextHeight(prompt, cw);
            Ui.Place(prompt.rectTransform, m, y, cw, ph);
            y += ph + 16f;

            switch (t.type)
            {
                case "choice": return Choice(c, t, y);
                case "gap": return Gap(c, t, y);
                case "tiles": return Tiles(c, t, y);
                case "pairs": return Pairs(c, t, y);
                case "zones": return Zones(c, t, y);
            }
            return y + Hint(c, y, "Tego typu zadania aplikacja jeszcze nie pokazuje.");
        }

        // Dolny pasek: SPRAWDŹ (szary, dopóki nie ma odpowiedzi) albo kolorowy pasek z wynikiem i DALEJ.
        float BuildBar()
        {
            bar = Ui.Rect("BottomBar", screen);
            bar.anchorMin = new Vector2(0f, 0f);
            bar.anchorMax = new Vector2(1f, 0f);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.anchoredPosition = Vector2.zero;

            float h;
            if (!answered)
            {
                bool ready = Ready();
                Button(bar, m, 18f, cw, "SPRAWDŹ", ready, true, Check);
                h = 18f + ButtonH + 20f;
            }
            else
            {
                var t = tasks[index];
                string head = lastCorrect ? "Dobrze!" : "Nie tym razem";
                string body = lastCorrect ? t.fact : t.explain;
                string sol = lastCorrect ? null : Solution(t);
                float ph = Feedback(bar, m, 10f, cw, head, body, sol);
                bool last = index == tasks.Count - 1;
                Button(bar, m, 10f + ph + 12f, cw, last ? "ZOBACZ WYNIK" : "DALEJ", true, true, Next);
                h = 10f + ph + 12f + ButtonH + 20f;
            }
            bar.sizeDelta = new Vector2(0f, h);
            return h;
        }

        // Tylko dolny pasek (przy wpisywaniu odpowiedzi – żeby nie zgubić klawiatury, reszta ekranu zostaje).
        void RebuildBar()
        {
            if (bar != null) UnityEngine.Object.Destroy(bar.gameObject);
            BuildBar();
        }

        float Feedback(RectTransform parent, float x, float y, float w, string head, string body, string sol)
        {
            const float pad = 14f, icon = 30f, gap = 10f, lip = 4f;
            float tw = w - 2 * pad - icon - gap;
            Color edge = lastCorrect ? Good : Bad;

            var title = Ui.Text("Head", parent, head, 17f, edge);
            float hh = Ui.TextHeight(title, tw);
            TextMeshProUGUI text = null, solution = null;
            float bh = 0f, sh = 0f;
            if (!string.IsNullOrEmpty(body))
            {
                text = Ui.Text("Body", parent, ContentBlocks.Rich(body), 14.5f, Palette.Text, FontWeight.Regular);
                text.richText = true;
                text.lineSpacing = 4f;
                bh = Ui.TextHeight(text, tw);
            }
            if (!string.IsNullOrEmpty(sol))
            {
                solution = Ui.Text("Solution", parent, sol, 14f, Palette.Text, FontWeight.SemiBold);
                solution.lineSpacing = 4f;
                sh = Ui.TextHeight(solution, tw);
            }
            float inner = hh + (bh > 0 ? 6f + bh : 0f) + (sh > 0 ? 8f + sh : 0f);
            float h = Mathf.Max(icon, inner) + 2 * pad;

            var card = LipCard.Create(parent, "Feedback", x, y, w, h + lip,
                lastCorrect ? GoodFill : BadFill, edge, lastCorrect ? GoodLip : BadLip, 18f, lip, 2f);
            var circle = Ui.Rounded("IconBg", card.Content, edge, icon * 0.5f);
            Ui.Place(circle.rectTransform, pad, pad, icon, icon);
            var ic = Ui.Icon("Icon", circle.transform, lastCorrect ? IconId.Check : IconId.Exclaim, Color.white, 18f);
            Ui.Center(ic.rectTransform, 18f, 18f);

            float tx = pad + icon + gap, ty = pad;
            title.transform.SetParent(card.Content, false);
            Ui.Place(title.rectTransform, tx, ty, tw, hh);
            ty += hh;
            if (text != null)
            {
                text.transform.SetParent(card.Content, false);
                Ui.Place(text.rectTransform, tx, ty + 6f, tw, bh);
                ty += 6f + bh;
            }
            if (solution != null)
            {
                solution.transform.SetParent(card.Content, false);
                Ui.Place(solution.rectTransform, tx, ty + 8f, tw, sh);
            }
            return h + lip;
        }

        // ---------------- wynik ----------------

        // 3 gwiazdki = bezbłędnie; 2 = co najmniej 2/3 poprawnych; 1 = co najmniej 1/3; mniej = 0 (trzeba powtórzyć).
        int Stars()
        {
            int total = Mathf.Max(1, tasks.Count);
            float ratio = (float)score / total;
            return score >= total ? 3 : ratio >= 0.66f ? 2 : ratio >= 0.34f ? 1 : 0;
        }

        // Zdobyta gwiazdka wskakuje: rośnie od zera z odbiciem i lekkim obrotem, po kolei, z „tyknięciem” telefonu.
        // Animacja tylko przy pierwszym pokazaniu wyniku (resultSaved = false), nie przy przebudowie ekranu.
        void PopStar(RectTransform rt, float size, int i)
        {
            // Obrót i skala wokół środka gwiazdki (Ui.Place ustawia punkt zaczepienia w lewym górnym rogu).
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition += new Vector2(size * 0.5f, -size * 0.5f);
            rt.localScale = Vector3.zero;
            var fxr = Fx_(screen);
            float delay = 0.45f + i * 0.28f;
            fxr.Add(delay, 0.01f, t => { }, Haptics.Tap);
            fxr.Add(delay, 0.45f, t =>
            {
                float k = QuizFx.BackOut(t, 2.6f);   // mocniejsze odbicie niż zwykle
                rt.localScale = new Vector3(k, k, 1f);
                rt.localRotation = Quaternion.Euler(0f, 0f, -25f * (1f - t));
            }, null);
        }

        float BuildSummary(RectTransform c)
        {
            int total = Mathf.Max(1, tasks.Count);
            float ratio = (float)score / total;
            int stars = Stars();
            float y = 30f;

            const float star = 54f, sgap = 14f;
            float sx = (W - 3 * star - 2 * sgap) * 0.5f;
            for (int i = 0; i < 3; i++)
            {
                bool on = i < stars;
                float lift = i == 1 ? 0f : 12f;   // środkowa gwiazdka trochę wyżej
                if (on)
                {
                    // Pod zdobytą gwiazdką szare „gniazdo” – gwiazdka wskakuje w nie po kolei.
                    var slot = Ui.Icon("StarSlot", c, IconId.Star, Palette.LockedFill, star);
                    Ui.Place(slot.rectTransform, sx + i * (star + sgap), y + lift, star, star);
                }
                var s = Ui.Icon("Star", c, IconId.Star, on ? Palette.Accent : Palette.LockedFill, star);
                Ui.Place(s.rectTransform, sx + i * (star + sgap), y + lift, star, star);
                if (on && !resultSaved) PopStar(s.rectTransform, star, i);
            }
            y += star + 12f + 26f;

            string head = stars == 3 ? "Bezbłędnie!" : stars == 2 ? "Dobra robota!" : stars == 1 ? "Zaliczone!" : "Spróbuj jeszcze raz";
            var title = Ui.Text("Title", c, head, 26f, Palette.Text, FontWeight.ExtraBold, TextAlignmentOptions.Top);
            float th = Ui.TextHeight(title, cw);
            Ui.Place(title.rectTransform, m, y, cw, th);
            y += th + 10f;

            var res = Ui.Text("Score", c, "Poprawne odpowiedzi: " + score + " z " + tasks.Count, 16f, Palette.TextMuted,
                FontWeight.SemiBold, TextAlignmentOptions.Top);
            Ui.Place(res.rectTransform, m, y, cw, 22f);
            y += 22f + 24f;

            ProgressBar.Create(c, m + 24f, y, cw - 48f, 16f, Palette.Track, ratio);
            y += 16f + 24f;

            // Co dalej: przy 0 gwiazdek – powtórka; przy 1–2 – można poprawić; plus porównanie z najlepszym wynikiem.
            string note;
            if (stars == 0) note = "Zdobądź co najmniej 1 gwiazdkę, żeby zaliczyć ten level.";
            else if (best > stars) note = "Twój najlepszy wynik to " + StarsText(best) + " – zostaje zapisany.";
            else if (best >= 1 && stars > best) note = "Nowy rekord! Poprzednio " + StarsText(best) + ".";
            else if (stars < 3) note = "Możesz spróbować ponownie i zdobyć 3 gwiazdki.";
            else note = null;
            if (note != null)
            {
                var n = Ui.Text("Note", c, note, 14.5f, Palette.TextMuted, FontWeight.Regular, TextAlignmentOptions.Top);
                float nh = Ui.TextHeight(n, cw - 32f);
                Ui.Place(n.rectTransform, m + 16f, y, cw - 32f, nh);
                y += nh;
            }
            return y;
        }

        static string StarsText(int n)
        {
            return n + (n == 1 ? " gwiazdka" : " gwiazdki");
        }

        // Na pierwszym pytaniu powtórki: przypomnienie najlepszego wyniku i zachęta do kompletu.
        float BestNote(RectTransform c, float y)
        {
            const float pad = 12f, star = 16f;
            string text = "Twój najlepszy wynik: " + StarsText(best) + " z 3. Spróbuj zdobyć komplet!";
            var t = Ui.Text("Best", c, text, 13.5f, Palette.Text, FontWeight.SemiBold);
            float tw = cw - 2 * pad - 3 * star - 14f;
            float th = Ui.TextHeight(t, tw);
            float h = Mathf.Max(star, th) + 2 * pad;
            var card = LipCard.Create(c, "BestNote", m, y, cw, h + 4f, SelFill, Palette.Accent, Palette.AccentLip, 14f, 4f, 2f);
            for (int i = 0; i < 3; i++)
            {
                var s = Ui.Icon("Star", card.Content, IconId.Star, i < best ? Palette.AccentLip : Palette.LockedFill, star);
                Ui.Place(s.rectTransform, pad + i * (star + 2f), (h - star) * 0.5f, star, star);
            }
            t.transform.SetParent(card.Content, false);
            Ui.Place(t.rectTransform, pad + 3 * star + 14f, (h - th) * 0.5f, tw, th);
            return h + 4f + 16f;
        }

        float BuildSummaryBar()
        {
            bar = Ui.Rect("BottomBar", screen);
            bar.anchorMin = new Vector2(0f, 0f);
            bar.anchorMax = new Vector2(1f, 0f);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.anchoredPosition = Vector2.zero;

            // 0 gwiazdek: tylko powtórka. 1–2: UKOŃCZ albo powtórka po lepszy wynik. 3: UKOŃCZ.
            int stars = Stars();
            float y = 10f;
            if (stars == 0)
            {
                // Niezaliczone: powtórka albo wyjście do rozdziału (level zostaje jak był – wcześniejszy wynik się nie zmienia).
                Button(bar, m, y, cw, "SPRÓBUJ PONOWNIE", true, true, Restart);
                y += ButtonH + 10f;
                Button(bar, m, y, cw, "WRÓĆ DO ROZDZIAŁU", true, false, app.Back);
            }
            else
            {
                if (stars < 3)
                {
                    Button(bar, m, y, cw, "SPRÓBUJ PONOWNIE", true, false, Restart);
                    y += ButtonH + 10f;
                }
                Button(bar, m, y, cw, "UKOŃCZ", true, true, () => onFinish(stars));
            }
            float h = y + ButtonH + 20f;
            bar.sizeDelta = new Vector2(0f, h);
            return h;
        }

        // ---------------- typy zadań ----------------

        // Wybór odpowiedzi (jedna albo kilka poprawnych).
        float Choice(RectTransform c, TaskDef t, float y)
        {
            if (t.multi) y += Hint(c, y, "Zaznacz wszystkie poprawne odpowiedzi.");
            foreach (int i in order)
            {
                var o = t.options[i];
                bool sel = picked.Contains(i);
                Look look = !answered ? (sel ? Look.Selected : Look.Normal)
                          : o.correct ? Look.Good
                          : sel ? Look.Bad : Look.Dim;
                int k = i;
                Action click = null;
                if (!answered)
                    click = () =>
                    {
                        if (t.multi) { if (!picked.Remove(k)) picked.Add(k); }
                        else { picked.Clear(); picked.Add(k); }
                        Show(false);
                    };
                y += Answer(c, m, y, cw, o.text, look, sel || (answered && o.correct), click) + 10f;
            }
            return y;
        }

        // Luka w zdaniu: wybór z propozycji albo wpisanie odpowiedzi.
        float Gap(RectTransform c, TaskDef t, float y)
        {
            bool typing = t.mode == "type";
            string fill = null;
            if (typing && answered) fill = typed;
            else if (!typing && gapPick >= 0) fill = words[gapPick];

            Color fc = !answered ? Palette.AccentLip : lastCorrect ? Good : Bad;
            string blank = string.IsNullOrEmpty(fill)
                ? "<color=#" + ColorUtility.ToHtmlStringRGB(Palette.TextLocked) + ">________</color>"
                : "<color=#" + ColorUtility.ToHtmlStringRGB(fc) + "><b>" + Escape(fill) + "</b></color>";
            string sentence = ContentBlocks.Rich(t.text ?? "");
            int at = sentence.IndexOf("___", StringComparison.Ordinal);
            if (at >= 0)
            {
                int end = at;
                while (end < sentence.Length && sentence[end] == '_') end++;
                sentence = sentence.Substring(0, at) + blank + sentence.Substring(end);
            }

            const float pad = 16f;
            var probe = Ui.Text("Sentence", c, sentence, 18f, Palette.Text, FontWeight.Regular);
            probe.richText = true;
            probe.lineSpacing = 6f;
            float sh = Ui.TextHeight(probe, cw - 2 * pad);
            float h = sh + 2 * pad;
            var card = LipCard.Create(c, "SentenceCard", m, y, cw, h + 4f, Palette.CardFill, Palette.CardEdge, Palette.CardLip, 16f, 4f, 2f);
            probe.transform.SetParent(card.Content, false);
            Ui.Place(probe.rectTransform, pad, pad, cw - 2 * pad, sh);
            y += h + 4f + 18f;

            if (typing)
            {
                Look look = !answered ? Look.Normal : lastCorrect ? Look.Good : Look.Bad;
                InputBox(c, m, y, cw, 52f, typed, look);
                return y + 56f;
            }

            if (!answered) y += Hint(c, y, "Stuknij słowo, które pasuje do luki.");
            return y + 12f + Flow(c, m, y, cw, order, k => words[k], k =>
            {
                if (!answered) return k == gapPick ? Look.Selected : Look.Normal;
                if (k == 0) return Look.Good;                 // pierwsza propozycja = poprawna odpowiedź
                return k == gapPick ? Look.Bad : Look.Dim;
            }, k => answered ? null : (Action)(() => { gapPick = gapPick == k ? -1 : k; Show(false); }));
        }

        // Kafelki: ułóż zdanie, stukając słowa po kolei; stuknięcie ułożonego słowa je oddaje.
        float Tiles(RectTransform c, TaskDef t, float y)
        {
            const float pad = 12f;
            float iw = cw - 2 * pad;
            float rowH = ChipSize("Ag", iw).y;
            float used = Flow(null, 0f, 0f, iw, placed, k => words[k], k => Look.Normal, k => null);
            float h = Mathf.Max(2 * rowH + ChipGap, used) + 2 * pad;

            Color edge = !answered ? Palette.CardEdge : lastCorrect ? Good : Bad;
            Color lip = !answered ? Palette.CardLip : lastCorrect ? GoodLip : BadLip;
            var area = LipCard.Create(c, "Answer", m, y, cw, h + 4f, Palette.CardFill, edge, lip, 16f, 4f, 2f);
            if (placed.Count == 0)
            {
                var ph = Ui.Text("Placeholder", area.Content, "Stuknij słowa poniżej w dobrej kolejności", 14f, Palette.TextLocked,
                    FontWeight.Regular, TextAlignmentOptions.Center);
                Ui.Stretch(ph.rectTransform, pad, 0f, pad, 0f);
            }
            Flow(area.Content, pad, pad, iw, placed, k => words[k],
                k => !answered ? Look.Normal : lastCorrect ? Look.Good : Look.Bad,
                k => answered ? null : (Action)(() => { placed.Remove(k); Show(false); }));
            y += h + 4f + 18f;

            var rest = new List<int>();
            foreach (int k in order) if (!placed.Contains(k)) rest.Add(k);
            if (answered || rest.Count == 0) return y;
            return y + 12f + Flow(c, m, y, cw, rest, k => words[k], k => Look.Normal, k => (Action)(() => { placed.Add(k); Show(false); }));
        }

        // Pary: stuknij pojęcie po lewej, potem pasujący opis po prawej. Połączone karty mają ten sam kolor i numer.
        float Pairs(RectTransform c, TaskDef t, float y)
        {
            if (!answered) y += Hint(c, y, "Stuknij pojęcie po lewej, a potem pasujący opis po prawej.");
            const float gap = 10f, pad = 12f, badge = 22f;
            float colW = (cw - gap) * 0.5f;
            float tw = colW - 2 * pad - badge - 6f;

            for (int row = 0; row < t.pairs.Count; row++)
            {
                int li = row, ri = order[row];
                int owner = OwnerOf(ri);
                float hl = TextH(t.pairs[li].left, ChipText, FontWeight.SemiBold, tw);
                float hr = TextH(t.pairs[ri].right, 14.5f, FontWeight.Regular, tw);
                float h = Mathf.Max(52f, Mathf.Max(hl, hr) + 2 * pad);

                // Lewa karta.
                Look ll; Color? lc = null;
                if (answered) ll = pairOf[li] == li ? Look.Good : Look.Bad;
                else if (pairSel == li) ll = Look.Selected;
                else { ll = Look.Normal; if (pairOf[li] >= 0) lc = PairColors[li % PairColors.Length]; }
                int l = li;
                PairCard(c, m, y, colW, h, t.pairs[li].left, ChipText, FontWeight.SemiBold, ll, lc,
                    pairOf[li] >= 0 ? li : -1, false,
                    answered ? null : (Action)(() =>
                    {
                        if (pairOf[l] >= 0) pairOf[l] = -1;           // stuknięcie połączonej = rozłączenie
                        pairSel = pairSel == l ? -1 : l;
                        Show(false);
                    }));

                // Prawa karta.
                Look rl; Color? rc = null;
                if (answered) rl = owner < 0 ? Look.Dim : owner == ri ? Look.Good : Look.Bad;
                else { rl = Look.Normal; if (owner >= 0) rc = PairColors[owner % PairColors.Length]; }
                int r = ri;
                PairCard(c, m + colW + gap, y, colW, h, t.pairs[ri].right, 14.5f, FontWeight.Regular, rl, rc,
                    owner, true,
                    answered ? null : (Action)(() =>
                    {
                        int o = OwnerOf(r);
                        if (pairSel >= 0)
                        {
                            if (o >= 0) pairOf[o] = -1;               // opis był już połączony – przepinamy
                            pairOf[pairSel] = r;
                            pairSel = -1;
                        }
                        else if (o >= 0) pairOf[o] = -1;
                        Show(false);
                    }));
                y += h + 4f + 10f;
            }
            return y;
        }

        // Strefy: stuknij element, potem strefę. Stuknięcie elementu w strefie oddaje go na dół.
        float Zones(RectTransform c, TaskDef t, float y)
        {
            if (!answered) y += Hint(c, y, zoneSel >= 0 ? "Teraz stuknij strefę." : "Stuknij element, a potem strefę, do której pasuje.");
            const float gap = 10f, pad = 10f, head = 34f;
            int nz = t.zones.Count;
            if (nz == 0) return y;
            int cols = nz <= 3 ? nz : 2;
            float zw = (cw - gap * (cols - 1)) / cols;
            float iw = zw - 2 * pad;

            for (int start = 0; start < nz; start += cols)
            {
                int end = Mathf.Min(nz, start + cols);
                float rowH = 0f;
                for (int z = start; z < end; z++)
                {
                    float hz = head;
                    foreach (int k in order) if (zoneOf[k] == z) hz += ChipSize(t.items[k].text, iw).y + ChipGap;
                    rowH = Mathf.Max(rowH, Mathf.Max(120f, hz + pad));
                }
                for (int z = start; z < end; z++)
                {
                    float x = m + (z - start) * (zw + gap);
                    bool target = !answered && zoneSel >= 0;
                    int zi = z;
                    var box = LipCard.Create(c, "Zone", x, y, zw, rowH + 4f, ZoneFill,
                        target ? Palette.Accent : Palette.CardEdge, target ? Palette.AccentLip : Palette.CardLip, 16f, 4f, 2f);
                    if (target) box.SetClickable(() => { zoneOf[zoneSel] = zi; zoneSel = -1; Show(false); });

                    var label = Ui.Text("Label", box.Content, t.zones[z].label, 13f, Palette.TextMuted, FontWeight.ExtraBold,
                        TextAlignmentOptions.Center);
                    Ui.Place(label.rectTransform, pad, 8f, iw, head - 12f);

                    float iy = head;
                    foreach (int k in order)
                    {
                        if (zoneOf[k] != z) continue;
                        int item = k;
                        Look look = !answered ? Look.Normal : ZoneIndex(t, t.items[k].zone) == z ? Look.Good : Look.Bad;
                        Vector2 s = ChipSize(t.items[k].text, iw);
                        Chip(box.Content, pad, iy, new Vector2(iw, s.y), t.items[k].text, look,
                            answered ? null : (Action)(() => { zoneOf[item] = -1; zoneSel = -1; Show(false); }));
                        iy += s.y + ChipGap;
                    }
                }
                y += rowH + 4f + gap;
            }

            var rest = new List<int>();
            foreach (int k in order) if (zoneOf[k] < 0) rest.Add(k);
            if (answered || rest.Count == 0) return y;
            y += 8f;
            return y + 12f + Flow(c, m, y, cw, rest, k => t.items[k].text, k => k == zoneSel ? Look.Selected : Look.Normal,
                k => (Action)(() => { zoneSel = zoneSel == k ? -1 : k; Show(false); }));
        }

        // ---------------- ocena ----------------

        bool Ready()
        {
            var t = tasks[index];
            switch (t.type)
            {
                case "choice": return picked.Count > 0;
                case "gap":
                    if (t.mode == "type") return Norm(input != null ? input.text : typed).Length > 0;
                    return gapPick >= 0;
                case "tiles": return placed.Count > 0;
                case "pairs": foreach (int p in pairOf) if (p < 0) return false; return true;
                case "zones": foreach (int z in zoneOf) if (z < 0) return false; return true;
            }
            return true;
        }

        bool Evaluate(TaskDef t)
        {
            switch (t.type)
            {
                case "choice":
                    for (int i = 0; i < t.options.Count; i++)
                        if (t.options[i].correct != picked.Contains(i)) return false;
                    return true;
                case "gap":
                {
                    string a = t.mode == "type" ? typed : gapPick >= 0 ? words[gapPick] : "";
                    foreach (var ok in t.answers) if (Norm(ok) == Norm(a)) return true;
                    return false;
                }
                case "tiles":
                {
                    var sb = new StringBuilder();
                    foreach (int k in placed) sb.Append(words[k]).Append(' ');
                    return Norm(sb.ToString()) == Norm(t.answer);
                }
                case "pairs":
                    for (int i = 0; i < pairOf.Length; i++) if (pairOf[i] != i) return false;
                    return true;
                case "zones":
                    for (int i = 0; i < zoneOf.Length; i++) if (zoneOf[i] != ZoneIndex(t, t.items[i].zone)) return false;
                    return true;
            }
            return true;
        }

        // Poprawna odpowiedź w pasku po błędzie – tam, gdzie nie widać jej na samych kartach.
        static string Solution(TaskDef t)
        {
            switch (t.type)
            {
                case "gap": return t.answers.Count > 0 ? "Poprawnie: " + t.answers[0] : null;
                case "tiles": return "Poprawnie: " + t.answer;
                case "pairs":
                {
                    var parts = new List<string>();
                    foreach (var p in t.pairs) parts.Add(p.left + " – " + p.right);
                    return "Poprawnie: " + string.Join("; ", parts.ToArray());
                }
                case "zones":
                {
                    var parts = new List<string>();
                    foreach (var z in t.zones)
                    {
                        var items = new List<string>();
                        foreach (var it in t.items) if (it.zone == z.id) items.Add(it.text);
                        parts.Add(z.label + ": " + string.Join(", ", items.ToArray()));
                    }
                    return "Poprawnie – " + string.Join(" · ", parts.ToArray());
                }
            }
            return null;
        }

        // Porównanie odpowiedzi: bez wielkości liter, interpunkcji i polskich znaków, z pojedynczymi spacjami.
        public static string Norm(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder(s.Length);
            bool space = false;
            foreach (char raw in s.Trim().ToLowerInvariant())
            {
                char ch = Fold(raw);
                if (char.IsLetterOrDigit(ch)) { if (space && sb.Length > 0) sb.Append(' '); sb.Append(ch); space = false; }
                else if (char.IsWhiteSpace(ch)) space = true;
            }
            return sb.ToString();
        }

        static char Fold(char c)
        {
            switch (c)
            {
                case 'ą': return 'a';
                case 'ć': return 'c';
                case 'ę': return 'e';
                case 'ł': return 'l';
                case 'ń': return 'n';
                case 'ó': return 'o';
                case 'ś': return 's';
                case 'ź': case 'ż': return 'z';
                case 'ä': return 'a';
                case 'ö': return 'o';
                case 'ü': return 'u';
                case 'ß': return 's';
            }
            return c;
        }

        // ---------------- pomocniki ----------------

        static int ZoneIndex(TaskDef t, string id)
        {
            for (int i = 0; i < t.zones.Count; i++) if (t.zones[i].id == id) return i;
            return -1;
        }

        int OwnerOf(int right)
        {
            for (int i = 0; i < pairOf.Length; i++) if (pairOf[i] == right) return i;
            return -1;
        }

        static int[] Filled(int n)
        {
            var a = new int[n];
            for (int i = 0; i < n; i++) a[i] = -1;
            return a;
        }

        // Losowa kolejność 0..n-1. notIdentity = nie zostawiaj kolejności 1:1 (pary i kafelki byłyby za łatwe).
        List<int> Shuffled(int n, bool notIdentity)
        {
            var list = new List<int>();
            for (int i = 0; i < n; i++) list.Add(i);
            for (int attempt = 0; attempt < 8; attempt++)
            {
                for (int i = n - 1; i > 0; i--)
                {
                    int j = rng.Next(i + 1);
                    int tmp = list[i]; list[i] = list[j]; list[j] = tmp;
                }
                if (!notIdentity || n < 2) break;
                bool identity = true;
                for (int i = 0; i < n; i++) if (list[i] != i) { identity = false; break; }
                if (!identity) break;
            }
            return list;
        }

        static List<string> SplitWords(string s)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(s)) return list;
            foreach (var w in s.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)) list.Add(w);
            return list;
        }

        // Znaki < i > w treści nie mogą zostać wzięte za znaczniki TextMeshPro.
        static string Escape(string s)
        {
            return string.IsNullOrEmpty(s) ? "" : s.Replace("<", "‹").Replace(">", "›");
        }

        float Hint(RectTransform c, float y, string text)
        {
            var h = Ui.Text("Hint", c, text, 13f, Palette.TextMuted, FontWeight.SemiBold);
            float th = Ui.TextHeight(h, cw);
            Ui.Place(h.rectTransform, m, y, cw, th);
            return th + 12f;
        }

        float TextH(string text, float size, FontWeight weight, float width)
        {
            var probe = Ui.Text("Probe", screen, ContentBlocks.Rich(text), size, Palette.Text, weight);
            probe.richText = true;
            float h = Ui.TextHeight(probe, width);
            UnityEngine.Object.Destroy(probe.gameObject);
            return h;
        }

        static void Colors(Look look, out Color fill, out Color edge, out Color lip, out Color text)
        {
            text = Palette.Text;
            switch (look)
            {
                case Look.Selected: fill = SelFill; edge = Palette.Accent; lip = Palette.AccentLip; break;
                case Look.Good: fill = GoodFill; edge = Good; lip = GoodLip; break;
                case Look.Bad: fill = BadFill; edge = Bad; lip = BadLip; break;
                case Look.Dim: fill = Palette.CardFill; edge = Palette.CardEdge; lip = Palette.CardLip; text = Palette.TextLocked; break;
                default: fill = Palette.CardFill; edge = Palette.CardEdge; lip = Palette.CardLip; break;
            }
        }

        LipCard Button(Transform parent, float x, float y, float w, string label, bool active, bool primary, Action onClick)
        {
            Color fill = !active ? Palette.LockedFill : primary ? Palette.Accent : Palette.CardFill;
            Color edge = !active ? Palette.LockedFill : primary ? Palette.Accent : Palette.CardEdge;
            Color lip = !active ? Palette.LockedLip : primary ? Palette.AccentLip : Palette.CardLip;
            var b = LipCard.Create(parent, "Button", x, y, w, ButtonH, fill, edge, lip, 16f, 5f, primary || !active ? 0f : 2f);
            var l = Ui.Text("Label", b.Content, label, 16f, !active ? Palette.TextLocked : primary ? Palette.OnAccent : Palette.TextMuted,
                FontWeight.ExtraBold, TextAlignmentOptions.Capline);
            Ui.Stretch(l.rectTransform);
            if (active && onClick != null) b.SetClickable(onClick);
            return b;
        }

        // Karta odpowiedzi (wybór): tekst i znaczek po prawej. Zwraca wysokość razem z wargą.
        float Answer(RectTransform parent, float x, float y, float w, string text, Look look, bool mark, Action onClick)
        {
            const float pad = 14f, icon = 20f;
            Color fill, edge, lip, tc;
            Colors(look, out fill, out edge, out lip, out tc);
            float tw = w - 2 * pad - icon - 8f;
            var probe = Ui.Text("Text", parent, ContentBlocks.Rich(text), 15.5f, tc, FontWeight.SemiBold);
            probe.richText = true;
            float th = Ui.TextHeight(probe, tw);
            float h = Mathf.Max(50f, th + 2 * pad);

            var card = LipCard.Create(parent, "Answer", x, y, w, h + 4f, fill, edge, lip, 16f, 4f, 2f);
            probe.transform.SetParent(card.Content, false);
            Ui.Place(probe.rectTransform, pad, (h - th) * 0.5f, tw, th);
            if (mark && look != Look.Dim)
            {
                IconId id = look == Look.Bad ? IconId.Exclaim : IconId.Check;
                Color ic = look == Look.Selected ? Palette.AccentLip : edge;
                var i = Ui.Icon("Mark", card.Content, id, ic, icon);
                Ui.Place(i.rectTransform, w - pad - icon, (h - icon) * 0.5f, icon, icon);
            }
            if (onClick != null) card.SetClickable(onClick);
            return h + 4f;
        }

        // Karta pary z kółkiem-numerem (numer pojęcia, z którym jest połączona).
        void PairCard(RectTransform parent, float x, float y, float w, float h, string text, float size, FontWeight weight,
            Look look, Color? linked, int number, bool badgeLeft, Action onClick)
        {
            const float pad = 12f, badge = 22f;
            Color fill, edge, lip, tc;
            Colors(look, out fill, out edge, out lip, out tc);
            if (linked.HasValue) { edge = linked.Value; lip = Palette.Darker(linked.Value); }

            var card = LipCard.Create(parent, "Pair", x, y, w, h + 4f, fill, edge, lip, 14f, 4f, 2f);
            float tw = w - 2 * pad - badge - 6f;
            float tx = badgeLeft ? pad + badge + 6f : pad;
            var t = Ui.Text("Text", card.Content, ContentBlocks.Rich(text), size, tc, weight, TextAlignmentOptions.Left);
            t.richText = true;
            float th = Ui.TextHeight(t, tw);
            Ui.Place(t.rectTransform, tx, (h - th) * 0.5f, tw, th);

            if (number >= 0)
            {
                Color bc = look == Look.Good ? Good : look == Look.Bad ? Bad : linked.HasValue ? linked.Value : Palette.TextMuted;
                var b = Ui.Rounded("Badge", card.Content, bc, badge * 0.5f);
                Ui.Place(b.rectTransform, badgeLeft ? pad : w - pad - badge, (h - badge) * 0.5f, badge, badge);
                var n = Ui.Text("Number", b.transform, (number + 1).ToString(), 12f, Color.white, FontWeight.ExtraBold,
                    TextAlignmentOptions.Center);
                Ui.Stretch(n.rectTransform);
            }
            if (onClick != null) card.SetClickable(onClick);
        }

        // Rozmiar kafelka (razem z wargą). Za długie słowa zawijają się do maxW.
        Vector2 ChipSize(string text, float maxW)
        {
            var probe = Ui.Text("Probe", screen, text, ChipText, Palette.Text, FontWeight.SemiBold);
            probe.textWrappingMode = TextWrappingModes.NoWrap;
            float tw = probe.GetPreferredValues(probe.text, 10000f, 0f).x;
            float w = Mathf.Min(maxW, Mathf.Max(44f, Mathf.Ceil(tw) + 2 * ChipPadX));
            probe.textWrappingMode = TextWrappingModes.Normal;
            float th = Ui.TextHeight(probe, w - 2 * ChipPadX);
            UnityEngine.Object.Destroy(probe.gameObject);
            return new Vector2(w, Mathf.Max(42f, th + 2 * ChipPadY) + 4f);
        }

        static void Chip(RectTransform parent, float x, float y, Vector2 size, string text, Look look, Action onClick)
        {
            Color fill, edge, lip, tc;
            Colors(look, out fill, out edge, out lip, out tc);
            var card = LipCard.Create(parent, "Chip", x, y, size.x, size.y, fill, edge, lip, 12f, 4f, 2f);
            var t = Ui.Text("Text", card.Content, text, ChipText, tc, FontWeight.SemiBold, TextAlignmentOptions.Center);
            Ui.Stretch(t.rectTransform, ChipPadX - 4f, 0f, ChipPadX - 4f, 0f);
            if (onClick != null) card.SetClickable(onClick);
        }

        // Kafelki w wierszach z zawijaniem. parent == null → tylko liczy wysokość.
        float Flow(RectTransform parent, float x0, float y0, float w, List<int> ids,
            Func<int, string> text, Func<int, Look> look, Func<int, Action> click)
        {
            if (ids.Count == 0) return 0f;
            float x = x0, y = y0, rowH = 0f;
            foreach (int id in ids)
            {
                Vector2 s = ChipSize(text(id), w);
                if (x > x0 && x + s.x > x0 + w) { x = x0; y += rowH + ChipGap; rowH = 0f; }
                if (parent != null) Chip(parent, x, y, s, text(id), look(id), click(id));
                x += s.x + ChipGap;
                rowH = Mathf.Max(rowH, s.y);
            }
            return y + rowH - y0;
        }

        // Pole do wpisania odpowiedzi (luka w trybie „wpisz”). Każda litera odświeża tylko dolny pasek.
        void InputBox(RectTransform parent, float x, float y, float w, float h, string value, Look look)
        {
            Color fill, edge, lip, tc;
            Colors(look, out fill, out edge, out lip, out tc);
            var card = LipCard.Create(parent, "Input", x, y, w, h + 4f, fill, edge, lip, 14f, 4f, 2f);

            var host = Ui.Rect("Field", card.Content);
            Ui.Stretch(host);
            host.gameObject.SetActive(false);   // pole konfigurujemy przed włączeniem, żeby nie startowało bez tekstu
            var hit = host.gameObject.AddComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0f);
            hit.raycastTarget = !answered;

            var area = Ui.Rect("TextArea", host);
            Ui.Stretch(area, 16f, 4f, 16f, 4f);
            area.gameObject.AddComponent<RectMask2D>();
            var ph = Ui.Text("Placeholder", area, "Wpisz odpowiedź…", 16f, Palette.TextLocked, FontWeight.Regular, TextAlignmentOptions.MidlineLeft);
            Ui.Stretch(ph.rectTransform);
            var txt = Ui.Text("Text", area, "", 17f, tc, FontWeight.SemiBold, TextAlignmentOptions.MidlineLeft);
            Ui.Stretch(txt.rectTransform);
            txt.textWrappingMode = TextWrappingModes.NoWrap;

            var field = host.gameObject.AddComponent<TMP_InputField>();
            field.textViewport = area;
            field.textComponent = txt;
            field.placeholder = ph;
            field.targetGraphic = hit;
            var font = Fonts.Get(FontWeight.SemiBold);
            if (font != null) field.fontAsset = font;
            field.pointSize = 17f;
            field.richText = false;
            field.lineType = TMP_InputField.LineType.SingleLine;
            field.characterLimit = 60;
            field.customCaretColor = true;
            field.caretColor = Palette.Text;
            field.selectionColor = new Color(Palette.Accent.r, Palette.Accent.g, Palette.Accent.b, 0.35f);
            field.text = value ?? "";
            field.interactable = !answered;
            host.gameObject.SetActive(true);

            if (!answered)
            {
                field.onValueChanged.AddListener(v => { typed = v; RebuildBar(); });
                field.onSubmit.AddListener(v => { typed = v; Check(); });
                input = field;
            }
        }
    }

    // Proste animacje ekranu zadań (kilka naraz). Działa tylko, gdy coś się animuje – potem się wyłącza.
    public class QuizFx : MonoBehaviour
    {
        class Tween { public float Delay, Dur, T; public Action<float> Step; public Action Done; }
        readonly List<Tween> list = new List<Tween>();

        public void Add(float delay, float dur, Action<float> step, Action done)
        {
            list.Add(new Tween { Delay = delay, Dur = dur, Step = step, Done = done });
            step(0f);
            enabled = true;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var tw = list[i];
                if (tw.Delay > 0f) { tw.Delay -= dt; continue; }
                tw.T += dt;
                float t = Mathf.Clamp01(tw.T / tw.Dur);
                // Obiekt mógł zniknąć (np. szybkie DALEJ) – wtedy po prostu kończymy tę animację.
                try { tw.Step(t); } catch (Exception) { t = 1f; }
                if (t >= 1f)
                {
                    list.RemoveAt(i);
                    if (tw.Done != null) try { tw.Done(); } catch (Exception) { }
                }
            }
            if (list.Count == 0) enabled = false;
        }

        public static float EaseOut(float t) { t = 1f - t; return 1f - t * t * t; }
        public static float BackOut(float t) { return BackOut(t, 1.7f); }
        public static float BackOut(float t, float s)
        {
            t -= 1f;
            return t * t * ((s + 1f) * t + s) + 1f;
        }
    }
}

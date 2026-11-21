using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FinanceQuest
{
    // Mini gra „Prawda czy Pułapka?” – ekran. Logika (punkty, kolejność, gwiazdki) jest w ScamRound (assembly Sim, z testami).
    // Karta jak „Tinder dla ofert”: przeciągnij w LEWO = PUŁAPKA, w PRAWO = BEZPIECZNE. Do tego dwa duże przyciski
    // i klawisze strzałek (Enter = Dalej). Po decyzji: błysk koloru, karta wyjaśnienia od dołu, „DALEJ” bez limitu czasu.
    // Karty pochodzą z edytora treści (Mini gra → „Prawda czy Pułapka?”); bez własnych kart gra bierze 6 domyślnych.
    public class ScamGame
    {
        public const string Id = "prawda-czy-pulapka";

        const float ButtonH = 58f, DecideH = 64f, DragLimit = 110f;
        const string Disclaimer = "To jest symulacja edukacyjna, nie stanowi doradztwa inwestycyjnego. Karty to fikcyjne przykłady.";

        // Kolory z ikoną i tekstem – nigdy sam kolor (czytelne też przy zaburzeniach widzenia barw).
        static readonly Color Safe = Palette.Hex("2F7D51"), SafeFill = Palette.Hex("E7F3EB"), SafeLip = Palette.Hex("235E3D");
        static readonly Color Trap = Palette.Hex("B8432D"), TrapFill = Palette.Hex("FBE7E2"), TrapLip = Palette.Hex("8C3322");
        static readonly Color Oops = Palette.Hex("E08A2E");   // błąd = pomarańczowy błysk (łagodniej niż czerwony)
        static readonly Color Bubble = Palette.Hex("EFECE5");

        enum Stage { Intro, Play, Summary }

        readonly FinanceQuestApp app;
        readonly RectTransform screen;
        readonly Action<int> onComplete;
        readonly Action onFinish;
        readonly List<ScamCard> source;
        readonly bool shuffle;
        readonly float W, m, cw;

        Stage stage = Stage.Intro;
        ScamRound round;
        bool completed, busy;
        int seed;
        int decidedDir;               // -1 = pułapka (w lewo), +1 = bezpieczne (w prawo)
        RectTransform holder;          // obracany „uchwyt” bieżącej karty (punkt zaczepienia na środku)
        Vector2 holderBase;
        CanvasGroup stampTrap, stampSafe;

        public static void Build(RectTransform screen, FinanceQuestApp app, MiniGameDef def, Action<int> onComplete, Action onFinish)
        {
            new ScamGame(screen, app, def, onComplete, onFinish).Rebuild(false);
        }

        ScamGame(RectTransform screen, FinanceQuestApp app, MiniGameDef def, Action<int> onComplete, Action onFinish)
        {
            this.screen = screen; this.app = app; this.onComplete = onComplete; this.onFinish = onFinish;
            W = app.ScreenWidth; m = ScreenKit.Margin; cw = W - 2 * m;
            source = Cards(def);
            shuffle = def != null && def.shuffle;
            app.SwipeForward = null;
            screen.gameObject.AddComponent<Keys>().Game = this;
        }

        // Karty z treści (puste pola pomijamy); bez kart – domyślna szóstka.
        static List<ScamCard> Cards(MiniGameDef def)
        {
            var list = new List<ScamCard>();
            if (def != null && def.cards != null)
                foreach (var c in def.cards)
                {
                    if (c == null || string.IsNullOrEmpty(c.text)) continue;
                    list.Add(new ScamCard
                    {
                        Type = string.IsNullOrEmpty(c.type) ? "sms" : c.type,
                        From = c.from ?? "",
                        Text = c.text,
                        Answer = c.answer == "safe" ? ScamAnswer.Safe : ScamAnswer.Trap,
                        Explain = c.explain ?? "",
                        Flags = c.flags ?? new List<string>(),
                    });
                }
            return list.Count > 0 ? list : ScamDeck.Default();
        }

        QuizFx Fx()
        {
            var f = screen.GetComponent<QuizFx>();
            if (f == null) f = screen.gameObject.AddComponent<QuizFx>();
            return f;
        }

        // ---------------- budowa ekranu ----------------

        void Rebuild(bool appear)
        {
            for (int i = screen.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(screen.GetChild(i).gameObject);
            holder = null; stampTrap = stampSafe = null;

            var content = ScreenKit.Scroll(screen, W, 0f);
            var bar = Ui.Rect("BottomBar", screen);
            bar.anchorMin = new Vector2(0f, 0f);
            bar.anchorMax = new Vector2(1f, 0f);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.anchoredPosition = Vector2.zero;

            float y = 8f, barH;
            switch (stage)
            {
                case Stage.Intro: y = Intro(content, y); barH = Bar(bar, "ZACZYNAMY", Start); break;
                case Stage.Play: y = Play(content, y, appear); barH = round.Answered ? FeedbackBar(bar) : Bar(bar, null, null); break;
                default: y = Summary(content, y); barH = SummaryBar(bar); break;
            }
            bar.sizeDelta = new Vector2(0f, barH);
            ((RectTransform)content.parent).offsetMin = new Vector2(0f, barH);
            ScreenKit.SetContentHeight(content, y + 20f);
        }

        // Dolny pasek: stały komunikat o symulacji (+ opcjonalnie jeden duży przycisk).
        float Bar(RectTransform bar, string label, Action onClick)
        {
            float y = 8f;
            if (label != null) { Button(bar, m, y, cw, label, true, onClick); y += ButtonH + 10f; }
            return y + Disclaim(bar, y) + 10f;
        }

        float Disclaim(RectTransform bar, float y)
        {
            var d = Ui.Text("Disclaimer", bar, Disclaimer, 10.5f, Palette.TextMuted, FontWeight.SemiBold, TextAlignmentOptions.Center);
            float h = Ui.TextHeight(d, cw);
            Ui.Place(d.rectTransform, m, y, cw, h);
            return h;
        }

        LipCard Button(Transform parent, float x, float y, float w, string label, bool primary, Action onClick)
        {
            var b = primary
                ? LipCard.Create(parent, "Button", x, y, w, ButtonH, Palette.Accent, Palette.Accent, Palette.AccentLip, 16f, 5f, 0f)
                : LipCard.Create(parent, "Button", x, y, w, ButtonH, Palette.CardFill, Palette.CardEdge, Palette.CardLip, 16f, 5f, 2f);
            var t = Ui.Text("Label", b.Content, label, 16f, primary ? Palette.OnAccent : Palette.Text, FontWeight.ExtraBold, TextAlignmentOptions.Capline);
            Ui.Stretch(t.rectTransform, 8, 0, 8, 0);
            b.SetClickable(onClick);
            return b;
        }

        float Para(Transform parent, string text, float x, float y, float w, float size, Color color, FontWeight weight = FontWeight.Regular,
            TextAlignmentOptions align = TextAlignmentOptions.TopLeft)
        {
            var t = Ui.Text("Text", parent, ContentBlocks.Rich(text), size, color, weight, align);
            t.richText = true;
            t.lineSpacing = 4f;
            float h = Ui.TextHeight(t, w);
            Ui.Place(t.rectTransform, x, y, w, h);
            return h;
        }

        // ---------------- intro ----------------

        float Intro(RectTransform c, float y)
        {
            y += 6f;
            y += Para(c, "Oceń 6 przykładowych wiadomości i ofert. Która jest uczciwa, a która to pułapka oszustów?", m, y, cw, 17f, Palette.Text) + 18f;
            y += Hint(c, y, false, "Przesuń w <b>PRAWO</b>, jeśli oferta jest bezpieczna.") + 10f;
            y += Hint(c, y, true, "Przesuń w <b>LEWO</b>, jeśli widzisz czerwoną flagę!") + 16f;
            y += Para(c, "Możesz też użyć dużych przycisków pod kartą albo strzałek na klawiaturze. Nie ma limitu czasu ani przegranej – po każdej karcie dowiesz się, dlaczego.",
                m, y, cw, 15f, Palette.TextMuted);
            return y;
        }

        float Hint(RectTransform c, float y, bool trap, string text)
        {
            const float h = 62f;
            var card = LipCard.Create(c, "Hint", m, y, cw, h, trap ? TrapFill : SafeFill, trap ? Trap : Safe, trap ? TrapLip : SafeLip, 16f, 3f, 2f);
            Mark(card.Content, 14f, (h - 3f - 34f) * 0.5f, 34f, !trap);
            var t = Ui.Text("Text", card.Content, text, 15.5f, Palette.Text, FontWeight.SemiBold, TextAlignmentOptions.MidlineLeft);
            t.richText = true;
            Ui.Place(t.rectTransform, 60f, 0f, cw - 74f, h - 3f);
            return h;
        }

        // Znak decyzji w kółku: ✓ (ikona) na zielonym albo × na czerwonym – zawsze z kształtem, nie tylko kolorem.
        static void Mark(Transform parent, float x, float y, float size, bool safe)
        {
            var bg = Ui.Rounded("Mark", parent, safe ? Safe : Trap, size * 0.5f);
            Ui.Place(bg.rectTransform, x, y, size, size);
            if (safe)
            {
                var ic = Ui.Icon("Check", bg.transform, IconId.Check, Color.white, size * 0.6f);
                Ui.Place(ic.rectTransform, size * 0.2f, size * 0.2f, size * 0.6f, size * 0.6f);
            }
            else
            {
                var t = Ui.Text("X", bg.transform, "×", size * 0.8f, Color.white, FontWeight.ExtraBold, TextAlignmentOptions.Center);
                Ui.Stretch(t.rectTransform, 0, -size * 0.06f, 0, size * 0.06f);
            }
        }

        void Start()
        {
            seed = Environment.TickCount;
            round = new ScamRound(source, shuffle, seed);
            completed = false;
            stage = Stage.Play;
            Haptics.Tap();
            Rebuild(true);
        }

        // ---------------- gra ----------------

        float Play(RectTransform c, float y, bool appear)
        {
            var card = round.Current;
            // Pasek postępu: „Karta 2 z 6” i licznik trafień.
            var k = Ui.Text("Kicker", c, "KARTA " + (round.Index + 1) + " Z " + round.Count, 11.5f, Palette.TextMuted);
            k.characterSpacing = 6f;
            Ui.Place(k.rectTransform, m, y, cw * 0.6f, 16f);
            var s = Ui.Text("Score", c, "Trafienia: " + round.Score, 13f, Palette.Text, FontWeight.ExtraBold, TextAlignmentOptions.TopRight);
            Ui.Place(s.rectTransform, m + cw * 0.4f, y - 1f, cw * 0.6f, 18f);
            y += 22f;
            ProgressBar.Create(c, m, y, cw, 12f, Palette.Track, (round.Index + (round.Answered ? 1f : 0f)) / round.Count);
            y += 12f + 18f;

            y += OfferCard(c, y, card, appear) + 18f;

            if (!round.Answered)
            {
                // Dwa duże przyciski: czerwony PUŁAPKA po lewej, zielony BEZPIECZNE po prawej (jak kierunek przeciągania).
                float half = (cw - 12f) * 0.5f;
                DecideButton(c, m, y, half, true);
                DecideButton(c, m + half + 12f, y, half, false);
                y += DecideH + 6f;
            }
            return y;
        }

        void DecideButton(RectTransform c, float x, float y, float w, bool trap)
        {
            var b = LipCard.Create(c, trap ? "TrapButton" : "SafeButton", x, y, w, DecideH, trap ? Trap : Safe, trap ? Trap : Safe, trap ? TrapLip : SafeLip, 16f, 5f, 0f);
            float iconS = 26f;
            string label = trap ? "PUŁAPKA" : "BEZPIECZNE";
            var t = Ui.Text("Label", b.Content, label, 15.5f, Color.white, FontWeight.ExtraBold, TextAlignmentOptions.MidlineLeft);
            float tw = t.GetPreferredValues(label, 1000f, 0f).x;
            float total = iconS + 8f + tw, x0 = (w - total) * 0.5f;
            Mark(b.Content, x0, (DecideH - 5f - iconS) * 0.5f, iconS, !trap);
            // Ikona w białym kółku byłaby niewidoczna na kolorze – dajemy jej obwódkę przez jaśniejsze tło.
            b.Content.Find("Mark").GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.22f);
            Ui.Place(t.rectTransform, x0 + iconS + 8f, 0f, tw + 4f, DecideH - 5f);
            b.SetClickable(() => Decide(trap ? ScamAnswer.Trap : ScamAnswer.Safe));
        }

        // Karta oferty: kolorowy pasek z rodzajem wiadomości, nadawca, treść. Leży w „uchwycie” obracanym przy przeciąganiu.
        float OfferCard(RectTransform c, float y, ScamCard card, bool appear)
        {
            const float pad = 18f, strip = 44f;
            string label; IconId icon; Color col;
            Look(card.Type, out label, out icon, out col);

            bool bubble = card.Type == "sms";
            string body = card.Type == "call" ? "„" + card.Text + "”" : card.Text;
            float tw = cw - 2 * pad - (bubble ? 24f : 0f);
            var probe = Ui.Text("Probe", c, body, 17f, Palette.Text, FontWeight.SemiBold);
            float th = Ui.TextHeight(probe, tw);
            UnityEngine.Object.DestroyImmediate(probe.gameObject);
            float fromH = string.IsNullOrEmpty(card.From) ? 0f : 24f;
            float h = Mathf.Max(230f, strip + 16f + fromH + th + (bubble ? 24f : 0f) + pad);

            holder = Ui.Rect("CardHolder", c);
            Ui.Place(holder, m, y, cw, h + ScreenKit.Lip);
            // Przezroczyste tło uchwytu łapie przeciąganie (napisy na karcie nie są „klikalne”).
            var hit = holder.gameObject.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);
            holder.pivot = new Vector2(0.5f, 0.5f);
            holder.anchoredPosition += new Vector2(cw * 0.5f, -(h + ScreenKit.Lip) * 0.5f);
            holderBase = holder.anchoredPosition;

            var lc = LipCard.Create(holder, "Offer", 0f, 0f, cw, h + ScreenKit.Lip, Palette.CardFill, Palette.CardEdge, Palette.CardLip, ScreenKit.CardRadius, ScreenKit.Lip, 2f);
            var top = Ui.Rounded("Strip", lc.Content, col, 14f);
            Ui.Place(top.rectTransform, 8f, 8f, cw - 16f, strip - 8f);
            var ic = Ui.Icon("Icon", top.transform, icon, Color.white, 20f);
            Ui.Place(ic.rectTransform, 12f, (strip - 8f - 20f) * 0.5f, 20f, 20f);
            var lt = Ui.Text("Type", top.transform, label.ToUpperInvariant(), 12f, Color.white, FontWeight.ExtraBold, TextAlignmentOptions.MidlineLeft);
            lt.characterSpacing = 4f;
            Ui.Place(lt.rectTransform, 40f, 0f, cw - 70f, strip - 8f);

            float cy = strip + 14f;
            if (fromH > 0f)
            {
                var f = Ui.Text("From", lc.Content, card.From, 14f, Palette.TextMuted, FontWeight.ExtraBold);
                f.overflowMode = TextOverflowModes.Ellipsis;
                f.textWrappingMode = TextWrappingModes.NoWrap;
                Ui.Place(f.rectTransform, pad, cy, cw - 2 * pad, 20f);
                cy += fromH;
            }
            if (bubble)
            {
                // SMS jako dymek wiadomości (link jest zwykłym tekstem – nic tu nie jest klikalne).
                var bub = Ui.Rounded("Bubble", lc.Content, Bubble, 16f);
                Ui.Place(bub.rectTransform, pad, cy, tw + 24f, th + 24f);
                var bt = Ui.Text("Text", bub.transform, body, 17f, Palette.Text, FontWeight.SemiBold);
                Ui.Place(bt.rectTransform, 12f, 12f, tw, th);
            }
            else
            {
                var bt = Ui.Text("Text", lc.Content, body, 17f, Palette.Text, FontWeight.SemiBold);
                Ui.Place(bt.rectTransform, pad, cy, tw, th);
            }

            // Pieczątki decyzji – pojawiają się przy przeciąganiu (i zostają po decyzji).
            stampTrap = Stamp(lc.Content, true);
            stampSafe = Stamp(lc.Content, false);

            if (round.Answered)
            {
                // Po decyzji karta lekko odchylona w stronę wyboru, z widoczną pieczątką.
                Pose(decidedDir * 26f);
                (decidedDir < 0 ? stampTrap : stampSafe).alpha = 1f;
            }
            else
            {
                var drag = holder.gameObject.AddComponent<ScamDrag>();
                drag.OnMove = x => { Pose(x); ShowStamps(x); };
                drag.OnRelease = Release;
                if (appear)
                {
                    // Nowa karta „wjeżdża” z lekkiego pomniejszenia.
                    var g = holder.gameObject.AddComponent<CanvasGroup>();
                    var hr = holder;
                    Fx().Add(0.02f, 0.32f, t =>
                    {
                        float e = QuizFx.BackOut(t);
                        float k = 0.9f + 0.1f * e;
                        hr.localScale = new Vector3(k, k, 1f);
                        g.alpha = Mathf.Clamp01(t * 2.5f);
                    }, null);
                }
            }
            return h + ScreenKit.Lip;
        }

        static void Look(string type, out string label, out IconId icon, out Color col)
        {
            switch (type)
            {
                case "social": label = "Reklama w social mediach"; icon = IconId.Spark; col = Palette.Hex("6B4A6E"); break;
                case "bank": label = "Aplikacja bankowa"; icon = IconId.Wallet; col = Palette.Petrol; break;
                case "article": label = "Artykuł sponsorowany"; icon = IconId.Book; col = Palette.Mustard; break;
                case "product": label = "Karta produktu"; icon = IconId.Chart; col = Palette.Moss; break;
                case "call": label = "Połączenie telefoniczne"; icon = IconId.Phone; col = Palette.Brick; break;
                default: label = "SMS"; icon = IconId.Phone; col = Palette.Slate; break;
            }
        }

        CanvasGroup Stamp(Transform parent, bool trap)
        {
            string text = trap ? "PUŁAPKA" : "BEZPIECZNE";
            var pill = Ui.Rounded(trap ? "StampTrap" : "StampSafe", parent, trap ? Trap : Safe, 10f);
            var t = Ui.Text("Label", pill.transform, text, 17f, Color.white, FontWeight.ExtraBold, TextAlignmentOptions.Center);
            t.characterSpacing = 4f;
            float w = t.GetPreferredValues(text, 1000f, 0f).x + 28f;
            // Pułapka w prawym górnym rogu (karta jedzie w lewo), bezpieczne w lewym – pieczątka „wychodzi” zza krawędzi.
            Ui.Place(pill.rectTransform, trap ? cw - w - 18f : 18f, 58f, w, 36f);
            Ui.Stretch(t.rectTransform);
            pill.rectTransform.localRotation = Quaternion.Euler(0f, 0f, trap ? 10f : -10f);
            var g = pill.gameObject.AddComponent<CanvasGroup>();
            g.alpha = 0f;
            g.blocksRaycasts = false;
            return g;
        }

        void Pose(float x)
        {
            if (holder == null) return;
            holder.anchoredPosition = holderBase + new Vector2(x, -Mathf.Abs(x) * 0.06f);
            holder.localRotation = Quaternion.Euler(0f, 0f, -x * 0.05f);
        }

        void ShowStamps(float x)
        {
            if (stampTrap != null) stampTrap.alpha = Mathf.Clamp01(-x / DragLimit);
            if (stampSafe != null) stampSafe.alpha = Mathf.Clamp01(x / DragLimit);
        }

        void Release(float x)
        {
            if (Mathf.Abs(x) >= DragLimit) { Decide(x < 0f ? ScamAnswer.Trap : ScamAnswer.Safe); return; }
            // Za krótko – karta wraca na środek.
            Fx().Add(0f, 0.22f, t =>
            {
                float px = x * (1f - QuizFx.EaseOut(t));
                Pose(px); ShowStamps(px);
            }, null);
        }

        void Decide(ScamAnswer a)
        {
            if (busy || round == null || round.Finished || round.Answered || stage != Stage.Play) return;
            bool ok = round.Answer(a);
            decidedDir = a == ScamAnswer.Trap ? -1 : 1;
            Haptics.Tap();
            Sfx.Play(ok ? Sfx.Id.Correct : Sfx.Id.Wrong);
            Rebuild(false);
            Flash(ok ? Safe : Oops);
            // Karta wyjaśnienia wysuwa się od dołu.
            var bar = screen.Find("BottomBar") as RectTransform;
            if (bar != null)
            {
                float h = bar.sizeDelta.y;
                bar.anchoredPosition = new Vector2(0f, -h);
                Fx().Add(0.02f, 0.34f, t => bar.anchoredPosition = new Vector2(0f, -h * (1f - QuizFx.BackOut(t))), null);
            }
        }

        // Delikatny błysk całego ekranu (zielony = dobrze, pomarańczowy = błąd) – bez dźwięku i bez migania.
        void Flash(Color col)
        {
            var img = Ui.Rect("Flash", screen).gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            Ui.Stretch(img.rectTransform);
            Fx().Add(0f, 0.55f, t => img.color = new Color(col.r, col.g, col.b, 0.22f * (1f - t)),
                () => { if (img != null) UnityEngine.Object.Destroy(img.gameObject); });
        }

        // Dolny pasek po decyzji: wynik (ikona + tekst), 1–2 zdania wyjaśnienia i DALEJ.
        float FeedbackBar(RectTransform bar)
        {
            var card = round.Current;
            bool ok = round.LastCorrect;
            bool trap = card.Answer == ScamAnswer.Trap;
            string head = ok ? (trap ? "Dobrze! To pułapka." : "Dobrze! To bezpieczne.")
                             : (trap ? "Uwaga – to była pułapka" : "To akurat było bezpieczne");
            const float pad = 16f;
            float tw = cw - 2 * pad;

            var probe = Ui.Text("Probe", bar, card.Explain, 15f, Palette.Text);
            float eh = string.IsNullOrEmpty(card.Explain) ? 0f : Ui.TextHeight(probe, tw);
            UnityEngine.Object.DestroyImmediate(probe.gameObject);
            float ph = pad + 30f + (eh > 0f ? 8f + eh : 0f) + pad;

            var panel = LipCard.Create(bar, "Feedback", m, 8f, cw, ph + 4f, ok ? SafeFill : TrapFill, ok ? Safe : Oops, ok ? SafeLip : Palette.Hex("B06A1E"), 18f, 4f, 2f);
            Mark(panel.Content, pad, pad, 30f, ok);
            var ht = Ui.Text("Head", panel.Content, head, 18f, ok ? SafeLip : Palette.Hex("8C4A12"), FontWeight.ExtraBold, TextAlignmentOptions.MidlineLeft);
            Ui.Place(ht.rectTransform, pad + 40f, pad, tw - 40f, 30f);
            if (eh > 0f) Para(panel.Content, card.Explain, pad, pad + 38f, tw, 15f, Palette.Text);

            float y = 8f + ph + 4f + 10f;
            bool last = round.Index == round.Count - 1;
            Button(bar, m, y, cw, last ? "ZOBACZ WYNIK" : "DALEJ", true, Next);
            y += ButtonH + 10f;
            return y + Disclaim(bar, y) + 10f;
        }

        void Next()
        {
            if (busy || round == null || !round.Answered) return;
            busy = true;
            Haptics.Tap();
            // Oceniona karta odlatuje w stronę decyzji, potem następna.
            var hr = holder;
            float from = decidedDir * 26f;
            Fx().Add(0f, 0.24f, t =>
            {
                if (hr == null) return;
                float e = t * t;
                Pose(Mathf.Lerp(from, decidedDir * W, e));
            }, () =>
            {
                busy = false;
                round.Next();
                if (round.Finished) stage = Stage.Summary;
                Rebuild(true);
            });
        }

        // ---------------- podsumowanie ----------------

        float Summary(RectTransform c, float y)
        {
            int stars = round.Stars();
            bool first = !completed;
            // Na końcu gry jest odznaka – dlatego zawsze dźwięk „osiągnięcia”, nie zwykły koniec levelu.
            if (first) { completed = true; if (onComplete != null) onComplete(stars); Sfx.Jingle(Sfx.Id.Achievement, 0.4f); }

            // Gwiazdki jak po teście – zdobyte wskakują po kolei (tylko przy pierwszym pokazaniu).
            const float star = 54f, sgap = 14f;
            float sx = (W - 3 * star - 2 * sgap) * 0.5f, sy = y + 18f;
            for (int i = 0; i < 3; i++)
            {
                bool on = i < stars;
                float lift = i == 1 ? 0f : 12f;
                if (on)
                {
                    var slot = Ui.Icon("StarSlot", c, IconId.Star, Palette.LockedFill, star);
                    Ui.Place(slot.rectTransform, sx + i * (star + sgap), sy + lift, star, star);
                }
                var s = Ui.Icon("Star", c, IconId.Star, on ? Palette.Accent : Palette.LockedFill, star);
                Ui.Place(s.rectTransform, sx + i * (star + sgap), sy + lift, star, star);
                if (on && first) PopStar(s.rectTransform, star, i);
            }
            y = sy + star + 12f + 22f;

            var score = Ui.Text("Result", c, round.Score + "/" + round.Count, 30f, Palette.Text, FontWeight.ExtraBold, TextAlignmentOptions.Top);
            Ui.Place(score.rectTransform, m, y, cw, 38f);
            y += 40f;
            y += Para(c, ScamRound.Verdict(round.Score, round.Count), m, y, cw, 17f, Palette.Text, FontWeight.SemiBold, TextAlignmentOptions.Top) + 20f;

            // Odznaka za ukończenie.
            const float bh = 64f, icon = 38f;
            var badge = LipCard.Create(c, "Badge", m, y, cw, bh, Palette.Brand, Palette.Brand, Palette.Darker(Palette.Brand, 0.7f), 16f, 4f, 0f);
            var bi = Ui.Icon("Star", badge.Content, IconId.Star, Palette.Accent, icon);
            Ui.Place(bi.rectTransform, 14f, (bh - 4f - icon) * 0.5f, icon, icon);
            var bt = Ui.Text("Title", badge.Content, "Odznaka: " + ScamDeck.BadgeName, 16f, Color.white, FontWeight.ExtraBold);
            Ui.Place(bt.rectTransform, 64f, 11f, cw - 76f, 22f);
            var bd = Ui.Text("Desc", badge.Content, "Za przejście wszystkich kart", 12.5f, Palette.WhiteSoft, FontWeight.SemiBold);
            Ui.Place(bd.rectTransform, 64f, 33f, cw - 76f, 18f);
            y += bh + 20f;

            // Gdzie był błąd – z krótkim powtórzeniem wyjaśnienia.
            var wrong = round.Mistakes();
            if (wrong.Count > 0)
            {
                y += Para(c, "Do powtórki", m, y, cw, 17f, Palette.Text, FontWeight.ExtraBold) + 8f;
                foreach (var w in wrong) y += Mistake(c, y, w) + 10f;
                y += 8f;
            }

            // Ściągawka czerwonych flag.
            y += Para(c, "Ściągawka – czerwone flagi", m, y, cw, 17f, Palette.Text, FontWeight.ExtraBold) + 8f;
            foreach (var f in ScamDeck.CheatSheet)
            {
                var dot = Ui.Rounded("Dot", c, Trap, 4f);
                Ui.Place(dot.rectTransform, m + 4f, y + 8f, 8f, 8f);
                y += Para(c, f, m + 22f, y, cw - 22f, 15.5f, Palette.Text) + 8f;
            }
            y += 12f;

            // Most do działania.
            y += Para(c, "Teraz, gdy wiesz, jak unikać pułapek, sprawdź bezpieczne sposoby na pomnażanie oszczędności.", m, y, cw, 15.5f, Palette.TextMuted);
            return y;
        }

        float Mistake(RectTransform c, float y, ScamCard card)
        {
            const float pad = 14f;
            string label; IconId icon; Color col;
            Look(card.Type, out label, out icon, out col);
            string head = label + " – " + (card.Answer == ScamAnswer.Trap ? "pułapka" : "bezpieczne");
            var probe = Ui.Text("Probe", c, card.Explain, 14f, Palette.Text);
            float eh = Ui.TextHeight(probe, cw - 2 * pad);
            UnityEngine.Object.DestroyImmediate(probe.gameObject);
            float h = pad + 20f + 6f + eh + pad;
            var lc = LipCard.Create(c, "Mistake", m, y, cw, h + 3f, Palette.CardFill, Palette.CardEdge, Palette.CardLip, 14f, 3f, 2f);
            var ht = Ui.Text("Head", lc.Content, head, 14.5f, col, FontWeight.ExtraBold);
            Ui.Place(ht.rectTransform, pad, pad, cw - 2 * pad, 20f);
            Para(lc.Content, card.Explain, pad, pad + 26f, cw - 2 * pad, 14f, Palette.Text);
            return h + 3f;
        }

        float SummaryBar(RectTransform bar)
        {
            float y = 8f;
            Button(bar, m, y, cw, "ZOBACZ BEZPIECZNE OSZCZĘDZANIE", true, () => { if (onFinish != null) onFinish(); });
            y += ButtonH + 10f;
            Button(bar, m, y, cw, "ZAGRAJ PONOWNIE", false, Start);
            y += ButtonH + 10f;
            return y + Disclaim(bar, y) + 10f;
        }

        void PopStar(RectTransform rt, float size, int i)
        {
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition += new Vector2(size * 0.5f, -size * 0.5f);
            rt.localScale = Vector3.zero;
            float delay = 0.35f + i * 0.28f;
            Fx().Add(delay, 0.01f, t => { }, Haptics.Tap);
            Fx().Add(delay, 0.45f, t =>
            {
                float k = QuizFx.BackOut(t, 2.6f);
                rt.localScale = new Vector3(k, k, 1f);
                rt.localRotation = Quaternion.Euler(0f, 0f, -25f * (1f - t));
            }, null);
        }

        // ---------------- klawiatura ----------------

        // Strzałki: ← pułapka, → bezpieczne; Enter / spacja: Dalej (albo start na ekranie powitalnym).
        void OnKeys(bool left, bool right, bool enter)
        {
            if (stage == Stage.Intro) { if (enter) Start(); return; }
            if (stage != Stage.Play || round == null) return;
            if (!round.Answered)
            {
                if (left) Decide(ScamAnswer.Trap);
                else if (right) Decide(ScamAnswer.Safe);
            }
            else if (enter) Next();
        }

        class Keys : MonoBehaviour
        {
            public ScamGame Game;
            void Update()
            {
                bool l, r, e;
#if ENABLE_INPUT_SYSTEM
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb == null) return;
                l = kb.leftArrowKey.wasPressedThisFrame;
                r = kb.rightArrowKey.wasPressedThisFrame;
                e = kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame;
#else
                l = Input.GetKeyDown(KeyCode.LeftArrow);
                r = Input.GetKeyDown(KeyCode.RightArrow);
                e = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space);
#endif
                if (l || r || e) Game.OnKeys(l, r, e);
            }
        }
    }

    // Przeciąganie karty palcem / myszą: ruch w poziomie, przy puszczeniu decyzja (albo powrót, gdy za krótko).
    // Przechwytuje przeciąganie, więc lista pod spodem się wtedy nie przewija, a gest nie jest swipem między ekranami.
    public class ScamDrag : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Action<float> OnMove, OnRelease;
        float x, scale = 1f;

        public void OnInitializePotentialDrag(PointerEventData e) { e.useDragThreshold = true; }

        public void OnBeginDrag(PointerEventData e)
        {
            var canvas = GetComponentInParent<Canvas>();
            scale = canvas != null ? Mathf.Max(0.01f, canvas.scaleFactor) : 1f;
            x = 0f;
            SwipeInput.Suppress = true;
        }

        public void OnDrag(PointerEventData e)
        {
            x += e.delta.x / scale;
            if (OnMove != null) OnMove(x);
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (OnRelease != null) OnRelease(x);
            x = 0f;
        }
    }
}

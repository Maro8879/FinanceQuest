using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FinanceQuest
{
    // Mini gra „Swipe the Scam” – ekran. Logika (punkty, kolejność, gwiazdki) jest w ScamRound (assembly Sim, z testami).
    // Karta jak „Tinder dla ofert”: przeciągnij w LEWO = SCAM, w PRAWO = SAFE. Do tego dwa duże przyciski
    // i klawisze strzałek (Enter = Next). Po decyzji: błysk koloru, czerwone flagi w treści, wyjaśnienie od dołu.
    // Karta = ilustracja typu u góry + treść wyglądająca jak w telefonie; zza karty zerka Detektyw Moneta (ScamArt.cs).
    // Id zostaje stare („prawda-czy-pulapka”), bo jest zapisane w plikach treści i w postępach graczy.
    public class ScamGame
    {
        public const string Id = "prawda-czy-pulapka";

        const float ButtonH = 58f, DecideH = 64f, DragLimit = 110f;
        const string Disclaimer = "Educational simulation, not investment advice. All cards are fictional examples.";
        const float HeadH = 96f, PhotoMinH = 130f;

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
        bool reactPending;             // metki flag wskakują raz – zaraz po decyzji, nie przy każdej przebudowie

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
                        Marks = c.flags ?? new List<string>(),   // z edytora: podświetlamy flagi, które dosłownie są w treści
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
                case Stage.Intro: y = Intro(content, y); barH = Bar(bar, "START SWIPING", Start); break;
                case Stage.Play: y = Play(content, y, appear); barH = round.Answered ? FeedbackBar(bar) : DecideBar(bar); break;
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

        // Strona startowa: panel z trzema kartami (środkowa duża, boczne mniejsze, lekko schowane pod nią), krótki haczyk
        // i dwie metki z kierunkami tuż nad START SWIPING – panel rośnie tak, żeby wypełnić ekran.
        // Animacje spokojne: karty wjeżdżają od dołu, środkowa kołysze się, a pieczątka SCAM / SAFE pojawia się
        // dopiero przy przechyleniu w daną stronę – tak jak w samej grze.
        float Intro(RectTransform c, float y)
        {
            // Najpierw teksty pod panelem (mierzymy je), potem panel dostaje całą resztę wysokości.
            var title = Ui.Text("Title", c, "Would you fall\n<color=#B8432D>for a scam?</color>", 28f, Palette.Text, FontWeight.ExtraBold);
            title.richText = true;
            title.lineSpacing = -4f;
            float th = Ui.TextHeight(title, cw);
            const string lead = "Real-looking texts, ads and calls. Spot the red flags before they spot you.";
            var probe = Ui.Text("Probe", c, lead, 15.5f, Palette.TextMuted, FontWeight.Regular);
            probe.lineSpacing = 4f;
            float sh = Ui.TextHeight(probe, cw);
            UnityEngine.Object.DestroyImmediate(probe.gameObject);
            const float pillH = 40f, noteH = 18f;
            float below = 20f + th + 8f + sh + 16f + pillH + 10f + noteH + 6f;
            float avail = app.ScreenHeight - AppHeader.Height - BarHeight(ButtonH) - y - 4f;
            float ph = Mathf.Max(240f, avail - below);

            var hero = Ui.Rounded("Hero", c, Palette.Petrol, 24f);
            Ui.Place(hero.rectTransform, m, y, cw, ph);
            hero.transform.SetAsFirstSibling();
            // Środkowa karta tak duża, jak pozwala panel; boczne o 20 % mniejsze i wsunięte pod nią.
            const float top = 22f, capH = 44f, tuck = 22f;
            float mh = ph - top - capH, mw = mh / 1.42f;
            float sw = mw * 0.8f, shh = mh * 0.8f;
            float need = mw + 2 * sw - 2 * tuck + 20f;
            if (need > cw) { float k = cw / need; mw *= k; mh *= k; sw *= k; shh *= k; }
            float mid = cw * 0.5f, sy = top + (mh - shh) * 0.5f;
            CanvasGroup scam, safe;
            MiniCard(hero.transform, "photo_sms", mid - mw * 0.5f - sw + tuck, sy, sw, shh, 6f, 0.10f, out scam, out safe);
            MiniCard(hero.transform, "photo_bank", mid + mw * 0.5f - tuck, sy, sw, shh, -6f, 0.22f, out scam, out safe);
            var center = MiniCard(hero.transform, "photo_social", mid - mw * 0.5f, top, mw, mh, 0f, 0.34f, out scam, out safe);
            var sway = center.gameObject.AddComponent<ScamSway>();
            sway.Delay = 1.6f;
            sway.Amp = 10f;
            sway.Left = scam;
            sway.Right = safe;
            var cap = Ui.Text("Caption", hero.transform, "6 cards · 1 swipe each", 13.5f, Palette.Hex("BFD3D8"), FontWeight.ExtraBold, TextAlignmentOptions.Center);
            Ui.Place(cap.rectTransform, 0f, ph - 30f, cw, 20f);
            FadeUp(cap.rectTransform, 0.8f);
            y += ph + 20f;

            Ui.Place(title.rectTransform, m, y, cw, th);
            FadeUp(title.rectTransform, 0.55f);
            y += th + 8f;
            Para(c, lead, m, y, cw, 15.5f, Palette.TextMuted);
            FadeUp((RectTransform)c.GetChild(c.childCount - 1), 0.75f);
            y += sh + 16f;

            float half = (cw - 8f) * 0.5f;
            Pill(c, m, y, half, "← SCAM", Trap, TrapFill, 0.95f);
            Pill(c, m + half + 8f, y, half, "SAFE →", Safe, SafeFill, 1.05f);
            y += pillH + 10f;
            var note = Ui.Text("Note", c, "No time limit · learn why after every card", 12f, Palette.TextMuted, FontWeight.SemiBold, TextAlignmentOptions.Center);
            Ui.Place(note.rectTransform, m, y, cw, noteH);
            return y + noteH;
        }

        // Mała karta na stronie startowej: zdjęcie u góry, dwie „linijki tekstu” i dwie ukryte pieczątki
        // (pokazuje je kołysanie – ScamSway – zależnie od strony przechylenia).
        RectTransform MiniCard(Transform parent, string photo, float x, float y, float w, float h, float rot, float delay,
            out CanvasGroup scamStamp, out CanvasGroup safeStamp)
        {
            var lc = LipCard.Create(parent, "Mini", x, y, w, h, Palette.CardFill, Palette.CardEdge, Palette.CardLip, 14f, 4f, 2f);
            ScamArt.Photo("Photo", lc.Content, ScamArt.Get(photo), 6f, 6f, w - 12f, h * 0.6f, 10f);
            var l1 = Ui.Rounded("Line", lc.Content, Palette.Hex("D8D2C6"), 4f);
            Ui.Place(l1.rectTransform, 12f, h * 0.6f + 16f, w * 0.6f, 8f);
            var l2 = Ui.Rounded("Line", lc.Content, Palette.Hex("E4DFD5"), 4f);
            Ui.Place(l2.rectTransform, 12f, h * 0.6f + 30f, w * 0.75f, 8f);
            scamStamp = MiniStamp(lc.Content, w, h, true);
            safeStamp = MiniStamp(lc.Content, w, h, false);
            var rt = (RectTransform)lc.transform;
            ScamArt.SetPivot(rt, new Vector2(0.5f, 0.5f));
            rt.localRotation = Quaternion.Euler(0f, 0f, rot);
            // Wjazd od dołu, spokojnie (bez podskoku).
            var end = rt.anchoredPosition;
            var g = rt.gameObject.AddComponent<CanvasGroup>();
            g.alpha = 0f;
            rt.anchoredPosition = end - new Vector2(0f, 160f);
            Fx().Add(delay, 0.5f, p =>
            {
                float k = QuizFx.EaseOut(p);
                rt.anchoredPosition = end - new Vector2(0f, 160f * (1f - k));
                g.alpha = Mathf.Clamp01(p * 2f);
            }, null);
            return rt;
        }

        CanvasGroup MiniStamp(Transform parent, float w, float h, bool trap)
        {
            string label = trap ? "SCAM" : "SAFE";
            float sw = Mathf.Min(74f, w - 10f);
            var st = Ui.Rounded("Stamp", parent, trap ? Trap : Safe, 8f);
            var stt = Ui.Text("Label", st.transform, label, 13f, Color.white, FontWeight.ExtraBold, TextAlignmentOptions.Center);
            stt.characterSpacing = 2f;
            Ui.Stretch(stt.rectTransform);
            Ui.Place(st.rectTransform, (w - sw) * 0.5f, h * 0.3f - 13f, sw, 26f);
            ScamArt.SetPivot(st.rectTransform, new Vector2(0.5f, 0.5f));
            st.rectTransform.localRotation = Quaternion.Euler(0f, 0f, trap ? 12f : -12f);
            var g = st.gameObject.AddComponent<CanvasGroup>();
            g.alpha = 0f;
            g.blocksRaycasts = false;
            return g;
        }

        void Pill(RectTransform c, float x, float y, float w, string text, Color col, Color fill, float delay)
        {
            var p = Ui.Rounded("Pill", c, fill, 20f);
            Ui.Place(p.rectTransform, x, y, w, 40f);
            var t = Ui.Text("Label", p.transform, text, 14f, col, FontWeight.ExtraBold, TextAlignmentOptions.Center);
            t.characterSpacing = 2f;
            Ui.Stretch(t.rectTransform);
            FadeUp(p.rectTransform, delay);
        }

        // Napis „wyłania się” z lekkiego przesunięcia w dół.
        void FadeUp(RectTransform rt, float delay)
        {
            var g = rt.gameObject.GetComponent<CanvasGroup>();
            if (g == null) g = rt.gameObject.AddComponent<CanvasGroup>();
            g.alpha = 0f;
            var end = rt.anchoredPosition;
            rt.anchoredPosition = end - new Vector2(0f, 14f);
            Fx().Add(delay, 0.35f, p =>
            {
                float k = QuizFx.EaseOut(p);
                rt.anchoredPosition = end - new Vector2(0f, 14f * (1f - k));
                g.alpha = k;
            }, null);
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
            Sfx.Play(Sfx.Id.Whoosh);
            Rebuild(true);
        }

        // ---------------- gra ----------------

        float Play(RectTransform c, float y, bool appear)
        {
            var card = round.Current;
            // W trakcie gry nad kartą nic – bez wyniku i lupy; wynik dopiero w podsumowaniu.
            // Karta wypełnia ekran aż do przycisków SCAM / SAFE (rośnie zdjęcie). Liczymy z wysokości paska z przyciskami,
            // także po decyzji – dzięki temu karta nie zmienia rozmiaru, gdy pojawia się wyjaśnienie.
            float fill = app.ScreenHeight - AppHeader.Height - DecideBarHeight() - y - 14f;
            y += OfferCard(c, y, card, appear, fill) + 18f;
            reactPending = false;
            return y;
        }

        float DecideBarHeight() { return BarHeight(DecideH); }

        // Wysokość dolnego paska z jednym rzędem przycisków o danej wysokości + informacją o symulacji.
        float BarHeight(float buttonH)
        {
            var probe = Ui.Text("Probe", screen, Disclaimer, 10.5f, Palette.TextMuted, FontWeight.SemiBold, TextAlignmentOptions.Center);
            float h = Ui.TextHeight(probe, cw);
            UnityEngine.Object.DestroyImmediate(probe.gameObject);
            return 8f + buttonH + 10f + h + 10f;
        }

        // Dolny pasek w trakcie gry: przyciski SCAM / SAFE zawsze widoczne (nie przewijają się z kartą).
        float DecideBar(RectTransform bar)
        {
            float y = 8f, half = (cw - 12f) * 0.5f;
            DecideButton(bar, m, y, half, true);
            DecideButton(bar, m + half + 12f, y, half, false);
            y += DecideH + 10f;
            return y + Disclaim(bar, y) + 10f;
        }

        void DecideButton(RectTransform c, float x, float y, float w, bool trap)
        {
            var b = LipCard.Create(c, trap ? "TrapButton" : "SafeButton", x, y, w, DecideH, trap ? Trap : Safe, trap ? Trap : Safe, trap ? TrapLip : SafeLip, 16f, 5f, 0f);
            float iconS = 26f;
            string label = trap ? "SCAM" : "SAFE";
            var t = Ui.Text("Label", b.Content, label, 15.5f, Color.white, FontWeight.ExtraBold, TextAlignmentOptions.MidlineLeft);
            float tw = t.GetPreferredValues(label, 1000f, 0f).x;
            float total = iconS + 8f + tw, x0 = (w - total) * 0.5f;
            Mark(b.Content, x0, (DecideH - 5f - iconS) * 0.5f, iconS, !trap);
            // Ikona w białym kółku byłaby niewidoczna na kolorze – dajemy jej obwódkę przez jaśniejsze tło.
            b.Content.Find("Mark").GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.22f);
            Ui.Place(t.rectTransform, x0 + iconS + 8f, 0f, tw + 4f, DecideH - 5f);
            b.SetClickable(() => Decide(trap ? ScamAnswer.Trap : ScamAnswer.Safe));
        }

        // Karta oferty: u góry ilustracja typu (telefon, gazeta…), niżej treść wyglądająca jak w telefonie
        // (nadawca z awatarem, dymek SMS-a, link). Po decyzji: czerwone flagi podświetlone w treści + metki pod spodem.
        // Leży w „uchwycie” obracanym przy przeciąganiu.
        float OfferCard(RectTransform c, float y, ScamCard card, bool appear, float fill)
        {
            const float pad = 18f, inset = 8f, avatar = 38f;
            var look = Look(card.Type);
            bool answered = round.Answered;
            bool bubble = card.Type == "sms" || card.Type == "call";
            string name, sub;
            SplitFrom(card, look, out name, out sub);

            string body = Body(card, answered);
            float tw = cw - 2 * pad - (bubble ? 24f : 0f);
            var probe = Ui.Text("Probe", c, body, 16f, Palette.Text, FontWeight.Regular);
            probe.richText = true;
            float th = Ui.TextHeight(probe, tw);
            UnityEngine.Object.DestroyImmediate(probe.gameObject);

            // Zdjęcie typu karty (photo_*.jpg) zajmuje większość karty; bez zdjęcia – mała ilustracja jak wcześniej.
            var photo = ScamArt.Get("photo_" + look.Art);
            float rest = inset + 14f + avatar + 10f + th + (bubble ? 24f : 0f) + 12f + 4f + ScreenKit.Lip;
            float headH = photo != null ? Mathf.Clamp(fill - rest, PhotoMinH, (cw - 2 * inset) * 1.15f) : HeadH - inset;
            float ay = inset + headH + 14f;               // rząd z awatarem
            float ty = ay + avatar + 10f;                 // treść
            float bodyH = th + (bubble ? 24f : 0f);
            float fy = ty + bodyH + 12f;                  // stopka: pytanie albo metki flag
            var chips = answered ? ChipLabels(card) : null;
            float fh = answered ? ChipsHeight(c, chips, cw - 2 * pad) : 0f;
            float h = answered ? fy + fh + 16f : fy + 4f;

            holder = Ui.Rect("CardHolder", c);
            Ui.Place(holder, m, y, cw, h + ScreenKit.Lip);
            // Przezroczyste tło uchwytu łapie przeciąganie (napisy na karcie nie są „klikalne”).
            var hit = holder.gameObject.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);
            holder.pivot = new Vector2(0.5f, 0.5f);
            holder.anchoredPosition += new Vector2(cw * 0.5f, -(h + ScreenKit.Lip) * 0.5f);
            holderBase = holder.anchoredPosition;

            var lc = LipCard.Create(holder, "Offer", 0f, 0f, cw, h + ScreenKit.Lip, Palette.CardFill, Palette.CardEdge, Palette.CardLip, ScreenKit.CardRadius, ScreenKit.Lip, 2f);

            // Nagłówek: zdjęcie przycięte maską do zaokrąglonych rogów (wysokość z proporcji zdjęcia – bez rozciągania).
            // Bez zdjęcia – ilustracja na jasnym tle w kolorze typu.
            Image head;
            if (photo != null) head = ScamArt.Photo("Head", lc.Content, photo, inset, inset, cw - 2 * inset, headH, 14f);
            else
            {
                head = Ui.Rounded("Head", lc.Content, look.Tint, 14f);
                Ui.Place(head.rectTransform, inset, inset, cw - 2 * inset, headH);
                var ill = ScamArt.Picture("Illustration", head.transform, "card_" + look.Art);
                float iw = Mathf.Min(ScamArt.IllW, cw - 2 * inset - 8f), ih = iw * ScamArt.IllH / ScamArt.IllW;
                Ui.Place(ill.rectTransform, (cw - 2 * inset - iw) * 0.5f, (headH - ih) * 0.5f, iw, ih);
            }
            var chip = Ui.Rounded("Type", head.transform, look.Color, 11f);
            var ct = Ui.Text("Label", chip.transform, look.Short, 10.5f, Color.white, FontWeight.ExtraBold, TextAlignmentOptions.Center);
            ct.characterSpacing = 3f;
            float cwid = ct.GetPreferredValues(look.Short, 1000f, 0f).x + 20f;
            Ui.Place(chip.rectTransform, 10f, 10f, cwid, 22f);
            Ui.Stretch(ct.rectTransform);

            // Nadawca: kółko z literą (albo słuchawką przy telefonie), nazwa i dopisek.
            var av = Ui.Rounded("Avatar", lc.Content, look.Color, avatar * 0.5f);
            Ui.Place(av.rectTransform, pad, ay, avatar, avatar);
            if (card.Type == "call")
            {
                var ic = Ui.Icon("Icon", av.transform, IconId.Phone, Color.white, 20f);
                Ui.Place(ic.rectTransform, (avatar - 20f) * 0.5f, (avatar - 20f) * 0.5f, 20f, 20f);
            }
            else
            {
                var at = Ui.Text("Letter", av.transform, Initial(name), 17f, Color.white, FontWeight.ExtraBold, TextAlignmentOptions.Center);
                Ui.Stretch(at.rectTransform);
            }
            float nx = pad + avatar + 10f;
            var nt = Ui.Text("From", lc.Content, name, 15f, Palette.Text, FontWeight.ExtraBold);
            nt.overflowMode = TextOverflowModes.Ellipsis;
            nt.textWrappingMode = TextWrappingModes.NoWrap;
            Ui.Place(nt.rectTransform, nx, ay + 1f, cw - nx - pad, 20f);
            var st = Ui.Text("Sub", lc.Content, sub, 12f, Palette.TextMuted, FontWeight.SemiBold);
            st.overflowMode = TextOverflowModes.Ellipsis;
            st.textWrappingMode = TextWrappingModes.NoWrap;
            Ui.Place(st.rectTransform, nx, ay + 21f, cw - nx - pad, 16f);

            if (bubble)
            {
                // SMS i rozmowa jako dymek wiadomości (link jest zwykłym tekstem – nic tu nie jest klikalne).
                var bub = Ui.Rounded("Bubble", lc.Content, Bubble, 16f);
                Ui.Place(bub.rectTransform, pad, ty, tw + 24f, th + 24f);
                var bt = Ui.Text("Text", bub.transform, body, 16f, Palette.Text, FontWeight.Regular);
                bt.richText = true;
                Ui.Place(bt.rectTransform, 12f, 12f, tw, th);
            }
            else
            {
                var bt = Ui.Text("Text", lc.Content, body, 16f, Palette.Text, FontWeight.Regular);
                bt.richText = true;
                Ui.Place(bt.rectTransform, pad, ty, tw, th);
            }

            if (answered) Chips(lc.Content, chips, pad, fy, cw - 2 * pad, card.Answer == ScamAnswer.Trap, reactPending);

            // Pieczątki decyzji – pojawiają się przy przeciąganiu (i zostają po decyzji).
            stampTrap = Stamp(lc.Content, true);
            stampSafe = Stamp(lc.Content, false);

            if (answered)
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
                        float e = QuizFx.EaseOut(t);
                        float k = 0.96f + 0.04f * e;
                        hr.localScale = new Vector3(k, k, 1f);
                        g.alpha = Mathf.Clamp01(t * 2.5f);
                    }, null);
                }
            }
            return h + ScreenKit.Lip;
        }

        struct CardLook { public string Label, Short, Sub, Art; public Color Color, Tint; }

        static CardLook Look(string type)
        {
            switch (type)
            {
                case "social": return new CardLook { Label = "Social media ad", Short = "AD", Sub = "Sponsored", Art = "social", Color = Palette.Hex("6B4A6E"), Tint = Palette.Hex("E6DCE8") };
                case "bank": return new CardLook { Label = "Banking app", Short = "BANK", Sub = "Banking app", Art = "bank", Color = Palette.Petrol, Tint = Palette.Hex("D7E6E8") };
                case "article": return new CardLook { Label = "Sponsored article", Short = "ARTICLE", Sub = "Sponsored article", Art = "article", Color = Palette.Mustard, Tint = Palette.Hex("EFE6D2") };
                case "product": return new CardLook { Label = "Product sheet", Short = "PRODUCT", Sub = "Product sheet", Art = "product", Color = Palette.Moss, Tint = Palette.Hex("DDE8E1") };
                case "call": return new CardLook { Label = "Phone call", Short = "CALL", Sub = "Incoming call", Art = "call", Color = Palette.Brick, Tint = Palette.Hex("F1DED8") };
                default: return new CardLook { Label = "SMS", Short = "SMS", Sub = "Text message · now", Art = "sms", Color = Palette.Slate, Tint = Palette.Hex("DCE4E8") };
            }
        }

        // „InvestAI · Sponsored” → nazwa „InvestAI”, dopisek „Sponsored”. Bez kropki – dopisek z typu karty.
        static void SplitFrom(ScamCard card, CardLook look, out string name, out string sub)
        {
            string from = string.IsNullOrEmpty(card.From) ? look.Label : card.From;
            int i = from.IndexOf(" · ", StringComparison.Ordinal);
            if (i > 0) { name = from.Substring(0, i); sub = from.Substring(i + 3); }
            else { name = from; sub = look.Sub; }
        }

        static string Initial(string name)
        {
            foreach (char ch in name) if (char.IsLetterOrDigit(ch)) return char.ToUpperInvariant(ch).ToString();
            return "?";
        }

        static readonly System.Text.RegularExpressions.Regex Link = new System.Text.RegularExpressions.Regex(
            @"(https?://\S+|www\.\S+|\b[a-z0-9-]+\.(ly|com|net|org|io|pl|eu|info)/\S*)",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Treść z formatowaniem: przed decyzją link wygląda jak link (niebieski, podkreślony),
        // po decyzji czerwone flagi są podświetlone – gracz widzi, po czym rozpoznać oszustwo.
        static string Body(ScamCard card, bool answered)
        {
            string s = card.Type == "call" ? "“" + card.Text + "”" : card.Text;
            if (!answered) return Link.Replace(s, "<color=#2B6CB0><u>$0</u></color>");
            if (card.Answer != ScamAnswer.Trap || card.Marks == null) return s;
            foreach (var mark in card.Marks)
            {
                if (string.IsNullOrEmpty(mark)) continue;
                int i = s.IndexOf(mark, StringComparison.OrdinalIgnoreCase);
                if (i < 0) continue;
                s = s.Substring(0, i) + "<mark=#B8432D2E><color=#B8432D><b>" + s.Substring(i, mark.Length) + "</b></color></mark>" + s.Substring(i + mark.Length);
            }
            return s;
        }

        // Metki pod kartą po decyzji: nazwy czerwonych flag (oszustwo) albo jedna zielona „bez czerwonych flag”.
        static List<string> ChipLabels(ScamCard card)
        {
            if (card.Answer == ScamAnswer.Trap && card.Flags != null && card.Flags.Count > 0) return card.Flags;
            return new List<string> { card.Answer == ScamAnswer.Trap ? "Red flag" : "No red flags" };
        }

        const float ChipH = 26f, ChipGap = 8f, ChipIcon = 14f;

        static float ChipWidth(TextMeshProUGUI probe, string label)
        {
            return probe.GetPreferredValues(label, 1000f, 0f).x + ChipIcon + 26f;
        }

        // Wysokość stopki z metkami (metki zawijają się do kolejnego rzędu, gdy się nie mieszczą).
        float ChipsHeight(RectTransform c, List<string> labels, float w)
        {
            var probe = Ui.Text("Probe", c, "", 12.5f, Color.white, FontWeight.ExtraBold);
            float x = 0f, rows = 1f;
            foreach (var l in labels)
            {
                float cwid = Mathf.Min(w, ChipWidth(probe, l));
                if (x > 0f && x + cwid > w) { rows++; x = 0f; }
                x += cwid + ChipGap;
            }
            UnityEngine.Object.DestroyImmediate(probe.gameObject);
            return rows * ChipH + (rows - 1f) * ChipGap;
        }

        void Chips(Transform parent, List<string> labels, float x0, float y0, float w, bool trap, bool pop)
        {
            float x = 0f, y = 0f;
            for (int i = 0; i < labels.Count; i++)
            {
                var chip = Ui.Rounded("Flag", parent, trap ? Trap : Safe, ChipH * 0.5f);
                var t = Ui.Text("Label", chip.transform, labels[i], 12.5f, Color.white, FontWeight.ExtraBold, TextAlignmentOptions.MidlineLeft);
                float cwid = Mathf.Min(w, ChipWidth(t, labels[i]));
                if (x > 0f && x + cwid > w) { x = 0f; y += ChipH + ChipGap; }
                Ui.Place(chip.rectTransform, x0 + x, y0 + y, cwid, ChipH);
                if (trap)
                {
                    var ic = Ui.Icon("Icon", chip.transform, IconId.Exclaim, Color.white, ChipIcon);
                    Ui.Place(ic.rectTransform, 9f, (ChipH - ChipIcon) * 0.5f, ChipIcon, ChipIcon);
                }
                else
                {
                    var ic = Ui.Icon("Icon", chip.transform, IconId.Check, Color.white, ChipIcon);
                    Ui.Place(ic.rectTransform, 9f, (ChipH - ChipIcon) * 0.5f, ChipIcon, ChipIcon);
                }
                Ui.Place(t.rectTransform, 9f + ChipIcon + 5f, 0f, cwid - ChipIcon - 22f, ChipH);
                t.overflowMode = TextOverflowModes.Ellipsis;
                t.textWrappingMode = TextWrappingModes.NoWrap;
                x += cwid + ChipGap;

                if (pop)
                {
                    // Metki wskakują po kolei, zaraz po decyzji (skala od środka metki).
                    var rt = chip.rectTransform;
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition += new Vector2(cwid * 0.5f, -ChipH * 0.5f);
                    rt.localScale = Vector3.zero;
                    Fx().Add(0.25f + i * 0.15f, 0.3f, p =>
                    {
                        float k = QuizFx.EaseOut(p);
                        rt.localScale = new Vector3(k, k, 1f);
                    }, null);
                }
            }
        }

        CanvasGroup Stamp(Transform parent, bool trap)
        {
            string text = trap ? "SCAM" : "SAFE";
            var pill = Ui.Rounded(trap ? "StampTrap" : "StampSafe", parent, trap ? Trap : Safe, 10f);
            var t = Ui.Text("Label", pill.transform, text, 17f, Color.white, FontWeight.ExtraBold, TextAlignmentOptions.Center);
            t.characterSpacing = 4f;
            float w = t.GetPreferredValues(text, 1000f, 0f).x + 28f;
            // SCAM w prawym górnym rogu (karta jedzie w lewo), SAFE w lewym – na ilustracji, żeby nie zasłaniać treści.
            Ui.Place(pill.rectTransform, trap ? cw - w - 18f : 18f, 44f, w, 36f);
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
            reactPending = true;
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
            Fx().Add(0f, 0.55f, t => img.color = new Color(col.r, col.g, col.b, 0.12f * (1f - t)),
                () => { if (img != null) UnityEngine.Object.Destroy(img.gameObject); });
        }

        // Dolny pasek po decyzji: wynik (ikona + tekst), 1–2 zdania wyjaśnienia i DALEJ.
        float FeedbackBar(RectTransform bar)
        {
            var card = round.Current;
            bool ok = round.LastCorrect;
            bool trap = card.Answer == ScamAnswer.Trap;
            string head = ok ? (trap ? "Correct! It's a scam." : "Correct! It's safe.")
                             : (trap ? "Careful – it was a scam." : "Actually, this one was safe.");
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
            Button(bar, m, y, cw, last ? "SEE RESULT" : "NEXT", true, Next);
            y += ButtonH + 10f;
            return y + Disclaim(bar, y) + 10f;
        }

        void Next()
        {
            if (busy || round == null || !round.Answered) return;
            busy = true;
            Haptics.Tap();
            Sfx.Play(Sfx.Id.Whoosh);
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
            var bt = Ui.Text("Title", badge.Content, "Badge: " + ScamDeck.BadgeName, 16f, Color.white, FontWeight.ExtraBold);
            Ui.Place(bt.rectTransform, 64f, 11f, cw - 76f, 22f);
            var bd = Ui.Text("Desc", badge.Content, "For finishing all the cards", 12.5f, Palette.WhiteSoft, FontWeight.SemiBold);
            Ui.Place(bd.rectTransform, 64f, 33f, cw - 76f, 18f);
            y += bh + 20f;

            // Gdzie był błąd – z krótkim powtórzeniem wyjaśnienia.
            var wrong = round.Mistakes();
            if (wrong.Count > 0)
            {
                y += Para(c, "To review", m, y, cw, 17f, Palette.Text, FontWeight.ExtraBold) + 8f;
                foreach (var w in wrong) y += Mistake(c, y, w) + 10f;
                y += 8f;
            }

            // Ściągawka czerwonych flag.
            y += Para(c, "Cheat sheet – red flags", m, y, cw, 17f, Palette.Text, FontWeight.ExtraBold) + 8f;
            foreach (var f in ScamDeck.CheatSheet)
            {
                var dot = Ui.Rounded("Dot", c, Trap, 4f);
                Ui.Place(dot.rectTransform, m + 4f, y + 8f, 8f, 8f);
                y += Para(c, f, m + 22f, y, cw - 22f, 15.5f, Palette.Text) + 8f;
            }
            y += 12f;

            // Most do działania.
            y += Para(c, "Now that you know how to avoid traps, check out safe ways to grow your savings.", m, y, cw, 15.5f, Palette.TextMuted);
            return y;
        }

        float Mistake(RectTransform c, float y, ScamCard card)
        {
            const float pad = 14f;
            var look = Look(card.Type);
            Color col = look.Color;
            string head = look.Label + " – " + (card.Answer == ScamAnswer.Trap ? "scam" : "safe");
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
            Button(bar, m, y, cw, "CONTINUE", true, () => { if (onFinish != null) onFinish(); });
            y += ButtonH + 10f;
            Button(bar, m, y, cw, "PLAY AGAIN", false, Start);
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

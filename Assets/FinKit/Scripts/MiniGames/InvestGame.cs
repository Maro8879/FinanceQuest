using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FinanceQuest
{
    // Mini gra „Pierwsze 12 miesięcy po zakupie” – ekran. Liczy InvestSim (osobno, z testami), tu tylko wygląd.
    // Przebieg: intro → wybór instrumentu → 12 tur „Następny miesiąc” z kartami wydarzeń → podsumowanie.
    // Ekran budujemy od nowa przy każdej zmianie (jak QuizScreen) – to kilka kart, więc jest szybko i prosto.
    public class InvestGame
    {
        public const string Id = "12-miesiecy";

        const float ButtonH = 58f, BarH = 112f, ChartH = 170f;
        const string Disclaimer = "To symulacja edukacyjna, nie stanowi doradztwa inwestycyjnego. Wyniki z przeszłości nie gwarantują przyszłych.";

        static readonly Color Good = Palette.Hex("3F8F5F"), Bad = Palette.Hex("C8553D");
        static readonly Color GoodFill = Palette.Hex("E7F3EB"), BadFill = Palette.Hex("FBE7E2"), SelFill = Palette.Hex("FFF6D1");

        enum Stage { Intro, Choose, Turn, Summary }
        enum Ev { None, Crash1, Crash2, Inflation, Dividend, Scam }

        readonly FinanceQuestApp app;
        readonly RectTransform screen;
        readonly Action onComplete, onFinish;
        readonly float W, m, cw;

        Stage stage = Stage.Intro;
        InvestSim sim;
        Ev ev = Ev.None;
        string feedback;          // tekst po decyzji na karcie wydarzenia (null = jeszcze bez decyzji)
        bool feedbackGood;
        double minY, maxY;        // zakres wykresu – stały przez całą grę
        bool completed;

        // onComplete: level zaliczony (raz, przy podsumowaniu). onFinish: „Zastosuj w praktyce” – dalej w aplikacji.
        public static void Build(RectTransform screen, FinanceQuestApp app, Action onComplete, Action onFinish)
        {
            new InvestGame(screen, app, onComplete, onFinish).Rebuild();
        }

        InvestGame(RectTransform screen, FinanceQuestApp app, Action onComplete, Action onFinish)
        {
            this.screen = screen; this.app = app; this.onComplete = onComplete; this.onFinish = onFinish;
            W = app.ScreenWidth; m = ScreenKit.Margin; cw = W - 2 * m;
            app.SwipeForward = null;
        }

        // ---------------- budowanie ekranu ----------------

        void Rebuild()
        {
            for (int i = screen.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(screen.GetChild(i).gameObject);
            var content = ScreenKit.Scroll(screen, W, 0f);
            ((RectTransform)content.parent).offsetMin = new Vector2(0f, BarH);   // lista kończy się nad dolnym paskiem
            var bar = BottomBar();

            float y = 8f;
            switch (stage)
            {
                case Stage.Intro: y = Intro(content, y, bar); break;
                case Stage.Choose: y = Choose(content, y); break;
                case Stage.Turn: y = Turn(content, y, bar); break;
                case Stage.Summary: y = Summary(content, y, bar); break;
            }
            ScreenKit.SetContentHeight(content, y + 24f);
            if (stage == Stage.Turn && ev != Ev.None) EventCard();
        }

        // Dolny pasek: stały komunikat o symulacji + miejsce na główny przycisk.
        RectTransform BottomBar()
        {
            var bar = Ui.Rect("BottomBar", screen);
            bar.anchorMin = new Vector2(0f, 0f);
            bar.anchorMax = new Vector2(1f, 0f);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.anchoredPosition = Vector2.zero;
            bar.sizeDelta = new Vector2(0f, BarH);
            var d = Ui.Text("Disclaimer", bar, Disclaimer, 10.5f, Palette.TextMuted, FontWeight.SemiBold, TextAlignmentOptions.Center);
            Ui.Place(d.rectTransform, m, 4f, cw, 28f);
            return bar;
        }

        LipCard MainButton(RectTransform bar, string label, Action onClick)
        {
            return Button(bar, m, 36f, cw, label, true, onClick);
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

        float Para(Transform parent, string text, float x, float y, float w, float size, Color color, FontWeight weight = FontWeight.Regular)
        {
            var t = Ui.Text("Text", parent, ContentBlocks.Rich(text), size, color, weight);
            t.richText = true;
            t.lineSpacing = 4f;
            float h = Ui.TextHeight(t, w);
            Ui.Place(t.rectTransform, x, y, w, h);
            return h;
        }

        float Kicker(Transform parent, string text, float y)
        {
            var k = Ui.Text("Kicker", parent, text, 11f, Palette.TextMuted);
            k.characterSpacing = 12f;
            Ui.Place(k.rectTransform, m, y, cw, 16);
            return 24f;
        }

        // ---------------- intro ----------------

        float Intro(RectTransform c, float y, RectTransform bar)
        {
            y += Kicker(c, "MINI GRA", y);
            y += Para(c, "Pierwsze 12 miesięcy po zakupie", m, y, cw, 24f, Palette.Text, FontWeight.ExtraBold) + 12f;
            y += Para(c, "Dostajesz wirtualne pieniądze. Wybierz, w co je włożyć, i przejdź rok miesiąc po miesiącu. " +
                         "Będą spadki, decyzje pod presją i jedna próba oszustwa. Zobacz, jak działają emocje.", m, y, cw, 16f, Palette.Text) + 18f;

            var card = LipCard.Create(c, "Money", m, y, cw, 104f, Palette.Brand, Palette.Brand, Palette.Darker(Palette.Brand, 0.7f), ScreenKit.CardRadius, ScreenKit.Lip, 0f);
            var l = Ui.Text("Label", card.Content, "TWÓJ WIRTUALNY KAPITAŁ", 11f, Palette.WhiteSoft);
            l.characterSpacing = 10f;
            Ui.Place(l.rectTransform, 18, 18, cw - 36, 16);
            var v = Ui.Text("Value", card.Content, Money(InvestConfig.Start), 34f, Color.white);
            Ui.Place(v.rectTransform, 18, 40, cw - 36, 44);
            y += 104f + 18f;

            y += Para(c, "Ok. 2–3 minuty · 12 tur = 12 miesięcy", m, y, cw, 13f, Palette.TextMuted, FontWeight.SemiBold);
            MainButton(bar, "ZACZYNAMY", () => Go(Stage.Choose));
            return y;
        }

        // ---------------- wybór instrumentu ----------------

        float Choose(RectTransform c, float y)
        {
            y += Kicker(c, "KROK 1 Z 2", y);
            y += Para(c, "W co inwestujesz " + Money(InvestConfig.Start) + "?", m, y, cw, 22f, Palette.Text, FontWeight.ExtraBold) + 16f;
            y += Option(c, y, Instrument.Etf, "ETF globalny", "Akcje firm z całego świata w jednym funduszu. Wyższy potencjał, duże wahania.", "RYZYKO: WYSOKIE", Bad) + 12f;
            y += Option(c, y, Instrument.Bonds, "Obligacje", "Pożyczasz pieniądze państwu lub firmom. Niższe wahania, umiarkowany zysk.", "RYZYKO: NISKIE", Palette.Mustard) + 12f;
            y += Option(c, y, Instrument.Deposit, "Lokata", "Gwarantowane odsetki ok. 3% rocznie. Żadnych wahań.", "BEZ WAHAŃ", Good) + 12f;
            return y;
        }

        float Option(RectTransform c, float y, Instrument inst, string title, string desc, string risk, Color riskColor)
        {
            const float pad = 16f;
            float tw = cw - 2 * pad;
            var probe = Ui.Text("Desc", c, desc, 14.5f, Palette.TextMuted, FontWeight.Regular);
            float dh = Ui.TextHeight(probe, tw);
            float h = pad + 26f + 6f + dh + 10f + 22f + pad;
            var card = LipCard.Create(c, "Option", m, y, cw, h, Palette.CardFill, Palette.CardEdge, Palette.CardLip, ScreenKit.CardRadius, ScreenKit.Lip, 2f);
            var t = Ui.Text("Title", card.Content, title, 19f, Palette.Text);
            Ui.Place(t.rectTransform, pad, pad, tw, 26f);
            probe.transform.SetParent(card.Content, false);
            Ui.Place(probe.rectTransform, pad, pad + 32f, tw, dh);
            Chip(card.Content, pad, pad + 32f + dh + 10f, risk, riskColor);
            card.SetClickable(() =>
            {
                sim = new InvestSim(inst);
                ChartRange(inst);
                ev = Ev.None;
                Go(Stage.Turn);
            });
            return h + ScreenKit.Lip;
        }

        // Mała etykieta w kolorze (ryzyko, „sprzedane”, odznaki).
        float Chip(Transform parent, float x, float y, string text, Color color)
        {
            var t = Ui.Text("Chip", parent, text, 11f, Color.white, FontWeight.ExtraBold, TextAlignmentOptions.Center);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.characterSpacing = 6f;
            float w = t.GetPreferredValues(text, 1000f, 0f).x + 20f;
            var bg = Ui.Rounded("ChipBg", parent, color, 11f);
            Ui.Place(bg.rectTransform, x, y, w, 22f);
            t.transform.SetParent(bg.transform, false);
            Ui.Stretch(t.rectTransform);
            return w;
        }

        // Zakres osi Y liczony z góry z najbardziej skrajnych scenariuszy – wykres nie zmienia skali w trakcie gry.
        void ChartRange(Instrument inst)
        {
            minY = double.MaxValue; maxY = double.MinValue;
            var plays = new[] { new InvestSim(inst), new InvestSim(inst), new InvestSim(inst) };
            for (int k = 0; k < InvestConfig.Months; k++)
            {
                foreach (var p in plays) p.NextMonth();
                if (plays[0].Month == InvestConfig.CrashMonth1) { plays[1].Decide(CrashChoice.BuyMore); plays[2].Decide(CrashChoice.Sell); }
                if (plays[0].Month == InvestConfig.CrashMonth2) plays[1].Decide(CrashChoice.BuyMore);
                if (plays[0].Month == InvestConfig.ScamMonth) plays[2].Scam(true);
            }
            foreach (var p in plays)
                foreach (var v in p.History) { minY = Math.Min(minY, v); maxY = Math.Max(maxY, v); }
            minY = Math.Min(minY, InvestConfig.Start);
            double pad = Math.Max(10.0, (maxY - minY) * 0.08);
            minY -= pad; maxY += pad;
        }

        // ---------------- tura ----------------

        float Turn(RectTransform c, float y, RectTransform bar)
        {
            string kicker = sim.Month == 0 ? "START · " + InstrumentName(sim.Instrument).ToUpperInvariant()
                                           : "MIESIĄC " + sim.Month + "/" + InvestConfig.Months + " · " + InstrumentName(sim.Instrument).ToUpperInvariant();
            y += Kicker(c, kicker, y);

            // Karta z wartością, zmianą i wykresem.
            const float pad = 16f;
            float h = pad + 18f + 44f + 22f + 12f + ChartH + (sim.Sold ? 26f : 0f) + pad;
            var card = LipCard.Create(c, "Portfolio", m, y, cw, h, Palette.CardFill, Palette.CardEdge, Palette.CardLip, ScreenKit.CardRadius, ScreenKit.Lip, 2f);
            var lab = Ui.Text("Label", card.Content, sim.Sold ? "TWOJA GOTÓWKA" : "WARTOŚĆ PORTFELA", 11f, Palette.TextMuted);
            lab.characterSpacing = 10f;
            Ui.Place(lab.rectTransform, pad, pad, cw - 2 * pad, 16f);
            var val = Ui.Text("Value", card.Content, Money(sim.Value), 34f, Palette.Text);
            Ui.Place(val.rectTransform, pad, pad + 18f, cw - 2 * pad, 44f);
            double ch = sim.ChangeFromPaid;
            var chg = Ui.Text("Change", card.Content, PctSigned(ch) + " od wpłaty (" + Money(sim.Paid) + ")", 14f,
                ch > 0.0005 ? Good : ch < -0.0005 ? Bad : Palette.TextMuted, FontWeight.ExtraBold);
            Ui.Place(chg.rectTransform, pad, pad + 62f, cw - 2 * pad, 20f);

            float cy = pad + 62f + 22f + 12f;
            var chart = Ui.Rect("Chart", card.Content).gameObject.AddComponent<LineChart>();
            Ui.Place(chart.rectTransform, pad, cy, cw - 2 * pad, ChartH);
            chart.raycastTarget = false;
            chart.Main.AddRange(sim.History);
            if (sim.Sold) chart.Ghost = sim.HoldHistory;
            chart.Baseline = InvestConfig.Start;
            chart.Min = minY; chart.Max = maxY;
            chart.MainColor = Palette.Brand; chart.GhostColor = Palette.TextMuted; chart.BaseColor = Palette.CardEdge;
            chart.SetVerticesDirty();
            // Kropka na końcu linii – „tu jesteś teraz”.
            var dot = Ui.Rounded("Dot", chart.transform, Palette.Accent, 6f);
            var drt = dot.rectTransform;
            drt.anchorMin = drt.anchorMax = new Vector2(0f, 1f);
            drt.pivot = new Vector2(0.5f, 0.5f);
            drt.sizeDelta = new Vector2(12f, 12f);
            Vector2 p = chart.Point(sim.Month, sim.Value);
            drt.anchoredPosition = new Vector2(p.x, p.y);
            var start = Ui.Text("Axis", card.Content, "start", 10.5f, Palette.TextMuted, FontWeight.SemiBold);
            Ui.Place(start.rectTransform, pad, cy + ChartH + 2f, 60f, 14f);
            var end = Ui.Text("Axis", card.Content, "12 mies.", 10.5f, Palette.TextMuted, FontWeight.SemiBold, TextAlignmentOptions.TopRight);
            Ui.Place(end.rectTransform, cw - pad - 80f, cy + ChartH + 2f, 80f, 14f);
            if (sim.Sold)
                Para(card.Content, "Sprzedane w miesiącu " + sim.SoldMonth + ". Przerywana linia: **gdybyś trzymał** – " + Money(sim.HoldHistory[sim.Month]) + ".",
                    pad, cy + ChartH + 18f, cw - 2 * pad, 12.5f, Palette.TextMuted, FontWeight.SemiBold);
            y += h + ScreenKit.Lip + 12f;

            // Mały licznik wartości realnej.
            var real = LipCard.Create(c, "Real", m, y, cw, 54f, Palette.Background, Palette.CardEdge, Palette.CardEdge, 14f, 0f, 2f);
            var rl = Ui.Text("Label", real.Content, "Realna wartość po inflacji (3%/rok)", 13f, Palette.TextMuted, FontWeight.SemiBold, TextAlignmentOptions.MidlineLeft);
            Ui.Place(rl.rectTransform, 14f, 0f, cw - 140f, 50f);
            var rv = Ui.Text("Value", real.Content, Money(sim.RealValue), 16f, Palette.Text, FontWeight.ExtraBold, TextAlignmentOptions.MidlineRight);
            Ui.Place(rv.rectTransform, cw - 140f, 0f, 126f, 50f);
            y += 54f + 12f;

            if (sim.Finished) MainButton(bar, "ZOBACZ WYNIK", () => Go(Stage.Summary));
            else MainButton(bar, sim.Month == 0 ? "PIERWSZY MIESIĄC" : "NASTĘPNY MIESIĄC", NextMonth);
            return y;
        }

        void NextMonth()
        {
            sim.NextMonth();
            int mo = sim.Month;
            ev = Ev.None;
            feedback = null;
            if (mo == InvestConfig.CrashMonth1) ev = sim.HasCrash ? Ev.Crash1 : Ev.Inflation;
            else if (mo == InvestConfig.CrashMonth2 && sim.HasCrash && !sim.Sold) ev = Ev.Crash2;
            else if (mo == InvestConfig.DividendMonth && sim.Instrument == Instrument.Etf && !sim.Sold) ev = Ev.Dividend;
            else if (mo == InvestConfig.ScamMonth) ev = Ev.Scam;
            if (ev != Ev.None) Haptics.Tap();
            Rebuild();
        }

        // ---------------- karta wydarzenia ----------------

        void EventCard()
        {
            // Przyciemnione tło łapie dotyk – najpierw decyzja, potem dalej.
            var dim = Ui.Rect("Dim", screen).gameObject.AddComponent<Image>();
            dim.color = new Color(0.07f, 0.1f, 0.11f, 0.55f);
            Ui.Stretch(dim.rectTransform);

            const float pad = 18f;
            float w = cw, tw = w - 2 * pad;
            string kicker, title, text;
            EventTexts(out kicker, out title, out text);

            var holder = Ui.Rect("EventCard", screen);
            Ui.Stretch(holder);
            float y = 0f;
            var probeT = Ui.Text("Probe", holder, title, 21f, Palette.Text);
            float th = Ui.TextHeight(probeT, tw);
            UnityEngine.Object.Destroy(probeT.gameObject);
            var probeX = Ui.Text("Probe", holder, ContentBlocks.Rich(feedback ?? text), 15f, Palette.Text, FontWeight.Regular);
            probeX.lineSpacing = 4f;
            float xh = Ui.TextHeight(probeX, tw);
            UnityEngine.Object.Destroy(probeX.gameObject);

            string[] options = feedback != null ? new[] { "DALEJ" } : Options();
            float buttonsH = options.Length * (ButtonH + 10f);
            float h = pad + 18f + th + 10f + xh + 18f + buttonsH + pad - 10f;

            float top = Mathf.Max(20f, (app.ScreenHeight - AppHeader.Height - h) * 0.45f);
            Color fill = feedback == null ? Palette.CardFill : feedbackGood ? GoodFill : BadFill;
            Color edge = feedback == null ? Palette.CardEdge : feedbackGood ? Good : Bad;
            var card = LipCard.Create(holder, "Card", m, top, w, h, fill, edge, edge, ScreenKit.CardRadius, ScreenKit.Lip, 2f);

            var k = Ui.Text("Kicker", card.Content, feedback != null ? (feedbackGood ? "DOBRZE" : "UWAGA") : kicker, 11f,
                feedback != null ? (feedbackGood ? Good : Bad) : Palette.TextMuted);
            k.characterSpacing = 12f;
            Ui.Place(k.rectTransform, pad, pad, tw, 16f);
            y = pad + 18f;
            var t = Ui.Text("Title", card.Content, title, 21f, Palette.Text);
            Ui.Place(t.rectTransform, pad, y, tw, th);
            y += th + 10f;
            y += Para(card.Content, feedback ?? text, pad, y, tw, 15f, Palette.Text) + 18f;

            for (int i = 0; i < options.Length; i++)
            {
                int idx = i;
                // Decyzje wszystkie w tym samym kolorze – żeby wygląd nie podpowiadał „dobrej” odpowiedzi.
                Button(card.Content, pad, y, tw, options[i], options.Length == 1, () => Decide(idx));
                y += ButtonH + 10f;
            }
        }

        string[] Options()
        {
            switch (ev)
            {
                case Ev.Crash1:
                case Ev.Crash2: return new[] { "Sprzedaję wszystko", "Trzymam", "Dokupuję za " + Money(InvestConfig.TopUp) };
                case Ev.Scam: return new[] { "Klikam w link", "Ignoruję i zgłaszam" };
                default: return new[] { "DALEJ" };
            }
        }

        void Decide(int i)
        {
            if (feedback != null || ev == Ev.Inflation || ev == Ev.Dividend) { ev = Ev.None; feedback = null; Rebuild(); return; }
            if (ev == Ev.Crash1 || ev == Ev.Crash2)
            {
                var choice = i == 0 ? CrashChoice.Sell : i == 1 ? CrashChoice.Hold : CrashChoice.BuyMore;
                double paperLoss = sim.Paid - sim.Value;
                sim.Decide(choice);
                if (choice == CrashChoice.Sell)
                {
                    feedbackGood = false;
                    feedback = paperLoss > 0
                        ? "Sprzedałeś. Strata **" + Money(paperLoss) + "** była dotąd tylko na papierze – teraz jest prawdziwa. Nie odrobisz jej, nawet gdy rynek wróci."
                        : "Sprzedałeś. Od teraz Twoje pieniądze leżą w gotówce i nie rosną.";
                }
                else if (choice == CrashChoice.Hold)
                {
                    feedbackGood = true;
                    feedback = "Trzymasz. Spadek to na razie **strata na papierze** – realna staje się dopiero, gdy sprzedasz. Rynki po krachach zwykle z czasem odbijają.";
                }
                else
                {
                    feedbackGood = true;
                    feedback = "Dokupiłeś w dołku za " + Money(InvestConfig.TopUp) + ". Za te same pieniądze masz teraz więcej jednostek niż przed spadkiem – gdy rynek odbije, zyskają najmocniej.";
                }
            }
            else if (ev == Ev.Scam)
            {
                bool clicked = i == 0;
                sim.Scam(clicked);
                feedbackGood = !clicked;
                feedback = clicked
                    ? "Straciłeś **" + Money(InvestConfig.ScamLoss) + "**. Czerwone flagi: presja czasu, skrócony link, nieznany numer. Bank nigdy nie prosi o logowanie przez link z SMS-a."
                    : "Świetnie! Presja czasu i skrócony link to typowe oszustwo. Bank nie wysyła takich linków – zgłoś SMS w swoim banku i usuń go.";
            }
            Haptics.Tap();
            Rebuild();
        }

        void EventTexts(out string kicker, out string title, out string text)
        {
            string pct = PctSigned(sim.ChangeFromPaid);
            bool etf = sim.Instrument == Instrument.Etf;
            switch (ev)
            {
                case Ev.Crash1:
                    kicker = "WIADOMOŚCI · MIESIĄC " + sim.Month;
                    title = "Rynek mocno spada";
                    text = etf
                        ? "Nagłówki krzyczą o krachu. Twój portfel: **" + pct + "**. Znajomi mówią, że trzeba uciekać. Co robisz?"
                        : "Akcje mocno spadają. Twoje obligacje też lekko tracą: **" + pct + "**. Znajomi sprzedają w panice. Co robisz?";
                    break;
                case Ev.Crash2:
                    kicker = "WIADOMOŚCI · MIESIĄC " + sim.Month;
                    title = "Drugie, głębsze dno";
                    text = "Kolejny miesiąc spadków – jesteś **" + pct + "** pod kreską. To najgorszy moment roku. Pamiętaj: strata na papierze staje się realna dopiero po sprzedaży.";
                    break;
                case Ev.Inflation:
                    kicker = "TYMCZASEM · MIESIĄC " + sim.Month;
                    title = "Akcje spadają, lokata rośnie";
                    text = "Twoja lokata spokojnie rośnie: **" + Money(sim.Value) + "**. Ale ceny w sklepach też rosną, ok. 3% rocznie. Realnie masz **" +
                           Money(sim.RealValue) + "** – inflacja zjada prawie cały zysk.";
                    break;
                case Ev.Dividend:
                    kicker = "DYWIDENDY · MIESIĄC " + sim.Month;
                    title = "Spółki wypłaciły dywidendy";
                    text = "Twój ETF jest **akumulujący** – sam reinwestuje dywidendy w kolejne akcje. Nic nie musisz robić, a zyski pracują dalej. To procent składany w praktyce.";
                    break;
                default:
                    kicker = "SMS · NIEZNANY NUMER";
                    title = "Twoje konto jest zagrożone!";
                    text = "„Wykryliśmy podejrzane logowanie. Zablokuj konto w ciągu 15 minut: **bit.ly/bank-pomoc**”";
                    break;
            }
            if (feedback != null) title = ev == Ev.Scam ? (feedbackGood ? "Oszustwo zatrzymane" : "To było oszustwo") : title;
        }

        // ---------------- podsumowanie ----------------

        float Summary(RectTransform c, float y, RectTransform bar)
        {
            if (!completed) { completed = true; if (onComplete != null) onComplete(); }

            y += Kicker(c, "PODSUMOWANIE · " + InstrumentName(sim.Instrument).ToUpperInvariant(), y);
            y += Para(c, "Twój rok w liczbach", m, y, cw, 24f, Palette.Text, FontWeight.ExtraBold) + 14f;

            // Wynik.
            const float pad = 16f;
            double ch = sim.ChangeFromPaid;
            var card = LipCard.Create(c, "Result", m, y, cw, 128f, Palette.Brand, Palette.Brand, Palette.Darker(Palette.Brand, 0.7f), ScreenKit.CardRadius, ScreenKit.Lip, 0f);
            var l = Ui.Text("Label", card.Content, "WARTOŚĆ KOŃCOWA", 11f, Palette.WhiteSoft);
            l.characterSpacing = 10f;
            Ui.Place(l.rectTransform, pad, pad, cw - 2 * pad, 16f);
            var v = Ui.Text("Value", card.Content, Money(sim.Value), 34f, Color.white);
            Ui.Place(v.rectTransform, pad, pad + 18f, cw - 2 * pad, 44f);
            var p = Ui.Text("Pct", card.Content, PctSigned(ch) + " od wpłaty · realnie " + Money(sim.RealValue), 14f,
                ch >= 0 ? Palette.Hex("BFE6CC") : Palette.Hex("F6C5B8"), FontWeight.ExtraBold);
            Ui.Place(p.rectTransform, pad, pad + 66f, cw - 2 * pad, 40f);
            y += 128f + ScreenKit.Lip + 14f;

            // Porównanie wariantów.
            y += Para(c, "Porównanie", m, y, cw, 17f, Palette.Text, FontWeight.ExtraBold) + 8f;
            string r1, r2, r3; double v1, v2, v3;
            if (sim.HasCrash)
            {
                r1 = "Twój wynik"; v1 = sim.Value;
                r2 = "Gdybyś sprzedał w panice"; v2 = InvestSim.PanicSell(sim.Instrument);
                r3 = "Gdybyś trzymał do końca"; v3 = InvestSim.HoldToEnd(sim.Instrument);
            }
            else
            {
                r1 = "Twój wynik (lokata)"; v1 = sim.Value;
                r2 = "Lokata realnie, po inflacji"; v2 = InvestSim.Real(InvestSim.HoldToEnd(Instrument.Deposit), InvestConfig.Months);
                r3 = "ETF trzymany do końca"; v3 = InvestSim.HoldToEnd(Instrument.Etf);
            }
            y += CompareRow(c, y, r1, v1, true) + 8f;
            y += CompareRow(c, y, r2, v2, false) + 8f;
            y += CompareRow(c, y, r3, v3, false) + 16f;

            // Wnioski.
            y += Para(c, "Wnioski", m, y, cw, 17f, Palette.Text, FontWeight.ExtraBold) + 6f;
            y += Para(c, Lessons(), m, y, cw, 15f, Palette.Text) + 16f;

            // Odznaki.
            y += Para(c, "Odznaki", m, y, cw, 17f, Palette.Text, FontWeight.ExtraBold) + 8f;
            y += Badge(c, y, "Pierwsze kroki", "Ukończyłeś symulację", sim.FirstSteps) + 8f;
            y += Badge(c, y, "Nie spanikowałeś", sim.HasCrash ? "Trzymałeś albo dokupiłeś w krachu" : "Lokata nie miała krachu – spróbuj ETF", sim.NoPanic) + 8f;
            y += Badge(c, y, "Czujny na oszustwa", "Zgłosiłeś podejrzany SMS", sim.ScamAware) + 16f;

            Button(c, m, y, cw, "ZAGRAJ PONOWNIE", false, () => { sim = null; Go(Stage.Choose); });
            y += ButtonH + 8f;
            MainButton(bar, "ZASTOSUJ W PRAKTYCE", () => { if (onFinish != null) onFinish(); });
            return y;
        }

        float CompareRow(RectTransform c, float y, string label, double value, bool mine)
        {
            const float h = 50f;
            var card = LipCard.Create(c, "Row", m, y, cw, h, mine ? SelFill : Palette.CardFill, mine ? Palette.Accent : Palette.CardEdge,
                mine ? Palette.AccentLip : Palette.CardLip, 14f, 3f, 2f);
            var l = Ui.Text("Label", card.Content, label, 14f, Palette.Text, mine ? FontWeight.ExtraBold : FontWeight.SemiBold, TextAlignmentOptions.MidlineLeft);
            Ui.Place(l.rectTransform, 14f, 0f, cw - 150f, h - 3f);
            double ch = value / InvestConfig.Start - 1.0;
            var v = Ui.Text("Value", card.Content, Money(value) + "\n<size=11>" + PctSigned(ch) + "</size>", 15f,
                Palette.Text, FontWeight.ExtraBold, TextAlignmentOptions.MidlineRight);
            v.richText = true;
            v.lineSpacing = -8f;
            Ui.Place(v.rectTransform, cw - 140f, 0f, 126f, h - 3f);
            return h;
        }

        float Badge(RectTransform c, float y, string title, string desc, bool got)
        {
            const float h = 58f, icon = 34f;
            var card = LipCard.Create(c, "Badge", m, y, cw, h, got ? Palette.CardFill : Palette.LockedFill, got ? Palette.CardEdge : Palette.LockedEdge,
                got ? Palette.CardLip : Palette.LockedLip, 14f, 3f, 2f);
            var circle = Ui.Rounded("Icon", card.Content, got ? Palette.Accent : Palette.LockedEdge, icon * 0.5f);
            Ui.Place(circle.rectTransform, 12f, (h - 3f - icon) * 0.5f, icon, icon);
            var ic = Ui.Icon("Glyph", circle.transform, got ? IconId.Star : IconId.Lock, got ? Palette.OnAccent : Palette.LockedIcon, 18f);
            Ui.Center(ic.rectTransform, 18f, 18f);
            var t = Ui.Text("Title", card.Content, title, 15f, got ? Palette.Text : Palette.TextLocked);
            Ui.Place(t.rectTransform, 58f, 9f, cw - 70f, 20f);
            var d = Ui.Text("Desc", card.Content, desc, 12.5f, got ? Palette.TextMuted : Palette.TextLocked, FontWeight.SemiBold);
            Ui.Place(d.rectTransform, 58f, 29f, cw - 70f, 18f);
            return h;
        }

        // 2–3 zdania dopasowane do wyborów gracza.
        string Lessons()
        {
            string s;
            if (!sim.HasCrash)
                s = "Lokata dała pewne odsetki, ale inflacja zjadła prawie cały zysk – realnie masz tyle, co na starcie. Bezpiecznie nie znaczy, że pieniądze rosną.";
            else if (sim.Sold)
                s = "Sprzedaż w dołku zamieniła stratę na papierze w realną. Gdybyś trzymał, miałbyś " + Money(InvestSim.HoldToEnd(sim.Instrument)) +
                    ". Największym wrogiem inwestora są emocje, nie spadki.";
            else if (sim.BoughtMore)
                s = "Dokupiłeś, gdy było tanio – po odbiciu te pieniądze urosły najmocniej. Spadek był okazją, a nie końcem świata.";
            else if (sim.Instrument == Instrument.Etf)
                s = "Przetrwałeś spadek o ok. 15% i rynek odrobił stratę. Wahania to cena za wyższy potencjał – liczy się czas, nie pojedyncze miesiące.";
            else
                s = "Obligacje wahały się mało i dały umiarkowany zysk. Mniejsze ryzyko to spokojniejsza głowa, ale i niższy potencjał.";
            if (sim.ScamClicked) s += " A SMS z linkiem kosztował Cię " + Money(InvestConfig.ScamLoss) + " – bank nigdy nie wysyła takich wiadomości.";
            return s;
        }

        // ---------------- pomocnicze ----------------

        void Go(Stage s)
        {
            stage = s;
            Haptics.Tap();
            Rebuild();
        }

        static string InstrumentName(Instrument i)
        {
            return i == Instrument.Etf ? "ETF globalny" : i == Instrument.Bonds ? "Obligacje" : "Lokata";
        }

        // 1 045,30 € – polski zapis (spacja tysięcy, przecinek dziesiętny), niezależnie od ustawień telefonu.
        public static string Money(double v)
        {
            string s = Math.Abs(v).ToString("0.00", CultureInfo.InvariantCulture);
            int dot = s.IndexOf('.');
            string whole = s.Substring(0, dot), frac = s.Substring(dot + 1);
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < whole.Length; i++)
            {
                if (i > 0 && (whole.Length - i) % 3 == 0) sb.Append(' ');   // twarda spacja – kwota nie łamie się na dwie linie
                sb.Append(whole[i]);
            }
            return (v < 0 ? "-" : "") + sb + "," + frac + " €";
        }

        static string PctSigned(double v)
        {
            double p = Math.Round(v * 1000.0) / 10.0;   // jedno miejsce po przecinku
            string s = Math.Abs(p).ToString("0.0", CultureInfo.InvariantCulture).Replace('.', ',');
            return (p > 0 ? "+" : p < 0 ? "−" : "") + s + "%";
        }
    }
}

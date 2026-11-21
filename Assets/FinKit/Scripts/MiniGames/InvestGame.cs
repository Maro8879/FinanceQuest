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
        const string Disclaimer = "This is an educational simulation, not investment advice. Past performance does not guarantee future results.";

        static readonly Color Good = Palette.Hex("3F8F5F"), Bad = Palette.Hex("C8553D");
        static readonly Color GoodFill = Palette.Hex("E7F3EB"), BadFill = Palette.Hex("FBE7E2"), SelFill = Palette.Hex("FFF6D1");

        enum Stage { Intro, Choose, Turn, Summary }
        enum Ev { None, Crash1, Crash2, Inflation, Dividend, Scam }

        readonly FinanceQuestApp app;
        readonly RectTransform screen;
        readonly Action<int> onComplete;    // dostaje zdobyte gwiazdki (1–3)
        readonly Action onFinish;
        readonly float W, m, cw;

        Stage stage = Stage.Intro;
        InvestSim sim;
        Ev ev = Ev.None;
        string feedback;          // tekst po decyzji na karcie wydarzenia (null = jeszcze bez decyzji)
        bool feedbackGood;
        double minY, maxY;        // zakres wykresu – stały przez całą grę
        bool completed;

        // onComplete: wynik w gwiazdkach (raz na rozgrywkę, przy podsumowaniu) – liczą się do gwiazdek rozdziału jak w testach.
        // onFinish: „Kontynuuj” – dalej w aplikacji.
        public static void Build(RectTransform screen, FinanceQuestApp app, Action<int> onComplete, Action onFinish)
        {
            new InvestGame(screen, app, onComplete, onFinish).Rebuild();
        }

        InvestGame(RectTransform screen, FinanceQuestApp app, Action<int> onComplete, Action onFinish)
        {
            this.screen = screen; this.app = app; this.onComplete = onComplete; this.onFinish = onFinish;
            W = app.ScreenWidth; m = ScreenKit.Margin; cw = W - 2 * m;
            app.SwipeForward = null;
            screen.gameObject.AddComponent<Ticker>().OnTick = Tick;
        }

        // ---------------- budowanie ekranu ----------------

        void Rebuild()
        {
            for (int i = screen.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(screen.GetChild(i).gameObject);
            var content = ScreenKit.Scroll(screen, W, 0f);
            float barH = BarHeight();
            ((RectTransform)content.parent).offsetMin = new Vector2(0f, barH);   // lista kończy się nad dolnym paskiem
            var bar = BottomBar(barH);

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
        // Na podsumowaniu dwa przyciski (KONTYNUUJ + ZAGRAJ PONOWNIE), więc pasek jest wyższy.
        float BarHeight() { return stage == Stage.Summary ? BarH + ButtonH + 10f : BarH; }

        RectTransform BottomBar(float barH)
        {
            var bar = Ui.Rect("BottomBar", screen);
            bar.anchorMin = new Vector2(0f, 0f);
            bar.anchorMax = new Vector2(1f, 0f);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.anchoredPosition = Vector2.zero;
            bar.sizeDelta = new Vector2(0f, barH);
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
            // Tytuł gry stoi w nagłówku (tytuł levelu) – tu od razu treść.
            y += 4f;
            y += Para(c, "You get virtual money. Choose where to put it and go through a year, month by month. " +
                         "Expect drops, decisions under pressure and one scam attempt. See how emotions work.", m, y, cw, 16f, Palette.Text) + 18f;

            var card = LipCard.Create(c, "Money", m, y, cw, 104f, Palette.Brand, Palette.Brand, Palette.Darker(Palette.Brand, 0.7f), ScreenKit.CardRadius, ScreenKit.Lip, 0f);
            var l = Ui.Text("Label", card.Content, "YOUR VIRTUAL CAPITAL", 11f, Palette.WhiteSoft);
            l.characterSpacing = 10f;
            Ui.Place(l.rectTransform, 18, 18, cw - 36, 16);
            var v = Ui.Text("Value", card.Content, Money(InvestConfig.Start), 34f, Color.white);
            Ui.Place(v.rectTransform, 18, 40, cw - 36, 44);
            y += 104f + 18f;
            MainButton(bar, "LET'S GO", () => Go(Stage.Choose));
            return y;
        }

        // ---------------- wybór instrumentu ----------------

        float Choose(RectTransform c, float y)
        {
            y += Kicker(c, "STEP 1 OF 2", y);
            y += Para(c, "Where do you invest " + Money(InvestConfig.Start) + "?", m, y, cw, 22f, Palette.Text, FontWeight.ExtraBold) + 16f;
            y += Option(c, y, Instrument.Etf, "Global ETF", "Shares of companies from all over the world in one fund. Higher potential, big swings.", "RISK: HIGH", Bad) + 12f;
            y += Option(c, y, Instrument.Bonds, "Bonds", "You lend money to governments or companies. Smaller swings, moderate return.", "RISK: LOW", Palette.Mustard) + 12f;
            y += Option(c, y, Instrument.Deposit, "Deposit", "Guaranteed interest of about 3% a year. No swings at all.", "NO SWINGS", Good) + 12f;
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
                playing = false;
                animT = 1f;
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

        // ---------------- tura: wykres leci sam, zatrzymuje się na wydarzeniach ----------------

        const float MonthSeconds = 0.75f;   // ile trwa jeden miesiąc animacji
        bool playing;                       // wykres leci (false = start albo pauza)
        float animT = 1f;                   // 0..1 – postęp bieżącego miesiąca
        TextMeshProUGUI kickerText, valueText, changeText, realText, zeroText;
        LineChart chart;
        RectTransform dot;

        float Turn(RectTransform c, float y, RectTransform bar)
        {
            var k = Ui.Text("Kicker", c, "", 11f, Palette.TextMuted);
            k.characterSpacing = 12f;
            Ui.Place(k.rectTransform, m, y, cw, 16);
            kickerText = k;
            y += 24f;

            // Karta z wartością, zmianą i wykresem. Wykres w % od wpłaty – spadek o 10% wygląda jak spadek o 10%.
            const float pad = 16f;
            float h = pad + 18f + 44f + 22f + 12f + ChartH + 18f + (sim.Sold ? 26f : 0f) + pad;
            var card = LipCard.Create(c, "Portfolio", m, y, cw, h, Palette.CardFill, Palette.CardEdge, Palette.CardLip, ScreenKit.CardRadius, ScreenKit.Lip, 2f);
            var lab = Ui.Text("Label", card.Content, sim.Sold ? "YOUR CASH" : "PORTFOLIO VALUE", 11f, Palette.TextMuted);
            lab.characterSpacing = 10f;
            Ui.Place(lab.rectTransform, pad, pad, cw - 2 * pad, 16f);
            valueText = Ui.Text("Value", card.Content, "", 34f, Palette.Text);
            Ui.Place(valueText.rectTransform, pad, pad + 18f, cw - 2 * pad, 44f);
            changeText = Ui.Text("Change", card.Content, "", 14f, Palette.TextMuted, FontWeight.ExtraBold);
            Ui.Place(changeText.rectTransform, pad, pad + 62f, cw - 2 * pad, 20f);

            float cy = pad + 62f + 22f + 12f;
            // CanvasRenderer dodajemy ręcznie – bez niego Unity nie rysuje wykresu i sypie błędami co klatkę (zamrożony ekran).
            var chartGo = Ui.Rect("Chart", card.Content).gameObject;
            chartGo.AddComponent<CanvasRenderer>();
            chart = chartGo.AddComponent<LineChart>();
            Ui.Place(chart.rectTransform, pad, cy, cw - 2 * pad - 34f, ChartH);   // z prawej miejsce na podpis „0%”
            chart.raycastTarget = false;
            chart.Baseline = 0.0;
            chart.MainColor = Good; chart.DownColor = Bad; chart.GhostColor = Palette.TextMuted; chart.BaseColor = Palette.CardEdge;
            zeroText = Ui.Text("Zero", card.Content, "0%", 10.5f, Palette.TextMuted, FontWeight.ExtraBold, TextAlignmentOptions.MidlineLeft);
            // Kropka na końcu linii – „tu jesteś teraz”.
            var d = Ui.Rounded("Dot", chart.transform, Palette.Accent, 6f);
            dot = d.rectTransform;
            dot.anchorMin = dot.anchorMax = new Vector2(0f, 1f);
            dot.pivot = new Vector2(0.5f, 0.5f);
            dot.sizeDelta = new Vector2(12f, 12f);
            var start = Ui.Text("Axis", card.Content, "start", 10.5f, Palette.TextMuted, FontWeight.SemiBold);
            Ui.Place(start.rectTransform, pad, cy + ChartH + 2f, 60f, 14f);
            var end = Ui.Text("Axis", card.Content, "12 mo.", 10.5f, Palette.TextMuted, FontWeight.SemiBold, TextAlignmentOptions.TopRight);
            Ui.Place(end.rectTransform, cw - pad - 34f - 80f, cy + ChartH + 2f, 80f, 14f);
            if (sim.Sold)
                Para(card.Content, "Sold in month " + sim.SoldMonth + ". Dashed line: **if you had held**.",
                    pad, cy + ChartH + 20f, cw - 2 * pad, 12.5f, Palette.TextMuted, FontWeight.SemiBold);
            y += h + ScreenKit.Lip + 12f;

            // Mały licznik wartości realnej.
            var real = LipCard.Create(c, "Real", m, y, cw, 54f, Palette.Background, Palette.CardEdge, Palette.CardEdge, 14f, 0f, 2f);
            var rl = Ui.Text("Label", real.Content, "Real value after inflation (3%/yr)", 13f, Palette.TextMuted, FontWeight.SemiBold, TextAlignmentOptions.MidlineLeft);
            Ui.Place(rl.rectTransform, 14f, 0f, cw - 140f, 50f);
            realText = Ui.Text("Value", real.Content, "", 16f, Palette.Text, FontWeight.ExtraBold, TextAlignmentOptions.MidlineRight);
            Ui.Place(realText.rectTransform, cw - 140f, 0f, 126f, 50f);
            y += 54f + 12f;

            if (sim.Finished && animT >= 1f) MainButton(bar, "SEE RESULT", () => Go(Stage.Summary));
            else if (!playing) MainButton(bar, sim.Month == 0 ? "START" : "RESUME", () => { playing = true; Haptics.Tap(); Rebuild(); });
            else Button(bar, m, 36f, cw, "PAUSE", false, () => { playing = false; Rebuild(); });

            UpdateView();
            return y;
        }

        // Co klatkę (z Ticker): animacja bieżącego miesiąca, potem następny – aż do wydarzenia albo końca roku.
        void Tick(float dt)
        {
            if (stage != Stage.Turn || sim == null || !playing || ev != Ev.None || chart == null) return;
            if (animT < 1f)
            {
                animT = Mathf.Min(1f, animT + dt / MonthSeconds);
                UpdateView();
                if (animT >= 1f) MonthDone();
                return;
            }
            if (sim.Finished) { playing = false; Rebuild(); return; }
            sim.NextMonth();
            animT = 0f;
            UpdateView();
        }

        // Koniec miesiąca: wydarzenie zatrzymuje wykres i pokazuje pytanie.
        void MonthDone()
        {
            int mo = sim.Month;
            ev = Ev.None;
            feedback = null;
            if (mo == InvestConfig.CrashMonth1) ev = sim.HasCrash ? Ev.Crash1 : Ev.Inflation;
            else if (mo == InvestConfig.CrashMonth2 && sim.HasCrash && !sim.Sold) ev = Ev.Crash2;
            else if (mo == InvestConfig.DividendMonth && sim.Instrument == Instrument.Etf && !sim.Sold) ev = Ev.Dividend;
            else if (mo == InvestConfig.ScamMonth) ev = Ev.Scam;
            if (ev != Ev.None) { Haptics.Tap(); Rebuild(); }
            else if (sim.Finished) { playing = false; Rebuild(); }
        }

        // Odświeża liczby, wykres i kropkę bez przebudowy ekranu (tanie – można co klatkę).
        void UpdateView()
        {
            int mo = sim.Month;
            float t = mo == 0 ? 1f : animT;
            int prev = Mathf.Max(0, mo - 1);
            double value = Lerp(sim.History[prev], sim.History[mo], t);
            double pct = Lerp(sim.PctAt(prev), sim.PctAt(mo), t);
            float when = prev + (mo == 0 ? 0f : t);

            kickerText.text = (mo == 0 ? "START" : "MONTH " + mo + "/" + InvestConfig.Months) + " · " + InstrumentName(sim.Instrument).ToUpperInvariant();
            valueText.text = Money(value);
            changeText.text = PctSigned(pct) + " since paying in (" + Money(sim.PaidHistory[mo]) + ")";
            changeText.color = pct > 0.0005 ? Good : pct < -0.0005 ? Bad : Palette.TextMuted;
            realText.text = Money(InvestSim.Real(value, when));

            FitRange();
            chart.Min = minY; chart.Max = maxY;
            chart.Main.Clear();
            for (int i = 0; i <= mo; i++) chart.Main.Add(sim.PctAt(i));
            if (sim.Sold)
            {
                if (chart.Ghost == null) chart.Ghost = new System.Collections.Generic.List<double>();
                chart.Ghost.Clear();
                for (int i = 0; i <= mo; i++) chart.Ghost.Add(sim.HoldPctAt(i));
            }
            else chart.Ghost = null;
            chart.LastT = t;
            chart.SetVerticesDirty();
            Vector2 p = chart.Point(when, pct);
            dot.anchoredPosition = new Vector2(p.x, p.y);
            Vector2 z = chart.Point(0f, 0.0);
            var zr = chart.rectTransform;
            Ui.Place(zeroText.rectTransform, zr.anchoredPosition.x + zr.sizeDelta.x + 4f, -zr.anchoredPosition.y - z.y - 8f, 30f, 16f);
        }

        static double Lerp(double a, double b, float t) { return a + (b - a) * t; }

        // Zakres osi Y (w %) dopasowany do instrumentu – dla ETF ok. −18%…+7%, więc dołek zajmuje większość wykresu.
        // Rozszerza się tylko wtedy, gdy Twój wynik wyjdzie poza niego (np. oszustwo).
        void ChartRange(Instrument inst)
        {
            minY = maxY = 0.0;
            var hold = new InvestSim(inst);
            for (int k = 0; k < InvestConfig.Months; k++)
            {
                hold.NextMonth();
                minY = Math.Min(minY, hold.PctAt(hold.Month)); maxY = Math.Max(maxY, hold.PctAt(hold.Month));
            }
            // Spokojne instrumenty (lokata, obligacje) dostają co najmniej ±3%, żeby drobne ruchy nie udawały krachu.
            minY = Math.Min(minY, -0.03); maxY = Math.Max(maxY, 0.03);
            double pad = (maxY - minY) * 0.12;
            minY -= pad; maxY += pad;
        }

        void FitRange()
        {
            double lo = minY, hi = maxY;
            for (int i = 0; i <= sim.Month; i++) { lo = Math.Min(lo, sim.PctAt(i)); hi = Math.Max(hi, sim.PctAt(i)); }
            double pad = (maxY - minY) * 0.06;
            if (lo < minY) minY = lo - pad;
            if (hi > maxY) maxY = hi + pad;
        }

        // Komponent tylko po to, żeby dostać Update co klatkę (InvestGame to zwykła klasa).
        class Ticker : MonoBehaviour
        {
            public Action<float> OnTick;
            void Update() { if (OnTick != null) OnTick(Time.unscaledDeltaTime); }
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

            string[] options = feedback != null ? new[] { "NEXT" } : Options();
            float buttonsH = options.Length * (ButtonH + 10f);
            float h = pad + 18f + th + 10f + xh + 18f + buttonsH + pad - 10f;

            float top = Mathf.Max(20f, (app.ScreenHeight - AppHeader.Height - h) * 0.45f);
            Color fill = feedback == null ? Palette.CardFill : feedbackGood ? GoodFill : BadFill;
            Color edge = feedback == null ? Palette.CardEdge : feedbackGood ? Good : Bad;
            var card = LipCard.Create(holder, "Card", m, top, w, h, fill, edge, edge, ScreenKit.CardRadius, ScreenKit.Lip, 2f);

            var k = Ui.Text("Kicker", card.Content, feedback != null ? (feedbackGood ? "WELL DONE" : "CAREFUL") : kicker, 11f,
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
                case Ev.Crash2: return new[] { "I sell everything", "I hold", "I buy more for " + Money(InvestConfig.TopUp) };
                case Ev.Scam: return new[] { "I click the link", "I ignore and report it" };
                default: return new[] { "NEXT" };
            }
        }

        void Decide(int i)
        {
            // DALEJ – wykres leci dalej sam (playing zostaje włączone).
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
                        ? "You sold. The **" + Money(paperLoss) + "** loss was only on paper until now – now it's real. You won't win it back, even when the market recovers."
                        : "You sold. From now on your money sits in cash and doesn't grow.";
                }
                else if (choice == CrashChoice.Hold)
                {
                    feedbackGood = true;
                    feedback = "You hold. For now the drop is a **paper loss** – it only becomes real when you sell. Markets usually recover from crashes over time.";
                }
                else
                {
                    feedbackGood = true;
                    feedback = "You bought more in the dip for " + Money(InvestConfig.TopUp) + ". The same money now buys more units than before the drop – when the market recovers, they gain the most.";
                }
            }
            else if (ev == Ev.Scam)
            {
                bool clicked = i == 0;
                sim.Scam(clicked);
                feedbackGood = !clicked;
                feedback = clicked
                    ? "You lost **" + Money(InvestConfig.ScamLoss) + "**. Red flags: time pressure, a shortened link, an unknown number. A bank never asks you to log in through a link in a text message."
                    : "Great! Time pressure and a shortened link are a typical scam. Banks don't send links like that – report the text to your bank and delete it.";
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
                    kicker = "NEWS · MONTH " + sim.Month;
                    title = "The market is falling hard";
                    text = etf
                        ? "Headlines scream crash. Your portfolio: **" + pct + "**. Friends say it's time to get out. What do you do?"
                        : "Stocks are falling hard. Your bonds are slightly down too: **" + pct + "**. Friends are panic-selling. What do you do?";
                    break;
                case Ev.Crash2:
                    kicker = "NEWS · MONTH " + sim.Month;
                    title = "A second, deeper dip";
                    text = "Another month of losses – you're **" + pct + "** down. This is the worst moment of the year. Remember: a paper loss only becomes real when you sell.";
                    break;
                case Ev.Inflation:
                    kicker = "MEANWHILE · MONTH " + sim.Month;
                    title = "Stocks fall, the deposit grows";
                    text = "Your deposit is growing steadily: **" + Money(sim.Value) + "**. But shop prices are rising too, about 3% a year. In real terms you have **" +
                           Money(sim.RealValue) + "** – inflation eats almost all of the gain.";
                    break;
                case Ev.Dividend:
                    kicker = "DIVIDENDS · MONTH " + sim.Month;
                    title = "Companies paid dividends";
                    text = "Your ETF is **accumulating** – it reinvests dividends into more shares by itself. You don't have to do anything, and the gains keep working. That's compound interest in practice.";
                    break;
                default:
                    kicker = "TEXT · UNKNOWN NUMBER";
                    title = "Your account is at risk!";
                    text = "“We detected a suspicious login. Block your account within 15 minutes: **bit.ly/bank-help**”";
                    break;
            }
            if (feedback != null) title = ev == Ev.Scam ? (feedbackGood ? "Scam stopped" : "That was a scam") : title;
        }

        // ---------------- podsumowanie ----------------

        float Summary(RectTransform c, float y, RectTransform bar)
        {
            int stars = Stars();
            bool first = !completed;
            if (first) { completed = true; if (onComplete != null) onComplete(stars); Sfx.Result(stars, false, 0.4f); }

            // Gwiazdki jak po teście: środkowa wyżej, zdobyte wskakują po kolei (tylko przy pierwszym pokazaniu).
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
            string head = stars == 3 ? "Full set!" : stars == 2 ? "Good job!" : "Completed!";
            var ht = Ui.Text("Title", c, head, 26f, Palette.Text, FontWeight.ExtraBold, TextAlignmentOptions.Top);
            Ui.Place(ht.rectTransform, m, y, cw, 34f);
            y += 34f + 4f;
            var hs = Ui.Text("Sub", c, "Stars: " + stars + " of 3 · " + InstrumentName(sim.Instrument), 16f, Palette.TextMuted, FontWeight.SemiBold, TextAlignmentOptions.Top);
            Ui.Place(hs.rectTransform, m, y, cw, 22f);
            y += 22f + 22f;

            // Wynik.
            const float pad = 16f;
            double ch = sim.ChangeFromPaid;
            var card = LipCard.Create(c, "Result", m, y, cw, 128f, Palette.Brand, Palette.Brand, Palette.Darker(Palette.Brand, 0.7f), ScreenKit.CardRadius, ScreenKit.Lip, 0f);
            var l = Ui.Text("Label", card.Content, "FINAL VALUE", 11f, Palette.WhiteSoft);
            l.characterSpacing = 10f;
            Ui.Place(l.rectTransform, pad, pad, cw - 2 * pad, 16f);
            var v = Ui.Text("Value", card.Content, Money(sim.Value), 34f, Color.white);
            Ui.Place(v.rectTransform, pad, pad + 18f, cw - 2 * pad, 44f);
            var p = Ui.Text("Pct", card.Content, PctSigned(ch) + " since paying in · real " + Money(sim.RealValue), 14f,
                ch >= 0 ? Palette.Hex("BFE6CC") : Palette.Hex("F6C5B8"), FontWeight.ExtraBold);
            Ui.Place(p.rectTransform, pad, pad + 66f, cw - 2 * pad, 40f);
            y += 128f + ScreenKit.Lip + 14f;

            // Porównanie wariantów.
            y += Para(c, "Comparison", m, y, cw, 17f, Palette.Text, FontWeight.ExtraBold) + 8f;
            string r1, r2, r3; double v1, v2, v3;
            if (sim.HasCrash)
            {
                r1 = "Your result"; v1 = sim.Value;
                r2 = "If you had panic-sold"; v2 = InvestSim.PanicSell(sim.Instrument);
                r3 = "If you had held to the end"; v3 = InvestSim.HoldToEnd(sim.Instrument);
            }
            else
            {
                r1 = "Your result (deposit)"; v1 = sim.Value;
                r2 = "Deposit in real terms, after inflation"; v2 = InvestSim.Real(InvestSim.HoldToEnd(Instrument.Deposit), InvestConfig.Months);
                r3 = "ETF held to the end"; v3 = InvestSim.HoldToEnd(Instrument.Etf);
            }
            y += CompareRow(c, y, r1, v1, true) + 8f;
            y += CompareRow(c, y, r2, v2, false) + 8f;
            y += CompareRow(c, y, r3, v3, false) + 16f;

            // Wnioski.
            y += Para(c, "Takeaways", m, y, cw, 17f, Palette.Text, FontWeight.ExtraBold) + 6f;
            y += Para(c, Lessons(), m, y, cw, 15f, Palette.Text) + 16f;

            // Za co są gwiazdki.
            y += Para(c, "How you earn stars", m, y, cw, 17f, Palette.Text, FontWeight.ExtraBold) + 8f;
            y += StarRow(c, y, "You finished the simulation", "A whole year, month by month", sim.FirstSteps) + 8f;
            y += StarRow(c, y, "You didn't panic", sim.HasCrash ? "You held or bought more in the crash" : "A deposit has no crash – try the ETF or bonds", sim.NoPanic) + 8f;
            y += StarRow(c, y, "Scam-aware", "You reported the suspicious text", sim.ScamAware) + 8f;

            MainButton(bar, "CONTINUE", () => { if (onFinish != null) onFinish(); });
            Button(bar, m, 36f + ButtonH + 10f, cw, "PLAY AGAIN", false, () => { sim = null; completed = false; Go(Stage.Choose); });
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

        int Stars() { return (sim.FirstSteps ? 1 : 0) + (sim.NoPanic ? 1 : 0) + (sim.ScamAware ? 1 : 0); }

        // Zdobyta gwiazdka wskakuje z obrotem (jak po teście w QuizScreen).
        void PopStar(RectTransform rt, float size, int i)
        {
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition += new Vector2(size * 0.5f, -size * 0.5f);
            rt.localScale = Vector3.zero;
            var fx = screen.GetComponent<QuizFx>();
            if (fx == null) fx = screen.gameObject.AddComponent<QuizFx>();
            float delay = 0.35f + i * 0.28f;
            fx.Add(delay, 0.01f, t => { }, Haptics.Tap);
            fx.Add(delay, 0.45f, t =>
            {
                float k = QuizFx.BackOut(t, 2.6f);
                rt.localScale = new Vector3(k, k, 1f);
                rt.localRotation = Quaternion.Euler(0f, 0f, -25f * (1f - t));
            }, null);
        }

        float StarRow(RectTransform c, float y, string title, string desc, bool got)
        {
            const float h = 58f, icon = 34f;
            var card = LipCard.Create(c, "Badge", m, y, cw, h, got ? Palette.CardFill : Palette.LockedFill, got ? Palette.CardEdge : Palette.LockedEdge,
                got ? Palette.CardLip : Palette.LockedLip, 14f, 3f, 2f);
            var ic = Ui.Icon("Star", card.Content, IconId.Star, got ? Palette.Accent : Palette.LockedEdge, icon);
            Ui.Place(ic.rectTransform, 12f, (h - 3f - icon) * 0.5f, icon, icon);
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
                s = "The deposit paid sure interest, but inflation ate almost all of the gain – in real terms you have what you started with. Safe doesn't mean your money grows.";
            else if (sim.Sold)
                s = "Selling in the dip turned a paper loss into a real one. If you had held, you'd have " + Money(InvestSim.HoldToEnd(sim.Instrument)) +
                    ". An investor's worst enemy is emotion, not drops.";
            else if (sim.BoughtMore)
                s = "You bought more when it was cheap – after the rebound that money grew the most. The drop was an opportunity, not the end of the world.";
            else if (sim.Instrument == Instrument.Etf)
                s = "You rode out a drop of about 15% and the market made up the loss. Swings are the price of higher potential – time matters, not single months.";
            else
                s = "Bonds swung little and gave a moderate return. Lower risk means more peace of mind, but also lower potential.";
            if (sim.ScamClicked) s += " And the text with a link cost you " + Money(InvestConfig.ScamLoss) + " – a bank never sends messages like that.";
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
            return i == Instrument.Etf ? "Global ETF" : i == Instrument.Bonds ? "Bonds" : "Deposit";
        }

        // 1,045.30 € – zapis angielski (przecinek tysięcy, kropka dziesiętna), niezależnie od ustawień telefonu.
        public static string Money(double v)
        {
            string s = Math.Abs(v).ToString("0.00", CultureInfo.InvariantCulture);
            int dot = s.IndexOf('.');
            string whole = s.Substring(0, dot), frac = s.Substring(dot + 1);
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < whole.Length; i++)
            {
                if (i > 0 && (whole.Length - i) % 3 == 0) sb.Append(',');
                sb.Append(whole[i]);
            }
            return (v < 0 ? "-" : "") + sb + "." + frac + " €";
        }

        static string PctSigned(double v)
        {
            double p = Math.Round(v * 1000.0) / 10.0;   // jedno miejsce po przecinku
            string s = Math.Abs(p).ToString("0.0", CultureInfo.InvariantCulture);
            return (p > 0 ? "+" : p < 0 ? "−" : "") + s + "%";
        }
    }
}

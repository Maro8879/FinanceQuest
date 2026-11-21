using System;
using System.Collections.Generic;

namespace FinanceQuest
{
    // Mini gra „Pierwsze 12 miesięcy po zakupie” – sama logika, bez UI (osobny assembly, żeby dało się ją testować).
    // Wszystko deterministyczne: te same wybory zawsze dają ten sam wynik, więc lekcja zawsze „się udaje”.

    public enum Instrument { Etf, Bonds, Deposit }
    public enum CrashChoice { None, Sell, Hold, BuyMore }

    // Liczby do gry w jednym miejscu – zmieniasz tu, gra liczy się sama.
    public static class InvestConfig
    {
        public const double Start = 1000.0;          // wirtualna wpłata
        public const double TopUp = 100.0;           // „Dokupuję za 100 €”
        public const double ScamLoss = 100.0;        // kliknięcie w link z SMS-a
        public const int Months = 12;
        public const double InflationMonthly = 0.25; // % – ok. 3% rocznie
        public const double DepositMonthly = 0.25;   // % – lokata ok. 3% rocznie

        // Miesięczne zmiany w %. ETF: dno ok. −15% w 5. miesiącu, koniec ok. +4,5%.
        public static readonly double[] Etf = { 1.2, 0.8, -2.5, -9.0, -6.5, 3.0, 5.5, 3.5, 3.0, 1.5, 3.0, 2.0 };
        public static readonly double[] Bonds = { 0.3, 0.3, 0.2, -0.6, 0.1, 0.4, 0.4, 0.3, 0.3, 0.3, 0.4, 0.3 };

        // Miesiące wydarzeń (po zmianie wartości w tym miesiącu).
        public const int CrashMonth1 = 4, CrashMonth2 = 5, DividendMonth = 7, ScamMonth = 8;

        public static double[] Path(Instrument i)
        {
            if (i == Instrument.Etf) return Etf;
            if (i == Instrument.Bonds) return Bonds;
            var d = new double[Months];
            for (int k = 0; k < Months; k++) d[k] = DepositMonthly;
            return d;
        }
    }

    public class InvestSim
    {
        public readonly Instrument Instrument;
        public int Month { get; private set; }            // 0 = tuż po zakupie, 12 = koniec
        public double Value { get; private set; }         // wartość portfela gracza
        public double Paid { get; private set; }          // ile w sumie wpłacił (1000 + dokupienia)
        public bool Sold { get; private set; }            // sprzedał – reszta gry w gotówce, bez wzrostu
        public int SoldMonth { get; private set; }
        public bool BoughtMore { get; private set; }
        public bool ScamDone { get; private set; }
        public bool ScamClicked { get; private set; }
        public CrashChoice Choice1 { get; private set; }  // decyzja w 1. spadku
        public CrashChoice Choice2 { get; private set; }  // decyzja w 2. spadku

        // Historia na wykres: [0] = start, [m] = koniec miesiąca m.
        public readonly List<double> History = new List<double>();
        // „Gdybyś trzymał” – sam instrument od 1000 €, bez Twoich decyzji (przerywana linia po sprzedaży).
        public readonly List<double> HoldHistory = new List<double>();

        readonly double[] path;

        public InvestSim(Instrument instrument)
        {
            Instrument = instrument;
            path = InvestConfig.Path(instrument);
            Value = Paid = InvestConfig.Start;
            History.Add(Value);
            HoldHistory.Add(Value);
        }

        public bool Finished { get { return Month >= InvestConfig.Months; } }
        public bool HasCrash { get { return Instrument != Instrument.Deposit; } }

        // Zmiana od wpłaty (np. -0.15 = -15%) – liczona od tego, co naprawdę wpłacił, także po dokupieniu.
        public double ChangeFromPaid { get { return Paid > 0 ? Value / Paid - 1.0 : 0.0; } }

        // Ile ta kwota jest warta w dzisiejszych pieniądzach (po inflacji od początku gry).
        public double RealValue { get { return Real(Value, Month); } }

        public static double Real(double value, int month)
        {
            return value / Math.Pow(1.0 + InvestConfig.InflationMonthly / 100.0, month);
        }

        public void NextMonth()
        {
            if (Finished) return;
            double r = path[Month] / 100.0;
            Month++;
            if (!Sold) Value *= 1.0 + r;                       // po sprzedaży gotówka stoi w miejscu
            History.Add(Value);
            HoldHistory.Add(HoldHistory[HoldHistory.Count - 1] * (1.0 + r));
        }

        // Decyzja w spadku (miesiąc 4 i 5). Druga decyzja ma sens tylko, gdy gracz jeszcze trzyma.
        public void Decide(CrashChoice c)
        {
            if (!HasCrash || Sold) return;
            if (Choice1 == CrashChoice.None) Choice1 = c; else Choice2 = c;
            if (c == CrashChoice.Sell) { Sold = true; SoldMonth = Month; }
            if (c == CrashChoice.BuyMore) { Value += InvestConfig.TopUp; Paid += InvestConfig.TopUp; BoughtMore = true; }
            History[History.Count - 1] = Value;   // punkt na wykresie od razu pokazuje dokupienie
        }

        // SMS od „banku”. Kliknięcie = strata z portfela (wpłata się nie zmienia, więc spada wynik).
        public void Scam(bool clicked)
        {
            if (ScamDone) return;
            ScamDone = true;
            ScamClicked = clicked;
            if (clicked) Value = Math.Max(0.0, Value - InvestConfig.ScamLoss);
            History[History.Count - 1] = Value;
        }

        // Wynik „gdybyś trzymał do końca”: sam instrument, 1000 € bez decyzji.
        public static double HoldToEnd(Instrument i)
        {
            double v = InvestConfig.Start;
            foreach (var r in InvestConfig.Path(i)) v *= 1.0 + r / 100.0;
            return v;
        }

        // Wynik „gdybyś sprzedał w panice”: sprzedaż w najgłębszym dołku (2. spadek), potem gotówka.
        public static double PanicSell(Instrument i)
        {
            var p = InvestConfig.Path(i);
            double v = InvestConfig.Start;
            for (int k = 0; k < InvestConfig.CrashMonth2; k++) v *= 1.0 + p[k] / 100.0;
            return v;
        }

        // Odznaki.
        public bool NoPanic { get { return HasCrash && !Sold && Choice1 != CrashChoice.None; } }
        public bool ScamAware { get { return ScamDone && !ScamClicked; } }
        public bool FirstSteps { get { return Finished; } }
    }
}

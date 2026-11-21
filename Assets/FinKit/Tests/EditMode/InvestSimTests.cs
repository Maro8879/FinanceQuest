using NUnit.Framework;

namespace FinanceQuest
{
    // Testy liczenia portfela w mini grze. Uruchamianie: Window → General → Test Runner → EditMode → Run All.
    public class InvestSimTests
    {
        static InvestSim Play(Instrument i, int months)
        {
            var s = new InvestSim(i);
            for (int k = 0; k < months; k++) s.NextMonth();
            return s;
        }

        [Test]
        public void Etf_HeldToEnd_EndsAboutPlus4_5Percent()
        {
            var s = Play(Instrument.Etf, 12);
            Assert.That(s.Value, Is.EqualTo(1045.37).Within(0.01));
            Assert.That(s.ChangeFromPaid, Is.EqualTo(0.0454).Within(0.0005));
            Assert.That(s.History.Count, Is.EqualTo(13));
        }

        [Test]
        public void Etf_BottomInMonth5_IsAboutMinus15Percent()
        {
            var s = Play(Instrument.Etf, 5);
            Assert.That(s.Value, Is.EqualTo(846.25).Within(0.01));
            Assert.That(InvestSim.PanicSell(Instrument.Etf), Is.EqualTo(846.25).Within(0.01));
        }

        [Test]
        public void Sell_FreezesValue_HoldLineKeepsGoing()
        {
            var s = Play(Instrument.Etf, 4);
            s.Decide(CrashChoice.Sell);
            double cash = s.Value;
            for (int k = 0; k < 8; k++) s.NextMonth();
            Assert.That(s.Value, Is.EqualTo(cash).Within(1e-9));
            Assert.That(s.HoldHistory[12], Is.EqualTo(InvestSim.HoldToEnd(Instrument.Etf)).Within(1e-9));
            Assert.IsTrue(s.Sold);
            Assert.IsFalse(s.NoPanic);
        }

        [Test]
        public void BuyMore_AddsMoneyAndGrowsWithMarket()
        {
            var s = Play(Instrument.Etf, 4);
            double before = s.Value;
            s.Decide(CrashChoice.BuyMore);
            Assert.That(s.Value, Is.EqualTo(before + 100).Within(1e-9));
            Assert.That(s.Paid, Is.EqualTo(1100).Within(1e-9));
            // Wykres w %: dokupienie nie zamienia spadku w „wzrost” – wynik nadal pod kreską.
            Assert.That(s.PctAt(4), Is.EqualTo((before + 100) / 1100 - 1).Within(1e-9));
            Assert.IsTrue(s.PctAt(4) < -0.08);
            s.NextMonth();   // -6.5%
            Assert.That(s.Value, Is.EqualTo((before + 100) * 0.935).Within(1e-6));
            Assert.IsTrue(s.NoPanic);
        }

        [Test]
        public void Scam_Click_Loses100_Report_LosesNothing()
        {
            var a = Play(Instrument.Bonds, 8);
            double v = a.Value;
            a.Scam(true);
            Assert.That(a.Value, Is.EqualTo(v - 100).Within(1e-9));
            Assert.IsFalse(a.ScamAware);

            var b = Play(Instrument.Bonds, 8);
            b.Scam(false);
            Assert.That(b.Value, Is.EqualTo(v).Within(1e-9));
            Assert.IsTrue(b.ScamAware);
        }

        [Test]
        public void Deposit_RealValueStaysAboutStart()
        {
            var s = Play(Instrument.Deposit, 12);
            Assert.That(s.Value, Is.EqualTo(1030.42).Within(0.01));
            Assert.That(s.RealValue, Is.EqualTo(1000).Within(0.01));
            s.Decide(CrashChoice.Sell);   // lokata nie ma spadków – decyzja nic nie robi
            Assert.IsFalse(s.Sold);
        }
    }
}

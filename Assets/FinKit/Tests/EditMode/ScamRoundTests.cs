using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace FinanceQuest
{
    // Testy mini gry „Prawda czy Pułapka?”. Uruchamianie: Window → General → Test Runner → EditMode → Run All.
    public class ScamRoundTests
    {
        static ScamRound Play(params bool[] right)
        {
            var r = new ScamRound(ScamDeck.Default());
            foreach (bool ok in right)
            {
                var good = r.Current.Answer;
                r.Answer(ok ? good : (good == ScamAnswer.Safe ? ScamAnswer.Trap : ScamAnswer.Safe));
                r.Next();
            }
            return r;
        }

        [Test]
        public void DefaultDeck_HasSixCards_FourTrapsTwoSafe()
        {
            var d = ScamDeck.Default();
            Assert.That(d.Count, Is.EqualTo(6));
            Assert.That(d.FindAll(c => c.Answer == ScamAnswer.Trap).Count, Is.EqualTo(4));
        }

        [Test]
        public void AllCorrect_ScoreSix_ThreeStars_NoMistakes()
        {
            var r = Play(true, true, true, true, true, true);
            Assert.That(r.Finished, Is.True);
            Assert.That(r.Score, Is.EqualTo(6));
            Assert.That(r.Stars(), Is.EqualTo(3));
            Assert.That(r.Mistakes(), Is.Empty);
        }

        [Test]
        public void TwoWrong_ScoreFour_TwoStars_MistakesListed()
        {
            var r = Play(true, false, true, true, false, true);
            Assert.That(r.Score, Is.EqualTo(4));
            Assert.That(r.Stars(), Is.EqualTo(2));
            Assert.That(r.Mistakes().Count, Is.EqualTo(2));
            Assert.That(r.Mistakes()[0].Type, Is.EqualTo("bank"));
        }

        [Test]
        public void AllWrong_StillFinishes_OneStar()
        {
            var r = Play(false, false, false, false, false, false);
            Assert.That(r.Finished, Is.True);
            Assert.That(r.Score, Is.EqualTo(0));
            Assert.That(r.Stars(), Is.EqualTo(1));
        }

        [Test]
        public void StarsFor_AllScoresOfSix()
        {
            int[] expected = { 1, 1, 1, 1, 2, 2, 3 };
            for (int s = 0; s <= 6; s++) Assert.That(ScamRound.StarsFor(s, 6), Is.EqualTo(expected[s]), "wynik " + s);
        }

        [Test]
        public void CannotAnswerTwice_OrSkipWithoutAnswer()
        {
            var r = new ScamRound(ScamDeck.Default());
            Assert.Throws<InvalidOperationException>(() => r.Next());
            r.Answer(ScamAnswer.Trap);
            Assert.Throws<InvalidOperationException>(() => r.Answer(ScamAnswer.Safe));
        }

        [Test]
        public void Shuffle_KeepsAllCards()
        {
            var r = new ScamRound(ScamDeck.Default(), true, 42);
            var seen = new HashSet<string>();
            while (!r.Finished) { seen.Add(r.Current.Text); r.Answer(ScamAnswer.Trap); r.Next(); }
            Assert.That(seen.Count, Is.EqualTo(6));
        }
    }
}

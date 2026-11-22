using System;
using System.Collections.Generic;

namespace FinanceQuest
{
    // Mini gra „Swipe the Scam” – sama logika, bez UI (w assembly Sim, żeby dało się ją testować).
    // Gracz ocenia każdą kartę jako BEZPIECZNE albo PUŁAPKA. Bez żyć i bez przegranej – zawsze przechodzi wszystkie karty.

    public enum ScamAnswer { Safe, Trap }

    public class ScamCard
    {
        public string Type;      // sms | social | bank | article | product | call – decyduje tylko o wyglądzie karty
        public string From;      // nadawca / nagłówek („BANK-INFO”, „InvestAI · Sponsored”…)
        public string Text;
        public ScamAnswer Answer;
        public string Explain;
        public List<string> Flags = new List<string>();   // nazwy czerwonych flag – metki pod kartą po decyzji
        public List<string> Marks = new List<string>();   // dokładne fragmenty treści do podświetlenia po decyzji
    }

    public static class ScamDeck
    {
        public const string BadgeName = "Scam Hunter";

        // Ściągawka na podsumowaniu – najważniejsze czerwone flagi.
        public static readonly string[] CheatSheet =
        {
            "Guaranteed high profit with no risk",
            "Time pressure (“only 15 minutes!”)",
            "Short links and login requests by SMS",
            "Requests for remote access or a transfer to a “safe account”",
        };

        // Domyślne karty – gdy w edytorze treści nie dodano własnych.
        public static List<ScamCard> Default()
        {
            return new List<ScamCard>
            {
                Card("social", "InvestAI · Sponsored",
                    "Discover the secret AI algorithm! Invest €250 and get a guaranteed €1,500 profit in 7 days. Zero risk!",
                    ScamAnswer.Trap,
                    "Red flag! On a real market, high returns always come with risk. Nobody can guarantee 500% in a week.",
                    new[] { "guaranteed profit", "no risk" }, "guaranteed €1,500 profit", "Zero risk!"),
                Card("bank", "Your bank · Offer",
                    "6-month term deposit: fixed interest of 4.0% a year. Deposits protected up to €100,000.",
                    ScamAnswer.Safe,
                    "Good call! A standard, legal bank offer with a rate known in advance and a legal deposit guarantee.",
                    new string[0]),
                Card("sms", "BANK-INFO",
                    "Your account has been blocked for security reasons. Tap bit.ly/bank-verify-99 within 15 minutes to unlock access.",
                    ScamAnswer.Trap,
                    "Phishing and time pressure! A real bank never sends short login links by SMS or threatens to block you in 15 minutes.",
                    new[] { "shortened link", "time pressure" }, "bit.ly/bank-verify-99", "within 15 minutes"),
                Card("article", "News24 · Sponsored article",
                    "A famous TV presenter reveals in an interview: “The government wants to hide this platform that lets every citizen earn €500 a day.”",
                    ScamAnswer.Trap,
                    "Fake celebrity endorsement (deepfake)! Scammers misuse famous faces to win your trust.",
                    new[] { "celebrity face", "too good to be true" }, "A famous TV presenter", "earn €500 a day"),
                Card("product", "Product sheet · ETF",
                    "Global ETF covering the 1,500 largest companies in the world. Annual fee (TER): 0.20%. Historical average return: about 7–8% a year (no guaranteed profit).",
                    ScamAnswer.Safe,
                    "Fair offer! A low fee, broad diversification and honest info that profit isn't guaranteed are signs of a good product.",
                    new string[0]),
                Card("call", "Unknown number · “Security team”",
                    "I'm calling from the security team. Someone is trying to steal your money – install AnyDesk so I can move it to a safe account.",
                    ScamAnswer.Trap,
                    "Phone spoofing! No bank employee will ever ask you to install remote-desktop software or move money to a “safe account”.",
                    new[] { "remote access", "“safe account”" }, "install AnyDesk", "safe account"),
            };
        }

        static ScamCard Card(string type, string from, string text, ScamAnswer answer, string explain, string[] flags, params string[] marks)
        {
            return new ScamCard { Type = type, From = from, Text = text, Answer = answer, Explain = explain,
                Flags = new List<string>(flags), Marks = new List<string>(marks) };
        }
    }

    public class ScamRound
    {
        readonly List<ScamCard> cards;
        readonly List<bool> correct = new List<bool>();   // wynik każdej ocenionej karty, po kolei
        int index;
        bool answered;

        // shuffle: losowa kolejność (seed – żeby dało się to powtórzyć w testach).
        public ScamRound(List<ScamCard> cards, bool shuffle = false, int seed = 0)
        {
            if (cards == null || cards.Count == 0) throw new ArgumentException("Brak kart.");
            this.cards = new List<ScamCard>(cards);
            if (shuffle)
            {
                var rnd = new Random(seed);
                for (int i = this.cards.Count - 1; i > 0; i--)
                {
                    int j = rnd.Next(i + 1);
                    var t = this.cards[i]; this.cards[i] = this.cards[j]; this.cards[j] = t;
                }
            }
        }

        public int Count { get { return cards.Count; } }
        public int Index { get { return index; } }               // 0..Count (Count = koniec)
        public bool Finished { get { return index >= cards.Count; } }
        public bool Answered { get { return answered; } }         // bieżąca karta już oceniona (czeka na „Dalej”)
        public ScamCard Current { get { return Finished ? null : cards[index]; } }
        public bool LastCorrect { get { return correct.Count > 0 && correct[correct.Count - 1]; } }

        public int Score
        {
            get { int s = 0; foreach (var c in correct) if (c) s++; return s; }
        }

        // Ocena bieżącej karty. Zwraca, czy trafiona. Drugi raz tej samej karty ocenić się nie da.
        public bool Answer(ScamAnswer a)
        {
            if (Finished) throw new InvalidOperationException("Gra skończona.");
            if (answered) throw new InvalidOperationException("Ta karta jest już oceniona.");
            bool ok = cards[index].Answer == a;
            correct.Add(ok);
            answered = true;
            return ok;
        }

        // „Dalej” – tylko po ocenie (bez limitu czasu, gracz sam decyduje).
        public void Next()
        {
            if (!answered) throw new InvalidOperationException("Najpierw oceń kartę.");
            answered = false;
            index++;
        }

        public List<ScamCard> Mistakes()
        {
            var list = new List<ScamCard>();
            for (int i = 0; i < correct.Count; i++) if (!correct[i]) list.Add(cards[i]);
            return list;
        }

        // Gwiazdki: komplet = 3, co najmniej 2/3 = 2, mniej = 1 (za samo ukończenie – gra uczy, nie karze).
        public static int StarsFor(int score, int count)
        {
            if (count <= 0) return 1;
            if (score >= count) return 3;
            if (score * 3 >= count * 2) return 2;
            return 1;
        }

        public int Stars() { return StarsFor(Score, Count); }

        // Komunikat na podsumowaniu – zachęcający, bez oceniania.
        public static string Verdict(int score, int count)
        {
            if (score >= count) return "Flawless! No scam will catch you out.";
            if (score * 3 >= count * 2) return "Sharp eye! You already know the main safety rules.";
            return "Good start! Scammers are creative – check the cheat sheet and try again.";
        }
    }
}

using System;
using System.Collections.Generic;

namespace FinanceQuest
{
    // Mini gra „Prawda czy Pułapka?” – sama logika, bez UI (w assembly Sim, żeby dało się ją testować).
    // Gracz ocenia każdą kartę jako BEZPIECZNE albo PUŁAPKA. Bez żyć i bez przegranej – zawsze przechodzi wszystkie karty.

    public enum ScamAnswer { Safe, Trap }

    public class ScamCard
    {
        public string Type;      // sms | social | bank | article | product | call – decyduje tylko o wyglądzie karty
        public string From;      // nadawca / nagłówek („BANK-INFO”, „Nieznany numer”…)
        public string Text;
        public ScamAnswer Answer;
        public string Explain;
        public List<string> Flags = new List<string>();
    }

    public static class ScamDeck
    {
        public const string BadgeName = "Łowca Scamu";

        // Ściągawka na podsumowaniu – najważniejsze czerwone flagi.
        public static readonly string[] CheatSheet =
        {
            "Gwarantowany wysoki zysk bez ryzyka",
            "Presja czasu („tylko 15 minut!”)",
            "Skrócone linki i prośby o logowanie z SMS-a",
            "Prośba o zdalny dostęp lub przelew na „konto techniczne”",
        };

        // Domyślne karty – gdy w edytorze treści nie dodano własnych.
        public static List<ScamCard> Default()
        {
            return new List<ScamCard>
            {
                Card("social", "InvestAI · Sponsorowane",
                    "Odkryj sekretny algorytm AI! Zainwestuj 250 € i odbierz gwarantowane 1 500 € zysku w 7 dni. Bez ryzyka!",
                    ScamAnswer.Trap,
                    "Red flag! Na legalnym rynku wysoki zysk zawsze oznacza ryzyko. Nikt nie może zagwarantować 500% zwrotu w tydzień.",
                    "gwarantowany zysk", "bez ryzyka"),
                Card("bank", "Twój bank · oferta",
                    "Lokata terminowa na 6 miesięcy: stałe oprocentowanie 4,0% w skali roku. Depozyt chroniony do 100 000 €.",
                    ScamAnswer.Safe,
                    "Dobry wybór! To standardowa, legalna oferta bankowa ze znanym z góry oprocentowaniem i ustawową gwarancją kapitału."),
                Card("sms", "BANK-INFO",
                    "Twoje konto zostało zablokowane ze względów bezpieczeństwa. Kliknij link bit.ly/bank-verify-99 w ciągu 15 minut, aby odblokować dostęp.",
                    ScamAnswer.Trap,
                    "Phishing i presja czasu! Prawdziwy bank nigdy nie wysyła skróconych linków do logowania SMS-em i nie straszy blokadą w 15 minut.",
                    "skrócony link", "presja czasu"),
                Card("article", "Portal Newsy24 · Artykuł sponsorowany",
                    "Znany prezenter telewizyjny ujawnia w wywiadzie: „Rząd chce ukryć tę platformę, dzięki której każdy obywatel zarabia 500 € dziennie”.",
                    ScamAnswer.Trap,
                    "Fałszywy wizerunek (deepfake)! Oszuści bezprawnie wykorzystują zdjęcia znanych osób, aby wzbudzić Twoje zaufanie.",
                    "wizerunek celebryty"),
                Card("product", "Karta produktu · ETF",
                    "Globalny ETF na 1 500 największych firm świata. Opłata roczna (TER): 0,20%. Historyczny średni zwrot: ok. 7–8% rocznie (brak gwarancji zysku).",
                    ScamAnswer.Safe,
                    "Uczciwa oferta! Niska opłata za zarządzanie, szeroka dywersyfikacja i uczciwa informacja o braku gwarancji zysku to cechy dobrego produktu."),
                Card("call", "Nieznany numer · „Dział bezpieczeństwa”",
                    "Dzwonię z działu bezpieczeństwa. Ktoś próbuje ukraść Twoje środki – zainstaluj aplikację AnyDesk, abym mógł przelać je na konto techniczne.",
                    ScamAnswer.Trap,
                    "Spoofing telefoniczny! Żaden pracownik banku nigdy nie prosi o instalację programu do zdalnego pulpitu ani o przelew na „konto techniczne”.",
                    "zdalny dostęp", "konto techniczne"),
            };
        }

        static ScamCard Card(string type, string from, string text, ScamAnswer answer, string explain, params string[] flags)
        {
            return new ScamCard { Type = type, From = from, Text = text, Answer = answer, Explain = explain, Flags = new List<string>(flags) };
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
            if (score >= count) return "Bezbłędnie! Żadna pułapka Cię nie zaskoczy.";
            if (score * 3 >= count * 2) return "Świetny refleks! Znasz już główne zasady bezpieczeństwa.";
            return "Dobry początek! Oszuści są pomysłowi – przejrzyj ściągawkę i spróbuj jeszcze raz.";
        }
    }
}

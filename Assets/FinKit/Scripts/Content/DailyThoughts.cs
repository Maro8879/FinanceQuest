using System;

namespace FinanceQuest
{
    // Cytat dnia nad trofeami – zdanie o pieniądzach i jego autor.
    // Wybór zależy od dnia roku, więc przez cały dzień widać tę samą myśl, a jutro inną.
    public static class DailyThoughts
    {
        static readonly string[] All =
        {
            "Nie chodzi o to, ile zarabiasz, tylko ile z tego zostaje.",
            "Najpierw zapłać sobie, potem rachunkom.",
            "Mały, regularny krok bije wielki zryw raz w roku.",
            "Poduszka finansowa to spokój, którego nie widać na koncie.",
            "Procent składany lubi cierpliwych.",
            "Nie wkładaj wszystkich jajek do jednego koszyka.",
            "Dług kosztuje więcej, niż pokazuje rata.",
            "Budżet to nie kajdany, tylko mapa.",
            "Ryzyko, którego nie rozumiesz, jest największe.",
            "Wydatki rosną razem z dochodami – chyba że ich pilnujesz.",
            "Opłaty po 1% potrafią zjeść lata zysków.",
            "Kupuj tylko to, co rozumiesz.",
            "Emocje to najdroższy doradca inwestycyjny.",
            "Cel bez kwoty i daty to tylko marzenie.",
            "Każda złotówka powinna mieć swoje zadanie.",
            "Rynek rośnie schodami, a spada windą.",
            "Oszczędzanie to nawyk, nie kwota.",
            "Czas na rynku znaczy więcej niż idealny moment wejścia.",
            "Tani kredyt też trzeba spłacić.",
            "Zanim zainwestujesz, zbuduj fundament.",
            "Inflacja po cichu podgryza pieniądze w skarpecie.",
            "Nie goń za modą – trzymaj się planu.",
            "Lepiej rozumieć jedno narzędzie niż mieć dziesięć.",
            "Najtańszy kredyt to ten, którego nie musisz brać.",
            "Wiedza o pieniądzach procentuje całe życie.",
            "Rozkładaj ryzyko, a nie tylko pieniądze.",
            "Patrz na RRSO, nie tylko na oprocentowanie.",
            "Plan na gorsze dni robi się w dobre dni.",
            "Mądre finanse bywają nudne – i bardzo dobrze.",
            "Każdy wielki portfel zaczynał od pierwszej złotówki.",
        };

        // TESTOWO – wyłącz przed wydaniem: wszystkie zdania wyżej są nasze, a nie tej osoby.
        // Przed wydaniem trzeba podmienić je na prawdziwe cytaty z prawdziwymi autorami.
        const string PlaceholderAuthor = "Warren Buffett";

        // Ile razy w tej sesji kliknięto w kartę – każde kliknięcie pokazuje kolejny cytat.
        static int skip;

        public static string Today()
        {
            return All[(DateTime.Now.DayOfYear + skip) % All.Length];
        }

        public static void Next() { skip++; }

        public static string TodayAuthor()
        {
            return PlaceholderAuthor;
        }
    }
}

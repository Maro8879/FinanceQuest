using System.Collections.Generic;

namespace FinanceQuest
{
    // Tymczasowa treść do etapu 1. Zniknie, gdy dojdą pliki JSON i importer.
    public static class DemoContent
    {
        public static List<CourseData> Build()
        {
            var list = new List<CourseData>();

            list.Add(Course("invest", "Podstawy inwestowania", "1CB0F6", IconId.Chart,
                Ch("basics", "Czym jest inwestowanie", "1CB0F6", 5),
                Ch("risk", "Zarządzanie ryzykiem", "1CB0F6", 5),
                Ch("divers", "Dywersyfikacja", "2EB872", 4),
                Ch("compound", "Procent składany", "CE82FF", 4)));

            list.Add(Course("platform", "Obsługa platformy inwestycyjnej", "CE82FF", IconId.Phone,
                Ch("account", "Zakładanie konta", "CE82FF", 4),
                Ch("orders", "Rodzaje zleceń", "1CB0F6", 4),
                Ch("fees", "Opłaty i podatki", "FF4B4B", 4)));

            list.Add(Course("budget", "Budżet domowy", "2EB872", IconId.Wallet,
                Ch("income", "Przychody i wydatki", "2EB872", 4),
                Ch("plan", "Plan na miesiąc", "1CB0F6", 4),
                Ch("save", "Poduszka finansowa", "CE82FF", 4),
                Ch("goals", "Cele oszczędnościowe", "FF4B4B", 4),
                Ch("habits", "Dobre nawyki", "00C2A8", 4)));

            list.Add(Course("credit", "Karty i kredyty", "FF4B4B", IconId.Card,
                Ch("cards", "Karta debetowa i kredytowa", "FF4B4B", 4),
                Ch("rrso", "RRSO i koszt kredytu", "1CB0F6", 4),
                Ch("bik", "Historia kredytowa", "CE82FF", 4),
                Ch("debt", "Wychodzenie z długu", "2EB872", 4)));

            // Id leveli dostają prefiks kursu i rozdziału, żeby były unikalne.
            foreach (var c in list)
                foreach (var ch in c.Chapters)
                {
                    ch.Id = c.Id + "/" + ch.Id;
                    foreach (var l in ch.Levels) l.Id = ch.Id + "/" + l.Number;
                }
            return list;
        }

        static CourseData Course(string id, string title, string hex, IconId icon, params ChapterData[] chapters)
        {
            var c = new CourseData { Id = id, Title = title, Color = Palette.Hex(hex), Icon = icon };
            c.Chapters.AddRange(chapters);
            return c;
        }

        static ChapterData Ch(string id, string title, string hex, int levels)
        {
            var ch = new ChapterData { Id = id, Title = title, Color = Palette.Hex(hex) };
            // Powtarzalny wzór typów, żeby było widać wszystkie ikony; ostatni level to test.
            var pattern = new[] { LevelKind.Lesson, LevelKind.MiniGame, LevelKind.Task };
            for (int i = 0; i < levels; i++)
            {
                var kind = i == levels - 1 ? LevelKind.Test : pattern[i % pattern.Length];
                ch.Levels.Add(new LevelData { Number = i + 1, Kind = kind });
            }
            return ch;
        }
    }
}

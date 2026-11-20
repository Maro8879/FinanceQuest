using System.Collections.Generic;

namespace FinanceQuest
{
    // Tymczasowa treść do etapu 1. Zniknie, gdy dojdą pliki JSON i importer.
    public static class DemoContent
    {
        public static List<CourseData> Build()
        {
            var list = new List<CourseData>();

            list.Add(Course("invest", "Podstawy", "Podstawy inwestowania", Palette.Petrol, IconId.Chart, CourseArt.Portfolio,
                Ch("basics", "Czym jest inwestowanie", Palette.Petrol, 5),
                Ch("risk", "Zarządzanie ryzykiem", Palette.Petrol, 5),
                Ch("divers", "Dywersyfikacja", Palette.Moss, 4),
                Ch("compound", "Procent składany", Palette.Mustard, 4)));

            list.Add(Course("short", "Trading", "Inwestowanie krótkoterminowe", Palette.Slate, IconId.Chart, CourseArt.Magnifier,
                Ch("trading", "Czym jest trading", Palette.Slate, 4),
                Ch("ta", "Analiza techniczna", Palette.Petrol, 4),
                Ch("position", "Zarządzanie pozycją", Palette.Brick, 4)));

            list.Add(Course("platform", "Platforma", "Obsługa platformy inwestycyjnej", Palette.Moss, IconId.Phone, CourseArt.Laptop,
                Ch("account", "Zakładanie konta", Palette.Moss, 4),
                Ch("orders", "Rodzaje zleceń", Palette.Petrol, 4),
                Ch("fees", "Opłaty i podatki", Palette.Brick, 4)));

            list.Add(Course("budget", "Budżet", "Budżet domowy", Palette.Mustard, IconId.Wallet, CourseArt.Wallet,
                Ch("income", "Przychody i wydatki", Palette.Mustard, 4),
                Ch("plan", "Plan na miesiąc", Palette.Petrol, 4),
                Ch("save", "Poduszka finansowa", Palette.Moss, 4),
                Ch("goals", "Cele oszczędnościowe", Palette.Brick, 4),
                Ch("habits", "Dobre nawyki", Palette.Slate, 4)));

            list.Add(Course("credit", "Kredyty", "Karty i kredyty", Palette.Brick, IconId.Card, CourseArt.Cards,
                Ch("cards", "Karta debetowa i kredytowa", Palette.Brick, 4),
                Ch("rrso", "RRSO i koszt kredytu", Palette.Petrol, 4),
                Ch("bik", "Historia kredytowa", Palette.Slate, 4),
                Ch("debt", "Wychodzenie z długu", Palette.Moss, 4)));

            // Id leveli dostają prefiks kursu i rozdziału, żeby były unikalne.
            foreach (var c in list)
                foreach (var ch in c.Chapters)
                {
                    ch.Id = c.Id + "/" + ch.Id;
                    foreach (var l in ch.Levels) l.Id = ch.Id + "/" + l.Number;
                }
            return list;
        }

        static CourseData Course(string id, string shortName, string title, UnityEngine.Color color, IconId icon, CourseArt art, params ChapterData[] chapters)
        {
            var c = new CourseData { Id = id, ShortName = shortName, Title = title, Color = color, Icon = icon, Art = art };
            c.Chapters.AddRange(chapters);
            return c;
        }

        static ChapterData Ch(string id, string title, UnityEngine.Color color, int levels)
        {
            var ch = new ChapterData { Id = id, Title = title, Color = color };
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

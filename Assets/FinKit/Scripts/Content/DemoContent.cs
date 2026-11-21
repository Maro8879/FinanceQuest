using System.Collections.Generic;

namespace FinanceQuest
{
    // Tymczasowa treść do etapu 1. Zniknie, gdy dojdą pliki JSON i importer.
    public static class DemoContent
    {
        public static List<CourseData> Build()
        {
            var list = new List<CourseData>();

            list.Add(Course("invest", "Basics", "Investing basics", Palette.Petrol, IconId.Chart, CourseArt.Portfolio,
                Ch("basics", "What is investing", Palette.Petrol, 5),
                Ch("risk", "Risk management", Palette.Petrol, 5),
                Ch("divers", "Diversification", Palette.Moss, 4),
                Ch("compound", "Compound interest", Palette.Mustard, 4)));

            list.Add(Course("short", "Trading", "Short-term investing", Palette.Slate, IconId.Chart, CourseArt.Magnifier,
                Ch("trading", "What is trading", Palette.Slate, 4),
                Ch("ta", "Technical analysis", Palette.Petrol, 4),
                Ch("position", "Position management", Palette.Brick, 4)));

            list.Add(Course("platform", "Platform", "Using an investment platform", Palette.Moss, IconId.Phone, CourseArt.Laptop,
                Ch("account", "Opening an account", Palette.Moss, 4),
                Ch("orders", "Order types", Palette.Petrol, 4),
                Ch("fees", "Fees and taxes", Palette.Brick, 4)));

            list.Add(Course("budget", "Budget", "Household budget", Palette.Mustard, IconId.Wallet, CourseArt.Wallet,
                Ch("income", "Income and expenses", Palette.Mustard, 4),
                Ch("plan", "Monthly plan", Palette.Petrol, 4),
                Ch("save", "Emergency fund", Palette.Moss, 4),
                Ch("goals", "Savings goals", Palette.Brick, 4),
                Ch("habits", "Good habits", Palette.Slate, 4)));

            list.Add(Course("credit", "Credit", "Cards and loans", Palette.Brick, IconId.Card, CourseArt.Cards,
                Ch("cards", "Debit and credit cards", Palette.Brick, 4),
                Ch("rrso", "APR and the cost of credit", Palette.Petrol, 4),
                Ch("bik", "Credit history", Palette.Slate, 4),
                Ch("debt", "Getting out of debt", Palette.Moss, 4)));

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

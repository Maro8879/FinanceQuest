using System;

namespace FinanceQuest
{
    // Cytat nad trofeami – prawdziwe, znane zdanie o pieniądzach i inwestowaniu z prawdziwym autorem.
    // Każdy cytat sprawdzony u źródła (list do akcjonariuszy, książka, wystąpienie) – nie dopisujemy „ładnych” cytatów bez źródła.
    // Start zależy od dnia roku, potem karta sama przechodzi przez kolejne.
    public static class DailyThoughts
    {
        static readonly string[,] All =
        {
            { "Be fearful when others are greedy, and greedy when others are fearful.", "Warren Buffett" },
            { "Price is what you pay; value is what you get.", "Warren Buffett" },
            { "If you aren't willing to own a stock for ten years, don't even think about owning it for ten minutes.", "Warren Buffett" },
            { "Our favorite holding period is forever.", "Warren Buffett" },
            { "It's far better to buy a wonderful company at a fair price than a fair company at a wonderful price.", "Warren Buffett" },
            { "For investors as a whole, returns decrease as motion increases.", "Warren Buffett" },
            { "Don't look for the needle in the haystack. Just buy the haystack!", "John C. Bogle" },
            { "Time is your friend; impulse is your enemy.", "John C. Bogle" },
            { "The investor's chief problem \u2014 and even his worst enemy \u2014 is likely to be himself.", "Benjamin Graham" },
            { "Know what you own, and know why you own it.", "Peter Lynch" },
            { "In the stock market, the most important organ is the stomach. It's not the brain.", "Peter Lynch" },
            { "The four most dangerous words in investing are: 'This time it's different.'", "Sir John Templeton" },
            { "Bull markets are born on pessimism, grow on skepticism, mature on optimism, and die on euphoria.", "Sir John Templeton" },
            { "The big money is not in the buying and the selling, but in the waiting.", "Charlie Munger" },
            { "Beware of little expenses; a small leak will sink a great ship.", "Benjamin Franklin" },
        };

        // Ile razy w tej sesji karta przeszła dalej (kliknięcie albo automatyczna zmiana).
        static int skip;

        static int Index { get { return (DateTime.Now.DayOfYear + skip) % All.GetLength(0); } }

        public static string Today() { return All[Index, 0]; }

        public static string TodayAuthor() { return All[Index, 1]; }

        public static void Next() { skip++; }
    }
}

using UnityEngine;

namespace FinanceQuest
{
    // Wszystkie kolory w jednym miejscu, żeby tryb ciemny (później) podmieniał je w jednym punkcie.
    public static class Palette
    {
        public static readonly Color Background = Hex("FFFFFF");
        public static readonly Color Text = Hex("3C3C3C");
        public static readonly Color TextMuted = Hex("8A8A8A");
        public static readonly Color TextLocked = Hex("AFAFAF");
        public static readonly Color Brand = Hex("2EB872");

        // Zwykła biała karta: szara ramka i szara warga.
        public static readonly Color CardFill = Hex("FFFFFF");
        public static readonly Color CardEdge = Hex("E5E5E5");
        public static readonly Color CardLip = Hex("E5E5E5");

        // Zablokowane = szare.
        public static readonly Color LockedFill = Hex("F2F2F2");
        public static readonly Color LockedEdge = Hex("E0E0E0");
        public static readonly Color LockedLip = Hex("E0E0E0");
        public static readonly Color LockedIcon = Hex("C8C8C8");

        // Złoto = ukończone.
        public static readonly Color Gold = Hex("FFC800");
        public static readonly Color GoldEdge = Hex("F0B400");
        public static readonly Color GoldLip = Hex("D99E00");

        // Pomarańczowy = aktualne miejsce i postęp.
        public static readonly Color Orange = Hex("FF9600");
        public static readonly Color OrangeLip = Hex("CC7700");

        public static readonly Color Track = Hex("E5E5E5");
        // Tor paska na kolorowej karcie: przyciemnienie zamiast szarości, żeby pasował do każdego koloru rozdziału.
        public static readonly Color TrackOnColor = new Color(0f, 0f, 0f, 0.18f);
        public static readonly Color WhiteSoft = new Color(1f, 1f, 1f, 0.8f);

        public static Color Hex(string hex)
        {
            Color c;
            ColorUtility.TryParseHtmlString("#" + hex, out c);
            return c;
        }

        // Warga ma być tym samym kolorem, tylko ciemniejszym – liczymy ją, żeby nie dopisywać drugiego koloru do każdego rozdziału.
        public static Color Darker(Color c, float k = 0.82f)
        {
            return new Color(c.r * k, c.g * k, c.b * k, c.a);
        }
    }
}

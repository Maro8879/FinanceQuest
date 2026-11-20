using UnityEngine;

namespace FinanceQuest
{
    // Wszystkie kolory w jednym miejscu, żeby tryb ciemny (później) podmieniał je w jednym punkcie.
    public static class Palette
    {
        // Paleta „petrol + żółć” (inspirowana Commerzbankiem): stonowane, ciepłe kolory.
        public static readonly Color Background = Hex("F5F3EE");   // ciepła złamana biel
        public static readonly Color Text = Hex("1D2B2F");
        public static readonly Color TextMuted = Hex("6F7A7C");
        public static readonly Color TextLocked = Hex("A39E93");
        public static readonly Color Brand = Hex("0E4A55");        // petrol

        // Kolory kursów i rozdziałów – jeden zestaw, żeby całość była spójna.
        public static readonly Color Petrol = Hex("0E4A55");
        public static readonly Color Moss = Hex("3F6B5C");
        public static readonly Color Mustard = Hex("9C7A2E");
        public static readonly Color Brick = Hex("8E4A36");
        public static readonly Color Slate = Hex("4A5D73");

        // Zwykła biała karta: ciepła szara ramka i warga.
        public static readonly Color CardFill = Hex("FFFFFF");
        public static readonly Color CardEdge = Hex("DAD5CA");
        public static readonly Color CardLip = Hex("DAD5CA");

        // Zablokowane = szare (ciepła szarość pasująca do tła).
        public static readonly Color LockedFill = Hex("E7E3DA");
        public static readonly Color LockedEdge = Hex("DAD5CA");
        public static readonly Color LockedLip = Hex("DAD5CA");
        public static readonly Color LockedIcon = Hex("A39E93");

        // Żółć = aktualne miejsce i postęp. Na żółtym piszemy ciemno, bo biały byłby nieczytelny.
        public static readonly Color Accent = Hex("F2C200");
        public static readonly Color AccentLip = Hex("B59100");
        public static readonly Color AccentDark = Hex("CDA400");   // ikony na białym tle
        public static readonly Color OnAccent = Hex("1D2B2F");

        public static readonly Color Track = Hex("DAD5CA");
        // Tor paska na kolorowej karcie: przyciemnienie zamiast szarości, żeby pasował do każdego koloru.
        public static readonly Color TrackOnColor = new Color(0f, 0f, 0f, 0.22f);
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

using TMPro;
using UnityEngine;

namespace FinanceQuest
{
    public enum FontWeight { Regular, SemiBold, ExtraBold }

    // Czcionka Rubik (licencja SIL OFL – Resources/Fonts/Rubik-OFL.txt).
    // Prawdziwe grubości zamiast sztucznego pogrubienia TMP, bo sztuczne „rozlewa” litery.
    // Font asset tworzymy w locie (dynamiczny atlas): nie trzeba nic klikać w Font Asset Creatorze,
    // a do atlasu trafiają tylko znaki, które faktycznie się pojawią.
    public static class Fonts
    {
        static readonly string[] Files = { "Fonts/Rubik-Regular", "Fonts/Rubik-SemiBold", "Fonts/Rubik-ExtraBold" };
        static readonly TMP_FontAsset[] cache = new TMP_FontAsset[3];
        static readonly bool[] missing = new bool[3];

        public static TMP_FontAsset Get(FontWeight weight)
        {
            int i = (int)weight;
            if (cache[i] != null) return cache[i];
            if (missing[i]) return null;

            var font = Resources.Load<Font>(Files[i]);
            if (font == null)
            {
                // Bez pliku zostaje domyślna czcionka TMP – aplikacja działa dalej, tylko inaczej wygląda.
                missing[i] = true;
                Debug.LogWarning("Finance Quest: brak czcionki " + Files[i] + ".ttf w Resources.");
                return null;
            }
            var asset = TMP_FontAsset.CreateFontAsset(font);
            asset.name = font.name + " (dynamic)";
            cache[i] = asset;
            return asset;
        }
    }
}

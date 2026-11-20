using System;
using System.Collections.Generic;
using UnityEngine;

namespace FinanceQuest
{
    // Wspólna pamięć podręczna wszystkich rysowanych grafik.
    // Najpierw szukamy wypieczonego PNG w Resources/Baked (szybko), a dopiero gdy go nie ma – rysujemy w kodzie.
    // Dzięki temu na telefonie nie liczymy dużych rysunków przy starcie, a w Editorze zmiany widać od razu po „Bake”.
    public static class DrawnPictures
    {
        public const string Folder = "Baked";

        // Ustawiane tylko przez narzędzie „Bake Drawn Pictures”: tekstury zostają czytelne, żeby dało się je zapisać do PNG.
        public static bool KeepReadable;

        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static Sprite Get(string key, Func<Sprite> draw)
        {
            Sprite s;
            if (cache.TryGetValue(key, out s) && s != null) return s;
            if (!KeepReadable) s = Resources.Load<Sprite>(Folder + "/" + key);
            if (s == null) s = draw();
            cache[key] = s;
            return s;
        }

        public static IEnumerable<KeyValuePair<string, Sprite>> Cached { get { return cache; } }

        public static void ClearCache() { cache.Clear(); }
    }
}

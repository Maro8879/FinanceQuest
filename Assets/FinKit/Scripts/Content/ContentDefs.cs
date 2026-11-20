using System;
using System.Collections.Generic;

namespace FinanceQuest
{
    // Format treści – dokładnie to, co leży w plikach JSON (pola nazwane jak klucze w JSON).
    // Zadanie jest „płaskie”: jedna klasa na wszystkie typy, a typ mówi, których pól używać.
    // Dzięki temu wystarczy wbudowany JsonUtility i nic się nie gubi przy serializacji assetu.

    [Serializable] public class OptionDef { public string text; public bool correct; }
    [Serializable] public class PairDef { public string left; public string right; }
    [Serializable] public class ZoneDef { public string id; public string label; public float x, y, w, h; }   // x,y,w,h: 0..1 względem obrazka
    [Serializable] public class ZoneItemDef { public string text; public string zone; }
    [Serializable] public class SettingDef { public string key; public string value; }

    [Serializable]
    public class TaskDef
    {
        public string type;                                   // choice | tiles | pairs | zones | gap
        public string prompt;
        // choice: odpowiedzi; multi = kilka poprawnych
        public List<OptionDef> options = new List<OptionDef>();
        public bool multi;
        // tiles: zdanie do ułożenia z kafelków + słowa-pułapki
        public string answer;
        public List<string> extra = new List<string>();
        // pairs: pary do połączenia
        public List<PairDef> pairs = new List<PairDef>();
        // zones: obrazek ze strefami i elementy do przeciągnięcia
        public string image;
        public List<ZoneDef> zones = new List<ZoneDef>();
        public List<ZoneItemDef> items = new List<ZoneItemDef>();
        // gap: tekst z luką „___”; answers = poprawne wersje (pierwsza główna, reszta alternatywy)
        public string text;
        public List<string> answers = new List<string>();
        public string mode;                                   // type = wpisywanie, choose = wybór z listy
        public List<string> wrong = new List<string>();       // gap/choose: błędne propozycje
        // wspólne: wyjaśnienie po złej odpowiedzi i ciekawostka po dobrej
        public string explain;
        public string fact;
    }

    // Blok treści na stronie lekcji. Jedna klasa na wszystkie rodzaje (jak w zadaniach) – typ mówi, których pól używać.
    // W tekście **słowo** = pogrubienie.
    [Serializable]
    public class BlockDef
    {
        public string type;                                   // heading | text | image | callout | list | divider | space
        public string text;                                   // heading, text, callout
        public string image;                                  // image: ścieżka w Resources bez rozszerzenia, np. Images/ryzyko
        public float width = 1f;                              // image: szerokość jako ułamek szerokości strony (0.3–1)
        public float maxHeight = 220f;                        // image: obrazek nie będzie wyższy niż tyle
        public string caption;                                // image: podpis pod obrazkiem
        public List<string> items = new List<string>();       // list: punkty listy
        public float size = 16f;                              // space: wysokość odstępu
    }

    [Serializable]
    public class PageDef
    {
        public List<BlockDef> blocks = new List<BlockDef>();
        // Stary format (jeden tekst + obrazek) – nadal czytany, gdy strona nie ma bloków.
        public string text;
        public string image;
    }

    [Serializable]
    public class MiniGameDef
    {
        public string game;
        public List<SettingDef> settings = new List<SettingDef>();
    }

    [Serializable]
    public class LevelDef
    {
        public string id;
        public string kind;                                   // lesson | minigame | task | test
        // lesson: najpierw strony (pages), potem – jeśli są – zadania sprawdzające (tasks).
        public string title;
        public List<PageDef> pages = new List<PageDef>();
        public MiniGameDef minigame = new MiniGameDef();
        public List<TaskDef> tasks = new List<TaskDef>();
    }

    // Kto i kiedy ostatnio zapisał plik – edytor pokazuje to, gdy pracujecie we dwóch.
    [Serializable] public class EditMeta { public string editedBy; public string editedAt; }

    [Serializable]
    public class ChapterDef
    {
        public int format = 1;
        public string id;
        public string title;
        public string color;                                  // #RRGGBB
        public EditMeta meta = new EditMeta();
        public List<LevelDef> levels = new List<LevelDef>();
    }

    [Serializable]
    public class CourseDef
    {
        public int format = 1;
        public string id;
        public string title;
        public string shortName;
        public string color;
        public string art;                                    // nazwa z CourseArt
        public string icon;                                   // nazwa z IconId
        public int order;
        public List<string> chapters = new List<string>();    // kolejność rozdziałów (id = nazwa pliku)
        public EditMeta meta = new EditMeta();
    }

    [Serializable]
    public class CourseBundle
    {
        public CourseDef course = new CourseDef();
        public List<ChapterDef> chapters = new List<ChapterDef>();
    }
}

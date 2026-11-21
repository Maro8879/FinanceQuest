using System;
using System.Collections.Generic;
using UnityEngine;

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
    // Wiersz tabelki (JsonUtility nie umie list w listach, więc wiersz to osobna klasa).
    [Serializable] public class TableRowDef { public List<string> cells = new List<string>(); public float height; }   // height: min. wysokość wiersza, 0 = sama się dobiera

    [Serializable]
    public class BlockDef
    {
        public string type;                                   // heading | text | image | video | callout | list | divider | space | table
        public string text;                                   // heading, text, callout
        public string image;                                  // image: ścieżka względem folderu kursu, np. obrazki/ryzyko.png
        public float width = 1f;                              // image: szerokość jako ułamek szerokości strony (0.3–1)
        public float maxHeight = 220f;                        // image: obrazek nie będzie wyższy niż tyle
        public string caption;                                // image, video: podpis pod obrazkiem / filmikiem
        public string video;                                  // video: ścieżka względem folderu kursu, np. filmy/ryzyko.mp4
        public string videoSub;                               // video: ta sama wersja z napisami (opcjonalnie) – w apce przycisk CC
        public List<string> items = new List<string>();       // list: punkty listy
        public float size = 16f;                              // space: wysokość odstępu
        public List<TableRowDef> rows = new List<TableRowDef>();   // table: wiersze (pierwszy = nagłówek, gdy header)
        public bool header = true;                            // table: pierwszy wiersz to nagłówek
        public List<float> colWidths = new List<float>();     // table: szerokość kolumn w % szerokości tabelki, 0 / brak = sama się dobiera
        public string style = "a";                            // table: a = karta z ciemnym nagłówkiem, b = bez ramki, same linie
        // Obrazek podpięty przez importer (nie ma go w JSON). Dzięki temu obrazki leżą obok treści,
        // a do gry trafiają razem z bazą treści – bez kopiowania do Resources.
        public Sprite sprite;
        // Filmik podpięty przez importer – tak samo jak obrazek (MP4 leży obok treści, nie w Resources).
        public UnityEngine.Video.VideoClip clip;
        public UnityEngine.Video.VideoClip clipSub;
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
        // „Prawda czy Pułapka?”: własne karty z edytora (puste = karty domyślne) i losowa kolejność.
        public List<ScamCardDef> cards = new List<ScamCardDef>();
        public bool shuffle;
    }

    // Karta mini gry „Prawda czy Pułapka?”. type: sms | social | bank | article | product | call; answer: safe | trap.
    [Serializable]
    public class ScamCardDef
    {
        public string type;
        public string from;
        public string text;
        public string answer;
        public string explain;
        public List<string> flags = new List<string>();
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

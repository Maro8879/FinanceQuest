using System.Collections.Generic;
using UnityEngine;

namespace FinanceQuest
{
    public enum LevelKind { Lesson, MiniGame, Task, Test }

    public enum IconId { Home, Back, Sound, SoundOff, Bell, BellOff, Language, Sun, Gear, Lock, Check, Book, Gamepad, Task, Star, Moon, Chart, Phone, Wallet, Card, Spark, Bulb, Exclaim, Vibrate, VibrateOff, Play, Pause, Close, Skip }

    // Stan widoczny na mapie: decyduje o kolorze (złoto / pomarańcz / szarość).
    public enum ItemState { Done, Current, Locked }

    public class LevelData
    {
        public string Id;      // unikalny w całej aplikacji, bo po nim zapisujemy postęp
        public int Number;
        public LevelKind Kind;
        public string Title;     // z pliku treści (może być puste – wtedy „Level N”)
        public LevelDef Def;     // pełna treść levelu (strony, mini gra, zadania) – używana przez ekran levelu
    }

    public class ChapterData
    {
        public string Id;
        public string Title;
        public Color Color;
        public List<LevelData> Levels = new List<LevelData>();
    }

    public class CourseData
    {
        public string Id;
        public string Title;
        public string ShortName;   // podpis pucharu na półce
        public Color Color;
        public IconId Icon;
        public CourseArt Art;   // ilustracja na karcie w menu
        public List<ChapterData> Chapters = new List<ChapterData>();

        public int LevelCount
        {
            get { int n = 0; foreach (var c in Chapters) n += c.Levels.Count; return n; }
        }
    }

    public static class ContentText
    {
        public static string KindLabel(LevelKind k)
        {
            switch (k)
            {
                case LevelKind.Lesson: return "Lesson";
                case LevelKind.MiniGame: return "Mini game";
                case LevelKind.Task: return "Exercise";
                default: return "Chapter test";
            }
        }

        public static IconId KindIcon(LevelKind k)
        {
            switch (k)
            {
                case LevelKind.Lesson: return IconId.Book;
                case LevelKind.MiniGame: return IconId.Gamepad;
                case LevelKind.Task: return IconId.Task;
                default: return IconId.Star;
            }
        }

        // Polska odmiana liczebników (1 rozdział, 2 rozdziały, 5 rozdziałów).
        public static string Plural(int n, string one, string few, string many)
        {
            if (n == 1) return n + " " + one;
            int d = n % 10, t = n % 100;
            if (d >= 2 && d <= 4 && (t < 12 || t > 14)) return n + " " + few;
            return n + " " + many;
        }
    }
}

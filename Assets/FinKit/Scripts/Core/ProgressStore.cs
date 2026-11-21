using System.Collections.Generic;
using UnityEngine;

namespace FinanceQuest
{
    // Trzyma tylko listę ukończonych leveli – reszta (odblokowanie, procenty) jest z niej liczona,
    // więc nie ma ryzyka, że zapisane stany się rozjadą.
    public class ProgressStore
    {
        public const string PrefsKey = "fq.progress.v1";
        readonly HashSet<string> done = new HashSet<string>();
        // Gwiazdki z testów i zadań (1–3) – zapamiętujemy najlepszy wynik każdego levelu.
        public const string StarsKey = "fq.stars.v1";
        readonly Dictionary<string, int> stars = new Dictionary<string, int>();

        public static ProgressStore Load()
        {
            var s = new ProgressStore();
            string raw = PlayerPrefs.GetString(PrefsKey, "");
            foreach (var id in raw.Split('|'))
                if (id.Length > 0) s.done.Add(id);
            foreach (var entry in PlayerPrefs.GetString(StarsKey, "").Split('|'))
            {
                int eq = entry.LastIndexOf('=');
                int n;
                if (eq > 0 && int.TryParse(entry.Substring(eq + 1), out n)) s.stars[entry.Substring(0, eq)] = n;
            }
            return s;
        }

        void Save()
        {
            PlayerPrefs.SetString(PrefsKey, string.Join("|", new List<string>(done).ToArray()));
            PlayerPrefs.Save();
        }

        public bool IsDone(LevelData l) { return done.Contains(l.Id); }

        // Najlepszy wynik levelu w gwiazdkach; -1 = level bez wyniku (np. zwykła lekcja).
        public int Stars(LevelData l)
        {
            int s;
            return stars.TryGetValue(l.Id, out s) ? s : -1;
        }

        // Czy level daje gwiazdki: ma zadania, które pokazuje ekran testu, albo jest gotową mini grą (InvestGame: 1–3 gwiazdki).
        public static bool GivesStars(LevelData l)
        {
            if (l.Kind == LevelKind.MiniGame)
                return l.Def != null && l.Def.minigame != null && l.Def.minigame.game == InvestGame.Id;
            return l.Def != null && l.Def.tasks != null && l.Def.tasks.Count > 0;
        }

        // Zdobyte gwiazdki / wszystkie możliwe (po 3 na każdy level z zadaniami) – dla rozdziału i całego kursu.
        public void StarTotals(ChapterData ch, out int got, out int max)
        {
            got = max = 0;
            foreach (var l in ch.Levels)
            {
                if (!GivesStars(l)) continue;
                max += 3;
                got += Mathf.Max(0, Stars(l));
            }
        }

        public void StarTotals(CourseData c, out int got, out int max)
        {
            got = max = 0;
            foreach (var ch in c.Chapters)
            {
                int g, m;
                StarTotals(ch, out g, out m);
                got += g; max += m;
            }
        }

        // Zapisuje wynik, tylko jeśli jest lepszy od dotychczasowego.
        public void SetStars(LevelData l, int value)
        {
            if (value <= Stars(l)) return;
            stars[l.Id] = value;
            SaveStars();
        }

        void SaveStars()
        {
            var parts = new List<string>();
            foreach (var kv in stars) parts.Add(kv.Key + "=" + kv.Value);
            PlayerPrefs.SetString(StarsKey, string.Join("|", parts.ToArray()));
            PlayerPrefs.Save();
        }

        // Trofeum „Start”: przyznawane za pierwszy ukończony level (rozpoczęcie edukacji finansowej).
        public bool Started { get { return done.Count > 0; } }

        // Ustawienia → reset: kasuje ukończone levele i gwiazdki we wszystkich kursach.
        public void ResetAll()
        {
            done.Clear();
            Save();
            stars.Clear();
            SaveStars();
        }

        public void Complete(LevelData l)
        {
            if (done.Add(l.Id)) Save();
        }

        public int DoneCount(ChapterData ch)
        {
            int n = 0;
            foreach (var l in ch.Levels) if (done.Contains(l.Id)) n++;
            return n;
        }

        public bool IsComplete(ChapterData ch) { return DoneCount(ch) == ch.Levels.Count; }

        public float ChapterProgress(ChapterData ch)
        {
            return ch.Levels.Count == 0 ? 0f : (float)DoneCount(ch) / ch.Levels.Count;
        }

        public bool IsCourseComplete(CourseData c) { return CourseProgress(c) >= 0.9999f; }

        public float CourseProgress(CourseData c)
        {
            int total = 0, d = 0;
            foreach (var ch in c.Chapters) { total += ch.Levels.Count; d += DoneCount(ch); }
            return total == 0 ? 0f : (float)d / total;
        }

        // Następny rozdział otwiera się dopiero po ukończeniu poprzedniego.
        public ItemState ChapterState(CourseData c, int index)
        {
            var ch = c.Chapters[index];
            if (IsComplete(ch)) return ItemState.Done;
            if (index == 0 || IsComplete(c.Chapters[index - 1])) return ItemState.Current;
            return ItemState.Locked;
        }

        // W rozdziale aktualny jest pierwszy nieukończony level; dalsze czekają.
        public ItemState LevelState(CourseData c, int chapterIndex, int levelIndex)
        {
            var ch = c.Chapters[chapterIndex];
            if (IsDone(ch.Levels[levelIndex])) return ItemState.Done;
            if (ChapterState(c, chapterIndex) == ItemState.Locked) return ItemState.Locked;
            for (int i = 0; i < ch.Levels.Count; i++)
                if (!IsDone(ch.Levels[i])) return i == levelIndex ? ItemState.Current : ItemState.Locked;
            return ItemState.Locked;
        }
    }
}

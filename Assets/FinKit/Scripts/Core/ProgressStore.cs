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

        public static ProgressStore Load()
        {
            var s = new ProgressStore();
            string raw = PlayerPrefs.GetString(PrefsKey, "");
            foreach (var id in raw.Split('|'))
                if (id.Length > 0) s.done.Add(id);
            return s;
        }

        void Save()
        {
            PlayerPrefs.SetString(PrefsKey, string.Join("|", new List<string>(done).ToArray()));
            PlayerPrefs.Save();
        }

        public bool IsDone(LevelData l) { return done.Contains(l.Id); }

        // Trofeum „Start”: przyznawane za pierwszy ukończony level (rozpoczęcie edukacji finansowej).
        public bool Started { get { return done.Count > 0; } }

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

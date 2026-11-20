using System;
using System.Collections.Generic;
using UnityEngine;

namespace FinanceQuest
{
    // Zamienia zaimportowaną treść (ContentDatabase) na modele, których używają ekrany.
    // Gdy bazy jeszcze nie ma (np. przed pierwszym importem), zwraca null – wtedy aplikacja bierze treść demo.
    public static class ContentLoader
    {
        public static List<CourseData> Load()
        {
            var db = Resources.Load<ContentDatabase>(ContentDatabase.ResourcePath);
            if (db == null || db.courses == null || db.courses.Count == 0) return null;

            var list = new List<CourseData>();
            foreach (var b in db.courses)
            {
                var cd = b.course;
                var course = new CourseData
                {
                    Id = cd.id,
                    Title = cd.title,
                    ShortName = string.IsNullOrEmpty(cd.shortName) ? cd.title : cd.shortName,
                    Color = ParseColor(cd.color, Palette.Petrol),
                    Art = ParseEnum(cd.art, CourseArt.Portfolio),
                    Icon = ParseEnum(cd.icon, IconId.Chart),
                };
                foreach (var chd in b.chapters)
                {
                    var ch = new ChapterData { Id = course.Id + "/" + chd.id, Title = chd.title, Color = ParseColor(chd.color, course.Color) };
                    int n = 0;
                    foreach (var ld in chd.levels)
                    {
                        n++;
                        ch.Levels.Add(new LevelData
                        {
                            // Postęp zapisujemy po id levelu z pliku, więc zmiana kolejności nie psuje postępu.
                            Id = ch.Id + "/" + (string.IsNullOrEmpty(ld.id) ? n.ToString() : ld.id),
                            Number = n,
                            Kind = ParseKind(ld.kind),
                            Title = ld.title,
                            Def = ld,
                        });
                    }
                    course.Chapters.Add(ch);
                }
                list.Add(course);
            }
            return list;
        }

        static LevelKind ParseKind(string k)
        {
            switch (k)
            {
                case "minigame": return LevelKind.MiniGame;
                case "task": return LevelKind.Task;
                case "test": return LevelKind.Test;
                default: return LevelKind.Lesson;
            }
        }

        static Color ParseColor(string hex, Color fallback)
        {
            if (string.IsNullOrEmpty(hex)) return fallback;
            Color c;
            return ColorUtility.TryParseHtmlString(hex.StartsWith("#") ? hex : "#" + hex, out c) ? c : fallback;
        }

        static T ParseEnum<T>(string s, T fallback) where T : struct
        {
            T v;
            return !string.IsNullOrEmpty(s) && Enum.TryParse(s, true, out v) ? v : fallback;
        }
    }
}

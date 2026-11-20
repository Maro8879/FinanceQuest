using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FinanceQuest.EditorTools
{
    // Importer treści: pliki JSON z Assets/FinanceQuest/Content -> jeden asset ContentDatabase w Resources.
    // Działa sam po każdej zmianie JSON-a (np. po zapisie w edytorze HTML albo po „Pull” z GitHuba).
    // Układ plików: Content/<kurs>/course.json + Content/<kurs>/<rozdział>.json (jeden rozdział = jeden plik).
    public class ContentImporter : AssetPostprocessor
    {
        public const string ContentDir = "Assets/FinanceQuest/Content";
        const string DbDir = "Assets/FinanceQuest/Resources/Content";
        const string DbPath = DbDir + "/ContentDatabase.asset";
        static bool scheduled;

        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            bool touched = imported.Concat(deleted).Concat(moved).Concat(movedFrom).Any(IsContentJson);
            if (!touched || scheduled) return;
            // Przebudowa po zakończeniu importu – tworzenie assetów w trakcie importu bywa zawodne.
            scheduled = true;
            EditorApplication.delayCall += () => { scheduled = false; Rebuild(); };
        }

        static bool IsContentJson(string p)
        {
            return p.Replace('\\', '/').StartsWith(ContentDir + "/") && p.EndsWith(".json");
        }

        [MenuItem("Finance Quest/Import Content")]
        public static void Rebuild()
        {
            if (!Directory.Exists(ContentDir)) { Debug.LogWarning("Finance Quest: brak folderu " + ContentDir); return; }

            var bundles = new List<CourseBundle>();
            int warnings = 0;
            foreach (var dir in Directory.GetDirectories(ContentDir).OrderBy(d => d))
            {
                string coursePath = Path.Combine(dir, "course.json");
                if (!File.Exists(coursePath)) continue;
                var b = new CourseBundle { course = Read<CourseDef>(coursePath) };
                if (b.course == null) { warnings++; continue; }
                if (string.IsNullOrEmpty(b.course.id)) b.course.id = Path.GetFileName(dir);

                // Rozdziały w kolejności z course.json; pliki spoza listy dopinamy na końcu (żeby nic nie zginęło).
                var files = Directory.GetFiles(dir, "*.json").Where(f => Path.GetFileName(f) != "course.json")
                    .ToDictionary(f => Path.GetFileNameWithoutExtension(f), f => f);
                var order = new List<string>(b.course.chapters ?? new List<string>());
                foreach (var id in files.Keys.OrderBy(k => k)) if (!order.Contains(id)) order.Add(id);

                foreach (var id in order)
                {
                    string f;
                    if (!files.TryGetValue(id, out f)) { Debug.LogWarning("Finance Quest: course.json w " + dir + " wskazuje brakujący rozdział „" + id + "”."); warnings++; continue; }
                    var ch = Read<ChapterDef>(f);
                    if (ch == null) { warnings++; continue; }
                    if (string.IsNullOrEmpty(ch.id)) ch.id = id;
                    warnings += Validate(ch, f);
                    b.chapters.Add(ch);
                }
                bundles.Add(b);
            }
            bundles = bundles.OrderBy(x => x.course.order).ThenBy(x => x.course.id).ToList();

            Directory.CreateDirectory(DbDir);
            var db = AssetDatabase.LoadAssetAtPath<ContentDatabase>(DbPath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<ContentDatabase>();
                AssetDatabase.CreateAsset(db, DbPath);
            }
            db.courses = bundles;
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            Debug.Log("Finance Quest: zaimportowano " + bundles.Count + " kursów, " + bundles.Sum(x => x.chapters.Count) +
                      " rozdziałów" + (warnings > 0 ? " – ostrzeżeń: " + warnings + " (szczegóły wyżej)" : "") + ".");
        }

        static T Read<T>(string path) where T : class
        {
            try { return JsonUtility.FromJson<T>(File.ReadAllText(path, Encoding.UTF8)); }
            catch (System.Exception e)
            {
                Debug.LogError("Finance Quest: nie da się odczytać " + path + " – " + e.Message);
                return null;
            }
        }

        // Te same reguły co w edytorze HTML – na wypadek ręcznej edycji pliku.
        static int Validate(ChapterDef ch, string file)
        {
            int w = 0;
            foreach (var l in ch.levels)
            {
                string where = file + " / " + (string.IsNullOrEmpty(l.title) ? l.id : l.title);
                if ((l.kind == "task" || l.kind == "test") && l.tasks.Count == 0) { Debug.LogWarning("Finance Quest: " + where + ": level bez zadań."); w++; }
                if (l.kind == "lesson" && l.pages.Count == 0) { Debug.LogWarning("Finance Quest: " + where + ": lekcja bez stron."); w++; }
                for (int i = 0; i < l.tasks.Count; i++)
                {
                    var t = l.tasks[i];
                    string tw = where + ", zadanie " + (i + 1) + ": ";
                    if (string.IsNullOrEmpty(t.prompt)) { Debug.LogWarning("Finance Quest: " + tw + "brak polecenia."); w++; }
                    if (t.type == "choice" && !t.options.Any(o => o.correct)) { Debug.LogWarning("Finance Quest: " + tw + "żadna odpowiedź nie jest poprawna."); w++; }
                    if (t.type == "pairs" && t.pairs.Count < 2) { Debug.LogWarning("Finance Quest: " + tw + "potrzebne co najmniej 2 pary."); w++; }
                    if (t.type == "gap" && (t.text == null || !t.text.Contains("___"))) { Debug.LogWarning("Finance Quest: " + tw + "tekst bez luki „___”."); w++; }
                    if (t.type == "gap" && t.answers.Count == 0) { Debug.LogWarning("Finance Quest: " + tw + "brak poprawnej odpowiedzi."); w++; }
                    if (t.type == "tiles" && string.IsNullOrEmpty(t.answer)) { Debug.LogWarning("Finance Quest: " + tw + "brak zdania do ułożenia."); w++; }
                    if (t.type == "zones" && t.items.Any(it => !t.zones.Any(z => z.id == it.zone))) { Debug.LogWarning("Finance Quest: " + tw + "element przypisany do nieistniejącej strefy."); w++; }
                }
            }
            return w;
        }
    }
}

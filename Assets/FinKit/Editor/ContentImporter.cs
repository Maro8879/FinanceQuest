using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;

namespace FinanceQuest.EditorTools
{
    // Importer treści: pliki JSON z Assets/FinKit/Content -> jeden asset ContentDatabase w Resources.
    // Działa sam po każdej zmianie JSON-a (np. po zapisie w edytorze HTML albo po „Pull” z GitHuba).
    // Układ plików: Content/<kurs>/course.json + Content/<kurs>/<rozdział>.json (jeden rozdział = jeden plik).
    public class ContentImporter : AssetPostprocessor
    {
        public const string ContentDir = "Assets/FinKit/Content";
        const string DbDir = "Assets/FinKit/Resources/Content";
        const string DbPath = DbDir + "/ContentDatabase.asset";
        static bool scheduled;

        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            bool touched = imported.Concat(deleted).Concat(moved).Concat(movedFrom).Any(IsContentFile);
            if (!touched || scheduled) return;
            // Przebudowa po zakończeniu importu – tworzenie assetów w trakcie importu bywa zawodne.
            scheduled = true;
            EditorApplication.delayCall += () => { scheduled = false; Rebuild(); };
        }

        // JSON-y, obrazki i filmiki w Content – nowy plik też musi przebudować bazę, żeby się podpiął.
        static bool IsContentFile(string p)
        {
            p = p.Replace('\\', '/');
            if (!p.StartsWith(ContentDir + "/")) return false;
            string ext = Path.GetExtension(p).ToLowerInvariant();
            return ext == ".json" || ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".mp4";
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
                    warnings += AttachImages(ch, dir.Replace('\\', '/'), b.course.id, f);
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

        // Podpina obrazki z folderu kursu (np. Content/invest/obrazki/x.png) jako Sprite do bloków.
        // Stare ścieżki do Resources (Images/x) zostają – gra wczyta je sama.
        static int AttachImages(ChapterDef ch, string courseDir, string courseId, string file)
        {
            int w = 0;
            foreach (var l in ch.levels)
                foreach (var page in l.pages)
                    foreach (var b in ContentBlocks.Of(page))
                    {
                        if (b.type == "video") { w += AttachVideo(b, courseDir, courseId, file); continue; }
                        if (b.type != "image") continue;
                        b.sprite = null;
                        b.image = ContentBlocks.NormalizeImagePath(b.image, courseId);
                        if (string.IsNullOrEmpty(b.image)) continue;
                        string assetPath = courseDir + "/" + b.image;
                        if (!File.Exists(assetPath))
                        {
                            if (Resources.Load<Sprite>(StripExt(b.image)) == null && Resources.Load<Texture2D>(StripExt(b.image)) == null)
                            { Debug.LogWarning("Finance Quest: " + file + ": nie ma obrazka " + assetPath + "."); w++; }
                            continue;
                        }
                        EnsureSprite(assetPath);
                        b.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                    }
            return w;
        }

        // Filmik z folderu kursu (np. Content/invest/filmy/x.mp4) jako VideoClip – działa jak obrazek, bez Resources.
        // Dwie wersje: bez napisów (video) i z napisami (videoSub) – każda może być pusta.
        static int AttachVideo(BlockDef b, string courseDir, string courseId, string file)
        {
            int w = 0;
            b.video = ContentBlocks.NormalizeImagePath(b.video, courseId);
            b.videoSub = ContentBlocks.NormalizeImagePath(b.videoSub, courseId);
            b.clip = LoadClip(b.video, courseDir, file, ref w);
            b.clipSub = LoadClip(b.videoSub, courseDir, file, ref w);
            return w;
        }

        static VideoClip LoadClip(string path, string courseDir, string file, ref int warnings)
        {
            if (string.IsNullOrEmpty(path)) return null;
            string assetPath = courseDir + "/" + path;
            var clip = AssetDatabase.LoadAssetAtPath<VideoClip>(assetPath);
            if (clip == null)
            {
                Debug.LogWarning("Finance Quest: " + file + ": nie ma filmiku " + assetPath + " (albo Unity jeszcze go nie zaimportowało).");
                warnings++;
            }
            return clip;
        }

        static string StripExt(string p)
        {
            string e = Path.GetExtension(p);
            if (p.StartsWith("Resources/")) p = p.Substring("Resources/".Length);
            return string.IsNullOrEmpty(e) ? p : p.Substring(0, p.Length - e.Length);
        }

        // Obrazek treści musi być Sprite'em. Bez mipmap (UI nie potrzebuje) i maks. 2048 px – lżej na telefonie.
        static void EnsureSprite(string assetPath)
        {
            var ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (ti == null || (ti.textureType == TextureImporterType.Sprite && !ti.mipmapEnabled && ti.maxTextureSize <= 2048)) return;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            if (ti.maxTextureSize > 2048) ti.maxTextureSize = 2048;
            ti.SaveAndReimport();
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
                for (int p = 0; p < l.pages.Count; p++)
                {
                    var blocks = ContentBlocks.Of(l.pages[p]);
                    string pw = where + ", strona " + (p + 1) + ": ";
                    if (blocks.Count == 0) { Debug.LogWarning("Finance Quest: " + pw + "pusta strona."); w++; }
                    foreach (var b in blocks)
                    {
                        if (b.type == "video" && string.IsNullOrEmpty(b.video) && string.IsNullOrEmpty(b.videoSub)) { Debug.LogWarning("Finance Quest: " + pw + "filmik bez ścieżki."); w++; }
                        if (b.type == "image" && string.IsNullOrEmpty(b.image)) { Debug.LogWarning("Finance Quest: " + pw + "obrazek bez ścieżki."); w++; }
                        if (b.type == "table" && (b.rows.Count == 0 || b.rows.TrueForAll(r => r.cells.TrueForAll(string.IsNullOrEmpty)))) { Debug.LogWarning("Finance Quest: " + pw + "pusta tabelka."); w++; }
                        if (b.type == "list" && b.items.Count == 0) { Debug.LogWarning("Finance Quest: " + pw + "pusta lista."); w++; }
                        if ((b.type == "text" || b.type == "heading" || b.type == "callout") && string.IsNullOrEmpty(b.text)) { Debug.LogWarning("Finance Quest: " + pw + "pusty blok tekstu."); w++; }
                    }
                }
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

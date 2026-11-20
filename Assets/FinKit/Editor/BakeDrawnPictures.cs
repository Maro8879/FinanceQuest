using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FinanceQuest.EditorTools
{
    // „Bake Drawn Pictures”: rysuje wszystkie grafiki z kodu i zapisuje je jako PNG w Resources/Baked.
    // Gra wczytuje potem gotowe pliki zamiast liczyć je przy starcie (duże rysunki trwają nawet ~1 s na telefonie).
    // Zapis bez kompresji, bez mipmap, z tym samym punktem zaczepienia, ramkami 9-slice i PPU co wersja z kodu –
    // żeby wypieczona grafika wyglądała i układała się identycznie.
    public static class BakeDrawnPictures
    {
        const string Dir = "Assets/FinKit/Resources/" + DrawnPictures.Folder;

        [MenuItem("Finance Quest/Bake Drawn Pictures")]
        static void Bake()
        {
            Directory.CreateDirectory(Dir);
            DrawnPictures.KeepReadable = true;
            DrawnPictures.ClearCache();
            var baked = new List<KeyValuePair<string, Sprite>>();
            try
            {
                DrawEverything();
                foreach (var kv in DrawnPictures.Cached)
                {
                    File.WriteAllBytes(Path.Combine(Dir, kv.Key + ".png"), kv.Value.texture.EncodeToPNG());
                    baked.Add(kv);
                }
                AssetDatabase.Refresh();
                foreach (var kv in baked) ApplyImportSettings(Dir + "/" + kv.Key + ".png", kv.Value);
                Debug.Log("Finance Quest: wypieczono " + baked.Count + " grafik do " + Dir);
            }
            finally
            {
                DrawnPictures.KeepReadable = false;
                DrawnPictures.ClearCache();
            }
        }

        // Po zmianie rysunków w kodzie: usuń stare PNG, żeby gra znów rysowała (albo upiecz od nowa).
        [MenuItem("Finance Quest/Clear Baked Pictures")]
        static void Clear()
        {
            if (AssetDatabase.IsValidFolder(Dir)) AssetDatabase.DeleteAsset(Dir);
            DrawnPictures.ClearCache();
            Debug.Log("Finance Quest: usunięto wypieczone grafiki.");
        }

        // Wywołuje każdą grafikę, której używa aplikacja – trafiają do pamięci podręcznej, skąd je zapisujemy.
        static void DrawEverything()
        {
            foreach (IconId id in System.Enum.GetValues(typeof(IconId))) ProceduralSprites.Icon(id);
            var unused = ProceduralSprites.Rounded; unused = ProceduralSprites.Ring; unused = ProceduralSprites.Candles;
            unused = WallStreetArt.Night;
            TrophyArt.Locked();
            TrophyArt.Earned(Palette.Accent, Palette.AccentLip, Palette.Brand);
            // Te same kursy co w grze: z zaimportowanych plików JSON, a gdy ich brak – demo.
            foreach (var course in ContentLoader.Load() ?? DemoContent.Build())
            {
                Illustrations.Get(course.Art, course.Color);
                TrophyArt.Earned(course.Color, Palette.Darker(course.Color, 0.78f), Palette.Accent);
            }
        }

        static void ApplyImportSettings(string path, Sprite source)
        {
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            if (imp == null) return;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.mipmapEnabled = false;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.filterMode = FilterMode.Bilinear;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.alphaIsTransparency = true;
            imp.npotScale = TextureImporterNPOTScale.None;
            imp.maxTextureSize = 2048;
            imp.spritePixelsPerUnit = source.pixelsPerUnit;

            var st = new TextureImporterSettings();
            imp.ReadTextureSettings(st);
            st.spriteMeshType = SpriteMeshType.FullRect;
            st.spriteAlignment = (int)SpriteAlignment.Custom;
            st.spritePivot = new Vector2(source.pivot.x / source.rect.width, source.pivot.y / source.rect.height);
            st.spriteBorder = source.border;
            st.spriteExtrude = 0;
            imp.SetTextureSettings(st);
            imp.SaveAndReimport();
        }
    }
}

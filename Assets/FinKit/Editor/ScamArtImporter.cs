using UnityEditor;

namespace FinanceQuest.EditorTools
{
    // Grafiki gry „Swipe the Scam” (Resources/ScamArt) importujemy jako sprite'y bez mipmap.
    // Rysunki (PNG) bez kompresji – mają ostre krawędzie, kompresja zrobiłaby brudne obwódki.
    // Zdjęcia (photo_*.jpg) z kompresją – bez niej każde zajmowałoby ponad 1 MB pamięci telefonu.
    public class ScamArtImporter : AssetPostprocessor
    {
        public override uint GetVersion() { return 2; }

        void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains("/Resources/ScamArt/")) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            bool photo = System.IO.Path.GetFileName(assetPath).StartsWith("photo_");
            ti.textureCompression = photo ? TextureImporterCompression.CompressedHQ : TextureImporterCompression.Uncompressed;
        }
    }
}

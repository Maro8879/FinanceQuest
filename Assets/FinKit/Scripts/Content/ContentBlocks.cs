using System.Collections.Generic;
using System.Text;

namespace FinanceQuest
{
    // Pomocniki dla bloków treści – wspólne dla ekranu lekcji i importera.
    public static class ContentBlocks
    {
        // Bloki strony. Stary format (sam tekst + obrazek) zamieniamy na bloki, żeby ekran znał tylko jeden sposób.
        public static List<BlockDef> Of(PageDef page)
        {
            if (page.blocks != null && page.blocks.Count > 0) return page.blocks;
            var list = new List<BlockDef>();
            if (!string.IsNullOrEmpty(page.text)) list.Add(new BlockDef { type = "text", text = page.text });
            if (!string.IsNullOrEmpty(page.image)) list.Add(new BlockDef { type = "image", image = page.image });
            return list;
        }

        // Ścieżka obrazka zawsze w postaci „obrazki/nazwa.png” (z ukośnikami /).
        // Pełne ścieżki z czyjegoś dysku (C:\Users\...\Content\kurs\obrazki\x.png) przycinamy do części po folderze kursu,
        // a stare ścieżki do Resources (…\Resources\Images\x.png) – do części po „Resources/”.
        public static string NormalizeImagePath(string path, string courseId)
        {
            if (string.IsNullOrEmpty(path)) return "";
            string p = path.Trim().Trim('"').Replace('\\', '/');
            if (!string.IsNullOrEmpty(courseId))
            {
                string marker = "/Content/" + courseId + "/";
                int i = p.IndexOf(marker, System.StringComparison.OrdinalIgnoreCase);
                if (i >= 0) return p.Substring(i + marker.Length);
            }
            int r = p.IndexOf("/Resources/", System.StringComparison.OrdinalIgnoreCase);
            if (r >= 0) return p.Substring(r + 1);
            return p.TrimStart('.', '/');
        }

        // **tekst** -> <b>tekst</b>. Pogrubienie rysuje prawdziwy Rubik ExtraBold (patrz Fonts), nie sztuczne pogrubienie.
        // Niesparowane ** zostawiamy jak jest, żeby literówka w treści nie pogrubiła całej reszty strony.
        public static string Rich(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder(s.Length + 16);
            int i = 0;
            while (i < s.Length)
            {
                int open = s.IndexOf("**", i, System.StringComparison.Ordinal);
                if (open < 0) { sb.Append(s, i, s.Length - i); break; }
                int close = s.IndexOf("**", open + 2, System.StringComparison.Ordinal);
                if (close < 0) { sb.Append(s, i, s.Length - i); break; }
                sb.Append(s, i, open - i).Append("<b>").Append(s, open + 2, close - open - 2).Append("</b>");
                i = close + 2;
            }
            return sb.ToString();
        }
    }
}

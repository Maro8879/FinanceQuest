using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FinanceQuest
{
    // Tabelka na stronie lekcji. Dwa wyglądy do wyboru w edytorze (pole „style”):
    //  a – biała karta z wargą, ciemny (petrolowy) nagłówek, co drugi wiersz lekko przyciemniony;
    //  b – bez ramki: nagłówek szarymi wersalikami, pod nim ciemna kreska, między wierszami jasne linie.
    // Pierwsza kolumna jest pogrubiona (to zwykle nazwy wierszy). W komórkach działa **pogrubienie**.
    public static class TableBlock
    {
        const float PadX = 8f, PadY = 9f, BodySize = 13.5f, HeadSize = 12.5f, MinCol = 44f;
        const float Lip = 5f, Border = 2f, Radius = 16f;

        // Zwraca wysokość tabelki.
        public static float Build(RectTransform parent, BlockDef b, float x, float y, float w)
        {
            var rows = new List<List<string>>();
            int cols = 0;
            foreach (var r in b.rows) { rows.Add(r.cells); cols = Mathf.Max(cols, r.cells.Count); }
            if (rows.Count == 0 || cols == 0) return 0f;
            bool a = b.style != "b";
            bool header = b.header && rows.Count > 1;

            // Karta (styl A) albo goły kontener (styl B). Komórki układamy we wnętrzu.
            float inner = a ? w - 2 * Border : w;
            RectTransform host;
            LipCard card = null;
            if (a)
            {
                card = LipCard.Create(parent, "Table", x, y, w, 100, Palette.CardFill, Palette.CardEdge, Palette.CardLip, Radius, Lip, Border);
                // Maska z zaokrąglonymi rogami – tło nagłówka i pasy wierszy nie wychodzą za kartę.
                var mask = Ui.Rounded("Mask", card.Face, Color.white, Radius - Border);
                Ui.Stretch(mask.rectTransform, Border, Border, Border, Border);
                mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
                host = mask.rectTransform;
            }
            else
            {
                host = Ui.Rect("Table", parent);
                Ui.Place(host, x, y, w, 100);
            }

            // Szerokości kolumn: ustawione w edytorze (%) albo według najdłuższego tekstu, dopasowane do szerokości tabelki.
            var widths = ColumnWidths(host, rows, cols, inner, header, b.colWidths);

            float cy = 0f;
            for (int r = 0; r < rows.Count; r++)
            {
                bool head = header && r == 0;
                var texts = new TextMeshProUGUI[cols];
                float rowH = 0f, cx = 0f;
                for (int c = 0; c < cols; c++)
                {
                    string cell = c < rows[r].Count ? rows[r][c] : "";
                    var t = CellText(host, cell, head, c == 0, a);
                    float th = Ui.TextHeight(t, widths[c] - 2 * PadX);
                    rowH = Mathf.Max(rowH, th);
                    texts[c] = t;
                }
                rowH += 2 * PadY;
                rowH = Mathf.Max(rowH, b.rows[r].height);   // wysokość z edytora to minimum – dłuższy tekst i tak się zmieści

                // Tło wiersza (pod tekstem – dodajemy je i przesuwamy na spód).
                Color bg = a ? (head ? Palette.Brand : ((r - (header ? 1 : 0)) % 2 == 0 ? Palette.CardFill : Palette.Background)) : Color.clear;
                if (bg.a > 0f)
                {
                    var rowBg = Ui.Rounded("RowBg", host, bg, 0.5f);
                    Ui.Place(rowBg.rectTransform, 0, cy, inner, rowH);
                    rowBg.transform.SetSiblingIndex(0);
                }
                for (int c = 0; c < cols; c++)
                {
                    Ui.Place(texts[c].rectTransform, cx + PadX, cy + PadY, widths[c] - 2 * PadX, rowH - 2 * PadY);
                    cx += widths[c];
                }
                cy += rowH;

                // Linie w stylu B: ciemna pod nagłówkiem, jasne między wierszami (bez linii pod ostatnim).
                if (!a && r < rows.Count - 1)
                {
                    var line = Ui.Rounded("Line", host, head ? Palette.Text : Palette.CardEdge, 1f);
                    Ui.Place(line.rectTransform, 0, cy, inner, 2f);
                    cy += 2f;
                }
            }

            if (a)
            {
                float h = cy + 2 * Border + Lip;
                ((RectTransform)card.transform).sizeDelta = new Vector2(w, h);
                return h;
            }
            host.sizeDelta = new Vector2(w, cy);
            return cy;
        }

        static TextMeshProUGUI CellText(Transform host, string text, bool head, bool firstCol, bool styleA)
        {
            TextMeshProUGUI t;
            if (head)
            {
                string s = styleA ? text : (text ?? "").ToUpperInvariant();
                t = Ui.Text("Head", host, ContentBlocks.Rich(s), styleA ? HeadSize : 12f, styleA ? Color.white : Palette.TextMuted, FontWeight.ExtraBold);
                if (!styleA) t.characterSpacing = 4f;
            }
            else
                t = Ui.Text("Cell", host, ContentBlocks.Rich(text), BodySize, Palette.Text, firstCol ? FontWeight.ExtraBold : FontWeight.Regular);
            t.richText = true;
            return t;
        }

        static float[] ColumnWidths(Transform host, List<List<string>> rows, int cols, float total, bool header, List<float> percent)
        {
            var pref = new float[cols];
            var probe = Ui.Text("Probe", host, "", BodySize, Palette.Text, FontWeight.ExtraBold);
            probe.richText = true;
            for (int r = 0; r < rows.Count; r++)
                for (int c = 0; c < cols && c < rows[r].Count; c++)
                {
                    probe.fontSize = header && r == 0 ? HeadSize : BodySize;
                    float pw = probe.GetPreferredValues(ContentBlocks.Rich(rows[r][c]), 10000f, 0f).x;
                    pref[c] = Mathf.Max(pref[c], pw + 2 * PadX);
                }
            Object.DestroyImmediate(probe.gameObject);

            var w = new float[cols];
            var auto = new List<int>();
            float fixedSum = 0f;
            for (int c = 0; c < cols; c++)
            {
                float p = percent != null && c < percent.Count ? percent[c] : 0f;
                if (p > 0f) { w[c] = total * p / 100f; fixedSum += w[c]; }
                else auto.Add(c);
            }

            // Kolumny z ustawionym % dostają swoją szerokość; jeśli razem nie wychodzi równo (np. 30% + 30%
            // bez kolumn „auto” albo ponad 100%), skalujemy je, żeby tabelka zawsze miała pełną szerokość.
            float room = total - MinCol * auto.Count;
            if (fixedSum > 0f && (auto.Count == 0 || fixedSum > room))
            {
                float k = (auto.Count == 0 ? total : Mathf.Max(0f, room)) / fixedSum;
                for (int c = 0; c < cols; c++) if (!auto.Contains(c)) w[c] *= k;
                fixedSum *= k;
            }
            if (auto.Count == 0) return w;

            // Pozostałe kolumny jak dotąd: mieści się – zapas po równo, nie mieści się – proporcjonalnie, ale nie węższe niż MinCol.
            float left = total - fixedSum, sum = 0f;
            foreach (int c in auto) { pref[c] = Mathf.Max(pref[c], MinCol); sum += pref[c]; }
            if (sum <= left)
            {
                float extra = (left - sum) / auto.Count;
                foreach (int c in auto) w[c] = pref[c] + extra;
            }
            else
            {
                float fixedMin = MinCol * auto.Count, rest = left - fixedMin, over = sum - fixedMin;
                foreach (int c in auto) w[c] = MinCol + (over > 0f ? (pref[c] - MinCol) / over * rest : rest / auto.Count);
            }
            return w;
        }
    }
}

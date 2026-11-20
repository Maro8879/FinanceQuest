using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FinanceQuest
{
    // Strony lekcji zbudowane z bloków (styl B – karty z „wargą”, jak reszta aplikacji).
    // Na dole kropki stron i przycisk DALEJ; ostatnia strona prowadzi do zadań albo kończy lekcję.
    // Zmiana strony przebudowuje tylko treść przewijanej listy – nagłówek i dolny pasek zostają.
    public class LessonPages
    {
        const float BarHeight = 100f;        // dolny pasek: kropki + przycisk
        const float ButtonH = 58f;           // razem z wargą
        const float TextSize = 16f;

        readonly List<PageDef> pages;
        readonly bool hasTasks;
        readonly Action onFinish;
        readonly float W;
        RectTransform content, dots;
        ScrollRect scroll;
        TextMeshProUGUI buttonLabel;
        int page;

        public static void Build(RectTransform screen, FinanceQuestApp app, LevelData level, Action onFinish)
        {
            new LessonPages(screen, app, level, onFinish);
        }

        LessonPages(RectTransform screen, FinanceQuestApp app, LevelData level, Action onFinish)
        {
            this.onFinish = onFinish;
            pages = level.Def.pages;
            hasTasks = level.Def.tasks != null && level.Def.tasks.Count > 0;
            W = app.ScreenWidth;

            content = ScreenKit.Scroll(screen, W, 0f);
            var view = (RectTransform)content.parent;
            view.offsetMin = new Vector2(0f, BarHeight);          // lista kończy się nad dolnym paskiem
            scroll = view.GetComponent<ScrollRect>();

            // Dolny pasek przyklejony do dołu ekranu.
            var bar = Ui.Rect("BottomBar", screen);
            bar.anchorMin = new Vector2(0f, 0f);
            bar.anchorMax = new Vector2(1f, 0f);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.anchoredPosition = Vector2.zero;
            bar.sizeDelta = new Vector2(0f, BarHeight);

            dots = Ui.Rect("Dots", bar);
            Ui.Place(dots, 0, 6, W, 8);

            float m = ScreenKit.Margin;
            var btn = LipCard.Create(bar, "Next", m, 28, W - 2 * m, ButtonH, Palette.Accent, Palette.Accent, Palette.AccentLip, 16f, 5f, 0f);
            buttonLabel = Ui.Text("Label", btn.Content, "DALEJ", 17f, Palette.OnAccent, FontWeight.ExtraBold, TextAlignmentOptions.Capline);
            Ui.Stretch(buttonLabel.rectTransform);
            btn.SetClickable(Next);

            // Swipe w lewo = następna strona (jak przycisk).
            app.SwipeForward = Next;
            ShowPage(0);
        }

        void Next()
        {
            if (page < pages.Count - 1) ShowPage(page + 1);
            else onFinish();
        }

        void ShowPage(int index)
        {
            page = index;
            for (int i = content.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(content.GetChild(i).gameObject);
            float h = BuildBlocks(content, ContentBlocks.Of(pages[page]));
            ScreenKit.SetContentHeight(content, h);
            content.anchoredPosition = Vector2.zero;              // nowa strona zawsze od góry
            if (scroll != null) scroll.velocity = Vector2.zero;

            bool last = page == pages.Count - 1;
            buttonLabel.text = !last ? "DALEJ" : hasTasks ? "DO ZADAŃ" : "UKOŃCZ";
            BuildDots();
        }

        void BuildDots()
        {
            for (int i = dots.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(dots.GetChild(i).gameObject);
            if (pages.Count < 2) return;                          // jedna strona – kropki nic nie mówią
            const float dot = 8f, wide = 20f, gap = 7f;
            float total = wide + (pages.Count - 1) * dot + (pages.Count - 1) * gap;
            float x = (W - total) * 0.5f;
            for (int i = 0; i < pages.Count; i++)
            {
                bool on = i == page;
                var d = Ui.Rounded("Dot", dots, on ? Palette.Accent : Palette.CardEdge, dot * 0.5f);
                Ui.Place(d.rectTransform, x, 0, on ? wide : dot, dot);
                x += (on ? wide : dot) + gap;
            }
        }

        // ---------- bloki ----------

        float BuildBlocks(RectTransform parent, List<BlockDef> blocks)
        {
            float m = ScreenKit.Margin, cw = W - 2 * m, y = 6f;

            string kicker = "LEKCJA" + (pages.Count > 1 ? " · STRONA " + (page + 1) + " Z " + pages.Count : "");
            var k = Ui.Text("Kicker", parent, kicker, 11f, Palette.TextMuted);
            k.characterSpacing = 12f;
            Ui.Place(k.rectTransform, m, y, cw, 16);
            y += 28f;

            for (int i = 0; i < blocks.Count; i++)
            {
                var b = blocks[i];
                switch (b.type)
                {
                    case "heading": y += Heading(parent, b, m, y, cw) + 12f; break;
                    case "text": y += Paragraph(parent, b.text, m, y, cw) + 14f; break;
                    case "image": y += Picture(parent, b, m, y, cw) + 16f; break;
                    case "callout": y += Callout(parent, b, m, y, cw) + 16f; break;
                    case "list": y += BulletList(parent, b, m, y, cw) + 10f; break;
                    case "table": y += TableBlock.Build(parent, b, m, y, cw) + 16f; break;
                    case "divider": y += Divider(parent, m, y + 4f, cw) + 22f; break;
                    case "space": y += Mathf.Clamp(b.size, 0f, 200f); break;
                }
            }
            return y + 24f;
        }

        static TextMeshProUGUI RichText(string name, Transform parent, string text, float size, FontWeight weight)
        {
            var t = Ui.Text(name, parent, ContentBlocks.Rich(text), size, Palette.Text, weight);
            t.richText = true;
            t.lineSpacing = 6f;
            return t;
        }

        static float Heading(RectTransform parent, BlockDef b, float x, float y, float w)
        {
            var t = RichText("Heading", parent, b.text, 24f, FontWeight.ExtraBold);
            t.lineSpacing = 0f;
            float h = Ui.TextHeight(t, w);
            Ui.Place(t.rectTransform, x, y, w, h);
            return h;
        }

        static float Paragraph(RectTransform parent, string text, float x, float y, float w)
        {
            var t = RichText("Text", parent, text, TextSize, FontWeight.Regular);
            float h = Ui.TextHeight(t, w);
            Ui.Place(t.rectTransform, x, y, w, h);
            return h;
        }

        // Obrazek w karcie z wargą; nie szerszy niż „width” i nie wyższy niż „maxHeight” (proporcje zostają).
        static float Picture(RectTransform parent, BlockDef b, float x, float y, float cw)
        {
            const float lip = 4f, border = 2f, radius = 16f;
            // Obrazek z folderu kursu podpina importer; stare ścieżki do Resources wczytujemy tutaj.
            var sprite = b.sprite != null ? b.sprite : LoadSprite(b.image);
            float w = cw * Mathf.Clamp(b.width <= 0f ? 1f : b.width, 0.3f, 1f);
            float maxH = b.maxHeight <= 0f ? 220f : b.maxHeight;
            float h;
            if (sprite != null)
            {
                float aspect = sprite.rect.height / Mathf.Max(1f, sprite.rect.width);
                h = (w - 2 * border) * aspect + 2 * border;
                if (h > maxH) { h = maxH; w = (h - 2 * border) / aspect + 2 * border; }
            }
            else h = Mathf.Min(120f, maxH);

            float px = x + (cw - w) * 0.5f;
            var card = LipCard.Create(parent, "Image", px, y, w, h + lip, Palette.CardFill, Palette.CardEdge, Palette.CardLip, radius, lip, border);
            if (sprite != null)
            {
                // Zaokrąglona maska, żeby rogi zdjęcia nie wystawały poza kartę.
                var mask = Ui.Rounded("Mask", card.Face, Color.white, radius - border);
                Ui.Stretch(mask.rectTransform, border, border, border, border);
                mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
                var img = Ui.Rect("Picture", mask.transform).gameObject.AddComponent<Image>();
                img.sprite = sprite;
                img.raycastTarget = false;
                Ui.Stretch(img.rectTransform);
            }
            else
            {
                var miss = Ui.Text("Missing", card.Content, "Brak obrazka\n" + b.image, 12.5f, Palette.TextMuted, FontWeight.SemiBold, TextAlignmentOptions.Center);
                Ui.Stretch(miss.rectTransform, 10, 10, 10, 10);
            }
            float used = h + lip;
            if (!string.IsNullOrEmpty(b.caption))
            {
                var c = RichText("Caption", parent, b.caption, 12.5f, FontWeight.SemiBold);
                c.color = Palette.TextMuted;
                c.lineSpacing = 0f;
                float chh = Ui.TextHeight(c, cw);
                Ui.Place(c.rectTransform, x, y + used + 6f, cw, chh);
                used += 6f + chh;
            }
            return used;
        }

        // Obrazki z Resources. Działa i dla tekstur zaimportowanych jako Sprite, i jako zwykła tekstura.
        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        static Sprite LoadSprite(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            path = ContentBlocks.NormalizeImagePath(path, null);
            if (path.StartsWith("Resources/")) path = path.Substring("Resources/".Length);
            string ext = System.IO.Path.GetExtension(path);
            if (!string.IsNullOrEmpty(ext)) path = path.Substring(0, path.Length - ext.Length);
            Sprite s;
            if (sprites.TryGetValue(path, out s)) return s;
            s = Resources.Load<Sprite>(path);
            if (s == null)
            {
                var tex = Resources.Load<Texture2D>(path);
                if (tex != null) s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            }
            sprites[path] = s;
            return s;
        }

        // Ramka „ważne”: biała karta z żółtą obwódką i żółtą wargą, wykrzyknik w kółku po lewej.
        static float Callout(RectTransform parent, BlockDef b, float x, float y, float w)
        {
            const float lip = 5f, border = 2f, pad = 13f, icon = 34f, gap = 11f;
            float tw = w - 2 * pad - icon - gap;
            var probe = RichText("Text", parent, b.text, 15f, FontWeight.Regular);
            float th = Ui.TextHeight(probe, tw);
            float inner = Mathf.Max(icon, th);
            float h = inner + 2 * (pad - 1f) + lip;

            var card = LipCard.Create(parent, "Callout", x, y, w, h, Palette.CardFill, Palette.Accent, Palette.AccentLip, 16f, lip, border);
            var circle = Ui.Rounded("IconBg", card.Content, Palette.Accent, icon * 0.5f);
            Ui.Place(circle.rectTransform, pad, pad - 1f + (inner - icon) * 0.5f, icon, icon);
            var ic = Ui.Icon("Exclaim", circle.transform, IconId.Exclaim, Palette.OnAccent, 20f);
            Ui.Center(ic.rectTransform, 20f, 20f);

            probe.transform.SetParent(card.Content, false);
            Ui.Place(probe.rectTransform, pad + icon + gap, pad - 1f + (inner - th) * 0.5f, tw, th);
            return h;
        }

        // Lista punktowana: żółta kropka i tekst – lekka, nie konkuruje z ramką „Ważne”.
        static float BulletList(RectTransform parent, BlockDef b, float x, float y, float w)
        {
            const float size = 15.5f, dot = 8f, gap = 11f, between = 7f;
            float start = y;
            foreach (var item in b.items)
            {
                if (string.IsNullOrEmpty(item)) continue;
                float tw = w - dot - gap;
                var t = RichText("Text", parent, item, size, FontWeight.Regular);
                t.lineSpacing = 2f;
                float th = Ui.TextHeight(t, tw);
                Ui.Place(t.rectTransform, x + dot + gap, y, tw, th);
                // Kropka na wysokości środka pierwszej linii (nie całego punktu).
                var d = Ui.Rounded("Dot", parent, Palette.Accent, dot * 0.5f);
                Ui.Place(d.rectTransform, x, y + size * 0.62f - dot * 0.5f, dot, dot);
                y += th + between;
            }
            return Mathf.Max(0f, y - start - between);
        }

        // Zwykła cienka linia na całą szerokość – oddziela części strony, nie przyciągając uwagi.
        static float Divider(RectTransform parent, float x, float y, float w)
        {
            const float t = 2f;
            var line = Ui.Rounded("Divider", parent, Palette.CardEdge, t * 0.5f);
            Ui.Place(line.rectTransform, x, y, w, t);
            return t;
        }
    }
}

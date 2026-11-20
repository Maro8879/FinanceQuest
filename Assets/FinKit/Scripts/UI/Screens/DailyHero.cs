using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FinanceQuest
{
    // Karta z cytatem nad trofeami: Wall Street w tle (dzień albo noc – według „Trybu ciemnego”),
    // cytat na półprzezroczystym panelu, pod nim autor.
    // Kliknięcie w kartę (karta się nie wciska) pokazuje kolejny cytat w stylu tablicy notowań:
    // stary odjeżdża w górę, nowy wjeżdża od dołu, autor chwilę po nim.
    public static class DailyHero
    {
        const float Radius = 22f;
        static readonly Color PanelNight = new Color(8f / 255f, 32f / 255f, 38f / 255f, 0.78f);
        static readonly Color PanelDay = new Color(1f, 1f, 1f, 0.84f);

        // Zwraca wysokość karty.
        public static float Build(RectTransform parent, float x, float y, float w)
        {
            bool night = Settings.DarkMode;
            // Wysokość z proporcji rysunku, żeby obraz się nie rozciągał.
            float h = Mathf.Round(w * WallStreetArt.H / WallStreetArt.W);
            var root = Ui.Rect("DailyThought", parent);
            Ui.Place(root, x, y, w, h);

            // Zaokrąglona maska, w niej rysunek.
            var mask = Ui.Rounded("Mask", root, Color.white, Radius);
            Ui.Stretch(mask.rectTransform);
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var art = Ui.Rect("Art", mask.transform).gameObject.AddComponent<Image>();
            art.sprite = night ? WallStreetArt.Night : WallStreetArt.Day;
            art.raycastTarget = false;
            Ui.Stretch(art.rectTransform);

            // Powiewające flagi nad masztami z obrazka (osobna, lekka warstwa).
            WavingFlags.Create(root, w, h);

            // Panel z cytatem – półprzezroczysty, żeby wysokie budynki mogły sięgać pod tekst.
            // RectMask2D przycina tekst, który przy zmianie cytatu wyjeżdża poza panel.
            const float inset = 10f, pad = 12f;
            var panel = Ui.Rounded("Panel", root, night ? PanelNight : PanelDay, 14f);
            panel.gameObject.AddComponent<RectMask2D>();
            float tw = w - 2 * inset - 2 * pad;
            var quote = Ui.Text("Quote", panel.transform, "„" + DailyThoughts.Today() + "”", 18f, night ? Color.white : Palette.Text,
                FontWeight.ExtraBold, TextAlignmentOptions.TopLeft);
            var author = Ui.Text("Author", panel.transform, "— " + DailyThoughts.TodayAuthor(), 13f,
                night ? new Color(1f, 1f, 1f, 0.7f) : Palette.TextMuted, FontWeight.SemiBold, TextAlignmentOptions.TopLeft);

            var swap = panel.gameObject.AddComponent<QuoteSwap>();
            swap.Init(panel.rectTransform, quote, author, inset, pad, w, tw);
            // Przezroczyste tło łapie kliknięcie w całą kartę.
            var hit = root.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            root.gameObject.AddComponent<DailyHeroTap>().OnTap = swap.Play;
            return h;
        }
    }

    // Łapie kliknięcie w kartę, a przeciąganie oddaje liście (żeby dało się przewijać menu, zaczynając od karty).
    public class DailyHeroTap : MonoBehaviour, IPointerClickHandler, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        public System.Action OnTap;
        ScrollRect scroll;
        ScrollRect Scroll { get { if (scroll == null) scroll = GetComponentInParent<ScrollRect>(); return scroll; } }

        public void OnPointerClick(PointerEventData e) { if (!e.dragging && OnTap != null) OnTap(); }
        public void OnInitializePotentialDrag(PointerEventData e) { if (Scroll != null) Scroll.OnInitializePotentialDrag(e); }
        public void OnBeginDrag(PointerEventData e) { if (Scroll != null) Scroll.OnBeginDrag(e); }
        public void OnDrag(PointerEventData e) { if (Scroll != null) Scroll.OnDrag(e); }
        public void OnEndDrag(PointerEventData e) { if (Scroll != null) Scroll.OnEndDrag(e); }
        public void OnScroll(PointerEventData e) { if (Scroll != null) Scroll.OnScroll(e); }
    }

    // Zmiana cytatu w stylu tablicy notowań. Komponent działa tylko w trakcie animacji (potem się wyłącza).
    public class QuoteSwap : MonoBehaviour
    {
        const float Duration = 0.45f, Shift = 22f, AuthorShift = 14f;
        RectTransform panel;
        TextMeshProUGUI quote, author;
        float inset, pad, w, tw, qy, ay, t, quoteAlpha, authorAlpha;
        bool swapped;

        public void Init(RectTransform panel, TextMeshProUGUI quote, TextMeshProUGUI author, float inset, float pad, float w, float tw)
        {
            this.panel = panel; this.quote = quote; this.author = author;
            this.inset = inset; this.pad = pad; this.w = w; this.tw = tw;
            quoteAlpha = quote.alpha; authorAlpha = author.alpha;   // autor w nocy jest półprzezroczysty – zachowujemy to
            Layout();
            enabled = false;
        }

        // Pozycje cytatu i autora + wysokość panelu dopasowana do tekstu.
        void Layout()
        {
            float qh = Ui.TextHeight(quote, tw);
            qy = pad;
            ay = pad + qh + 6f;
            Ui.Place(quote.rectTransform, pad, qy, tw, qh);
            Ui.Place(author.rectTransform, pad, ay, tw, 18f);
            Ui.Place(panel, inset, inset, w - 2 * inset, ay + 18f + pad);
        }

        public void Play()
        {
            if (enabled) return;   // trwa zmiana – kolejne kliknięcie poczeka na koniec
            t = 0f;
            swapped = false;
            enabled = true;
        }

        void Update()
        {
            t += Time.unscaledDeltaTime / Duration;
            if (t >= 0.5f && !swapped)
            {
                // Połowa animacji: stary tekst już zniknął – podmieniamy na nowy (wjedzie od dołu).
                swapped = true;
                DailyThoughts.Next();
                quote.text = "„" + DailyThoughts.Today() + "”";
                author.text = "— " + DailyThoughts.TodayAuthor();
                Layout();
            }
            if (t >= 1f)
            {
                Set(quote, qy, 0f, 0f, quoteAlpha);
                Set(author, ay, 0f, 0f, authorAlpha);
                enabled = false;
                return;
            }
            // Pierwsza połowa: odjazd w górę. Druga: przyjazd od dołu. Autor rusza chwilę po cytacie.
            float dir = t < 0.5f ? -1f : 1f;
            float pq = t < 0.5f ? Ease(t * 2f) : 1f - Ease(t * 2f - 1f);
            float pa = t < 0.5f ? Ease(Mathf.Clamp01((t * 2f - 0.2f) / 0.8f)) : 1f - Ease(Mathf.Clamp01((t * 2f - 1.15f) / 0.85f));
            Set(quote, qy, dir * pq * Shift, pq, quoteAlpha);
            Set(author, ay, dir * pa * AuthorShift, pa, authorAlpha);
        }

        void Set(TextMeshProUGUI text, float baseY, float offset, float fade, float baseAlpha)
        {
            text.rectTransform.anchoredPosition = new Vector2(pad, -(baseY + offset));
            text.alpha = baseAlpha * (1f - fade);
        }

        static float Ease(float x) { x = 1f - x; return 1f - x * x * x; }
    }
}

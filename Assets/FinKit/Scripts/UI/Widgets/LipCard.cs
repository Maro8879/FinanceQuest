using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FinanceQuest
{
    // Karta/przycisk w stylu „kreskówkowym”: pod spodem ciemniejsza warga.
    // Przy dotyku wierzch zapada się na wargę, a po puszczeniu odbija ze sprężynką.
    public class LipCard : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerClickHandler,
        IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        public RectTransform Face;     // to, co się zapada
        public RectTransform Content;  // tu dokładamy napisy i ikony
        public Image Lip, Edge, Fill;
        public float LipHeight;
        public Action OnClick;

        float pos, vel, target;        // 0 = w górze, 1 = wciśnięta
        bool pressed;
        ScrollRect scroll;
        LipSpring spring;

        // h = całkowita wysokość razem z wargą.
        public static LipCard Create(Transform parent, string name, float x, float y, float w, float h,
            Color fill, Color edge, Color lip, float radius, float lipHeight, float border)
        {
            var root = Ui.Rounded(name, parent, lip, radius);
            Ui.Place(root.rectTransform, x, y, w, h);
            var card = root.gameObject.AddComponent<LipCard>();
            card.Lip = root;
            card.LipHeight = lipHeight;

            card.Edge = Ui.Rounded("Face", root.transform, border > 0f ? edge : fill, radius);
            card.Face = card.Edge.rectTransform;
            Ui.Stretch(card.Face, 0, 0, 0, lipHeight);
            if (border > 0f)
            {
                card.Fill = Ui.Rounded("Fill", card.Face, fill, Mathf.Max(1f, radius - border));
                Ui.Stretch(card.Fill.rectTransform, border, border, border, border);
            }
            else card.Fill = card.Edge;

            card.Content = Ui.Rect("Content", card.Face);
            Ui.Stretch(card.Content);
            // Sprężynka liczy się w osobnym komponencie, który włączamy tylko na czas animacji.
            // (Samej karty nie wyłączamy, bo wyłączony komponent nie dostaje zdarzeń dotyku.)
            card.spring = root.gameObject.AddComponent<LipSpring>();
            card.spring.Card = card;
            card.spring.enabled = false;
            return card;
        }

        RectTransform bgLayer;

        // Warstwa pod napisami, przycięta do zaokrąglonego kształtu karty – na grafiki w tle.
        RectTransform BackgroundLayer(float radius)
        {
            if (bgLayer != null) return bgLayer;
            var mask = Ui.Rounded("BgMask", Face, Color.white, radius);
            Ui.Stretch(mask.rectTransform);
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            mask.transform.SetSiblingIndex(Content.GetSiblingIndex());   // pod treścią
            bgLayer = mask.rectTransform;
            return bgLayer;
        }

        // Duża, półprzezroczysta ikona w tle karty (karty kursów).
        public Image AddBackgroundIcon(IconId icon, float radius, float size, float alpha)
        {
            var img = Ui.Icon("BgIcon", BackgroundLayer(radius), icon, new Color(1f, 1f, 1f, alpha), size);
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(-size * 0.22f, -size * 0.06f);
            rt.localEulerAngles = new Vector3(0f, 0f, -14f);
            return img;
        }

        // Ilustracja przy prawej krawędzi, wyśrodkowana w pionie, pod napisami (karty kursów w menu).
        public Image AddIllustration(Sprite sprite, float w, float h, float rightMargin)
        {
            var img = Ui.Rect("Illustration", Face).gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            img.transform.SetSiblingIndex(Content.GetSiblingIndex());
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-rightMargin, 0f);
            rt.sizeDelta = new Vector2(w, h);
            return img;
        }

        // Wykres świecowy na całej szerokości karty, przy dolnej krawędzi (karty rozdziałów).
        public Image AddBackgroundChart(float radius, Color color)
        {
            var img = Ui.Rect("BgChart", BackgroundLayer(radius)).gameObject.AddComponent<Image>();
            img.sprite = ProceduralSprites.Candles;
            img.color = color;
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            float w = ((RectTransform)transform).sizeDelta.x;
            rt.sizeDelta = new Vector2(0f, w / 4f);   // proporcje tekstury 4:1, żeby świece się nie rozciągały
            rt.anchoredPosition = new Vector2(0f, 2f);
            return img;
        }

        // Rosnące słupki w prawym dolnym rogu – mały wykres wzrostu (karty rozdziałów i nagłówek rozdziału).
        // Słupki wystają lekko poza dół karty, maska je przycina. seed zmienia, który słupek jest trochę wyższy.
        public void AddBackgroundBars(float radius, Color color, int seed)
        {
            var layer = BackgroundLayer(radius);
            float h = ((RectTransform)transform).sizeDelta.y - LipHeight;
            const int count = 6;
            const float bw = 16f, gap = 8f, right = 8f;
            for (int i = 0; i < count; i++)
            {
                // Niskie słupki (najwyższy ok. 40% karty), żeby nie wchodziły pod napisy i pasek postępu.
                float bh = 8f + i * Mathf.Min(h * 0.065f, 6.5f) + ((i + seed) % 3 == 0 ? 4f : 0f);
                var bar = Ui.Rounded("Bar", layer, color, 4f);
                var rt = bar.rectTransform;
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f);
                rt.sizeDelta = new Vector2(bw, bh + 8f);
                rt.anchoredPosition = new Vector2(-right - (count - 1 - i) * (bw + gap), -8f);
            }
        }

        // Łagodna linia wykresu na całej karcie (karty rozdziałów i nagłówek rozdziału).
        public WaveLine AddBackgroundLine(float radius, Color color, float phase)
        {
            var go = Ui.Rect("BgLine", BackgroundLayer(radius)).gameObject;
            go.AddComponent<CanvasRenderer>();
            var line = go.AddComponent<WaveLine>();
            line.Phase = phase;
            line.color = color;
            line.raycastTarget = false;
            Ui.Stretch(line.rectTransform);
            return line;
        }

        // Bardzo duży, półprzezroczysty numer na środku karty, w tle pod napisami (kafelki leveli).
        // Wyśrodkowany po kształcie cyfr (MidlineGeoAligned), a nie po linii tekstu – inaczej cyfra siedziałaby za wysoko.
        public TMPro.TextMeshProUGUI AddBackgroundNumberCenter(string text, float radius, Color color, float size)
        {
            var t = Ui.Text("BgNumber", BackgroundLayer(radius), text, size, color, FontWeight.ExtraBold, TMPro.TextAlignmentOptions.MidlineGeoAligned);
            t.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            t.overflowMode = TMPro.TextOverflowModes.Overflow;
            t.characterSpacing = -6f;
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size * 2f, size * 1.4f);
            // Środek twarzy karty (bez cienia pod spodem). Poprawka -1.6% rozmiaru zmierzona na screenie z Unity:
            // bez niej cyfra była 2.5 px nad środkiem (10 px odstępu u góry, 15 px u dołu).
            rt.anchoredPosition = new Vector2(0f, LipHeight * 0.5f - size * 0.016f);
            return t;
        }

        // Łamana linia „giełdowa” od połowy karty w prawo, z kropką (karty rozdziałów i nagłówek rozdziału).
        public TrendLine AddBackgroundTrend(float radius, Color color, int seed)
        {
            var go = Ui.Rect("BgTrend", BackgroundLayer(radius)).gameObject;
            go.AddComponent<CanvasRenderer>();
            var line = go.AddComponent<TrendLine>();
            line.Seed = seed;
            line.color = color;
            line.raycastTarget = false;
            Ui.Stretch(line.rectTransform);
            return line;
        }

        // Duży, półprzezroczysty numer w prawym dolnym rogu, przycięty przez kartę (karty leveli).
        public TMPro.TextMeshProUGUI AddBackgroundNumber(string text, float radius, Color color, float size)
        {
            var t = Ui.Text("BgNumber", BackgroundLayer(radius), text, size, color, FontWeight.ExtraBold, TMPro.TextAlignmentOptions.BottomRight);
            t.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            t.characterSpacing = -6f;
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f);
            rt.sizeDelta = new Vector2(size * 2f, size);
            rt.anchoredPosition = new Vector2(4f, -size * 0.28f);   // wystaje poza dół karty – maska go przytnie
            return t;
        }

        // Żółta obwódka wokół karty = „tu jesteś”. Nie zapada się przy dotyku (siedzi na korzeniu karty).
        public void AddFocusRing(Color color, float radius, float gap, float thickness)
        {
            var r = Ui.Rect("FocusRing", transform).gameObject.AddComponent<Image>();
            r.sprite = ProceduralSprites.Ring;
            r.type = Image.Type.Sliced;
            float outer = radius + gap + thickness;
            // Promień zewnętrzny decyduje o skali; grubość wychodzi z proporcji RingRatio (ok. 3 przy promieniu 26).
            r.pixelsPerUnitMultiplier = ProceduralSprites.RoundTexRadius / outer;
            r.color = color;
            r.raycastTarget = false;
            Ui.Stretch(r.rectTransform, -(gap + thickness), -(gap + thickness), -(gap + thickness), -(gap + thickness));
            r.transform.SetSiblingIndex(0);
        }

        // Klikalne karty łapią dotyk na całej powierzchni (razem z wargą).
        public void SetClickable(Action onClick)
        {
            OnClick = onClick;
            Lip.raycastTarget = true;
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (OnClick == null) return;
            pressed = true;
            target = 1f;
            spring.enabled = true;
        }

        public void OnPointerUp(PointerEventData e) { Release(); }

        public void OnPointerClick(PointerEventData e)
        {
            if (OnClick == null) return;
            // Palec się przesunął (przewijanie, swipe) – to nie było kliknięcie. Zabezpieczenie na wypadek,
            // gdyby przeciąganie się nie zaczęło (np. ruch tuż poniżej progu, a potem szybkie puszczenie).
            if (e.dragging || (e.position - e.pressPosition).sqrMagnitude > DragThreshold() * DragThreshold()) return;
            Haptics.Tap();   // tyknięcie od razu – opóźnienie poniżej dotyczy tylko zmiany ekranu
            // Chwilę czekamy, żeby było widać odbicie, zanim ekran się zmieni.
            FinanceQuestApp.RunLater(0.09f, OnClick);
        }

        // Ustawia wciśnięcie wprost (0 = w górze, 1 = wciśnięta) – do własnych, płynnych animacji z zewnątrz
        // (np. przycisk SEE MORE, który sam się wciska). Gdy ktoś akurat trzyma palec na karcie, nic nie robimy.
        public bool SetPress(float p)
        {
            if (pressed || spring.enabled) return false;
            if (pos == p) return true;   // bez zmiany = nic nie przerysowujemy (ważne: wołane co klatkę)
            pos = p; vel = 0f; target = 0f;
            Apply();
            return true;
        }

        void Release()
        {
            if (!pressed) return;
            pressed = false;
            target = 0f;
            spring.enabled = true;
        }

        // Zwraca true, gdy animacja się skończyła.
        public bool Step(float dt)
        {
            dt = Mathf.Min(dt, 1f / 30f);
            // Wciskanie: sztywno i bez drgań. Puszczanie: luźniej, z małym przestrzeleniem = „sprężynka”.
            float k = pressed ? 2600f : 700f;
            float damping = 2f * Mathf.Sqrt(k) * (pressed ? 1f : 0.32f);
            float acc = -k * (pos - target) - damping * vel;
            vel += acc * dt;
            pos += vel * dt;
            if (!pressed && Mathf.Abs(pos - target) < 0.002f && Mathf.Abs(vel) < 0.02f)
            {
                pos = target; vel = 0f;
                Apply();
                return true;
            }
            Apply();
            return false;
        }

        void Apply()
        {
            float s = Mathf.Clamp(pos, -0.4f, 1f) * LipHeight;
            Face.offsetMin = new Vector2(0f, LipHeight - s);
            Face.offsetMax = new Vector2(0f, -s);
        }


        // --- Przeciąganie przekazujemy do listy, żeby dało się przewijać, zaczynając palcem na karcie. ---
        ScrollRect Scroll
        {
            get { if (scroll == null) scroll = GetComponentInParent<ScrollRect>(); return scroll; }
        }

        public void OnInitializePotentialDrag(PointerEventData e) { if (Scroll != null) Scroll.OnInitializePotentialDrag(e); }
        // Karta sama obsługuje przeciąganie (przekazuje je liście), więc Unity nie anuluje jej kliknięcia,
        // gdy palec zaczyna przewijać – robimy to tu: po rozpoczęciu przewijania / swipe'a puszczenie palca
        // nie otworzy karty, a karta od razu odbija w górę.
        public void OnBeginDrag(PointerEventData e) { e.eligibleForClick = false; Release(); if (Scroll != null) Scroll.OnBeginDrag(e); }

        // Próg „to już jest przesunięcie” – ten sam co w EventSystem (ustawiany w FinanceQuestApp na ok. 3 mm).
        static float DragThreshold()
        {
            var es = EventSystem.current;
            return es != null ? es.pixelDragThreshold : 10f;
        }
        public void OnDrag(PointerEventData e) { if (Scroll != null) Scroll.OnDrag(e); }
        public void OnEndDrag(PointerEventData e) { if (Scroll != null) Scroll.OnEndDrag(e); }
        public void OnScroll(PointerEventData e) { if (Scroll != null) Scroll.OnScroll(e); }
    }
}

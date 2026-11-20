using System.Collections.Generic;
using UnityEngine;

namespace FinanceQuest
{
    // Przejście „kaskada kart”: stary ekran szybko gaśnie i lekko odjeżdża,
    // a karty nowego wskakują po kolei z małą sprężynką.
    // Ruszamy tylko pozycją i przezroczystością gotowych elementów – bez przeliczania layoutu.
    public class CascadeTransition
    {
        const float OldFade = 0.13f;     // jak szybko znika stary ekran
        const float StartDelay = 0.08f;  // nowe karty startują, gdy stary już prawie zniknął
        const float Stagger = 0.045f;    // odstęp między kolejnymi kartami
        const float ItemTime = 0.30f;    // czas wskoczenia jednej karty
        const float Drop = 26f;          // skąd wskakują (w jednostkach ekranu)
        const int MaxRank = 7;           // dalsze karty i tak są poza ekranem – nie czekamy na nie

        class Item
        {
            public RectTransform Rt;
            public CanvasGroup Group;
            public Vector2 Base;
            public int Rank;
        }

        readonly RectTransform oldScreen;
        readonly CanvasGroup oldGroup;
        readonly List<Item> items = new List<Item>();
        readonly float dir;
        readonly bool horizontal;
        const float Slide = 44f;          // przy swipie karty wjeżdżają z boku o tyle jednostek
        public readonly float Duration;
        float time;

        public RectTransform OldScreen { get { return oldScreen; } }

        public CascadeTransition(RectTransform oldScreen, RectTransform newScreen, bool back, bool horizontal, float viewHeight)
        {
            this.oldScreen = oldScreen;
            this.horizontal = horizontal;
            // Przy „wstecz” ruch jest odwrócony – karty spadają z góry, stary ekran zjeżdża w dół.
            dir = back ? -0.8f : 1f;
            if (oldScreen != null)
            {
                oldGroup = oldScreen.GetComponent<CanvasGroup>();
                oldGroup.blocksRaycasts = false;   // stary ekran nie reaguje już na dotyk
            }
            Collect(newScreen, viewHeight);
            int maxRank = 0;
            foreach (var it in items) maxRank = Mathf.Max(maxRank, it.Rank);
            Duration = StartDelay + maxRank * Stagger + ItemTime;
            Apply();
        }

        // Elementy do animowania: dzieci listy przewijanej albo (gdy listy nie ma) dzieci ekranu.
        // Elementy w tym samym wierszu (ta sama wysokość) wskakują razem.
        void Collect(RectTransform screen, float viewHeight)
        {
            var parent = screen.Find("Scroll/Content") as RectTransform;
            if (parent == null) parent = screen;

            var tops = new List<float>();
            for (int i = 0; i < parent.childCount; i++)
            {
                float top = -((RectTransform)parent.GetChild(i)).anchoredPosition.y;
                if (!tops.Contains(top)) tops.Add(top);
            }
            tops.Sort();

            for (int i = 0; i < parent.childCount; i++)
            {
                var rt = (RectTransform)parent.GetChild(i);
                float top = -rt.anchoredPosition.y;
                // Poza ekranem nie animujemy – i tak tego nie widać, a oszczędzamy pracę.
                if (top > viewHeight) continue;
                var g = rt.GetComponent<CanvasGroup>();
                if (g == null) g = rt.gameObject.AddComponent<CanvasGroup>();
                items.Add(new Item { Rt = rt, Group = g, Base = rt.anchoredPosition, Rank = Mathf.Min(tops.IndexOf(top), MaxRank) });
            }
        }

        // Zwraca true, gdy przejście się skończyło.
        public bool Step(float dt, AppHeader header)
        {
            time += dt;
            Apply();
            if (header != null) header.Tick(Mathf.Clamp01(time / Duration));
            return time >= Duration;
        }

        void Apply()
        {
            if (oldScreen != null)
            {
                float o = Mathf.Clamp01(time / OldFade);
                oldGroup.alpha = 1f - o;
                // Swipe: stary ekran ucieka w stronę ruchu palca; zwykłe przejście: lekko w górę/dół.
                oldScreen.anchoredPosition = horizontal ? new Vector2(-16f * o * dir, 0f) : new Vector2(0f, 10f * o * dir);
            }
            foreach (var it in items)
            {
                float q = Mathf.Clamp01((time - StartDelay - it.Rank * Stagger) / ItemTime);
                float e = EaseOutBack(q);
                it.Group.alpha = Mathf.Clamp01(q * 1.6f);
                it.Rt.anchoredPosition = horizontal
                    ? it.Base + new Vector2((1f - e) * Slide * dir, 0f)    // w głąb: z prawej, wstecz: z lewej
                    : it.Base - new Vector2(0f, (1f - e) * Drop * dir);
            }
        }

        // Kończy natychmiast (np. gdy użytkownik szybko przechodzi dalej): wszystko na swoje miejsce.
        public void Finish()
        {
            foreach (var it in items)
            {
                if (it.Rt == null) continue;
                it.Rt.anchoredPosition = it.Base;
                // CanvasGroup był potrzebny tylko na czas animacji.
                Object.Destroy(it.Group);
            }
            items.Clear();
        }

        // Lekkie przestrzelenie na końcu = „sprężynka”.
        static float EaseOutBack(float p)
        {
            const float c1 = 1.5f, c3 = c1 + 1f;
            float x = p - 1f;
            return 1f + c3 * x * x * x + c1 * x * x;
        }
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FinanceQuest
{
    // Kokpit nad kursami: trofea w jednym rzędzie przewijanym w bok, ułożone falą (na przemian wyżej i niżej).
    // Rząd sięga do krawędzi ekranu, więc widać, że jest go więcej. Bez karty pod spodem – stoi wprost na tle.
    public static class Cockpit
    {
        const float Pad = 4f;               // wyrównanie z nagłówkiem „KURSY”
        const float TW = 54f, TH = 62f;     // rozmiar pucharu
        const float Step = 74f;             // odstęp między kolejnymi pucharami
        const float Wave = 28f;             // o ile niżej stoją co drugie puchary
        const float LabelH = 16f;

        // Zwraca wysokość kokpitu.
        public static float Build(RectTransform parent, float x, float y, float w, FinanceQuestApp app)
        {
            var root = Ui.Rect("Cockpit", parent);
            Ui.Place(root, x, y, w, 300);
            float inner = w - 2 * Pad;
            float cy = 0f;

            // Trofea: „Start” (za rozpoczęcie edukacji) + po jednym na kurs + „Mistrz” za ukończenie wszystkich kursów.
            int courses = app.Courses.Count, done = 0;
            foreach (var c in app.Courses) if (app.Progress.IsCourseComplete(c)) done++;
            bool master = courses > 0 && done == courses;
            int total = 2 + courses, earned = (app.Progress.Started ? 1 : 0) + done + (master ? 1 : 0);

            Section(root, "YOUR TROPHIES", Pad, cy, inner * 0.7f);
            var cnt = Ui.Text("Count", root, earned + " / " + total, 13f, Palette.Text, FontWeight.ExtraBold, TextAlignmentOptions.CaplineRight);
            Ui.Place(cnt.rectTransform, Pad + inner - 60, cy, 60, 16);
            cy += 24f;

            // Okno przewijania od krawędzi do krawędzi ekranu (wychodzi poza margines listy).
            float rowH = Wave + TH + 4f + LabelH + 4f;
            var view = Ui.Rect("TrophyScroll", root);
            Ui.Place(view, -x, cy, w + 2 * x, rowH);
            var hit = view.gameObject.AddComponent<Image>();
            hit.color = Color.clear;                   // łapie przeciąganie w pustych miejscach
            view.gameObject.AddComponent<RectMask2D>();

            var content = Ui.Rect("Content", view);
            float first = x + Pad;                     // pierwszy puchar równo z marginesem listy
            float contentW = first + (total - 1) * Step + TW + x + Pad;
            Ui.Place(content, 0, 0, contentW, rowH);

            for (int i = 0; i < total; i++)
            {
                Sprite art;
                string name;
                bool got;
                TrophyKind kind;
                Color baseColor = Palette.Text;   // Start i Mistrz stoją na ciemnym cokole
                if (i == 0)
                {
                    // Trofeum za rozpoczęcie: złota flaga.
                    got = app.Progress.Started;
                    name = "Start";
                    kind = TrophyKind.Start;
                }
                else if (i == total - 1)
                {
                    got = master;
                    name = "Master";
                    kind = TrophyKind.Master;
                }
                else
                {
                    var course = app.Courses[i - 1];
                    got = app.Progress.IsCourseComplete(course);
                    name = course.ShortName;
                    kind = TrophyArt.ForCourse(course);
                    baseColor = course.Color;
                }
                art = got ? TrophyArt.Earned(kind, baseColor) : TrophyArt.Locked(kind);
                float sx = first + i * Step, sy = i % 2 == 0 ? 0f : Wave;   // fala: co drugi puchar niżej
                var img = Ui.Rect("Trophy_" + name, content).gameObject.AddComponent<Image>();
                img.sprite = art;
                img.raycastTarget = false;
                Ui.Place(img.rectTransform, sx, sy, TW, TH);

                var lab = Ui.Text("Label", content, name, 12f, got ? Palette.Text : Palette.TextLocked, FontWeight.SemiBold, TextAlignmentOptions.Capline);
                lab.textWrappingMode = TextWrappingModes.NoWrap;
                lab.enableAutoSizing = true;
                lab.fontSizeMin = 9f;
                lab.fontSizeMax = 12f;
                Ui.Place(lab.rectTransform, sx + TW * 0.5f - Step * 0.5f + 2f, sy + TH + 4f, Step - 4f, LabelH);
            }

            var sr = view.gameObject.AddComponent<SideScroll>();
            sr.viewport = view;
            sr.content = content;
            sr.horizontal = true;
            sr.vertical = false;
            sr.movementType = ScrollRect.MovementType.Elastic;
            sr.decelerationRate = 0.135f;
            sr.scrollSensitivity = 20f;

            cy += rowH;
            root.sizeDelta = new Vector2(w, cy);
            return cy;
        }

        static void Section(Transform parent, string text, float x, float y, float w)
        {
            var t = Ui.Text("Section", parent, text, 12.5f, Palette.TextMuted, FontWeight.ExtraBold, TextAlignmentOptions.CaplineLeft);
            t.characterSpacing = 4f;   // rozstrzelone wersaliki = nagłówek sekcji
            Ui.Place(t.rectTransform, x, y, w, 16);
        }
    }

    // Poziome przewijanie wewnątrz pionowej listy menu: ruch bardziej w pionie oddajemy liście,
    // więc menu da się przewijać także palcem położonym na trofeach.
    public class SideScroll : ScrollRect
    {
        ScrollRect parentScroll;
        bool routeToParent;

        ScrollRect Parent
        {
            get
            {
                if (parentScroll == null && transform.parent != null) parentScroll = transform.parent.GetComponentInParent<ScrollRect>();
                return parentScroll;
            }
        }

        public override void OnInitializePotentialDrag(PointerEventData e)
        {
            if (Parent != null) Parent.OnInitializePotentialDrag(e);
            base.OnInitializePotentialDrag(e);
        }

        public override void OnBeginDrag(PointerEventData e)
        {
            routeToParent = Mathf.Abs(e.delta.y) > Mathf.Abs(e.delta.x);
            if (routeToParent) { if (Parent != null) Parent.OnBeginDrag(e); }
            else { SwipeInput.Suppress = true; base.OnBeginDrag(e); }   // przewijanie trofeów ≠ swipe do kursu
        }

        public override void OnDrag(PointerEventData e)
        {
            if (routeToParent) { if (Parent != null) Parent.OnDrag(e); }
            else base.OnDrag(e);
        }

        public override void OnEndDrag(PointerEventData e)
        {
            if (routeToParent) { if (Parent != null) Parent.OnEndDrag(e); }
            else base.OnEndDrag(e);
            routeToParent = false;
        }

        public override void OnScroll(PointerEventData e)
        {
            // Kółko myszy przewija menu w pionie, nie trofea.
            if (Parent != null) Parent.OnScroll(e);
        }
    }
}

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
            // Chwilę czekamy, żeby było widać odbicie, zanim ekran się zmieni.
            FinanceQuestApp.RunLater(0.09f, OnClick);
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
        public void OnBeginDrag(PointerEventData e) { Release(); if (Scroll != null) Scroll.OnBeginDrag(e); }
        public void OnDrag(PointerEventData e) { if (Scroll != null) Scroll.OnDrag(e); }
        public void OnEndDrag(PointerEventData e) { if (Scroll != null) Scroll.OnEndDrag(e); }
        public void OnScroll(PointerEventData e) { if (Scroll != null) Scroll.OnScroll(e); }
    }
}

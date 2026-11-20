using UnityEngine;

namespace FinanceQuest
{
    // Wykrywa szybkie przesunięcie palcem (lub myszą w Editorze) w lewo/prawo.
    // Działa obok przewijania listy: liczy się tylko ruch wyraźnie poziomy, krótki i dość długi.
    public class SwipeInput
    {
        const float MinDistance = 70f;     // w jednostkach ekranu (niezależnie od rozdzielczości)
        const float MaxTime = 0.6f;        // dłuższy ruch to raczej przeciąganie niż swipe
        const float HorizontalRatio = 1.6f; // poziomo musi być wyraźnie więcej niż pionowo

        bool down;
        Vector2 start, last;
        float startTime;

        // Zwraca +1 = swipe w prawo (wstecz), -1 = w lewo (w głąb), 0 = nic.
        public int Poll(float canvasScale)
        {
            Vector2 pos;
            bool pressed = ReadPointer(out pos);

            if (pressed)
            {
                if (!down) { down = true; start = pos; startTime = Time.unscaledTime; }
                last = pos;   // po puszczeniu pozycja dotyku bywa nieaktualna – liczymy od ostatniej znanej
                return 0;
            }
            if (!down) return 0;

            down = false;
            Vector2 d = (last - start) * (1f / Mathf.Max(0.01f, canvasScale));
            float dt = Time.unscaledTime - startTime;
            if (dt <= MaxTime && Mathf.Abs(d.x) >= MinDistance && Mathf.Abs(d.x) >= Mathf.Abs(d.y) * HorizontalRatio)
                return d.x > 0f ? 1 : -1;
            return 0;
        }

        bool ReadPointer(out Vector2 pos)
        {
#if ENABLE_INPUT_SYSTEM
            // Pointer obejmuje i mysz, i ekran dotykowy (także symulator urządzenia w Editorze).
            var p = UnityEngine.InputSystem.Pointer.current;
            if (p == null) { pos = last; return false; }
            pos = p.position.ReadValue();
            return p.press.isPressed;
#else
            pos = Input.mousePosition;   // na telefonie dotyk jest domyślnie symulowany jako mysz
            return Input.GetMouseButton(0);
#endif
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace FinanceQuest
{
    // Pasek postępu: szary tor + pomarańczowe wypełnienie z jasnym „odblaskiem” u góry.
    public class ProgressBar : MonoBehaviour
    {
        RectTransform fill;
        float width, height;

        public static ProgressBar Create(Transform parent, float x, float y, float w, float h, Color track, float value)
        {
            var bg = Ui.Rounded("ProgressBar", parent, track, h * 0.5f);
            Ui.Place(bg.rectTransform, x, y, w, h);
            var bar = bg.gameObject.AddComponent<ProgressBar>();
            bar.width = w;
            bar.height = h;

            var f = Ui.Rounded("Fill", bg.transform, Palette.Orange, h * 0.5f);
            bar.fill = f.rectTransform;
            bar.fill.anchorMin = new Vector2(0f, 0f);
            bar.fill.anchorMax = new Vector2(0f, 1f);
            bar.fill.pivot = new Vector2(0f, 0.5f);
            bar.fill.anchoredPosition = Vector2.zero;

            var shine = Ui.Rounded("Shine", f.transform, new Color(1f, 1f, 1f, 0.4f), 2f);
            var srt = shine.rectTransform;
            srt.anchorMin = new Vector2(0f, 1f);
            srt.anchorMax = new Vector2(1f, 1f);
            srt.pivot = new Vector2(0.5f, 1f);
            float inset = h * 0.45f;
            srt.offsetMin = new Vector2(inset, -h * 0.25f - 4f);
            srt.offsetMax = new Vector2(-inset, -h * 0.25f);

            bar.SetValue(value);
            return bar;
        }

        public void SetValue(float v)
        {
            v = Mathf.Clamp01(v);
            fill.gameObject.SetActive(v > 0.001f);
            // Minimalna szerokość = wysokość, żeby mały postęp był kółkiem, a nie zgniecioną kreską.
            fill.sizeDelta = new Vector2(Mathf.Max(height, width * v), 0f);
        }
    }
}

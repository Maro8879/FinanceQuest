using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FinanceQuest
{
    // Pomocniki do budowania ekranu z kodu. Układ liczymy ręcznie (bez LayoutGroup),
    // żeby nie było kosztownych przeliczeń layoutu w trakcie animacji.
    public static class Ui
    {
        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        // Pozycja od lewego górnego rogu rodzica, Y rośnie w dół – tak jak czytamy makietę.
        public static void Place(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
        }

        public static void Stretch(RectTransform rt, float left = 0, float top = 0, float right = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        public static void Center(RectTransform rt, float w, float h, float dx = 0, float dy = 0)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(dx, dy);
            rt.sizeDelta = new Vector2(w, h);
        }

        // Zaokrąglony prostokąt o zadanym promieniu w jednostkach ekranu.
        public static Image Rounded(string name, Transform parent, Color color, float radius)
        {
            var img = Rect(name, parent).gameObject.AddComponent<Image>();
            img.sprite = ProceduralSprites.Rounded;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = ProceduralSprites.RoundTexRadius / Mathf.Max(0.5f, radius);
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public static Image Icon(string name, Transform parent, IconId icon, Color color, float size)
        {
            var img = Rect(name, parent).gameObject.AddComponent<Image>();
            img.sprite = ProceduralSprites.Icon(icon);
            img.color = color;
            img.raycastTarget = false;
            img.rectTransform.sizeDelta = new Vector2(size, size);
            return img;
        }

        public static TextMeshProUGUI Text(string name, Transform parent, string text, float size, Color color,
            bool bold = true, TextAlignmentOptions align = TextAlignmentOptions.TopLeft)
        {
            var t = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.raycastTarget = false;   // napisy nie łapią dotyku – mniej pracy dla raycastera
            return t;
        }

        // Wysokość tekstu przy danej szerokości – potrzebna, żeby karty rosły przy dwulinijkowych tytułach.
        public static float TextHeight(TextMeshProUGUI t, float width)
        {
            return Mathf.Ceil(t.GetPreferredValues(t.text, width, 0f).y);
        }
    }
}

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FinanceQuest
{
    // Układ A: „Finance Quest” w jednej linii, pod nim motto (menu) albo tytuł (kurs, rozdział) – oba od tej samej
    // lewej krawędzi. Po prawej przycisk.
    // Nagłówek wspólny dla wszystkich ekranów. Przy zmianie ekranu zostaje na miejscu,
    // a płynnie zmienia się tylko tytuł i ikona przycisku (dwa egzemplarze: stary znika, nowy się pojawia).
    public class AppHeader
    {
        public const float Height = 80f;
        const float Margin = 16f, Btn = 40f, LogoY = 12f, SubY = 46f;

        public RectTransform Root;
        TextMeshProUGUI front, back;
        CanvasGroup frontGroup, backGroup;
        Image frontIcon, backIcon;
        Action onRight;
        Action onDev;
        LipCard devButton;
        float titleW;
        CanvasGroup buttonGroup;
        bool frontButton = true, backButton = true;   // czy przycisk jest na nowym / starym ekranie
        bool swapping;

        public static AppHeader Create(RectTransform parent, float width)
        {
            var h = new AppHeader();
            h.Root = Ui.Rect("Header", parent);
            Ui.Place(h.Root, 0, 0, width, Height);

            var logo = Ui.Text("Logo", h.Root, "Finance Quest", 25f, Palette.Brand, FontWeight.ExtraBold, TextAlignmentOptions.TopLeft);
            logo.textWrappingMode = TextWrappingModes.NoWrap;
            Ui.Place(logo.rectTransform, Margin, LogoY, width - 2 * Margin - Btn - 8f, 32);
            logo.enableAutoSizing = true;
            logo.fontSizeMin = 18f;
            logo.fontSizeMax = 25f;

            h.front = MakeTitle(h.Root, width, out h.frontGroup);
            h.back = MakeTitle(h.Root, width, out h.backGroup);
            h.backGroup.alpha = 0f;

            var b = LipCard.Create(h.Root, "RightButton", width - Margin - Btn, 16, Btn, Btn + 3,
                Palette.CardFill, Palette.CardEdge, Palette.CardLip, 12f, 3f, 2f);
            h.frontIcon = Ui.Icon("Icon", b.Content, IconId.Home, Palette.TextLocked, 20f);
            h.backIcon = Ui.Icon("IconOld", b.Content, IconId.Home, Palette.TextLocked, 20f);
            Ui.Center(h.frontIcon.rectTransform, 20, 20);
            Ui.Center(h.backIcon.rectTransform, 20, 20);
            h.backIcon.enabled = false;
            h.buttonGroup = b.gameObject.AddComponent<CanvasGroup>();
            // Przycisk woła aktualną akcję – ekran ustawia ją przy każdym Set().
            b.SetClickable(() => { if (h.onRight != null) h.onRight(); });

            // Przycisk deweloperski (pomiń level na 100%) – obok przycisku po prawej, tylko gdy ekran go ustawi.
            h.titleW = width - 2 * Margin - Btn - 8f;
            h.devButton = LipCard.Create(h.Root, "DevButton", width - Margin - 2 * Btn - 8f, 16, Btn, Btn + 3,
                Palette.Brick, Palette.Brick, Palette.Darker(Palette.Brick, 0.7f), 12f, 3f, 0f);
            var dl = Ui.Text("Label", h.devButton.Content, "DEV", 11f, Color.white, FontWeight.ExtraBold, TextAlignmentOptions.Capline);
            Ui.Stretch(dl.rectTransform);
            h.devButton.SetClickable(() => { if (h.onDev != null) h.onDev(); });
            h.devButton.gameObject.SetActive(false);
            return h;
        }

        static TextMeshProUGUI MakeTitle(RectTransform parent, float width, out CanvasGroup group)
        {
            // Linijka pod logo: zaczyna się dokładnie tam, gdzie logo; kończy przed przyciskiem.
            var t = Ui.Text("Title", parent, "", 17f, Palette.Text, FontWeight.ExtraBold, TextAlignmentOptions.TopLeft);
            Ui.Place(t.rectTransform, Margin, SubY, width - 2 * Margin - Btn - 8f, 24);
            t.fontSizeMin = 13f;
            t.fontSizeMax = 17f;
            group = t.gameObject.AddComponent<CanvasGroup>();
            return t;
        }

        // Wywoływane przez ekran podczas budowania.
        public string CurrentTitle { get { return front.text; } }

        public void Set(string text, bool tagline, IconId icon, Color iconColor, Action onRightClick)
        {
            front.text = text;
            var f = Fonts.Get(tagline ? FontWeight.SemiBold : FontWeight.ExtraBold);
            if (f != null) front.font = f;
            front.color = tagline ? Palette.TextMuted : Palette.Text;
            // Zawsze jedna linia: przy braku miejsca zmniejsza się czcionka, a nie przybywa linii.
            front.enableAutoSizing = true;
            front.textWrappingMode = TextWrappingModes.NoWrap;
            front.fontSizeMax = tagline ? 14f : 17f;
            front.fontSizeMin = tagline ? 11f : 12f;
            front.fontSize = front.fontSizeMax;
            frontIcon.sprite = ProceduralSprites.Icon(icon);
            frontIcon.color = iconColor;
            onRight = onRightClick;
            // Bez akcji = bez przycisku (np. menu – ustawienia są w kokpicie).
            frontButton = onRightClick != null;
            if (!swapping) ApplyButton(frontButton ? 1f : 0f);
        }

        // DEV: tylko w Unity Editorze i w buildzie deweloperskim – w zwykłym buildzie dla użytkowników go nie ma.
        public void SetDev(Action onClick)
        {
            onDev = Debug.isDebugBuild ? onClick : null;
            bool on = onDev != null;
            devButton.gameObject.SetActive(on);
            // Tytuł kończy się przed przyciskiem DEV, żeby na niego nie wchodził.
            var r = front.rectTransform;
            r.sizeDelta = new Vector2(on ? titleW - Btn - 8f : titleW, r.sizeDelta.y);
        }

        void ApplyButton(float alpha)
        {
            buttonGroup.alpha = alpha;
            buttonGroup.blocksRaycasts = frontButton;
        }

        // Przed zbudowaniem nowego ekranu: obecny tytuł staje się „starym”, a nowy dostaje świeży egzemplarz.
        public void BeginSwap()
        {
            var t = front; front = back; back = t;
            var g = frontGroup; frontGroup = backGroup; backGroup = g;
            var i = frontIcon; frontIcon = backIcon; backIcon = i;
            backButton = frontButton;
            frontIcon.enabled = true;
            swapping = true;
            Tick(0f);
        }

        // p = postęp przejścia 0..1. Gdy tytuł się nie zmienia (kurs → rozdział), nic nie miga.
        public void Tick(float p)
        {
            if (!swapping) return;
            bool sameTitle = front.text == back.text;
            bool sameIcon = frontIcon.sprite == backIcon.sprite;
            float outA = 1f - Mathf.Clamp01(p / 0.25f);
            float inA = Mathf.Clamp01((p - 0.15f) / 0.35f);
            // Przycisk pojawia się / znika płynnie, gdy jest tylko na jednym z ekranów.
            ApplyButton(frontButton && backButton ? 1f : frontButton ? inA : backButton ? outA : 0f);
            backGroup.alpha = sameTitle ? 0f : outA;
            frontGroup.alpha = sameTitle ? 1f : inA;
            backIcon.enabled = !sameIcon;
            if (!sameIcon)
            {
                SetAlpha(backIcon, outA);
                SetAlpha(frontIcon, inA);
            }
        }

        public void EndSwap()
        {
            swapping = false;
            frontGroup.alpha = 1f;
            backGroup.alpha = 0f;
            backIcon.enabled = false;
            SetAlpha(frontIcon, 1f);
            ApplyButton(frontButton ? 1f : 0f);
        }

        static void SetAlpha(Image img, float a)
        {
            var c = img.color; c.a = a; img.color = c;
        }
    }
}

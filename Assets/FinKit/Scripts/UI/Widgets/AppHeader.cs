using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FinanceQuest
{
    // Układ A: logo INVESTOMM (znak + napis) w jednej linii, pod nim motto (menu) albo tytuł (kurs, rozdział) – oba od tej samej
    // lewej krawędzi. Po prawej przycisk.
    // Nagłówek wspólny dla wszystkich ekranów. Przy zmianie ekranu zostaje na miejscu,
    // a płynnie zmienia się tylko tytuł i ikona przycisku (dwa egzemplarze: stary znika, nowy się pojawia).
    public class AppHeader
    {
        public const float Height = 96f;   // logo + motto albo logo + tytuł ekranu (24 px, jak nagłówek w treści)
        const float Margin = 16f, Btn = 40f;

        // Układ logo (wariant A): znak ma wysokość wielkich liter INVESTOMM (+3 px nad i pod),
        // a motto WERSALIKAMI idzie pod całym logo od lewej krawędzi znaku.
        // Położenia liczone z metryk czcionki Rubik (na 1 jednostkę rozmiaru): górna linia tekstu i wysokość wielkiej litery.
        const float Asc = 0.935f, Cap = 0.70f;
        const float WordSize = 34f, MottoSize = 10.5f;
        const float BtnY = 16f;                                                 // górna krawędź przycisku po prawej
        // +3 px więcej niż góra przycisku: przycisk ma cień od spodu, więc „na oko” jego środek jest niżej.
        const float CapTop = BtnY + 3f + 3f;
        const float LogoY = CapTop - (Asc - Cap) * WordSize;                    // górna krawędź pola napisu
        const float Baseline = CapTop + Cap * WordSize;                         // linia bazowa INVESTOMM
        const float MarkPad = 3f;                                               // znak wystaje 3 px nad i pod litery
        const float MarkSize = Cap * WordSize + 2f * MarkPad;
        const float TextX = Margin + MarkSize + 8f;                             // napis INVESTOMM obok znaku
        const float SubY = Baseline + 17f - Asc * MottoSize;                    // linia bazowa motta 17 px pod napisem
        // Tytuł ekranu (kurs, rozdział, level) wygląda jak nagłówek w treści lekcji (24 px, ExtraBold) i stoi wyraźnie
        // niżej niż motto, żeby nie wyglądał na część logo. Wielkie litery zaczynają się 20 px pod napisem INVESTCOMM
        // (bliżej treści niż logo – tytuł „należy” do ekranu, nie do logo),
        // czyli już pod przyciskiem po prawej – dlatego tytuł może zająć całą szerokość.
        const float TitleSize = 24f;
        const float TitleY = Baseline + 20f - (Asc - Cap) * TitleSize;
        // Motto: odstępy między literami ok. 0.15 litery – wersaliki z odstępem czyta się lżej.
        const float MottoSpacing = 15f;

        public RectTransform Root;
        TextMeshProUGUI front, back;
        CanvasGroup frontGroup, backGroup;
        Image frontIcon, backIcon;
        Action onRight;
        Action onDev;
        LipCard devButton;
        CanvasGroup buttonGroup;
        bool frontButton = true, backButton = true;   // czy przycisk jest na nowym / starym ekranie
        bool swapping;

        public static AppHeader Create(RectTransform parent, float width)
        {
            var h = new AppHeader();
            h.Root = Ui.Rect("Header", parent);
            Ui.Place(h.Root, 0, 0, width, Height);

            // Logo INVESTOMM: duży znak (schodki) po lewej, obok napis, pod napisem motto / tytuł ekranu.
            var mark = Ui.Rect("LogoMark", h.Root).gameObject.AddComponent<Image>();
            mark.sprite = BrandLogo.Icon;
            mark.raycastTarget = false;
            Ui.Place(mark.rectTransform, Margin, CapTop - MarkPad, MarkSize, MarkSize);
            // „COMM” w ciemniejszym żółtym – czytelny na jasnym tle.
            var logo = Ui.Text("Logo", h.Root, "INVEST<color=#C9A200>COMM</color>", WordSize, Palette.Brand, FontWeight.ExtraBold, TextAlignmentOptions.TopLeft);
            logo.textWrappingMode = TextWrappingModes.NoWrap;
            logo.characterSpacing = 2f;
            Ui.Place(logo.rectTransform, TextX, LogoY, width - TextX - Margin - Btn - 8f, WordSize * 1.3f);
            // Zmniejsza się tylko na bardzo wąskich ekranach (wtedy wyrównanie do znaku jest minimalnie mniej równe).
            logo.enableAutoSizing = true;
            logo.fontSizeMin = 22f;
            logo.fontSizeMax = WordSize;

            h.front = MakeTitle(h.Root, width, out h.frontGroup);
            h.back = MakeTitle(h.Root, width, out h.backGroup);
            h.backGroup.alpha = 0f;

            var b = LipCard.Create(h.Root, "RightButton", width - Margin - Btn, BtnY, Btn, Btn + 3,
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
            h.devButton = LipCard.Create(h.Root, "DevButton", width - Margin - 2 * Btn - 8f, BtnY, Btn, Btn + 3,
                Palette.Brick, Palette.Brick, Palette.Darker(Palette.Brick, 0.7f), 12f, 3f, 0f);
            var dl = Ui.Text("Label", h.devButton.Content, "DEV", 11f, Color.white, FontWeight.ExtraBold, TextAlignmentOptions.Capline);
            Ui.Stretch(dl.rectTransform);
            h.devButton.SetClickable(() => { if (h.onDev != null) h.onDev(); });
            h.devButton.gameObject.SetActive(false);
            return h;
        }

        static TextMeshProUGUI MakeTitle(RectTransform parent, float width, out CanvasGroup group)
        {
            // Linijka pod logo: zaczyna się od lewej krawędzi znaku. Położenie i wielkość ustawia Set() (motto albo tytuł).
            var t = Ui.Text("Title", parent, "", TitleSize, Palette.Text, FontWeight.ExtraBold, TextAlignmentOptions.TopLeft);
            Ui.Place(t.rectTransform, Margin, TitleY, width - 2 * Margin, TitleSize * 1.3f);
            group = t.gameObject.AddComponent<CanvasGroup>();
            return t;
        }

        // Wywoływane przez ekran podczas budowania.
        public string CurrentTitle { get { return front.text; } }

        public void Set(string text, bool tagline, IconId icon, Color iconColor, Action onRightClick)
        {
            // Motto wersalikami z odstępami (wariant A logo); tytuły ekranów zwykłe.
            front.text = tagline ? text.ToUpperInvariant() : text;
            front.characterSpacing = tagline ? MottoSpacing : 0f;
            var f = Fonts.Get(FontWeight.ExtraBold);
            if (f != null) front.font = f;
            front.color = tagline ? Palette.TextMuted : Palette.Text;
            // Zawsze jedna linia: przy braku miejsca zmniejsza się czcionka, a nie przybywa linii.
            front.enableAutoSizing = true;
            front.textWrappingMode = TextWrappingModes.NoWrap;
            // Motto: małe, tuż pod logo. Tytuł: duży, niżej; za długi zmniejsza się do 16 px.
            front.fontSizeMax = tagline ? MottoSize : TitleSize;
            front.fontSizeMin = tagline ? 8f : 16f;
            var fr = front.rectTransform;
            fr.anchoredPosition = new Vector2(fr.anchoredPosition.x, -(tagline ? SubY : TitleY));
            fr.sizeDelta = new Vector2(fr.sizeDelta.x, (tagline ? MottoSize : TitleSize) * 1.3f);
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
            // Tytuł stoi pod przyciskami, więc DEV na niego nie wchodzi – szerokość bez zmian.
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

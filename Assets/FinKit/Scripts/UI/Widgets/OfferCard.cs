using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FinanceQuest
{
    // Blok „karta ofert” na stronie lekcji: ciemna karta z tytułem i logo banku, pod spodem wiersze
    // (np. konta oszczędnościowe z oprocentowaniem albo ETF-y z kolorową plakietką) i żółty przycisk SEE MORE.
    // Rysowana przez aplikację, a nie jako obrazek – dlatego zawsze ma szerokość strony, tekst jest ostry,
    // a przycisk naprawdę się klika. Przycisk co kilka sekund sam się wciska i błyska, żeby było widać, że można go nacisnąć.
    public static class OfferCard
    {
        static readonly Color Bg = Palette.Hex("0B3A45");
        static readonly Color BgLip = Palette.Hex("072830");
        static readonly Color Row = Palette.Hex("164F5C");
        static readonly Color Soft = new Color(1f, 1f, 1f, 0.75f);

        const float Pad = 14f, Lip = 5f, Radius = 18f, RowGap = 8f, Bar = 4f, Badge = 40f;

        public static float Build(RectTransform parent, BlockDef b, Sprite logo, float x, float y, float w)
        {
            // Najpierw wszystko budujemy w karcie o tymczasowej wysokości, a na końcu ustawiamy właściwą
            // (wysokości tekstów znamy dopiero po ich utworzeniu).
            var card = LipCard.Create(parent, "OfferCard", x, y, w, 100f, Bg, Bg, BgLip, Radius, Lip, 0f);
            var c = card.Content;
            float inner = w - 2 * Pad, cy = Pad + 2f;

            // ---- nagłówek: tytuł po lewej, logo + nazwa banku po prawej, wyśrodkowane względem siebie w pionie ----
            const float logoH = 26f;
            float lw = 0f, bw = 0f, logoW = 0f;
            bool hasBrand = !string.IsNullOrEmpty(b.caption);
            TextMeshProUGUI brand = null;
            if (logo != null)
            {
                lw = logoH * logo.rect.width / Mathf.Max(1f, logo.rect.height);
                if (hasBrand)
                {
                    brand = Ui.Text("Brand", c, b.caption, 10.5f, Color.white, FontWeight.ExtraBold, TextAlignmentOptions.MidlineLeft);
                    brand.characterSpacing = 6f;
                    brand.textWrappingMode = TextWrappingModes.NoWrap;
                    bw = brand.GetPreferredValues(b.caption, 1000f, 0f).x;
                }
                logoW = lw + (brand != null ? 6f + bw : 0f);
            }
            float titleW = inner - (logoW > 0f ? logoW + 10f : 0f);
            var title = Ui.Text("Title", c, b.text ?? "", 19f, Color.white, FontWeight.ExtraBold);
            float th = Ui.TextHeight(title, titleW);
            float headH = Mathf.Max(th, logoW > 0f ? logoH : 0f);
            Ui.Place(title.rectTransform, Pad, cy + (headH - th) * 0.5f, titleW, th);
            if (logo != null)
            {
                float ly = cy + (headH - logoH) * 0.5f;
                var img = Ui.Rect("Logo", c).gameObject.AddComponent<Image>();
                img.sprite = logo;
                img.preserveAspect = true;
                img.raycastTarget = false;
                Ui.Place(img.rectTransform, Pad + inner - logoW, ly, lw, logoH);
                if (brand != null) Ui.Place(brand.rectTransform, Pad + inner - bw, ly, bw + 2f, logoH);
            }
            cy += headH + 12f;

            // ---- wiersze ----
            foreach (var o in b.offers)
            {
                cy += BuildRow(c, o, Pad, cy, inner) + RowGap;
            }

            // ---- przycisk ----
            string label = string.IsNullOrEmpty(b.button) ? "SEE MORE" : b.button;
            cy += 4f;
            float btnH = 38f;
            var probe = Ui.Text("Probe", c, label, 13f, Palette.OnAccent, FontWeight.ExtraBold);
            float lwid = probe.GetPreferredValues(label, 1000f, 0f).x;
            UnityEngine.Object.DestroyImmediate(probe.gameObject);
            float btnW = lwid + 18f + 8f + 34f;   // tekst + strzałka + odstęp + marginesy
            BuildButton(c, Pad + inner - btnW, cy, btnW, btnH, label, b.url);
            cy += btnH + 3f + Pad;

            ((RectTransform)card.transform).sizeDelta = new Vector2(w, cy + Lip);
            return cy + Lip;
        }

        // Wiersz: żółty pasek z lewej, opcjonalna plakietka (np. „iShares”), tytuł, wartość po prawej, opis pod spodem.
        static float BuildRow(RectTransform parent, OfferDef o, float x, float y, float w)
        {
            // Żółty pasek z zaokrąglonymi lewymi rogami bez maski (maska = dodatkowe rysowanie na telefonie):
            // żółty zaokrąglony prostokąt pod spodem i tło wiersza przesunięte o szerokość paska nad nim.
            var bar = Ui.Rounded("Bar", parent, Palette.Accent, 10f);
            var bg = Ui.Rounded("Row", parent, Row, 10f);

            bool hasBadge = !string.IsNullOrEmpty(o.badge);
            w -= Bar;   // dalej liczymy wewnątrz tła wiersza (zaczyna się za paskiem)
            float left = 12f, right = 12f, tx = left + (hasBadge ? Badge + 10f : 0f);
            float tw = w - tx - right;

            float vw = 0f;
            TextMeshProUGUI val = null;
            if (!string.IsNullOrEmpty(o.value))
            {
                val = Ui.Text("Value", bg.transform, o.value, 14f, Color.white, FontWeight.SemiBold, TextAlignmentOptions.TopRight);
                val.textWrappingMode = TextWrappingModes.NoWrap;
                vw = Mathf.Min(val.GetPreferredValues(o.value, 1000f, 0f).x, tw * 0.55f);
            }
            var t1 = Ui.Text("Title", bg.transform, o.title ?? "", 14f, Color.white, FontWeight.ExtraBold);
            float t1w = tw - (vw > 0f ? vw + 8f : 0f);
            float t1h = Ui.TextHeight(t1, t1w);
            float h = t1h;
            TextMeshProUGUI t2 = null;
            float t2h = 0f;
            if (!string.IsNullOrEmpty(o.desc))
            {
                t2 = Ui.Text("Desc", bg.transform, o.desc, 12f, Soft, FontWeight.Regular);
                t2h = Ui.TextHeight(t2, tw);
                h += 3f + t2h;
            }
            float content = Mathf.Max(h, hasBadge ? Badge : 0f);
            float rowH = content + 20f;
            Ui.Place(bar.rectTransform, x, y, 24f, rowH);
            Ui.Place(bg.rectTransform, x + Bar, y, w, rowH);

            float ty = 10f + (content - h) * 0.5f;
            Ui.Place(t1.rectTransform, tx, ty, t1w, t1h);
            if (val != null) Ui.Place(val.rectTransform, w - right - vw, ty, vw, t1h);
            if (t2 != null) Ui.Place(t2.rectTransform, tx, ty + t1h + 3f, tw, t2h);

            if (hasBadge)
            {
                Color bc = string.IsNullOrEmpty(o.badgeColor) ? Palette.Brand : Palette.Hex(o.badgeColor.TrimStart('#'));
                var badge = Ui.Rounded("Badge", bg.transform, bc, 8f);
                Ui.Place(badge.rectTransform, left, 10f + (content - Badge) * 0.5f, Badge, Badge);
                var bt = Ui.Text("Label", badge.transform, o.badge, 9f, Color.white, FontWeight.ExtraBold, TextAlignmentOptions.Center);
                bt.textWrappingMode = TextWrappingModes.NoWrap;
                bt.enableAutoSizing = true;
                bt.fontSizeMin = 6f;
                bt.fontSizeMax = 9f;
                Ui.Stretch(bt.rectTransform, 2f, 2f, 2f, 2f);
            }
            return rowH;
        }

        static void BuildButton(RectTransform parent, float x, float y, float w, float h, string label, string url)
        {
            var btn = LipCard.Create(parent, "SeeMore", x, y, w, h + 3f, Palette.Accent, Palette.Accent, Palette.AccentLip, h * 0.5f, 3f, 0f);
            // Błysk: jasny, ukośny pasek przelatuje po przycisku. Maska z kształtu przycisku, żeby nie wychodził
            // poza zaokrąglone końce (jedna maska na jeden przycisk – na telefonie bez znaczenia).
            btn.Face.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var shine = Ui.Rect("Shine", btn.Content).gameObject.AddComponent<Image>();
            shine.color = new Color(1f, 1f, 1f, 0.6f);
            shine.raycastTarget = false;
            var sr = shine.rectTransform;
            sr.anchorMin = sr.anchorMax = new Vector2(0f, 0.5f);
            sr.pivot = new Vector2(0.5f, 0.5f);
            sr.sizeDelta = new Vector2(16f, h * 2f);
            sr.localRotation = Quaternion.Euler(0f, 0f, -20f);
            sr.anchoredPosition = new Vector2(-30f, 0f);
            var t = Ui.Text("Label", btn.Content, label, 13f, Palette.OnAccent, FontWeight.ExtraBold, TextAlignmentOptions.MidlineLeft);
            t.textWrappingMode = TextWrappingModes.NoWrap;
            Ui.Place(t.rectTransform, 17f, 0f, w - 17f - 34f, h);
            // Strzałka w prawo = odwrócona strzałka „wstecz” (czcionka Rubik nie ma znaku →).
            var arrow = Ui.Icon("Arrow", btn.Content, IconId.Back, Palette.OnAccent, 16f);
            Ui.Place(arrow.rectTransform, w - 17f - 16f, (h - 16f) * 0.5f, 16f, 16f);
            arrow.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            arrow.rectTransform.anchoredPosition += new Vector2(8f, -8f);
            arrow.rectTransform.localScale = new Vector3(-1f, 1f, 1f);

            // Link na razie opcjonalny – bez niego przycisk tylko się wciska (z wibracją).
            btn.SetClickable(() => { if (!string.IsNullOrEmpty(url)) Application.OpenURL(url); });

            var pulse = btn.gameObject.AddComponent<OfferPulse>();
            pulse.Button = btn;
            pulse.Shine = sr;
            pulse.Width = w;
        }
    }

    // Co 3 sekundy przycisk sam się wciska (płynnie: w dół, chwila, w górę), a zaraz potem przelatuje po nim błysk –
    // żeby było widać, że można go kliknąć. Liczymy to sami z krzywych (bez sprężynki), więc ruch jest równy,
    // a co klatkę zmienia się tylko położenie dwóch elementów.
    public class OfferPulse : MonoBehaviour
    {
        public LipCard Button;
        public RectTransform Shine;
        public float Width;
        const float Every = 3f;
        const float Down = 0.12f, Hold = 0.08f, Up = 0.2f;          // wciśnięcie
        const float ShineStart = 0.3f, ShineLength = 0.6f;           // błysk tuż po puszczeniu
        float lastShine = -1f;
        float t = 1.2f;      // pierwsza animacja chwilę po wejściu na stronę

        void Update()
        {
            if (Button == null) return;
            t += Time.unscaledDeltaTime;
            float p = t % Every;

            float press = 0f;
            if (p < Down) press = Smooth(p / Down);
            else if (p < Down + Hold) press = 1f;
            else if (p < Down + Hold + Up) press = 1f - Smooth((p - Down - Hold) / Up);
            Button.SetPress(press);

            if (Shine != null)
            {
                float s = Mathf.Clamp01((p - ShineStart) / ShineLength);
                if (s >= 1f) s = 0f;   // po przelocie wraca od razu na start (schowany z lewej) – i stoi
                if (s != lastShine) { lastShine = s; Shine.anchoredPosition = new Vector2(Mathf.Lerp(-30f, Width + 30f, Smooth(s)), 0f); }
            }
        }

        static float Smooth(float x) { return x * x * (3f - 2f * x); }
    }
}

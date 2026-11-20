using TMPro;
using UnityEngine;

namespace FinanceQuest
{
    // Kokpit nad kursami: trofea na półkach, po 3 w rzędzie. Bez karty pod spodem – stoi wprost na tle
    // (jeden pusty kontener, żeby kaskada animowała go w całości). Ustawienia są pod zębatką w nagłówku.
    public static class Cockpit
    {
        static readonly Color Wood = Palette.Hex("B9875A");
        const float Pad = 4f;          // wyrównanie z nagłówkiem „KURSY”
        const int PerShelf = 3;
        const float TW = 54f, TH = 62f;   // rozmiar pucharu na półce

        // Zwraca wysokość kokpitu.
        public static float Build(RectTransform parent, float x, float y, float w, FinanceQuestApp app)
        {
            var root = Ui.Rect("Cockpit", parent);
            Ui.Place(root, x, y, w, 300);
            float inner = w - 2 * Pad;
            float cy = 0f;

            // Trofea: „Start” (za rozpoczęcie edukacji) + po jednym na kurs.
            int total = 1 + app.Courses.Count, earned = app.Progress.Started ? 1 : 0;
            foreach (var c in app.Courses) if (app.Progress.IsCourseComplete(c)) earned++;

            Section(root, "TWOJE TROFEA", Pad, cy, inner * 0.7f);
            var cnt = Ui.Text("Count", root, earned + " / " + total, 13f, Palette.Text, FontWeight.ExtraBold, TextAlignmentOptions.CaplineRight);
            Ui.Place(cnt.rectTransform, Pad + inner - 60, cy, 60, 16);
            cy += 26f;

            float slot = inner / PerShelf;
            for (int start = 0; start < total; start += PerShelf)
            {
                for (int i = start; i < Mathf.Min(start + PerShelf, total); i++)
                {
                    Sprite art;
                    string name;
                    bool got;
                    if (i == 0)
                    {
                        // Trofeum za rozpoczęcie: żółty puchar z petrolową gwiazdką – odróżnia się od pucharów kursów.
                        got = app.Progress.Started;
                        name = "Start";
                        art = got ? TrophyArt.Earned(Palette.Accent, Palette.AccentLip, Palette.Brand) : TrophyArt.Locked();
                    }
                    else
                    {
                        var course = app.Courses[i - 1];
                        got = app.Progress.IsCourseComplete(course);
                        name = course.ShortName;
                        art = got ? TrophyArt.Earned(course.Color, Palette.Darker(course.Color, 0.78f), Palette.Accent) : TrophyArt.Locked();
                    }
                    float sx = Pad + slot * (i - start);
                    var img = Ui.Rect("Trophy_" + name, root).gameObject.AddComponent<UnityEngine.UI.Image>();
                    img.sprite = art;
                    img.raycastTarget = false;
                    Ui.Place(img.rectTransform, sx + (slot - TW) * 0.5f, cy, TW, TH);

                    var lab = Ui.Text("Label", root, name, 12f, got ? Palette.Text : Palette.TextLocked, FontWeight.SemiBold, TextAlignmentOptions.Capline);
                    lab.textWrappingMode = TextWrappingModes.NoWrap;
                    lab.enableAutoSizing = true;
                    lab.fontSizeMin = 9f;
                    lab.fontSizeMax = 12f;
                    Ui.Place(lab.rectTransform, sx + 2, cy + TH + 4, slot - 4, 14);
                }
                cy += TH + 22f;

                // Deska półki z ciemniejszą wargą (ten sam „kreskówkowy” styl co karty).
                var lip = Ui.Rounded("PlankLip", root, Palette.Darker(Wood, 0.75f), 5f);
                Ui.Place(lip.rectTransform, Pad, cy, inner, 13);
                var top = Ui.Rounded("Plank", root, Wood, 5f);
                Ui.Place(top.rectTransform, Pad, cy, inner, 9);
                cy += 13f + 16f;
            }

            cy -= 12f;
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
}

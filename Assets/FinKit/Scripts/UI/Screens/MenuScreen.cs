using UnityEngine;

namespace FinanceQuest
{
    // Ekran 1: kokpit (trofea + ustawienia) i lista kursów z paskiem postępu każdego kursu.
    public static class MenuScreen
    {
        public static void Build(RectTransform screen, FinanceQuestApp app)
        {
            float W = app.ScreenWidth;
            // Zębatka w prawym górnym rogu otwiera ekran ustawień.
            app.Header.Set("an easy and fun way to learn finance", true, IconId.Gear, Palette.TextMuted,
                () => app.Push(r => SettingsScreen.Build(r, app)));

            var content = ScreenKit.Scroll(screen, W, 0f);
            var last = app.LastCourse;
            if (last != null) app.SwipeForward = () => app.Push(r => CourseScreen.Build(r, app, last), true);
            float y = 6f, cw = W - 2 * ScreenKit.Margin;

            // Wstęp: „Myśl na dziś” na tle nocnej Wall Street – dużo przestrzeni, nic nie przytłacza.
            y += DailyHero.Build(content, ScreenKit.Margin, y, cw) + 24f;

            // Kokpit nad kursami: półki z trofeami.
            y += Cockpit.Build(content, ScreenKit.Margin, y, cw, app) + 18f;

            // „Kontynuuj”: następny level w kursie, który właśnie robisz (znika, gdy wszystko ukończone).
            float cont = ContinueCard.Build(content, ScreenKit.Margin, y, cw, app);
            if (cont > 0f) y += cont + 22f;

            // Zaraz po ankiecie: krótki komunikat, ile kursów polecamy (pokazuje się raz).
            var recommended = Recommendations.Get();
            if (Recommendations.JustFinished && recommended.Count > 0)
            {
                Recommendations.JustFinished = false;
                y += RecommendNote(content, ScreenKit.Margin, y, cw, recommended.Count) + 16f;
            }

            var label = Ui.Text("CoursesLabel", content, "COURSES", 12.5f, Palette.TextMuted, FontWeight.ExtraBold, TMPro.TextAlignmentOptions.CaplineLeft);
            label.characterSpacing = 4f;
            Ui.Place(label.rectTransform, ScreenKit.Margin + 4, y, cw, 16);
            y += 16f + 10f;

            // Polecane kursy na górze (w kolejności z ankiety), potem reszta.
            var ordered = new System.Collections.Generic.List<CourseData>();
            foreach (var id in recommended) foreach (var c in app.Courses) if (c.Id == id) ordered.Add(c);
            foreach (var c in app.Courses) if (!ordered.Contains(c)) ordered.Add(c);
            // Wszystkie karty mają tę samą wysokość – liczoną dla najwyższego przypadku (plakietka + 2 linie tytułu).
            var layout = CardLayout.Measure(content, cw, recommended.Count > 0);
            foreach (var course in ordered)
                y += CourseCard(content, ScreenKit.Margin, y, cw, course, app, recommended.Contains(course.Id), layout) + 14f;
            ScreenKit.SetContentHeight(content, y + 24f);
        }

        // Cała karta w kolorze kursu, po prawej ilustracja tematyczna (mieści się w karcie z marginesem).
        // Ciemny pasek „Mamy dla Ciebie N kursów” – wynik ankiety (wariant C).
        static float RecommendNote(RectTransform parent, float x, float y, float w, int count)
        {
            const float pad = 14f, icon = 26f, h = 62f;
            var bg = Ui.Rounded("RecommendNote", parent, Palette.Text, 16f);
            Ui.Place(bg.rectTransform, x, y, w, h);
            var ic = Ui.Icon("Spark", bg.transform, IconId.Spark, Palette.Accent, icon);
            Ui.Place(ic.rectTransform, pad, (h - icon) * 0.5f, icon, icon);
            float tx = pad + icon + 12f, tw = w - tx - pad;
            var t1 = Ui.Text("Title", bg.transform, "We have " + ContentText.Plural(count, "course", "courses", "courses") + " for you", 14.5f, Color.white);
            Ui.Place(t1.rectTransform, tx, 12f, tw, 20f);
            var t2 = Ui.Text("Sub", bg.transform, count == 1 ? "We marked it as “Recommended”" : "We marked them as “Recommended”", 12f,
                new Color(1f, 1f, 1f, 0.7f), FontWeight.SemiBold);
            Ui.Place(t2.rectTransform, tx, 33f, tw, 18f);
            return h;
        }

        // Żółta plakietka „★ POLECANY DLA CIEBIE”; zwraca jej wysokość.
        static float RecommendedTag(Transform parent, float x, float y)
        {
            const float bh = 22f, star = 12f;
            var tag = Ui.Text("Label", parent, "RECOMMENDED FOR YOU", 11f, Palette.OnAccent, FontWeight.ExtraBold, TMPro.TextAlignmentOptions.CaplineLeft);
            tag.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            float bw = tag.GetPreferredValues("RECOMMENDED FOR YOU", 1000f, 0f).x + star + 22f;
            var pill = Ui.Rounded("RecommendedTag", parent, Palette.Accent, 8f);
            Ui.Place(pill.rectTransform, x, y, bw, bh);
            var si = Ui.Icon("Star", pill.transform, IconId.Star, Palette.OnAccent, star);
            Ui.Place(si.rectTransform, 8f, (bh - star) * 0.5f, star, star);
            tag.transform.SetParent(pill.transform, false);
            Ui.Place(tag.rectTransform, 8f + star + 4f, 0f, bw - star - 14f, bh);
            return bh;
        }

        // Wymiary wspólne dla wszystkich kart kursów (liczone raz na ekran).
        class CardLayout
        {
            public const float Pad = 16f, ArtMargin = 10f, Gap = 8f, TagGap = 8f, TagH = 22f, TitleSize = 17f;
            public float TextW, TwoLines, Face;

            public static CardLayout Measure(Transform parent, float w, bool anyTag)
            {
                var l = new CardLayout();
                l.TextW = w - Pad - Illustrations.BoxW - ArtMargin - Gap;
                // Wysokość dwóch linii tytułu mierzymy na próbce – zależy od fontu i rozmiaru.
                var probe = Ui.Text("Probe", parent, "Ag\nAg", TitleSize, Color.white);
                l.TwoLines = Ui.TextHeight(probe, l.TextW);
                Object.DestroyImmediate(probe.gameObject);
                // plakietka (jeśli jakaś karta ją ma) + 2 linie tytułu + podpis + odstęp + pasek
                float h = Pad + (anyTag ? TagH + TagGap : 0f) + l.TwoLines + 19f + 12f + 16f + Pad;
                l.Face = Mathf.Max(h, Illustrations.BoxH + 16f);
                return l;
            }
        }

        static float CourseCard(RectTransform parent, float x, float y, float w, CourseData course, FinanceQuestApp app, bool recommended,
            CardLayout l)
        {
            const float pad = CardLayout.Pad;
            float value = app.Progress.CourseProgress(course);
            var card = LipCard.Create(parent, "Course_" + course.Id, x, y, w, l.Face + ScreenKit.Lip,
                course.Color, course.Color, Palette.Darker(course.Color), ScreenKit.CardRadius, ScreenKit.Lip, 0f);

            // Góra: plakietka „★ POLECANY DLA CIEBIE” (tylko polecane), pod nią tytuł (najwyżej 2 linie, dłuższy z „…”) i podpis.
            float top = pad;
            if (recommended) top += RecommendedTag(card.Content, pad, pad - 2f) + CardLayout.TagGap;
            var t = Ui.Text("Title", card.Content, course.Title, CardLayout.TitleSize, Color.white);
            t.overflowMode = TMPro.TextOverflowModes.Ellipsis;
            t.maxVisibleLines = 2;
            float th = Mathf.Min(Ui.TextHeight(t, l.TextW), l.TwoLines);
            Ui.Place(t.rectTransform, pad, top, l.TextW, th);
            string sub = ContentText.Plural(course.Chapters.Count, "chapter", "chapters", "chapters") + " · " +
                         ContentText.Plural(course.LevelCount, "level", "levels", "levels");
            var st = Ui.Text("Subtitle", card.Content, sub, 12.5f, Palette.WhiteSoft, FontWeight.SemiBold);
            Ui.Place(st.rectTransform, pad, top + th + 1f, l.TextW, 18f);
            // Gwiazdki całego kursu za podpisem.
            int sg, sm;
            app.Progress.StarTotals(course, out sg, out sm);
            float sw = st.GetPreferredValues(sub, 1000f, 0f).x;
            ScreenKit.StarCount(card.Content, pad + sw + 10f, top + th + 1f, sg, sm, Palette.Accent, Color.white);

            // Dół: pasek zawsze przy dolnej krawędzi – procenty w kolejnych kartach stoją w jednej linii.
            ScreenKit.BarRow(card.Content, pad, l.Face - pad - 16f, l.TextW, value, Palette.TrackOnColor, Color.white, ScreenKit.Pct(value));

            // Ilustracja wyśrodkowana w pionie (AddIllustration kotwiczy ją do środka prawej krawędzi).
            // Ilustracje są przycięte do rysunku, a tu wpasowane w pole z zachowaniem proporcji –
            // dzięki temu mała sadzonka i szeroki wykres zajmują podobnie dużo miejsca.
            var art = card.AddIllustration(Illustrations.Get(course.Art, course.Color), Illustrations.BoxW, Illustrations.BoxH, CardLayout.ArtMargin);
            art.preserveAspect = true;
            // preserveAspect dosuwa obrazek do strony pivota – przy pivocie z prawej wąskie ilustracje (kiełek, drzewo)
            // lądowały przy prawej krawędzi. Pivot na środku = każda ilustracja wyśrodkowana w swoim polu.
            art.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            art.rectTransform.anchoredPosition = new Vector2(-CardLayout.ArtMargin - Illustrations.BoxW * 0.5f, 0f);

            card.SetClickable(() => app.Push(r => CourseScreen.Build(r, app, course)));
            return l.Face + ScreenKit.Lip;
        }
    }
}

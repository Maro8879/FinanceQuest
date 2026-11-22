using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace FinanceQuest
{
    // Ekran ankiety: pasek postępu, „PYTANIE 1 Z 2”, pytanie i odpowiedzi jako kafelki.
    // Kliknięty kafelek robi się żółty z ptaszkiem; DALEJ przechodzi dalej, po ostatnim pytaniu – wynik w menu.
    public class SurveyScreen
    {
        const float BarHeight = 96f, ButtonH = 58f, AnswerGap = 10f;

        readonly FinanceQuestApp app;
        readonly List<SurveyQuestion> questions;
        readonly SurveyAnswer[] chosen;
        readonly RectTransform screen;
        int index;

        public static void Build(RectTransform screen, FinanceQuestApp app)
        {
            new SurveyScreen(screen, app, Survey.Questions(app.Courses));
        }

        SurveyScreen(RectTransform screen, FinanceQuestApp app, List<SurveyQuestion> questions)
        {
            this.screen = screen;
            this.app = app;
            this.questions = questions;
            chosen = new SurveyAnswer[questions.Count];
            // Domek = pomiń ankietę (można ją powtórzyć w ustawieniach).
            app.Header.Set("Let's find your courses", true, IconId.Home, Palette.TextLocked, () => { Recommendations.Skip(); app.Back(); });
            Show();
        }

        // Cały ekran budujemy od nowa przy każdym wyborze – to kilka kafelków, więc jest szybko i prosto.
        void Show()
        {
            for (int i = screen.childCount - 1; i >= 0; i--) Object.Destroy(screen.GetChild(i).gameObject);
            float W = app.ScreenWidth, m = ScreenKit.Margin, cw = W - 2 * m;
            var q = questions[index];

            var content = ScreenKit.Scroll(screen, W, 0f);
            ((RectTransform)content.parent).offsetMin = new Vector2(0f, BarHeight);
            float y = 6f;

            ProgressBar.Create(content, m, y, cw, 12f, Palette.Track, (index + (chosen[index] != null ? 1f : 0.5f)) / questions.Count);
            y += 12f + 22f;

            var kick = Ui.Text("Kicker", content, "QUESTION " + (index + 1) + " OF " + questions.Count, 11.5f, Palette.TextMuted);
            kick.characterSpacing = 6f;
            Ui.Place(kick.rectTransform, m, y, cw, 16f);
            y += 22f;

            var title = Ui.Text("Question", content, q.Text, 22f, Palette.Text);
            float th = Ui.TextHeight(title, cw);
            Ui.Place(title.rectTransform, m, y, cw, th);
            y += th + 18f;

            for (int i = 0; i < q.Answers.Count; i++)
                y += Answer(content, m, y, cw, q.Answers[i], chosen[index] == q.Answers[i]) + AnswerGap;
            ScreenKit.SetContentHeight(content, y + 16f);

            // DALEJ przyklejony do dołu; nieaktywny (szary), dopóki nic nie wybrano.
            var bar = Ui.Rect("BottomBar", screen);
            bar.anchorMin = new Vector2(0f, 0f);
            bar.anchorMax = new Vector2(1f, 0f);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.anchoredPosition = Vector2.zero;
            bar.sizeDelta = new Vector2(0f, BarHeight);
            bool ready = chosen[index] != null;
            bool last = index == questions.Count - 1;
            var btn = LipCard.Create(bar, "Next", m, 18f, cw, ButtonH,
                ready ? Palette.Accent : Palette.LockedFill, ready ? Palette.Accent : Palette.LockedFill, ready ? Palette.AccentLip : Palette.LockedLip, 16f, 5f, 0f);
            var label = Ui.Text("Label", btn.Content, last ? "SEE RECOMMENDED COURSES" : "NEXT", 16f,
                ready ? Palette.OnAccent : Palette.TextLocked, FontWeight.ExtraBold, TextAlignmentOptions.Capline);
            Ui.Stretch(label.rectTransform);
            if (ready) btn.SetClickable(Next);
        }

        float Answer(RectTransform parent, float x, float y, float w, SurveyAnswer a, bool selected)
        {
            const float pad = 14f, dot = 14f;
            float textX = pad + (a.Dot.HasValue ? dot + 12f : 0f);
            float tw = w - textX - pad - (selected ? 30f : 0f);
            var probe = Ui.Text("Text", parent, a.Text, 15f, Palette.Text);
            float th = Ui.TextHeight(probe, tw);
            float h = Mathf.Max(48f, th + 2 * pad);

            var card = LipCard.Create(parent, "Answer", x, y, w, h + 4f,
                selected ? Palette.Hex("FFF6D1") : Palette.CardFill, selected ? Palette.Accent : Palette.CardEdge,
                selected ? Palette.AccentLip : Palette.CardLip, 16f, 4f, 2f);
            if (a.Dot.HasValue)
            {
                var d = Ui.Rounded("Dot", card.Content, a.Dot.Value, dot * 0.5f);
                Ui.Place(d.rectTransform, pad, (h - dot) * 0.5f, dot, dot);
            }
            probe.transform.SetParent(card.Content, false);
            Ui.Place(probe.rectTransform, textX, (h - th) * 0.5f, tw, th);
            if (selected)
            {
                var ic = Ui.Icon("Check", card.Content, IconId.Check, Palette.AccentLip, 20f);
                Ui.Place(ic.rectTransform, w - pad - 22f, (h - 20f) * 0.5f, 20f, 20f);
            }
            card.SetClickable(() => { chosen[index] = a; Show(); });
            return h + 4f;
        }

        void Next()
        {
            if (index < questions.Count - 1) { index++; Show(); return; }
            Recommendations.Save(new List<SurveyAnswer>(chosen));
            app.Home();   // wynik pokazuje menu: polecane kursy na górze z plakietką (także gdy ankieta była z ustawień)
        }
    }
}

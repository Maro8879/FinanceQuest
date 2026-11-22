using System.Collections.Generic;
using UnityEngine;

namespace FinanceQuest
{
    // Ankieta na start: pytania z odpowiedziami, każda odpowiedź dodaje punkty wybranym kursom.
    // Kursy z największą liczbą punktów są „polecane” (na górze menu, z plakietką).
    public class SurveyAnswer
    {
        public string Text;
        public Color? Dot;                                          // kropka w kolorze kursu (gdy odpowiedź = kurs)
        public Dictionary<string, int> Points = new Dictionary<string, int>();   // id kursu -> punkty
    }

    public class SurveyQuestion
    {
        public string Text;
        public List<SurveyAnswer> Answers = new List<SurveyAnswer>();
    }

    public static class Survey
    {
        // Id kursów (nazwy folderów w Content).
        const string Basics = "invest", Short = "horyzont-0-2", Medium = "horyzont-3-10", Long = "horyzont-10-40",
                     Trading = "credit", Platform = "obsluga-platformy-inwestycyjne";

        // Ankieta na start: 5 pytań o użytkownika. Każda odpowiedź daje punkty kursom, które do niego pasują;
        // 2 kursy z największą liczbą punktów są „polecane”. Punkty dla kursów, których nie ma w aplikacji,
        // są pomijane – żeby nie polecać czegoś, czego nie da się otworzyć.
        public static List<SurveyQuestion> Questions(List<CourseData> courses)
        {
            var have = new HashSet<string>();
            foreach (var c in courses) have.Add(c.Id);
            var list = new List<SurveyQuestion>();

            System.Func<string, SurveyQuestion> Q = text => { var q = new SurveyQuestion { Text = text }; list.Add(q); return q; };
            System.Action<SurveyQuestion, string, object[]> A = (q, text, pts) =>
            {
                var a = new SurveyAnswer { Text = text };
                for (int i = 0; i + 1 < pts.Length; i += 2)
                    if (have.Contains((string)pts[i])) a.Points[(string)pts[i]] = (int)pts[i + 1];
                q.Answers.Add(a);
            };

            var q1 = Q("Have you ever invested money?");
            A(q1, "Never, I'm a complete beginner", new object[] { Basics, 3 });
            A(q1, "I have savings, but never invested", new object[] { Basics, 2, Short, 1 });
            A(q1, "I've bought a few stocks or ETFs", new object[] { Platform, 2, Medium, 1 });
            A(q1, "I invest regularly", new object[] { Long, 2, Trading, 1 });

            // Wiek: im młodszy, tym dłużej może inwestować – więcej punktów dla długiego horyzontu.
            var q2 = Q("How old are you?");
            A(q2, "Under 25", new object[] { Long, 2, Basics, 1 });
            A(q2, "25–34", new object[] { Long, 2, Medium, 1 });
            A(q2, "35–49", new object[] { Medium, 2, Long, 1 });
            A(q2, "50+", new object[] { Short, 2, Medium, 1 });

            var q3 = Q("What's your main goal?");
            A(q3, "Build an emergency fund", new object[] { Short, 2 });
            A(q3, "Save for something big (car, flat, trip)", new object[] { Medium, 2 });
            A(q3, "Grow my wealth long-term", new object[] { Long, 2 });
            A(q3, "Try active trading", new object[] { Trading, 3 });

            var q4 = Q("How do you feel about risk?");
            A(q4, "I don't want to lose anything – safety first", new object[] { Short, 3 });
            A(q4, "Small ups and downs are OK", new object[] { Medium, 2 });
            A(q4, "I accept big swings for higher long-term gains", new object[] { Long, 2 });
            A(q4, "I like taking risks", new object[] { Trading, 2, Basics, 1 });

            var q5 = Q("Do you already have an investment account?");
            A(q5, "Yes, and I use it", new object[] { Trading, 1 });
            A(q5, "Yes, but I don't know how to use it", new object[] { Platform, 3 });
            A(q5, "No, but I want one", new object[] { Platform, 2 });
            A(q5, "No, not yet", new object[] { Basics, 1 });

            return list;
        }
    }

    // Wynik ankiety zapamiętany na telefonie: czy już była i które kursy polecamy (w kolejności).
    public static class Recommendations
    {
        public const string DoneKey = "fq.survey.done", ListKey = "fq.survey.recommended";
        const int MaxCourses = 2;

        // PROTOTYP: zawsze polecamy te dwa kursy, niezależnie od odpowiedzi (także po pominięciu ankiety).
        // Przed wydaniem ustaw na false – wtedy polecenia liczą się z punktów za odpowiedzi.
        public const bool PrototypeFixed = true;
        static readonly string[] PrototypeCourses = { "horyzont-0-2", "invest" };   // Short Horizon, Investing Basics

        // Ustawiane zaraz po ankiecie – menu pokazuje wtedy raz komunikat „Mamy dla Ciebie…”.
        public static bool JustFinished;

        public static bool Done { get { return PlayerPrefs.GetInt(DoneKey, 0) == 1; } }

        public static List<string> Get()
        {
            var s = PlayerPrefs.GetString(ListKey, "");
            return string.IsNullOrEmpty(s) ? new List<string>() : new List<string>(s.Split(','));
        }

        public static bool IsRecommended(CourseData c) { return Get().Contains(c.Id); }

        // Sumuje punkty z odpowiedzi i zapisuje najlepsze kursy (tylko te, które dostały jakiekolwiek punkty).
        public static void Save(List<SurveyAnswer> chosen)
        {
            var score = new Dictionary<string, int>();
            var order = new List<string>();
            foreach (var a in chosen)
                if (a != null)
                    foreach (var p in a.Points)
                    {
                        if (!score.ContainsKey(p.Key)) { score[p.Key] = 0; order.Add(p.Key); }
                        score[p.Key] += p.Value;
                    }
            // Najwięcej punktów pierwsze; przy remisie – ten wybrany wcześniej.
            order.Sort((x, y) => score[y] != score[x] ? score[y].CompareTo(score[x]) : order.IndexOf(x).CompareTo(order.IndexOf(y)));
            var best = new List<string>();
            foreach (var id in order) if (score[id] > 0 && best.Count < MaxCourses) best.Add(id);
            if (PrototypeFixed) best = new List<string>(PrototypeCourses);
            PlayerPrefs.SetString(ListKey, string.Join(",", best.ToArray()));
            PlayerPrefs.SetInt(DoneKey, 1);
            PlayerPrefs.Save();
            JustFinished = best.Count > 0;
        }

        // Pominięta ankieta: nie pytamy ponownie przy starcie (można ją powtórzyć w ustawieniach).
        public static void Skip()
        {
            if (PrototypeFixed)
            {
                PlayerPrefs.SetString(ListKey, string.Join(",", PrototypeCourses));
                JustFinished = true;
            }
            PlayerPrefs.SetInt(DoneKey, 1);
            PlayerPrefs.Save();
        }
    }
}

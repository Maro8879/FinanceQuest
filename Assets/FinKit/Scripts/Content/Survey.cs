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
        // TESTOWO – wyłącz przed wydaniem: 2 pytania, odpowiedziami są nazwy 4–5 losowych kursów,
        // a wybrany kurs dostaje punkty. Prawdziwe pytania przygotujesz sam w edytorze treści.
        public static List<SurveyQuestion> Test(List<CourseData> courses)
        {
            var list = new List<SurveyQuestion>();
            string[] questions = { "Który kurs brzmi dla Ciebie najciekawiej?", "Od którego kursu chcesz zacząć?" };
            var rng = new System.Random();
            foreach (var text in questions)
            {
                var q = new SurveyQuestion { Text = text };
                var pool = new List<CourseData>(courses);
                int count = Mathf.Min(pool.Count, rng.Next(4, 6));   // 4 albo 5 odpowiedzi
                for (int i = 0; i < count; i++)
                {
                    int k = rng.Next(pool.Count);
                    var c = pool[k];
                    pool.RemoveAt(k);
                    var a = new SurveyAnswer { Text = c.Title, Dot = c.Color };
                    a.Points[c.Id] = 3;
                    q.Answers.Add(a);
                }
                list.Add(q);
            }
            return list;
        }
    }

    // Wynik ankiety zapamiętany na telefonie: czy już była i które kursy polecamy (w kolejności).
    public static class Recommendations
    {
        public const string DoneKey = "fq.survey.done", ListKey = "fq.survey.recommended";
        const int MaxCourses = 2;

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
            PlayerPrefs.SetString(ListKey, string.Join(",", best.ToArray()));
            PlayerPrefs.SetInt(DoneKey, 1);
            PlayerPrefs.Save();
            JustFinished = best.Count > 0;
        }

        // Pominięta ankieta: nie pytamy ponownie przy starcie (można ją powtórzyć w ustawieniach).
        public static void Skip()
        {
            PlayerPrefs.SetInt(DoneKey, 1);
            PlayerPrefs.Save();
        }
    }
}

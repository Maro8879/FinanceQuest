using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FinanceQuest
{
    // Punkt startowy: tworzy Canvas, trzyma treść i postęp, przełącza ekrany.
    // Cały interfejs powstaje z kodu, więc scena może być pusta.
    public class FinanceQuestApp : MonoBehaviour
    {
        // Wymiary projektu (jak w makiecie). Na telefonie skalujemy tak, żeby ta szerokość zawsze się mieściła.
        public const float DesignWidth = 390f, DesignHeight = 844f;
        // Na tablecie / szerokim oknie Editora treść nie rozlewa się na całą szerokość.
        public const float MaxContentWidth = 520f;

        public static FinanceQuestApp Instance { get; private set; }

        public List<CourseData> Courses { get; private set; }
        public ProgressStore Progress { get; private set; }

        Canvas canvas;
        RectTransform safeRoot, screenHost, body, currentScreen;
        public AppHeader Header { get; private set; }
        CascadeTransition transition;
        SwipeInput swipe;

        // Dokąd prowadzi swipe w lewo z bieżącego ekranu (ustawia ekran przy budowaniu; null = nigdzie).
        public Action SwipeForward;
        // Ekran może sam obsłużyć „wstecz” (np. filmik na pełnym ekranie najpierw się zamyka). true = obsłużone.
        [System.NonSerialized] public Func<bool> BackOverride;
        // Ostatnio oglądany kurs – cel swipe'a w lewo w menu.
        [System.NonSerialized] public CourseData LastCourse;   // tylko w pamięci – Unity nie ma tego zapisywać
        // Co właśnie się odblokowało – ekran rozdziału / kursu pokaże to animacją (raz) i wyczyści.
        [System.NonSerialized] public string UnlockedLevel, UnlockedChapter;

        // Reset postępu ze wszystkich kursów (z ustawień). Czyścimy też to, co pamiętamy tylko w trakcie sesji.
        public void ResetProgress()
        {
            Progress.ResetAll();
            LastCourse = null;
            UnlockedLevel = UnlockedChapter = null;
        }

        // Wołane po ukończeniu levelu: zapisuje postęp i zapamiętuje, co się przez to otworzyło.
        public void CompleteLevel(CourseData course, LevelData level)
        {
            Progress.Complete(level);
            UnlockedLevel = UnlockedChapter = null;
            for (int c = 0; c < course.Chapters.Count; c++)
            {
                var ch = course.Chapters[c];
                for (int i = 0; i < ch.Levels.Count; i++)
                {
                    if (Progress.LevelState(course, c, i) != ItemState.Current) continue;
                    UnlockedLevel = ch.Levels[i].Id;
                    // Pierwszy level nowego rozdziału = otworzył się cały rozdział.
                    if (i == 0 && c > 0 && !ch.Levels.Contains(level)) UnlockedChapter = ch.Id;
                    return;
                }
            }
        }
        readonly List<Action<RectTransform>> stack = new List<Action<RectTransform>>();
        float navLockUntil;
        int lastW, lastH;
        Rect lastSafe;

        // Startujemy sami po załadowaniu sceny – nie trzeba niczego ustawiać w Inspektorze.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoStart()
        {
            if (FindAnyObjectByType<FinanceQuestApp>() != null) return;
            new GameObject("FinanceQuestApp").AddComponent<FinanceQuestApp>();
        }

        void Awake()
        {
            Instance = this;
            // Telefony domyślnie trzymają 30 FPS – animacje sprężynek wyglądają wtedy szarpanie.
            Application.targetFrameRate = 60;

            // Treść z importera (pliki JSON); dopóki jej nie ma – przykładowa treść z kodu.
            Courses = ContentLoader.Load() ?? DemoContent.Build();
            swipe = new SwipeInput();
            Progress = ProgressStore.Load();
            BuildCanvas();
            EnsureEventSystem();
        }

        void Start()
        {
            ApplyScale();
            // Pierwsze uruchomienie: od razu ankieta (pod nią menu – domek albo koniec ankiety wraca do menu).
            if (Recommendations.Done) Push(r => MenuScreen.Build(r, this));
            else PushPath(r => MenuScreen.Build(r, this), r => SurveyScreen.Build(r, this));
        }

        void Update()
        {
            // Obrót ekranu / zmiana okna Game w Editorze → przeliczamy skalę i budujemy ekran od nowa.
            if (Screen.width != lastW || Screen.height != lastH || Screen.safeArea != lastSafe)
            {
                ApplyScale();
                Rebuild(Nav.None);
            }
            if (transition != null && transition.Step(Time.unscaledDeltaTime, Header)) EndTransition();
            if (BackPressed() && (BackOverride == null || !BackOverride())) Back();

            if (transition == null)
            {
                int s = swipe.Poll(canvas.scaleFactor);
                if (s > 0) BackSwipe();
                else if (s < 0 && SwipeForward != null) SwipeForward();
            }
        }

        // ---------------- nawigacja ----------------

        public void Push(Action<RectTransform> build) { Push(build, false); }

        // horizontal = przejście po swipie: karty wjeżdżają z boku, zgodnie z ruchem palca.
        public void Push(Action<RectTransform> build, bool horizontal)
        {
            if (Time.unscaledTime < navLockUntil) return;   // chroni przed podwójnym tapnięciem
            stack.Add(build);
            Rebuild(Nav.Forward, horizontal);
        }

        // Kilka ekranów naraz (np. kurs → rozdział → level z karty „Kontynuuj”): jedno przejście,
        // a „wstecz” cofa potem po kolei przez rozdział i kurs.
        public void PushPath(params Action<RectTransform>[] builds)
        {
            if (Time.unscaledTime < navLockUntil || builds.Length == 0) return;
            stack.AddRange(builds);
            Rebuild(Nav.Forward);
        }

        // Ustawia ekran, z którego nie wolno wyjść swipem w prawo (level – łatwo przez przypadek przerwać lekcję lub test).
        // Przycisk w nagłówku i systemowe „wstecz” dalej działają.
        [System.NonSerialized] public bool BlockSwipeBack;

        // Podmienia bieżący ekran na inny (np. level → następny level): „wstecz” z nowego wraca tam, gdzie wróciłby stary.
        public void ReplaceTop(Action<RectTransform> build)
        {
            if (stack.Count == 0 || Time.unscaledTime < navLockUntil) return;
            stack[stack.Count - 1] = build;
            Rebuild(Nav.Forward, true);
        }

        void BackSwipe()
        {
            if (BlockSwipeBack) return;
            if (stack.Count <= 1 || Time.unscaledTime < navLockUntil) return;
            stack.RemoveAt(stack.Count - 1);
            Rebuild(Nav.Back, true);
        }

        public void Back()
        {
            if (stack.Count <= 1 || Time.unscaledTime < navLockUntil) return;
            stack.RemoveAt(stack.Count - 1);
            Rebuild(Nav.Back);
        }

        // Przebudowa bieżącego ekranu w miejscu (bez przejścia), z zachowaniem przewinięcia listy.
        [System.NonSerialized] public bool Refreshing;
        public void Refresh()
        {
            if (currentScreen == null) return;
            var oldList = currentScreen.Find("Scroll/Content") as RectTransform;
            Vector2 scroll = oldList != null ? oldList.anchoredPosition : Vector2.zero;
            Refreshing = true;
            try { Rebuild(Nav.None); }
            finally { Refreshing = false; }
            var list = currentScreen.Find("Scroll/Content") as RectTransform;
            if (list != null) list.anchoredPosition = scroll;
        }

        public void Home()
        {
            if (stack.Count <= 1 || Time.unscaledTime < navLockUntil) return;
            stack.RemoveRange(1, stack.Count - 1);
            Rebuild(Nav.Back);
        }

        enum Nav { None, Forward, Back }

        // Ekran zawsze budujemy od zera – po powrocie widać świeży postęp i nie trzeba nic synchronizować.
        // Stary ekran żyje jeszcze chwilę, żeby mógł płynnie zniknąć.
        void Rebuild(Nav nav, bool horizontal = false)
        {
            if (stack.Count == 0) return;
            navLockUntil = Time.unscaledTime + 0.25f;
            EndTransition();   // poprzednie przejście (jeśli trwa) kończymy od razu

            var old = currentScreen;
            bool animate = nav != Nav.None || old == null;
            if (!animate && old != null) { old.gameObject.SetActive(false); Destroy(old.gameObject); old = null; }

            if (animate && old != null) Header.BeginSwap();
            SwipeForward = null;   // nowy ekran sam ustawi, dokąd prowadzi swipe w lewo
            BlockSwipeBack = false;
            BackOverride = null;
            currentScreen = Ui.Rect("Screen", body);
            Ui.Stretch(currentScreen);
            currentScreen.gameObject.AddComponent<CanvasGroup>();
            stack[stack.Count - 1](currentScreen);

            if (animate)
                transition = new CascadeTransition(old, currentScreen, nav == Nav.Back, horizontal, ScreenHeight - AppHeader.Height);
            if (animate && old != null) Header.Tick(0f);
        }

        void EndTransition()
        {
            if (transition == null) return;
            transition.Finish();
            if (transition.OldScreen != null)
            {
                transition.OldScreen.gameObject.SetActive(false);
                Destroy(transition.OldScreen.gameObject);
            }
            Header.EndSwap();
            transition = null;
        }

        public static void RunLater(float delay, Action action)
        {
            if (Instance != null) Instance.StartCoroutine(Instance.Later(delay, action));
        }

        IEnumerator Later(float delay, Action action)
        {
            yield return new WaitForSecondsRealtime(delay);
            action();
        }

        // ---------------- budowa sceny ----------------

        void BuildCanvas()
        {
            var go = new GameObject("UI", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Bez CanvasScalera: skalę liczymy sami (ApplyScale), żeby znać dokładną szerokość
            // już w chwili budowania ekranu (CanvasScaler ustawia ją dopiero w swoim Update).
            go.AddComponent<GraphicRaycaster>();

            var bg = Ui.Rect("Background", go.transform).gameObject.AddComponent<Image>();
            bg.color = Palette.Background;
            bg.raycastTarget = false;
            Ui.Stretch(bg.rectTransform);

            safeRoot = Ui.Rect("SafeArea", go.transform);
            screenHost = Ui.Rect("ScreenHost", safeRoot);
            // Ekrany siedzą pod nagłówkiem; nagłówek jest wspólny i nie znika przy przejściach.
            body = Ui.Rect("Body", screenHost);
            Ui.Stretch(body, 0, AppHeader.Height, 0, 0);
        }

        void ApplyScale()
        {
            lastW = Screen.width;
            lastH = Screen.height;
            lastSafe = Screen.safeArea;

            float scale = Mathf.Min(lastW / DesignWidth, lastH / DesignHeight);
            canvas.scaleFactor = Mathf.Max(0.1f, scale);

            // Notch / zaokrąglone rogi telefonu: treść tylko w bezpiecznym obszarze.
            safeRoot.anchorMin = new Vector2(lastSafe.xMin / lastW, lastSafe.yMin / lastH);
            safeRoot.anchorMax = new Vector2(lastSafe.xMax / lastW, lastSafe.yMax / lastH);
            safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;

            float safeWidth = lastSafe.width / canvas.scaleFactor;
            float w = Mathf.Min(safeWidth, MaxContentWidth);
            screenHost.anchorMin = new Vector2(0.5f, 0f);
            screenHost.anchorMax = new Vector2(0.5f, 1f);
            screenHost.pivot = new Vector2(0.5f, 0.5f);
            screenHost.anchoredPosition = Vector2.zero;
            screenHost.sizeDelta = new Vector2(w, 0f);

            // Nagłówek budujemy na nowo tylko, gdy zmieniła się szerokość.
            if (Header == null || Mathf.Abs(Header.Root.sizeDelta.x - w) > 0.5f)
            {
                EndTransition();
                if (Header != null) Destroy(Header.Root.gameObject);
                Header = AppHeader.Create(screenHost, w);
            }
        }

        // Ekrany pytają o szerokość, żeby ręcznie rozłożyć elementy.
        public float ScreenWidth { get { return screenHost.sizeDelta.x; } }
        public float ScreenHeight { get { return lastSafe.height / canvas.scaleFactor; } }

        static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }

        // Przycisk „wstecz” na Androidzie przychodzi jako Escape.
        static bool BackPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            return kb != null && kb.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }
    }
}

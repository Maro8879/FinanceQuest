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
        RectTransform safeRoot, screenHost, currentScreen;
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

            Courses = DemoContent.Build();
            Progress = ProgressStore.Load();
            BuildCanvas();
            EnsureEventSystem();
        }

        void Start()
        {
            ApplyScale();
            Push(r => MenuScreen.Build(r, this));
        }

        void Update()
        {
            // Obrót ekranu / zmiana okna Game w Editorze → przeliczamy skalę i budujemy ekran od nowa.
            if (Screen.width != lastW || Screen.height != lastH || Screen.safeArea != lastSafe)
            {
                ApplyScale();
                Rebuild();
            }
            if (BackPressed()) Back();
        }

        // ---------------- nawigacja ----------------

        public void Push(Action<RectTransform> build)
        {
            if (Time.unscaledTime < navLockUntil) return;   // chroni przed podwójnym tapnięciem
            stack.Add(build);
            Rebuild();
        }

        public void Back()
        {
            if (stack.Count <= 1 || Time.unscaledTime < navLockUntil) return;
            stack.RemoveAt(stack.Count - 1);
            Rebuild();
        }

        public void Home()
        {
            if (stack.Count <= 1 || Time.unscaledTime < navLockUntil) return;
            stack.RemoveRange(1, stack.Count - 1);
            Rebuild();
        }

        // Ekran zawsze budujemy od zera – po powrocie widać świeży postęp i nie trzeba nic synchronizować.
        void Rebuild()
        {
            if (stack.Count == 0) return;
            navLockUntil = Time.unscaledTime + 0.15f;
            if (currentScreen != null)
            {
                currentScreen.gameObject.SetActive(false);
                Destroy(currentScreen.gameObject);
            }
            currentScreen = Ui.Rect("Screen", screenHost);
            Ui.Stretch(currentScreen);
            var group = currentScreen.gameObject.AddComponent<CanvasGroup>();
            stack[stack.Count - 1](currentScreen);
            StartCoroutine(FadeIn(group));
        }

        // Krótkie pojawienie się ekranu. Zmiana alfy CanvasGroup nie przebudowuje siatek, więc jest tania.
        static IEnumerator FadeIn(CanvasGroup g)
        {
            const float time = 0.14f;
            float t = 0f;
            g.alpha = 0f;
            while (g != null && t < time)
            {
                t += Time.unscaledDeltaTime;
                g.alpha = Mathf.Clamp01(t / time);
                yield return null;
            }
            if (g != null) g.alpha = 1f;
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

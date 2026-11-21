using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;

namespace FinanceQuest
{
    // Filmik w lekcji: obraz w karcie z wargą, pod nim pasek (play/pauza, przewijanie, czas).
    // Filmik otwiera się sam na pełnym ekranie i od razu gra (jak reels). Na pełnym ekranie tap = pauza / dalej,
    // ✕ w rogu (albo „wstecz”) i koniec filmu = powrót do lekcji. W lekcji zostaje karta – tap otwiera film ponownie.
    // Gdy filmik ma drugą wersję z napisami, w pasku jest przycisk „CC” – przełącza wersję w tym samym momencie filmu.
    // Przy przełączaniu przenosimy ten sam obiekt (nie tworzymy drugiego odtwarzacza),
    // więc filmik gra dalej bez przerwy i bez ponownego wczytywania.
    public class VideoBlock : MonoBehaviour, IPointerClickHandler,
        IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        const float BarH = 36f, Lip = 4f, Border = 2f, Radius = 16f;
        const float MaxInlineH = 260f;     // poziomy / kwadratowy filmik
        const float MaxReelH = 400f;       // pionowy (reels 9:16) – wyższy, ale zostaje miejsce na tekst lekcji
        const float PlayW = 42f, TimeW = 84f, TimeWShort = 42f, Big = 54f;
        const float Corner = 36f, CornerGap = 10f;   // okrągłe przyciski w rogach (✕ i CC)
        static readonly Color CornerBg = new Color(0f, 0f, 0f, 0.45f);
        const string SubsKey = "fq_video_subs";   // wybór napisów pamiętamy dla wszystkich filmików

        VideoPlayer player;
        RawImage picture;
        RectTransform stage, home, overlay, bar, track, fill, knob, bigPlay;
        Image playIcon, ccBg;
        TextMeshProUGUI timeLabel, ccLabel;
        RectTransform cc, close, skip;
        VideoClip mainClip, subClip;
        RenderTexture hold;         // ostatnia klatka na czas przełączania wersji – bez mignięcia czernią
        bool subsOn, switching, swapReady, resumePlay;
        bool starting;              // play już kliknięty / auto-start, ale film jeszcze się wczytuje
        double resumeTime;
        Vector2 inlineSize;
        float aspect, trackW, timeW = TimeW, shownK = -1f, dragK;
        double length;
        bool full, dragging, shownPlaying;
        int shownSec = -1;

        // Zwraca wysokość bloku razem z podpisem.
        // autoOpen: po wejściu na stronę filmik sam otwiera się na pełnym ekranie i gra (pierwszy filmik na stronie).
        public static float Build(RectTransform parent, BlockDef b, float x, float y, float cw, bool autoOpen = false)
        {
            // Wersja bez napisów jest główna; gdy jest tylko jedna z dwóch, gra ona i nie ma przycisku CC.
            var clip = b.clip != null ? b.clip : b.clipSub;
            var sub = b.clip != null ? b.clipSub : null;
            float aspect = clip != null && clip.height > 0 ? (float)clip.width / clip.height : 16f / 9f;
            float w = cw;
            float vh = (w - 2 * Border) / aspect;
            float maxH = aspect < 1f ? MaxReelH : MaxInlineH;
            if (vh > maxH)
            {
                // Karta dokładnie w proporcjach filmu (np. 9:16) – bez czarnych pasów po bokach.
                vh = maxH;
                w = vh * aspect + 2 * Border;
            }
            float h = vh + BarH + 2 * Border;
            float px = x + (cw - w) * 0.5f;
            var card = LipCard.Create(parent, "Video", px, y, w, h + Lip, Palette.CardFill, Palette.CardEdge, Palette.CardLip, Radius, Lip, Border);

            if (clip == null)
            {
                var miss = Ui.Text("Missing", card.Content, "No video\n" + b.video, 12.5f, Palette.TextMuted, FontWeight.SemiBold, TextAlignmentOptions.Center);
                Ui.Stretch(miss.rectTransform, 10, 10, 10, 10);
            }
            else
            {
                // Zaokrąglona maska, żeby rogi filmu nie wystawały poza kartę.
                var mask = Ui.Rounded("Mask", card.Face, Color.white, Radius - Border);
                Ui.Stretch(mask.rectTransform, Border, Border, Border, Border);
                mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
                var vb = Ui.Rect("Stage", mask.transform).gameObject.AddComponent<VideoBlock>();
                vb.Init(clip, sub, aspect, mask.rectTransform, new Vector2(w - 2 * Border, vh + BarH));
                if (autoOpen) vb.AutoOpen();
            }

            float used = h + Lip;
            if (!string.IsNullOrEmpty(b.caption))
            {
                var c = Ui.Text("Caption", parent, ContentBlocks.Rich(b.caption), 12.5f, Palette.TextMuted, FontWeight.SemiBold);
                c.richText = true;
                float ch = Ui.TextHeight(c, cw);
                Ui.Place(c.rectTransform, x, y + used + 6f, cw, ch);
                used += 6f + ch;
            }
            return used;
        }

        void Init(VideoClip clip, VideoClip sub, float aspect, RectTransform home, Vector2 size)
        {
            mainClip = clip;
            subClip = sub;
            subsOn = sub != null && PlayerPrefs.GetInt(SubsKey, 0) == 1;
            this.aspect = aspect;
            this.home = home;
            inlineSize = size;
            length = clip.length;
            stage = (RectTransform)transform;
            Ui.Stretch(stage);
            stage.gameObject.AddComponent<RectMask2D>();   // przycina film wypełniający pełny ekran (patrz Layout)

            // Ciemne tło łapie tap w obraz (także w pasach po bokach węższego filmiku).
            var bg = gameObject.AddComponent<Image>();
            bg.color = Palette.Text;

            picture = Ui.Rect("Picture", stage).gameObject.AddComponent<RawImage>();
            picture.raycastTarget = false;
            picture.color = Color.clear;   // do pierwszej klatki – inaczej byłby biały prostokąt

            var circle = Ui.Rounded("BigPlay", stage, Palette.Accent, Big * 0.5f);
            bigPlay = circle.rectTransform;
            var bi = Ui.Icon("Icon", bigPlay, IconId.Play, Palette.OnAccent, 26f);
            Ui.Center(bi.rectTransform, 26f, 26f, 2f, 0f);   // trójkąt optycznie w środku koła

            // ✕ tylko na pełnym ekranie – tap w obraz pauzuje, więc wyjście musi mieć własny przycisk.
            var closeBg = Ui.Rounded("Close", stage, CornerBg, Corner * 0.5f);
            closeBg.raycastTarget = true;
            closeBg.gameObject.AddComponent<TapCatcher>().OnTap = Minimize;
            close = closeBg.rectTransform;
            var ci = Ui.Icon("Icon", close, IconId.Close, Color.white, 16f);
            Ui.Center(ci.rectTransform, 16f, 16f);

            // Pomiń filmik: okrągły przycisk jak ✕ i CC, w lewym dolnym rogu nad paskiem – tylko na pauzie na pełnym ekranie.
            var skipBg = Ui.Rounded("Skip", stage, CornerBg, Corner * 0.5f);
            skipBg.raycastTarget = true;
            skipBg.gameObject.AddComponent<TapCatcher>().OnTap = Skip;
            skip = skipBg.rectTransform;
            var si = Ui.Icon("Icon", skip, IconId.Skip, Color.white, 16f);
            Ui.Center(si.rectTransform, 16f, 16f, 1f, 0f);

            bar = Ui.Rect("Bar", stage);
            var barBg = bar.gameObject.AddComponent<Image>();
            barBg.color = new Color(0f, 0f, 0f, 0.28f);
            bar.gameObject.AddComponent<TapCatcher>();   // tap w pasek nie przełącza pełnego ekranu

            var playHit = Ui.Rect("Play", bar);
            playHit.gameObject.AddComponent<Image>().color = Color.clear;
            playHit.gameObject.AddComponent<TapCatcher>().OnTap = TogglePlay;
            Ui.Place(playHit, 0, 0, PlayW, BarH);
            playIcon = Ui.Icon("Icon", playHit, IconId.Play, Color.white, 18f);
            Ui.Center(playIcon.rectTransform, 18f, 18f, 2f, 0f);

            track = Ui.Rect("Track", bar);
            track.gameObject.AddComponent<Image>().color = Color.clear;   // cała wysokość paska łapie palec, nie tylko cienka linia
            track.gameObject.AddComponent<Scrubber>().OnScrub = Scrub;
            var line = Ui.Rounded("Line", track, new Color(1f, 1f, 1f, 0.3f), 2f);
            fill = Ui.Rounded("Fill", track, Palette.Accent, 2f).rectTransform;
            knob = Ui.Rounded("Knob", track, Palette.Accent, 7f).rectTransform;

            timeLabel = Ui.Text("Time", bar, "", 12f, Color.white, FontWeight.SemiBold, TextAlignmentOptions.MidlineRight);

            if (sub != null)
            {
                // CC w prawym górnym rogu, wygląda jak ✕ – widać go także, gdy film gra (pasek wtedy znika).
                cc = Ui.Rect("Subs", stage);
                cc.gameObject.AddComponent<Image>().color = Color.clear;   // pole dotyku trochę większe niż kółko
                cc.gameObject.AddComponent<TapCatcher>().OnTap = ToggleSubs;
                ccBg = Ui.Rounded("Circle", cc, CornerBg, Corner * 0.5f);
                Ui.Center(ccBg.rectTransform, Corner, Corner);
                ccLabel = Ui.Text("Label", ccBg.transform, "CC", 14f, Color.white, FontWeight.ExtraBold, TextAlignmentOptions.Center);
                Ui.Stretch(ccLabel.rectTransform);
                ShowSubsState();
            }

            player = gameObject.AddComponent<VideoPlayer>();
            player.playOnAwake = false;
            player.source = VideoSource.VideoClip;
            player.clip = subsOn ? sub : clip;
            // APIOnly: bierzemy gotową teksturę dekodera, bez dodatkowej kopii do RenderTexture – mniej pracy dla telefonu.
            player.renderMode = VideoRenderMode.APIOnly;
            player.audioOutputMode = VideoAudioOutputMode.Direct;
            player.isLooping = false;
            player.skipOnDrop = true;   // na słabszym telefonie lepiej zgubić klatkę, niż rozjechać obraz z dźwiękiem
            // Po końcu: z powrotem do lekcji, a film na start – gotowy do ponownego obejrzenia.
            player.loopPointReached += p => { p.Pause(); p.time = 0; if (full) Minimize(); };
            player.prepareCompleted += OnPrepared;
            player.seekCompleted += p => { if (switching) FinishSwitch(); };
            player.Prepare();   // pierwsza klatka służy za okładkę, zanim ktoś kliknie

            // Gdy ekran lekcji znika (np. przebudowa), pełny ekran musi zniknąć razem z nim – inaczej filmik zostałby na wierzchu.
            home.gameObject.AddComponent<Guard>().Owner = this;

            Layout(inlineSize.x, inlineSize.y);
        }

        // Rozkład wewnątrz „sceny” o rozmiarze w×h: obraz (z zachowaniem proporcji) i pasek na dole.
        void Layout(float w, float h)
        {
            // Na pełnym ekranie film zajmuje całą wysokość, a pasek (tylko na pauzie) leży na nim.
            float vh = full ? h : h - BarH;
            float pw = w, ph = w / aspect;
            if (ph > vh) { ph = vh; pw = vh * aspect; }
            // Na pełnym ekranie reels wypełnia ekran jak w Instagramie: zamiast pasów ucinamy odrobinę z boków
            // (najwyżej ~15%, żeby nie obciąć napisów ani ważnych rzeczy przy krawędzi).
            float fillK = Mathf.Max(w / pw, vh / ph);
            if (full && fillK > 1f && fillK < 1.18f) { pw *= fillK; ph *= fillK; }
            Ui.Place(picture.rectTransform, (w - pw) * 0.5f, (vh - ph) * 0.5f, pw, ph);
            Ui.Place(bigPlay, (w - Big) * 0.5f, (vh - Big) * 0.5f, Big, Big);

            Ui.Place(bar, 0, h - BarH, w, BarH);
            Ui.Place(close, CornerGap, CornerGap, Corner, Corner);
            close.gameObject.SetActive(full);
            Ui.Place(skip, CornerGap, h - BarH - CornerGap - Corner, Corner, Corner);
            skip.gameObject.SetActive(full && !player.isPlaying);
            if (cc != null) Ui.Place(cc, w - CornerGap - Corner - 4f, CornerGap - 4f, Corner + 8f, Corner + 8f);
            // Wąska karta (reels w treści): sam bieżący czas zamiast „0:12 / 0:45”, żeby suwak był wygodny.
            timeW = w < 300f ? TimeWShort : TimeW;
            shownSec = -1;
            trackW = Mathf.Max(20f, w - PlayW - timeW - 14f);
            Ui.Place(track, PlayW, 0, trackW, BarH);
            Ui.Place((RectTransform)track.Find("Line"), 0, BarH * 0.5f - 2f, trackW, 4f);
            Ui.Place(timeLabel.rectTransform, w - timeW - 12f, 0, timeW, BarH);
            shownK = -1f;
            shownPlaying = !player.isPlaying;   // wymusza odświeżenie ikon przy najbliższym Update
            Update();
        }

        void Update()
        {
            if (player == null) return;
            // W trakcie przełączania wersji trzymamy zapamiętaną klatkę, aż nowa wersja dojdzie do tego samego miejsca.
            if (hold != null && swapReady && player.texture != null) ReleaseHold();
            var tex = player.texture;
            if (hold == null && tex != picture.texture)
            {
                picture.texture = tex;
                picture.color = tex != null ? Color.white : Color.clear;
            }

            if (starting && player.isPlaying) starting = false;
            // Do pierwszej klatki udajemy „gra” – inaczej na moment mignąłby duży przycisk play.
            bool playing = switching ? resumePlay : player.isPlaying || starting;
            if (playing != shownPlaying)
            {
                shownPlaying = playing;
                playIcon.sprite = ProceduralSprites.Icon(playing ? IconId.Pause : IconId.Play);
                bigPlay.gameObject.SetActive(!playing);
                bar.gameObject.SetActive(!full || !playing);   // na pełnym ekranie pasek tylko na pauzie – nic nie zasłania filmu
                skip.gameObject.SetActive(full && !playing);
            }

            // Pozycja suwaka: przy przeciąganiu palec, inaczej czas filmu. Przesuwamy tylko, gdy coś się zmieniło.
            double t = dragging ? dragK * length : switching ? resumeTime : player.time;
            float k = length > 0 ? Mathf.Clamp01((float)(t / length)) : 0f;
            if (Mathf.Abs(k - shownK) * trackW > 0.5f)
            {
                shownK = k;
                Ui.Place(fill, 0, BarH * 0.5f - 2f, Mathf.Max(4f, k * trackW), 4f);
                Ui.Place(knob, k * trackW - 7f, BarH * 0.5f - 7f, 14f, 14f);
            }
            int sec = (int)t;
            if (sec != shownSec)
            {
                shownSec = sec;
                timeLabel.text = timeW < TimeW ? Clock(t) : Clock(t) + " / " + Clock(length);
            }

            // Na pełnym ekranie poziomy ruch palca to przewijanie filmu, nie zmiana strony.
            if (full) SwipeInput.Suppress = true;
        }

        static string Clock(double s)
        {
            int t = Mathf.Max(0, (int)s);
            return (t / 60) + ":" + (t % 60).ToString("00");
        }

        // Pełny ekran od razu przy budowaniu strony – bez mignięcia małej karty w lekcji.
        // Nakładka leży nad całym ekranem aplikacji, więc animacja przejścia strony jej nie rusza.
        void AutoOpen()
        {
            Maximize();
            starting = true;
            player.Play();
        }

        // ---------- swipe ----------
        // Na pełnym ekranie szybki ruch palcem w lewo = pomiń filmik (jak przycisk ⏭).
        // W lekcji przeciąganie oddajemy liście, żeby dało się przewijać stronę, zaczynając palcem na filmiku.
        const float SwipeMin = 70f, SwipeTime = 0.6f, SwipeRatio = 1.6f;
        Vector2 dragStart;
        float dragStartTime;

        ScrollRect scrollParent;
        ScrollRect ScrollParent { get { if (scrollParent == null) scrollParent = GetComponentInParent<ScrollRect>(); return scrollParent; } }

        public void OnInitializePotentialDrag(PointerEventData e) { if (!full && ScrollParent != null) ScrollParent.OnInitializePotentialDrag(e); }

        public void OnBeginDrag(PointerEventData e)
        {
            if (!full) { if (ScrollParent != null) ScrollParent.OnBeginDrag(e); return; }
            dragStart = LocalPoint(e);
            dragStartTime = Time.unscaledTime;
        }

        public void OnDrag(PointerEventData e) { if (!full && ScrollParent != null) ScrollParent.OnDrag(e); }

        public void OnEndDrag(PointerEventData e)
        {
            if (!full) { if (ScrollParent != null) ScrollParent.OnEndDrag(e); return; }
            // Kierunek liczymy w układzie filmu – na obróconym (poziomym) filmiku „w lewo” to w lewo względem obrazu.
            Vector2 d = LocalPoint(e) - dragStart;
            if (Time.unscaledTime - dragStartTime <= SwipeTime && d.x <= -SwipeMin && Mathf.Abs(d.x) >= Mathf.Abs(d.y) * SwipeRatio)
            {
                Haptics.Tap();
                Skip();
            }
        }

        public void OnScroll(PointerEventData e) { if (!full && ScrollParent != null) ScrollParent.OnScroll(e); }

        Vector2 LocalPoint(PointerEventData e)
        {
            Vector2 p;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(stage, e.position, e.pressEventCamera, out p);
            return p;
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (full) TogglePlay();
            else
            {
                Haptics.Tap();
                Maximize();
                if (!player.isPlaying) player.Play();
            }
        }

        void TogglePlay()
        {
            Haptics.Tap();
            if (switching) { resumePlay = !resumePlay; return; }   // zadziała, gdy druga wersja będzie gotowa
            if (starting) { starting = false; player.Pause(); return; }
            if (player.isPlaying) player.Pause();
            else player.Play();
        }

        void Scrub(float k, bool done)
        {
            dragging = !done;
            dragK = k;
            if (!done) return;
            // Szukamy w filmie dopiero po puszczeniu – skakanie co klatkę przy przeciąganiu zacina słabsze telefony.
            if (switching) resumeTime = k * length;
            else if (player.canSetTime) player.time = k * length;
        }

        // ---------- napisy: dwie wersje tego samego filmu ----------

        void ToggleSubs()
        {
            Haptics.Tap();
            subsOn = !subsOn;
            PlayerPrefs.SetInt(SubsKey, subsOn ? 1 : 0);
            ShowSubsState();

            if (!switching)
            {
                resumeTime = player.isPrepared ? player.time : 0;
                resumePlay = player.isPlaying;
                HoldFrame();
            }
            // Jeden odtwarzacz zamiast dwóch naraz – dwa dekodery wideo to za dużo dla przeciętnego telefonu.
            switching = true;
            swapReady = false;
            player.clip = subsOn ? subClip : mainClip;
            player.Prepare();
        }

        void OnPrepared(VideoPlayer p)
        {
            if (!switching) return;
            if (resumeTime > 0.05 && p.canSetTime) p.time = resumeTime;   // dalej w seekCompleted
            else FinishSwitch();
        }

        void FinishSwitch()
        {
            switching = false;
            if (resumePlay) player.Play();
            swapReady = true;
        }

        // Kopia bieżącej klatki – zostaje na ekranie, gdy odtwarzacz wczytuje drugą wersję.
        void HoldFrame()
        {
            var src = picture.texture;
            if (src == null || hold != null) return;
            hold = RenderTexture.GetTemporary(src.width, src.height, 0);
            Graphics.Blit(src, hold);
            picture.texture = hold;
        }

        void ReleaseHold()
        {
            if (hold == null) return;
            if (picture.texture == hold) picture.texture = null;
            RenderTexture.ReleaseTemporary(hold);
            hold = null;
        }

        void ShowSubsState()
        {
            if (ccBg == null) return;
            // Tło jak przy ✕; włączone napisy = żółte litery, wyłączone = przygaszone białe.
            ccLabel.color = subsOn ? Palette.Accent : new Color(1f, 1f, 1f, 0.6f);
        }

        void Maximize()
        {
            var canvas = home.GetComponentInParent<Canvas>().rootCanvas;
            var root = (RectTransform)canvas.transform;
            overlay = Ui.Rect("VideoFullscreen", root);
            Ui.Stretch(overlay);
            overlay.gameObject.AddComponent<Image>().color = Color.black;
            overlay.gameObject.AddComponent<TapCatcher>().OnTap = TogglePlay;   // tap w czarne pole obok obrazu działa jak tap w obraz

            // Obraz i pasek tylko w bezpiecznym obszarze (notch, zaokrąglone rogi); czarne tło na cały ekran.
            float s = canvas.scaleFactor;
            Rect safe = Screen.safeArea;
            float l = safe.xMin / s, r = (Screen.width - safe.xMax) / s, bt = safe.yMin / s, tp = (Screen.height - safe.yMax) / s;
            Vector2 size = root.rect.size - new Vector2(l + r, tp + bt);

            // Poziomy filmik na pionowym ekranie obracamy o 90° – wystarczy obrócić telefon w lewo.
            // Aplikacja zostaje w pionie, bo zmiana orientacji przebudowałaby lekcję od pierwszej strony.
            bool rotate = aspect > 1.05f && size.y > size.x;
            float sw = rotate ? size.y : size.x, sh = rotate ? size.x : size.y;
            stage.SetParent(overlay, false);
            Ui.Center(stage, sw, sh, (l - r) * 0.5f, (bt - tp) * 0.5f);
            stage.localRotation = Quaternion.Euler(0f, 0f, rotate ? -90f : 0f);
            full = true;
            Layout(sw, sh);

            var app = FinanceQuestApp.Instance;
            if (app != null) app.BackOverride = OnBack;   // „wstecz” na Androidzie najpierw zamyka pełny ekran
        }

        void Minimize()
        {
            if (!full) return;
            Haptics.Tap();
            full = false;
            // Zamknięcie = koniec oglądania: film staje (w lekcji nie gra „w tle” pod tekstem).
            starting = false;
            if (switching) resumePlay = false; else player.Pause();
            stage.SetParent(home, false);
            stage.localRotation = Quaternion.identity;
            Ui.Stretch(stage);
            Layout(inlineSize.x, inlineSize.y);
            if (overlay != null) Destroy(overlay.gameObject);
            overlay = null;
            ReleaseBack();
        }

        bool OnBack() { Minimize(); return true; }

        // Pomiń: zamyka filmik i kończy lekcję – przejście do kolejnego levelu (albo do zadań, jeśli lekcja je ma).
        void Skip()
        {
            Minimize();
            var app = FinanceQuestApp.Instance;
            if (app == null) return;
            if (app.SkipLesson != null) app.SkipLesson();
            else if (app.SwipeForward != null) app.SwipeForward();
        }

        void ReleaseBack()
        {
            var app = FinanceQuestApp.Instance;
            if (app != null && app.BackOverride == (Func<bool>)OnBack) app.BackOverride = null;
        }

        void HomeGone()
        {
            if (overlay != null) Destroy(overlay.gameObject);   // razem z nim znika scena i odtwarzacz
            overlay = null;
            ReleaseBack();
        }

        void OnDestroy() { ReleaseBack(); ReleaseHold(); }

        // Pilnuje karty w treści: gdy znika, sprząta pełny ekran.
        class Guard : MonoBehaviour
        {
            public VideoBlock Owner;
            void OnDestroy() { if (Owner != null) Owner.HomeGone(); }
        }

        // Łapie tap (bez niego tap „przeleciałby” wyżej i przełączył pełny ekran). Przewijanie listy działa dalej.
        class TapCatcher : MonoBehaviour, IPointerClickHandler
        {
            public Action OnTap;
            public void OnPointerClick(PointerEventData e) { if (OnTap != null) OnTap(); }
        }

        // Suwak czasu: palec na pasku = pozycja w filmie (działa też na obróconym pełnym ekranie).
        class Scrubber : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
        {
            public Action<float, bool> OnScrub;
            public void OnPointerDown(PointerEventData e) { SwipeInput.Suppress = true; Send(e, false); }
            public void OnBeginDrag(PointerEventData e) { SwipeInput.Suppress = true; }
            public void OnDrag(PointerEventData e) { SwipeInput.Suppress = true; Send(e, false); }
            public void OnEndDrag(PointerEventData e) { Send(e, true); }
            public void OnPointerUp(PointerEventData e) { Send(e, true); }
            public void OnPointerClick(PointerEventData e) { }

            void Send(PointerEventData e, bool done)
            {
                var rt = (RectTransform)transform;
                Vector2 local;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, e.position, e.pressEventCamera, out local)) return;
                Rect r = rt.rect;
                if (OnScrub != null) OnScrub(Mathf.Clamp01((local.x - r.xMin) / Mathf.Max(1f, r.width)), done);
            }
        }
    }
}

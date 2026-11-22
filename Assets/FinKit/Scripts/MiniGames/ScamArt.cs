using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FinanceQuest
{
    // Grafiki gry „Swipe the Scam” (Resources/ScamArt): zdjęcia kart, zapasowe ilustracje i lupa.
    // Wczytujemy raz i trzymamy w pamięci – przebudowa ekranu po każdej karcie nie czyta plików od nowa.
    public static class ScamArt
    {
        // Ilustracja karty (zapas, gdy brak zdjęcia): 240 × 90 jednostek. Zdjęcie karty (photo_*.jpg): szerokość / wysokość.
        public const float IllW = 240f, IllH = 90f;
        public const float PhotoAspect = 1.216f;

        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static Sprite Get(string name)
        {
            Sprite s;
            if (cache.TryGetValue(name, out s)) return s;
            s = Resources.Load<Sprite>("ScamArt/" + name);
            if (s == null)
            {
                // Gdy Unity zaimportowało plik jako zwykłą teksturę (przed ustawieniami importu) – robimy sprite sami.
                var tex = Resources.Load<Texture2D>("ScamArt/" + name);
                if (tex != null) s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            }
            cache[name] = s;
            return s;
        }

        public static Image Picture(string name, Transform parent, string sprite)
        {
            var img = Ui.Rect(name, parent).gameObject.AddComponent<Image>();
            img.sprite = Get(sprite);
            img.preserveAspect = true;
            img.raycastTarget = false;
            if (img.sprite == null) img.color = new Color(0f, 0f, 0f, 0f);   // brak pliku – nic nie rysujemy zamiast białego prostokąta
            return img;
        }

        // Zdjęcie przycięte „na wypełnienie” (jak object-fit: cover) w zaokrąglonej ramce – bez rozciągania,
        // niezależnie od szerokości ekranu. Ramka (maska) jest zwracana, zdjęcie leży w niej.
        public static Image Photo(string name, Transform parent, Sprite photo, float x, float y, float w, float h, float radius)
        {
            var frame = Ui.Rounded(name, parent, Color.white, radius);
            Ui.Place(frame.rectTransform, x, y, w, h);
            frame.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            var raw = Ui.Rect("Photo", frame.transform).gameObject.AddComponent<RawImage>();
            raw.raycastTarget = false;
            Ui.Stretch(raw.rectTransform);
            if (photo != null)
            {
                var tex = photo.texture;
                raw.texture = tex;
                float ta = (float)tex.width / tex.height, fa = w / Mathf.Max(1f, h);
                raw.uvRect = fa > ta
                    ? new Rect(0f, (1f - ta / fa) * 0.5f, 1f, ta / fa)     // ramka szersza – tniemy górę i dół
                    : new Rect((1f - fa / ta) * 0.5f, 0f, fa / ta, 1f);    // ramka węższa – tniemy boki
            }
            return frame;
        }

        // Zmiana punktu obrotu bez przesuwania obrazka (Ui.Place ustawia punkt w lewym górnym rogu).
        public static void SetPivot(RectTransform rt, Vector2 pivot)
        {
            var size = rt.sizeDelta;
            var d = pivot - rt.pivot;
            rt.pivot = pivot;
            rt.anchoredPosition += new Vector2(d.x * size.x, d.y * size.y);
        }
    }

    // Lupa wychylająca się zza karty. Spokojnie się kołysze, przy przeciąganiu karty lekko się przechyla,
    // po odpowiedzi w szkle pojawia się ✓ (dobrze) albo ! (źle) i krótki ruch. Liczymy tylko obrót jednego obrazka.
    public class ScamLupa : MonoBehaviour
    {
        public enum State { Idle, Ok, Oops }

        // Obszar rysunku względem środka szkła (jednostki rysunku; skala 3 = PNG 1:1 przy 2×).
        const float BoxL = 60f, BoxT = 60f, BoxW = 130f, BoxH = 175f, ArtScale = 3f, PivotY = 100f;

        public float Drag;          // -1..1 – jak daleko przeciągnięto kartę (lekkie przechylenie w tę stronę)
        RectTransform rt;
        State state;
        float t, react = -1f;

        public static ScamLupa Create(Transform parent, float cx, float cy, float scale, State state)
        {
            string sprite = state == State.Ok ? "lupa_ok" : state == State.Oops ? "lupa_oops" : "lupa_idle";
            var img = ScamArt.Picture("Lupa", parent, sprite);
            float k = scale / ArtScale;
            Ui.Place(img.rectTransform, cx - BoxL * k, cy - BoxT * k, BoxW * k, BoxH * k);
            // Obrót wokół końca rączki – lupa „kiwa się”, a nie kręci w miejscu.
            ScamArt.SetPivot(img.rectTransform, new Vector2(BoxL / BoxW, (BoxH - BoxT - PivotY) / BoxH + 0f));
            var me = img.gameObject.AddComponent<ScamLupa>();
            me.rt = img.rectTransform;
            me.state = state;
            me.t = Random.value * 4f;
            return me;
        }

        public void React() { react = 0f; }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            t += dt;
            float a = 3f * Mathf.Sin(t * 1.6f) - 5f * Mathf.Clamp(Drag, -1f, 1f);
            if (react >= 0f)
            {
                react += dt;
                float p = Mathf.Clamp01(react / 0.5f);
                if (state == State.Ok) a += 5f * Mathf.Sin(Mathf.PI * p);                       // krótkie „kiwnięcie”
                else if (state == State.Oops) a += 3f * Mathf.Sin(react * 25f) * (1f - p);      // delikatne drżenie
                if (p >= 1f) react = -1f;
            }
            rt.localRotation = Quaternion.Euler(0f, 0f, a);
        }
    }

    // Powolne kołysanie w lewo i w prawo (środkowa karta na stronie startowej „pokazuje”, jak się przesuwa).
    public class ScamSway : MonoBehaviour
    {
        public float Delay, Amp = 26f, Period = 2.4f, RotAmp = 7f;
        public CanvasGroup Left, Right;   // pieczątki SCAM (przechył w lewo) i SAFE (w prawo)
        Vector2 basePos;
        float t;
        bool started;

        void Update()
        {
            t += Time.unscaledDeltaTime;
            var rt = (RectTransform)transform;
            if (!started) { if (t < Delay) return; started = true; basePos = rt.anchoredPosition; t = 0f; }
            float s = Mathf.Sin(t / Period * Mathf.PI * 2f);
            rt.anchoredPosition = basePos + new Vector2(s * Amp, -Mathf.Abs(s) * 3f);
            rt.localRotation = Quaternion.Euler(0f, 0f, -s * RotAmp);
            if (Left != null) Left.alpha = Mathf.Clamp01(-s * 1.8f - 0.3f);
            if (Right != null) Right.alpha = Mathf.Clamp01(s * 1.8f - 0.3f);
        }
    }
}

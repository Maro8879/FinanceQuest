using UnityEngine;
using UnityEngine.UI;

namespace FinanceQuest
{
    // Animacja odblokowania (wariant B – „fala od kłódki”): kłódka przykuca, podskakuje i znika,
    // a od jej miejsca rozlewa się koło, w którym widać już odblokowaną kartę. Lecą iskierki, na końcu wskakuje START.
    // Pod spodem leży zwykła zablokowana karta, nad nią odblokowana w okrągłej masce – po animacji maska i stara karta
    // znikają, więc potem ekran działa dokładnie jak bez animacji (nic nie liczy się co klatkę).
    public class UnlockReveal : MonoBehaviour
    {
        const float End = 1.3f;
        const int Sparks = 8;

        RectTransform box, locked, final, wave, icon, pill;
        Image waveImg;
        RectTransform[] sparks;
        Vector2[] dirs;
        Vector2 center, iconBase;
        float w, h, maxR, t, delay;
        bool started;

        // Kontener dla obu wersji karty. Przejście ekranu animuje go jako jedną całość.
        public static RectTransform Box(RectTransform parent, string name, float x, float y)
        {
            var b = Ui.Rect(name, parent);
            Ui.Place(b, x, y, 10f, 10f);
            return b;
        }

        // Obie karty są już zbudowane w kontenerze w punkcie (0,0) i mają ten sam rozmiar w×h.
        // delay: po zwykłym przejściu czekamy, aż karty ekranu skończą wskakiwać; po odświeżeniu w miejscu – prawie od razu.
        public static void Play(RectTransform box, RectTransform lockedCard, RectTransform finalCard, float w, float h, float delay)
        {
            Ui.Place(box, box.anchoredPosition.x, -box.anchoredPosition.y, w, h);
            lockedCard.SetAsFirstSibling();

            var r = box.gameObject.AddComponent<UnlockReveal>();
            r.delay = delay;
            r.box = box; r.locked = lockedCard; r.final = finalCard; r.w = w; r.h = h;
            r.icon = Find(lockedCard, "StateIcon");
            r.pill = Find(finalCard, "StartPill");
            if (r.pill != null) r.pill.localScale = Vector3.zero;

            // Koło-maska: na razie zerowe, więc odblokowanej karty nie widać i nie da się jej kliknąć.
            r.waveImg = Ui.Rounded("Wave", box, Color.white, 1f);
            r.wave = r.waveImg.rectTransform;
            r.wave.anchorMin = r.wave.anchorMax = new Vector2(0f, 1f);
            r.wave.pivot = new Vector2(0.5f, 0.5f);
            r.wave.sizeDelta = Vector2.zero;
            r.wave.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            finalCard.SetParent(r.wave, false);
            // Karta trzyma się środka koła – koło rośnie, a karta stoi w miejscu.
            finalCard.anchorMin = finalCard.anchorMax = new Vector2(0.5f, 0.5f);
            finalCard.pivot = new Vector2(0f, 1f);
        }

        static RectTransform Find(RectTransform root, string name)
        {
            foreach (var rt in root.GetComponentsInChildren<RectTransform>(true))
                if (rt.name == name) return rt;
            return null;
        }

        // Pozycje liczymy dopiero przy starcie – wtedy układ karty jest już na pewno policzony.
        void Begin()
        {
            started = true;
            if (icon != null)
            {
                Vector3 local = box.InverseTransformPoint(icon.TransformPoint(icon.rect.center));
                center = new Vector2(local.x, -local.y);   // od lewego górnego rogu, oś Y w dół
                iconBase = icon.anchoredPosition;
            }
            else center = new Vector2(w * 0.5f, h * 0.5f);

            // Promień do najdalszego rogu – koło musi przykryć całą kartę.
            maxR = 0f;
            foreach (var p in new[] { Vector2.zero, new Vector2(w, 0f), new Vector2(0f, h), new Vector2(w, h) })
                maxR = Mathf.Max(maxR, Vector2.Distance(center, p));
            maxR += 6f;

            wave.anchoredPosition = new Vector2(center.x, -center.y);
            final.anchoredPosition = new Vector2(-center.x, center.y);

            sparks = new RectTransform[Sparks];
            dirs = new Vector2[Sparks];
            for (int i = 0; i < Sparks; i++)
            {
                var s = Ui.Icon("Spark", box, IconId.Star, i % 2 == 0 ? Color.white : Palette.Accent, 8f + (i * 5 % 4));
                s.rectTransform.anchorMin = s.rectTransform.anchorMax = new Vector2(0f, 1f);
                s.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                s.rectTransform.localScale = Vector3.zero;
                float a = (i / (float)Sparks + 0.03f * (i % 3)) * Mathf.PI * 2f;
                dirs[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                sparks[i] = s.rectTransform;
            }
        }

        void Update()
        {
            t += Time.unscaledDeltaTime;
            if (t < delay) return;
            if (!started) Begin();
            float a = t - delay;

            // Kłódka: przykucnięcie, podskok, zniknięcie.
            if (icon != null)
            {
                float sq = Mathf.Sin(Mathf.PI * Seg(a, 0f, 0.18f)) * 0.15f;
                float up = Mathf.Sin(Mathf.PI * Seg(a, 0.1f, 0.45f)) * 14f;
                float gone = 1f - Seg(a, 0.38f, 0.52f);
                icon.localScale = new Vector3((1f + sq) * gone, (1f - sq) * gone, 1f);
                icon.anchoredPosition = iconBase + new Vector2(0f, up);
            }

            // Fala: koło rośnie od kłódki (szybko, potem wyhamowuje).
            float k = Seg(a, 0.4f, 1.0f);
            if (k > 0f && !tapped) { tapped = true; Haptics.Tap(); }
            float d = Mathf.Max(2f, EaseOut(k) * maxR * 2f);
            wave.sizeDelta = new Vector2(d, d);
            waveImg.pixelsPerUnitMultiplier = ProceduralSprites.RoundTexRadius / (d * 0.5f);

            // Iskierki rozlatują się od kłódki i bledną.
            float p = Seg(a, 0.42f, 1.1f);
            for (int i = 0; i < Sparks; i++)
            {
                float dist = 10f + EaseOut(p) * (30f + (i % 3) * 6f);
                sparks[i].anchoredPosition = new Vector2(center.x + dirs[i].x * dist, -(center.y + dirs[i].y * dist));
                float s = p <= 0f || p >= 1f ? 0f : 1f - p * 0.6f;
                sparks[i].localScale = new Vector3(s, s, 1f);
                sparks[i].localRotation = Quaternion.Euler(0f, 0f, p * 90f);
            }

            if (pill != null)
            {
                float b = BackOut(Seg(a, 0.95f, 1.3f));
                pill.localScale = new Vector3(b, b, 1f);
            }

            if (a >= End) Finish();
        }

        bool tapped;

        // Sprzątanie: odblokowana karta wraca na zwykłe miejsce, maska i stara karta znikają.
        void Finish()
        {
            final.SetParent(box, false);
            final.anchorMin = final.anchorMax = new Vector2(0f, 1f);
            final.pivot = new Vector2(0f, 1f);
            final.anchoredPosition = Vector2.zero;
            if (pill != null) pill.localScale = Vector3.one;
            Destroy(wave.gameObject);
            Destroy(locked.gameObject);
            foreach (var s in sparks) Destroy(s.gameObject);
            Destroy(this);
        }

        static float Seg(float t, float a, float b) { return Mathf.Clamp01((t - a) / (b - a)); }
        static float EaseOut(float t) { t = 1f - t; return 1f - t * t * t; }
        static float BackOut(float t)
        {
            const float s = 1.7f;
            t -= 1f;
            return t * t * ((s + 1f) * t + s) + 1f;
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace FinanceQuest
{
    // Złoto = ukończone: co jakiś czas przez kartę przelatuje połysk,
    // a na krawędzi pojawiają się iskierki – zawsze po jednej, w losowym miejscu.
    public class GoldShine : MonoBehaviour
    {
        RectTransform area, band, spark, fx;
        Image bandImg, sparkImg;
        float w, h;
        float shineT = -1f, shineWait, sparkT = -1f, sparkWait;
        float sparkRot;

        const float ShineTime = 0.75f, SparkTime = 0.7f;

        public static void Attach(LipCard card, float radius)
        {
            var g = card.gameObject.AddComponent<GoldShine>();
            g.area = card.Face;

            // Osobny Canvas: animacja przebudowuje tylko ten mały kawałek, a nie cały ekran.
            g.fx = Ui.Rect("GoldFx", card.Face);
            Ui.Stretch(g.fx);
            g.fx.gameObject.AddComponent<Canvas>();

            // Maska w kształcie karty, żeby połysk nie wychodził poza zaokrąglone rogi.
            var mask = Ui.Rounded("ShineMask", g.fx, Color.white, radius);
            Ui.Stretch(mask.rectTransform);
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            var b = Ui.Rect("Band", mask.transform).gameObject.AddComponent<Image>();
            b.sprite = ProceduralSprites.ShineBand;
            b.color = new Color(1f, 1f, 1f, 0.45f);
            b.raycastTarget = false;
            g.bandImg = b;
            g.band = b.rectTransform;
            g.band.anchorMin = g.band.anchorMax = new Vector2(0f, 0.5f);
            g.band.localEulerAngles = new Vector3(0f, 0f, -18f);

            g.sparkImg = Ui.Icon("Spark", g.fx, IconId.Spark, Color.white, 14f);
            g.spark = g.sparkImg.rectTransform;
            g.spark.anchorMin = g.spark.anchorMax = new Vector2(0f, 0f);

            g.bandImg.enabled = false;
            g.sparkImg.enabled = false;
            g.shineWait = Random.Range(0.4f, 2.0f);   // różne karty nie błyskają jednocześnie
            g.sparkWait = Random.Range(0.1f, 0.6f);
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;

            if (shineT < 0f)
            {
                shineWait -= dt;
                if (shineWait <= 0f)
                {
                    // Sprawdzamy widoczność tylko na starcie cyklu – poza ekranem nic nie animujemy.
                    if (IsOnScreen()) { Measure(); shineT = 0f; bandImg.enabled = true; }
                    else shineWait = 1f;
                }
            }
            else
            {
                shineT += dt;
                float u = shineT / ShineTime;
                if (u >= 1f) { shineT = -1f; bandImg.enabled = false; shineWait = Random.Range(2.5f, 4.5f); }
                else
                {
                    u = u * u * (3f - 2f * u);
                    band.sizeDelta = new Vector2(46f, h * 2.2f);
                    band.anchoredPosition = new Vector2(Mathf.Lerp(-50f, w + 50f, u), 0f);
                }
            }

            if (sparkT < 0f)
            {
                sparkWait -= dt;
                if (sparkWait <= 0f)
                {
                    if (IsOnScreen()) { Measure(); StartSpark(); }
                    else sparkWait = 1f;
                }
            }
            else
            {
                sparkT += dt;
                float u = sparkT / SparkTime;
                if (u >= 1f) { sparkT = -1f; sparkImg.enabled = false; sparkWait = Random.Range(0.15f, 0.7f); }
                else
                {
                    float s = Mathf.Sin(u * Mathf.PI);
                    spark.localScale = new Vector3(s, s, 1f);
                    spark.localEulerAngles = new Vector3(0f, 0f, sparkRot + u * 60f);
                }
            }
        }

        void StartSpark()
        {
            // Losowy punkt na obwodzie karty, lekko do środka, żeby iskierka „siedziała” na krawędzi.
            float per = 2f * (w + h);
            float t = Random.value * per;
            const float inset = 4f;
            Vector2 p;
            if (t < w) p = new Vector2(t, h - inset);
            else if ((t -= w) < h) p = new Vector2(w - inset, h - t);
            else if ((t -= h) < w) p = new Vector2(w - t, inset);
            else { t -= w; p = new Vector2(inset, t); }

            spark.anchoredPosition = p;
            spark.localScale = Vector3.zero;
            sparkRot = Random.Range(0f, 45f);
            float size = Random.Range(10f, 15f);
            spark.sizeDelta = new Vector2(size, size);
            sparkImg.enabled = true;
            sparkT = 0f;
        }

        void Measure()
        {
            var r = area.rect;
            w = r.width;
            h = r.height;
        }

        static readonly Vector3[] corners = new Vector3[4];

        bool IsOnScreen()
        {
            // Canvas jest w trybie Overlay, więc rogi w świecie = piksele ekranu.
            area.GetWorldCorners(corners);
            return corners[2].y > 0f && corners[0].y < Screen.height && corners[2].x > 0f && corners[0].x < Screen.width;
        }
    }
}

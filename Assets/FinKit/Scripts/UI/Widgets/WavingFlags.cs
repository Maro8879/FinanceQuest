using UnityEngine;
using UnityEngine.UI;

namespace FinanceQuest
{
    // Powiewające flagi USA na grafice Wall Street (wariant C: spokojny powiew z podmuchami).
    // Materiał to jedna mała tekstura (pasy, kanton, gwiazdki) naciągnięta na siatkę, którą co klatkę wyginamy falą.
    // Tanio: 3 flagi = ok. 160 wierzchołków, własny Canvas (co klatkę przebudowuje się tylko on, a nie całe menu),
    // a gdy karta jest poza ekranem – nic nie liczymy.
    public class WavingFlags : MaskableGraphic
    {
        const int NU = 4, NV = 10;              // siatka: w poprzek flagi x wzdłuż zwisu
        const float Period = 2f;                // jeden cykl fali i podmuchu (s)
        const float AmpCalm = 1.8f, AmpGust = 1.4f;
        const float ClothPpu = 5f;              // pikseli tekstury na jednostkę rysunku
        const int Pad = 2;                      // przezroczysty brzeg tekstury = miękkie krawędzie materiału

        static Texture2D cloth;
        static float clothW, clothH;            // rozmiar materiału w jednostkach rysunku
        static readonly Vector3[] corners = new Vector3[4];
        float sx, sy;

        // Warstwa na całą kartę (te same wymiary co rysunek), wierzchołki liczone w jednostkach rysunku.
        public static WavingFlags Create(Transform parent, float w, float h)
        {
            // Osobny Canvas na obiekcie nadrzędnym (nie na tym samym co flagi – wtedy Unity nie dokłada CanvasRenderera).
            var layer = Ui.Rect("FlagsLayer", parent);
            Ui.Place(layer, 0, 0, w, h);
            layer.gameObject.AddComponent<Canvas>();
            var rt = Ui.Rect("Flags", layer);
            Ui.Place(rt, 0, 0, w, h);
            rt.gameObject.AddComponent<CanvasRenderer>();
            var f = rt.gameObject.AddComponent<WavingFlags>();
            f.raycastTarget = false;
            f.sx = w / WallStreetArt.W;
            f.sy = h / WallStreetArt.H;
            return f;
        }

        public override Texture mainTexture { get { return Cloth; } }

        void Update()
        {
            if (OnScreen()) SetVerticesDirty();
        }

        // Canvas menu jest w trybie Overlay, więc rogi w przestrzeni świata = piksele ekranu.
        bool OnScreen()
        {
            rectTransform.GetWorldCorners(corners);
            return corners[1].y > 0f && corners[0].y < Screen.height;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var tex = Cloth;
            float t = Time.unscaledTime;
            float phase = t * Mathf.PI * 2f / Period;
            // Podmuch: fala rośnie w pierwszej połowie cyklu i opada, w drugiej jest spokojnie.
            float amp = AmpCalm + AmpGust * Mathf.Max(0f, Mathf.Sin(phase));
            float padU = Pad / (clothW * ClothPpu), padV = Pad / (clothH * ClothPpu);
            float ang = WallStreetArt.PoleAngle * Mathf.Deg2Rad;
            var dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * WallStreetArt.PoleLength;
            var c32 = (Color32)color;

            foreach (float x in WallStreetArt.FlagX)
            {
                var root = new Vector2(x, WallStreetArt.FlagY);
                var a0 = root + dir * 0.35f;
                var b0 = root + dir * 0.95f;
                float flagPhase = x * 0.02f + phase;     // każda flaga trochę przesunięta w fazie
                int start = vh.currentVertCount;
                for (int j = 0; j <= NV; j++)
                    for (int i = 0; i <= NU; i++)
                    {
                        // Siatka sięga też na przezroczysty brzeg tekstury – stąd u/v trochę poza 0..1.
                        float u = -padU + (1f + 2f * padU) * i / NU;
                        float v = -padV + (1f + 2f * padV) * j / NV;
                        float vc = Mathf.Max(0f, v);
                        var h = a0 + (b0 - a0) * u;
                        float wave = Mathf.Sin(vc * Mathf.PI * 1.6f + flagPhase + u * 0.8f) * amp * vc;
                        float px = h.x + wave - vc * 2.5f;
                        float py = h.y + v * WallStreetArt.FlagDrop;
                        var uv = new Vector2((Pad + u * clothW * ClothPpu) / tex.width, 1f - (Pad + v * clothH * ClothPpu) / tex.height);
                        vh.AddVert(new Vector3(px * sx, -py * sy, 0f), c32, uv);
                    }
                for (int j = 0; j < NV; j++)
                    for (int i = 0; i < NU; i++)
                    {
                        int k = start + j * (NU + 1) + i;
                        vh.AddTriangle(k, k + 1, k + NU + 2);
                        vh.AddTriangle(k, k + NU + 2, k + NU + 1);
                    }
            }
        }

        // Płaska flaga (pasy pionowo, bo flaga zwisa z ukośnego masztu) – tworzona raz, ok. 80 x 140 px.
        static Texture2D Cloth
        {
            get
            {
                if (cloth != null) return cloth;
                clothW = WallStreetArt.PoleLength * 0.6f;
                clothH = WallStreetArt.FlagDrop;
                int iw = Mathf.CeilToInt(clothW * ClothPpu), ih = Mathf.CeilToInt(clothH * ClothPpu);
                int tw = iw + 2 * Pad, th = ih + 2 * Pad;
                var px = new Color32[tw * th];
                const int stripes = 7;
                float cantonU = 3f / stripes, cantonV = 0.42f, starR = 0.7f * ClothPpu;
                for (int y = 0; y < th; y++)
                    for (int x = 0; x < tw; x++)
                    {
                        float u = (x + 0.5f - Pad) / iw, v = (y + 0.5f - Pad) / ih;
                        bool inside = u >= 0f && u <= 1f && v >= 0f && v <= 1f;
                        float cu = Mathf.Clamp01(u), cv = Mathf.Clamp01(v);   // brzeg: kolor sąsiada, przezroczysty (bez ciemnej obwódki)
                        Color col = Mathf.FloorToInt(cu * stripes) % 2 == 0 ? WallStreetArt.Red : WallStreetArt.FlagWhite;
                        if (cu >= cantonU && cv <= cantonV)
                        {
                            col = WallStreetArt.Navy;
                            for (int i = 0; i < 3; i++)
                                for (int j = 0; j < 3; j++)
                                {
                                    float su = cantonU + (1f - cantonU) * (i + 0.5f) / 3f, sv = cantonV * (j + 0.5f) / 3f;
                                    float d = new Vector2((cu - su) * iw, (cv - sv) * ih).magnitude;
                                    col = Color.Lerp(col, Color.white, Mathf.Clamp01(starR - d + 0.5f));
                                }
                        }
                        // Delikatne fałdy w poprzek – materiał wygląda na miękki.
                        for (int k = 1; k < 4; k++)
                            if (cv >= k * 0.25f - 0.04f && cv <= k * 0.25f) col = Color.Lerp(col, Color.black, 0.08f);
                        col.a = inside ? 1f : 0f;
                        px[(th - 1 - y) * tw + x] = col;
                    }
                cloth = new Texture2D(tw, th, TextureFormat.RGBA32, false);
                cloth.name = "fq_flag_cloth";
                cloth.filterMode = FilterMode.Bilinear;
                cloth.wrapMode = TextureWrapMode.Clamp;
                cloth.SetPixels32(px);
                cloth.Apply(false, true);
                return cloth;
            }
        }
    }
}

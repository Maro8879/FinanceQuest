using System.Collections.Generic;
using UnityEngine;

namespace FinanceQuest
{
    // Sprite'y rysowane w kodzie. Każdy powstaje raz i siedzi w pamięci podręcznej,
    // żeby przebudowa ekranu nie rysowała ich od nowa.
    public static class ProceduralSprites
    {
        // Promień narożnika w teksturze „zaokrąglonego prostokąta”. Image skaluje go przez pixelsPerUnitMultiplier.
        public const float RoundTexRadius = 48f;
        const int IconSize = 96;


        // Jeden 9-slice na wszystkie karty, przyciski i paski – mniej tekstur = lepszy batching.
        public static Sprite Rounded { get { return DrawnPictures.Get("rounded", MakeRounded); } }

        static Sprite MakeRounded()
        {
            {
                int size = (int)RoundTexRadius * 2 + 5;
                float half = size * 0.5f;
                var c = new SdfCanvas(size, size, size);
                c.Add(p => SdfCanvas.RoundRect(p, new Vector2(half, half), new Vector2(size - 2, size - 2), RoundTexRadius));
                float b = Mathf.Floor(half);
                return Sprite.Create(c.ToTexture("fq_rounded"), new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                    100f, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
            }
        }

        // Obwódka (sam kontur) zaokrąglonego prostokąta – 9-slice, do wyróżnienia aktualnego miejsca.
        // Grubość względem promienia jest stała (RingRatio), bo w 9-slice skalują się razem.
        public const float RingRatio = 3f / 26f;
        public static Sprite Ring { get { return DrawnPictures.Get("ring", MakeRing); } }

        static Sprite MakeRing()
        {
            {
                int size = (int)RoundTexRadius * 2 + 5;
                float half = size * 0.5f;
                float t = RoundTexRadius * RingRatio;
                var c = new SdfCanvas(size, size, size);
                c.Add(p => Mathf.Abs(SdfCanvas.RoundRect(p, new Vector2(half, half), new Vector2(size - 2 - t, size - 2 - t), RoundTexRadius - t * 0.5f)) - t * 0.5f);
                float b = Mathf.Floor(half);
                return Sprite.Create(c.ToTexture("fq_ring"), new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                    100f, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
            }
        }

        // Wykres świecowy w tle kart rozdziałów. Dane z prostego generatora o stałym ziarnie,
        // więc wykres jest zawsze ten sam (nie „skacze” między uruchomieniami).
        public static Sprite Candles { get { return DrawnPictures.Get("candles", MakeCandles); } }

        static Sprite MakeCandles()
        {
            {
                const int w = 512, h = 128, n = 14;
                var c = new SdfCanvas(w, h, w);   // jednostka = piksel

                // Delikatna siatka w tle.
                for (int g = 1; g <= 3; g++)
                {
                    float gy = h * g / 4f;
                    c.Add(p => SdfCanvas.Capsule(p, new Vector2(0, gy), new Vector2(w, gy), 0.8f), 0.35f);
                }

                // Błądzenie losowe z lekkim trendem w górę – „rynek, który rośnie”.
                var open = new float[n]; var close = new float[n]; var hi = new float[n]; var lo = new float[n];
                uint seed = 20261120u;
                float price = 50f;
                for (int i = 0; i < n; i++)
                {
                    open[i] = price;
                    price += (Rand(ref seed) * 2f - 1f) * 24f + 1.2f;
                    close[i] = price;
                    hi[i] = Mathf.Max(open[i], close[i]) + Rand(ref seed) * 7f + 2f;
                    lo[i] = Mathf.Min(open[i], close[i]) - Rand(ref seed) * 7f - 2f;
                }
                float min = float.MaxValue, max = float.MinValue;
                for (int i = 0; i < n; i++) { min = Mathf.Min(min, lo[i]); max = Mathf.Max(max, hi[i]); }
                // Cena -> wysokość (oś Y w dół): najwyższa cena przy górze, najniższa przy dole.
                System.Func<float, float> Y = v => 12f + (1f - (v - min) / (max - min)) * (h - 24f);

                float step = w / (float)n, body = step * 0.52f;
                var avg = new Vector2[n];
                for (int i = 0; i < n; i++)
                {
                    float x = step * (i + 0.5f);
                    float top = Y(Mathf.Max(open[i], close[i])), bot = Y(Mathf.Min(open[i], close[i]));
                    float bh = Mathf.Max(6f, bot - top), cy = (top + bot) * 0.5f;
                    float yHi = Y(hi[i]), yLo = Y(lo[i]);
                    bool up = close[i] >= open[i];
                    // Knot nad i pod korpusem (nie przez środek, żeby puste świece były czytelne).
                    c.Add(p => SdfCanvas.Capsule(p, new Vector2(x, yHi), new Vector2(x, cy - bh * 0.5f), 1.3f));
                    c.Add(p => SdfCanvas.Capsule(p, new Vector2(x, cy + bh * 0.5f), new Vector2(x, yLo), 1.3f));
                    // Wzrost = pełna świeca, spadek = pusta (sam kontur) – rozróżnienie bez drugiego koloru.
                    if (up) c.Add(p => SdfCanvas.RoundRect(p, new Vector2(x, cy), new Vector2(body, bh), 2f));
                    else c.Add(p => Mathf.Abs(SdfCanvas.RoundRect(p, new Vector2(x, cy), new Vector2(body - 2.6f, bh - 2.6f), 1.5f)) - 1.3f);

                    // Średnia z 3 świec – gładka linia trendu.
                    float sum = 0f; int cnt = 0;
                    for (int k = Mathf.Max(0, i - 2); k <= i; k++) { sum += close[k]; cnt++; }
                    avg[i] = new Vector2(x, Y(sum / cnt));
                }
                for (int i = 1; i < n; i++)
                {
                    Vector2 a = avg[i - 1], b = avg[i];
                    c.Add(p => SdfCanvas.Capsule(p, a, b, 1.6f), 0.7f);
                }

                return Sprite.Create(c.ToTexture("fq_candles"), new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f),
                    100f, 0, SpriteMeshType.FullRect);
            }
        }

        // Prosty generator liczb (0..1) o stałym ziarnie – ten sam wynik na każdym urządzeniu.
        static float Rand(ref uint s)
        {
            s = s * 1664525u + 1013904223u;
            return (s >> 8) / 16777216f;
        }

        public static Sprite Icon(IconId id)
        {
            return DrawnPictures.Get("icon_" + id, () =>
            {
                var c = new SdfCanvas(IconSize, IconSize, 24f);
                DrawIcon(c, id);
                // FullRect: siatka „Tight” wymaga czytelnej tekstury, a my zwalniamy jej kopię z RAM.
                return Sprite.Create(c.ToTexture("fq_icon_" + id), new Rect(0, 0, IconSize, IconSize), new Vector2(0.5f, 0.5f),
                    100f, 0, SpriteMeshType.FullRect);
            });
        }

        static Vector2 V(float x, float y) { return new Vector2(x, y); }

        static Vector2 Quad(Vector2 a, Vector2 b, Vector2 c, float t)
        {
            float u = 1f - t;
            return a * (u * u) + b * (2f * u * t) + c * (t * t);
        }

        static void AddQuad(List<Vector2> l, Vector2 a, Vector2 b, Vector2 c)
        {
            for (int i = 1; i <= 10; i++) l.Add(Quad(a, b, c, i / 10f));
        }

        // Obrys dzwonka z krzywych: rozszerzony dół, proste boki, okrągła kopuła.
        static Vector2[] BellBody(float cx, float top, float bottom, float half, float flare)
        {
            var l = new List<Vector2> { V(cx - half - flare, bottom) };
            AddQuad(l, V(cx - half - flare, bottom), V(cx - half, bottom - 2.5f), V(cx - half, bottom - 6f));
            AddQuad(l, V(cx - half, bottom - 6f), V(cx - half, top), V(cx, top));
            AddQuad(l, V(cx, top), V(cx + half, top), V(cx + half, bottom - 6f));
            AddQuad(l, V(cx + half, bottom - 6f), V(cx + half, bottom - 2.5f), V(cx + half + flare, bottom));
            return l.ToArray();
        }

        // Ukośna kreska z „odstępem” po obu stronach (wycięcie), żeby była czytelna na ikonie pod spodem.
        static void StrikeThrough(SdfCanvas c)
        {
            c.Cut(p => SdfCanvas.Capsule(p, V(3.5f, 3.5f), V(20.5f, 20.5f), 3f));
            c.Add(p => SdfCanvas.Capsule(p, V(3.5f, 3.5f), V(20.5f, 20.5f), 1.4f));
        }

        // Ikony projektowane na siatce 24x24 (oś Y w dół), rysowane na biało – kolor nadaje Image.
        static void DrawIcon(SdfCanvas c, IconId id)
        {
            switch (id)
            {
                case IconId.Home:
                {
                    var house = new[] { V(12, 3.5f), V(21, 11), V(19.5f, 11), V(19.5f, 20.5f), V(4.5f, 20.5f), V(4.5f, 11), V(3, 11) };
                    c.Add(p => SdfCanvas.Polygon(p, house) - 0.6f);
                    c.Cut(p => SdfCanvas.RoundRect(p, V(12, 17.5f), V(4.4f, 7f), 1.2f));
                    break;
                }
                case IconId.Back:
                    // Strzałka „wróć”: trzon + dwa ramiona grotu.
                    c.Add(p => SdfCanvas.Capsule(p, V(5.5f, 12), V(19, 12), 1.9f));
                    c.Add(p => SdfCanvas.Capsule(p, V(5.5f, 12), V(11.5f, 6), 1.9f));
                    c.Add(p => SdfCanvas.Capsule(p, V(5.5f, 12), V(11.5f, 18), 1.9f));
                    break;
                case IconId.Sound:
                {
                    var spk = new[] { V(3.5f, 9), V(8, 9), V(13, 4.5f), V(13, 19.5f), V(8, 15), V(3.5f, 15) };
                    c.Add(p => SdfCanvas.Polygon(p, spk) - 0.4f);
                    // Fala dźwięku: prawa połówka pierścienia.
                    c.Add(p => Mathf.Max(Mathf.Abs((p - V(12, 12)).magnitude - 7f) - 1.1f, 15.5f - p.x));
                    break;
                }
                case IconId.Bell:
                {
                    // Klasyczny dzwonek: kopuła z rozszerzonym dołem, kulka na górze, serce pod spodem.
                    var bell = BellBody(12f, 4.5f, 18f, 6.2f, 2.2f);
                    c.Add(p => SdfCanvas.Polygon(p, bell) - 0.5f);
                    c.Add(p => SdfCanvas.Circle(p, V(12, 3.6f), 1.7f));
                    c.Add(p => SdfCanvas.Capsule(p, V(10.6f, 20.6f), V(13.4f, 20.6f), 1.6f));
                    break;
                }
                case IconId.Language:
                {
                    // Dymek z literami „Aa” – symbol języka. Litery wycięte, odpowiednio grube, żeby były czytelne w małym rozmiarze.
                    c.Add(p => SdfCanvas.RoundRect(p, V(12, 10.5f), V(21, 15.5f), 4.5f));
                    var tail = new[] { V(6.5f, 17), V(12, 17), V(6.8f, 22.5f) };
                    c.Add(p => SdfCanvas.Polygon(p, tail));
                    c.Cut(p => SdfCanvas.Capsule(p, V(5.7f, 14.6f), V(8.6f, 6.2f), 1.3f));
                    c.Cut(p => SdfCanvas.Capsule(p, V(8.6f, 6.2f), V(11.5f, 14.6f), 1.3f));
                    c.Cut(p => SdfCanvas.Capsule(p, V(7f, 11.7f), V(10.2f, 11.7f), 1.15f));
                    c.Cut(p => Mathf.Abs((p - V(16.2f, 11.5f)).magnitude - 2.3f) - 1.25f);
                    c.Cut(p => SdfCanvas.Capsule(p, V(18.6f, 8.6f), V(18.6f, 14.6f), 1.25f));
                    break;
                }
                case IconId.Sun:
                    c.Add(p => SdfCanvas.Circle(p, V(12, 12), 4.6f));
                    for (int i = 0; i < 8; i++)
                    {
                        float ang = i * Mathf.PI / 4f;
                        Vector2 d = V(Mathf.Cos(ang), Mathf.Sin(ang));
                        Vector2 r0 = V(12, 12) + d * 7.4f, r1 = V(12, 12) + d * 9.8f;
                        c.Add(p => SdfCanvas.Capsule(p, r0, r1, 1.3f));
                    }
                    break;
                case IconId.Gear:
                    // Zębatka: koło z 8 zębami i otworem w środku.
                    c.Add(p => SdfCanvas.Circle(p, V(12, 12), 7.6f));
                    for (int i = 0; i < 8; i++)
                    {
                        // Zęby jako krótkie, szerokie prostokąty obrócone wokół środka.
                        float deg = i * 45f, rad = deg * Mathf.Deg2Rad;
                        Vector2 ctr = V(12, 12) + V(Mathf.Cos(rad), Mathf.Sin(rad)) * 8.6f;
                        float cs = Mathf.Cos(-rad), sn = Mathf.Sin(-rad);
                        c.Add(p =>
                        {
                            Vector2 q = p - ctr;
                            q = V(q.x * cs - q.y * sn, q.x * sn + q.y * cs);
                            return SdfCanvas.RoundRect(q, V(0, 0), V(4.2f, 5f), 1.2f);
                        });
                    }
                    c.Cut(p => SdfCanvas.Circle(p, V(12, 12), 3.2f));
                    break;
                case IconId.Vibrate:
                    // Telefon w ramce + po dwie krótkie kreski drgań z każdej strony.
                    c.Add(p => SdfCanvas.RoundRect(p, V(12, 12), V(9.5f, 16), 2.4f));
                    c.Cut(p => SdfCanvas.RoundRect(p, V(12, 12), V(6.3f, 12.8f), 1f));
                    c.Add(p => SdfCanvas.Capsule(p, V(4.6f, 9.5f), V(4.6f, 14.5f), 0.9f));
                    c.Add(p => SdfCanvas.Capsule(p, V(19.4f, 9.5f), V(19.4f, 14.5f), 0.9f));
                    c.Add(p => SdfCanvas.Capsule(p, V(1.9f, 10.8f), V(1.9f, 13.2f), 0.9f));
                    c.Add(p => SdfCanvas.Capsule(p, V(22.1f, 10.8f), V(22.1f, 13.2f), 0.9f));
                    break;
                case IconId.SoundOff:
                case IconId.BellOff:
                case IconId.VibrateOff:
                    // Wersja „wyłączona” = ta sama ikona przekreślona ukośną kreską.
                    DrawIcon(c, id == IconId.SoundOff ? IconId.Sound : id == IconId.BellOff ? IconId.Bell : IconId.Vibrate);
                    StrikeThrough(c);
                    break;
                case IconId.Lock:
                    c.Add(p => SdfCanvas.RoundRect(p, V(12, 15.5f), V(15, 10.5f), 3f));
                    // Kabłąk = górna połowa pierścienia + dwie nóżki.
                    c.Add(p => Mathf.Max(Mathf.Abs((p - V(12, 8.5f)).magnitude - 4f) - 1.4f, p.y - 8.5f));
                    c.Add(p => SdfCanvas.Capsule(p, V(8, 8.5f), V(8, 11), 1.4f));
                    c.Add(p => SdfCanvas.Capsule(p, V(16, 8.5f), V(16, 11), 1.4f));
                    c.Cut(p => SdfCanvas.Circle(p, V(12, 15), 1.6f));
                    break;
                case IconId.Check:
                    c.Add(p => SdfCanvas.Capsule(p, V(5, 12.5f), V(10, 17.5f), 1.9f));
                    c.Add(p => SdfCanvas.Capsule(p, V(10, 17.5f), V(19, 7), 1.9f));
                    break;
                case IconId.Book:
                {
                    var left = new[] { V(2.5f, 5), V(11.2f, 6.2f), V(11.2f, 20), V(2.5f, 18.8f) };
                    var right = new[] { V(12.8f, 6.2f), V(21.5f, 5), V(21.5f, 18.8f), V(12.8f, 20) };
                    c.Add(p => SdfCanvas.Polygon(p, left) - 0.5f);
                    c.Add(p => SdfCanvas.Polygon(p, right) - 0.5f);
                    break;
                }
                case IconId.Gamepad:
                    c.Add(p => SdfCanvas.RoundRect(p, V(12, 12.5f), V(21, 12), 6f));
                    c.Cut(p => SdfCanvas.Capsule(p, V(7, 10), V(7, 15), 0.9f));
                    c.Cut(p => SdfCanvas.Capsule(p, V(4.5f, 12.5f), V(9.5f, 12.5f), 0.9f));
                    c.Cut(p => SdfCanvas.Circle(p, V(16, 11), 1.4f));
                    c.Cut(p => SdfCanvas.Circle(p, V(18.6f, 13.8f), 1.4f));
                    break;
                case IconId.Task:
                    c.Add(p => SdfCanvas.RoundRect(p, V(12, 12.5f), V(16, 19), 3f));
                    c.Cut(p => SdfCanvas.Capsule(p, V(8, 9), V(16, 9), 1f));
                    c.Cut(p => SdfCanvas.Capsule(p, V(8, 13), V(16, 13), 1f));
                    c.Cut(p => SdfCanvas.Capsule(p, V(8, 17), V(13, 17), 1f));
                    break;
                case IconId.Star:
                {
                    var pts = new Vector2[10];
                    for (int i = 0; i < 10; i++)
                    {
                        float a = -Mathf.PI / 2f + i * Mathf.PI / 5f;
                        float r = i % 2 == 0 ? 10.5f : 4.6f;
                        pts[i] = V(12 + Mathf.Cos(a) * r, 13 + Mathf.Sin(a) * r);
                    }
                    c.Add(p => SdfCanvas.Polygon(p, pts) - 0.6f);
                    break;
                }
                case IconId.Moon:
                    c.Add(p => SdfCanvas.Circle(p, V(12, 12), 8.5f));
                    c.Cut(p => SdfCanvas.Circle(p, V(16.5f, 8), 7.5f));
                    break;
                case IconId.Chart:
                    c.Add(p => SdfCanvas.Capsule(p, V(5, 19), V(5, 13), 1.8f));
                    c.Add(p => SdfCanvas.Capsule(p, V(10, 19), V(10, 6), 1.8f));
                    c.Add(p => SdfCanvas.Capsule(p, V(15, 19), V(15, 10), 1.8f));
                    c.Add(p => SdfCanvas.Capsule(p, V(20, 19), V(20, 4), 1.8f));
                    break;
                case IconId.Phone:
                    c.Add(p => SdfCanvas.RoundRect(p, V(12, 12), V(13, 21), 3.2f));
                    c.Cut(p => SdfCanvas.RoundRect(p, V(12, 11), V(9, 13), 1.2f));
                    c.Add(p => SdfCanvas.Capsule(p, V(8.8f, 14), V(11, 11.5f), 0.8f));
                    c.Add(p => SdfCanvas.Capsule(p, V(11, 11.5f), V(12.6f, 13), 0.8f));
                    c.Add(p => SdfCanvas.Capsule(p, V(12.6f, 13), V(15.2f, 8.8f), 0.8f));
                    break;
                case IconId.Wallet:
                    c.Add(p => SdfCanvas.RoundRect(p, V(12, 13.5f), V(20, 14), 3f));
                    c.Add(p => SdfCanvas.RoundRect(p, V(11, 6.5f), V(14, 4), 1.5f));
                    c.Cut(p => SdfCanvas.RoundRect(p, V(19, 13.5f), V(7, 5.4f), 2f));
                    c.Add(p => SdfCanvas.Circle(p, V(18.6f, 13.5f), 1.2f));
                    break;
                case IconId.Card:
                    c.Add(p => SdfCanvas.RoundRect(p, V(12, 12), V(21, 15), 3f));
                    c.Cut(p => SdfCanvas.RoundRect(p, V(12, 9.5f), V(23, 3), 0f));
                    c.Cut(p => SdfCanvas.Capsule(p, V(6, 15.5f), V(10, 15.5f), 0.9f));
                    break;
                case IconId.Bulb:
                {
                    // Żarówka: bańka + zwężenie + dwa paski gwintu (ramka „Ważne” na stronach lekcji).
                    var neck = new[] { V(7.6f, 12.4f), V(16.4f, 12.4f), V(14.6f, 16.6f), V(9.4f, 16.6f) };
                    c.Add(p => SdfCanvas.Circle(p, V(12, 8.8f), 6.9f));
                    c.Add(p => SdfCanvas.Polygon(p, neck));
                    c.Add(p => SdfCanvas.Capsule(p, V(9.4f, 18.6f), V(14.6f, 18.6f), 1.25f));
                    c.Add(p => SdfCanvas.Capsule(p, V(10.4f, 21.4f), V(13.6f, 21.4f), 1.2f));
                    break;
                }
                case IconId.Exclaim:
                    // Wykrzyknik: kreska zwężająca się ku dołowi + kropka (ramka „Ważne”).
                    c.Add(p => SdfCanvas.Polygon(p, new[] { V(9.6f, 3.5f), V(14.4f, 3.5f), V(13.3f, 15.2f), V(10.7f, 15.2f) }) - 0.6f);
                    c.Add(p => SdfCanvas.Circle(p, V(12, 19.6f), 2.3f));
                    break;
                case IconId.Play:
                    // Trójkąt odtwarzania z lekko zaokrąglonymi rogami (filmik w lekcji).
                    c.Add(p => SdfCanvas.Polygon(p, new[] { V(7, 4.5f), V(19.5f, 12), V(7, 19.5f) }) - 1f);
                    break;
                case IconId.Pause:
                    c.Add(p => SdfCanvas.RoundRect(p, V(8.5f, 12), V(4.4f, 15f), 1.2f));
                    c.Add(p => SdfCanvas.RoundRect(p, V(15.5f, 12), V(4.4f, 15f), 1.2f));
                    break;
                case IconId.Close:
                    c.Add(p => SdfCanvas.Capsule(p, V(6, 6), V(18, 18), 2f));
                    c.Add(p => SdfCanvas.Capsule(p, V(18, 6), V(6, 18), 2f));
                    break;
                case IconId.Spark:
                {
                    var s = new[] { V(12, 0.5f), V(14.4f, 9.6f), V(23.5f, 12), V(14.4f, 14.4f), V(12, 23.5f), V(9.6f, 14.4f), V(0.5f, 12), V(9.6f, 9.6f) };
                    c.Add(p => SdfCanvas.Polygon(p, s));
                    break;
                }
            }
        }
    }
}

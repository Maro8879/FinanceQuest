using UnityEngine;

namespace FinanceQuest
{
    // Efekty dźwiękowe (pliki w Resources/Sounds). Wszystko przechodzi przez jedno miejsce,
    // żeby przełącznik „Dźwięki” w ustawieniach działał wszędzie i żeby nic nie grało podwójnie.
    public static class Sfx
    {
        public enum Id { Correct, Wrong, Level, Chapter, Achievement, Whoosh }

        static readonly string[] Files = { "correct", "wrong", "level", "chapter", "achievement", "whoosh" };
        // Szum przejścia jest cichszy – słychać go często, nie może męczyć.
        static readonly float[] Volume = { 0.8f, 0.8f, 0.9f, 0.9f, 0.9f, 0.45f };

        static AudioClip[] clips;
        static AudioSource shots, jingle;

        // Wczytujemy na starcie aplikacji – pierwszy dźwięk nie ma wtedy opóźnienia na ładowanie.
        public static void Init(GameObject host)
        {
            if (shots != null) return;
            clips = new AudioClip[Files.Length];
            for (int i = 0; i < Files.Length; i++) clips[i] = Resources.Load<AudioClip>("Sounds/" + Files[i]);
            shots = host.AddComponent<AudioSource>();
            jingle = host.AddComponent<AudioSource>();
            shots.playOnAwake = jingle.playOnAwake = false;
        }

        // Krótkie dźwięki (odpowiedź, przejście) – mogą na siebie zachodzić.
        public static void Play(Id id)
        {
            var c = Clip(id);
            if (c != null) shots.PlayOneShot(c, Volume[(int)id]);
        }

        // Melodyjka wyniku. Opóźnienie, żeby zagrała razem z wskakującymi gwiazdkami;
        // nowa melodyjka ucina poprzednią, więc dwie nigdy nie grają naraz.
        public static void Jingle(Id id, float delay = 0f)
        {
            var c = Clip(id);
            if (c == null) return;
            jingle.Stop();
            jingle.clip = c;
            jingle.volume = Volume[(int)id];
            jingle.PlayDelayed(delay);
        }

        // Dźwięk po zaliczeniu: test rozdziału ma swój, komplet gwiazdek – „osiągnięcie”, reszta – koniec levelu.
        public static void Result(int stars, bool chapterTest, float delay)
        {
            if (stars == 0) return;   // niezaliczone – bez fanfar
            Jingle(chapterTest ? Id.Chapter : stars >= 3 ? Id.Achievement : Id.Level, delay);
        }

        static AudioClip Clip(Id id)
        {
            if (shots == null || !Settings.Sound) return null;
            return clips[(int)id];
        }
    }
}

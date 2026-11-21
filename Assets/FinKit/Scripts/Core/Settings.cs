using UnityEngine;

namespace FinanceQuest
{
    // Ustawienia użytkownika zapisywane w PlayerPrefs.
    // Na razie tylko zapamiętujemy wybór – tryb ciemny, dźwięki i przypomnienia podepniemy w swoich etapach.
    public static class Settings
    {
        const string DarkKey = "fq.set.dark", SoundKey = "fq.set.sound", RemindKey = "fq.set.remind";

        public static bool DarkMode
        {
            get { return PlayerPrefs.GetInt(DarkKey, 0) == 1; }
            set { PlayerPrefs.SetInt(DarkKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static bool Sound
        {
            get { return PlayerPrefs.GetInt(SoundKey, 1) == 1; }
            set { PlayerPrefs.SetInt(SoundKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        // Wibracja przy kliknięciach – domyślnie włączona, delikatna.
        const string VibeKey = "fq.set.vibe";
        public static bool Vibration
        {
            get { return PlayerPrefs.GetInt(VibeKey, 1) == 1; }
            set { PlayerPrefs.SetInt(VibeKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static bool Reminders
        {
            get { return PlayerPrefs.GetInt(RemindKey, 1) == 1; }
            set { PlayerPrefs.SetInt(RemindKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        // Język interfejsu: na razie zapamiętujemy wybór, tłumaczenia dojdą później.
        const string LangKey = "fq.set.lang";
        public static bool English
        {
            get { return PlayerPrefs.GetString(LangKey, "pl") == "en"; }
            set { PlayerPrefs.SetString(LangKey, value ? "en" : "pl"); PlayerPrefs.Save(); }
        }
    }
}

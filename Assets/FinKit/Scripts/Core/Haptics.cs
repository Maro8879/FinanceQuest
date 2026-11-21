using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace FinanceQuest
{
    // Krótkie „tyknięcie” telefonu przy kliknięciu. Handheld.Vibrate() jest za długie (pół sekundy brzęczenia),
    // dlatego na Androidzie i iOS wołamy systemowe, delikatne efekty. W Editorze nic się nie dzieje.
    public static class Haptics
    {
        public static void Tap()
        {
            if (!Settings.Vibration) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            AndroidTap();
#elif UNITY_IOS && !UNITY_EDITOR
            FQ_HapticTap();
#endif
        }

        // Unity dodaje uprawnienie VIBRATE do manifestu tylko, gdy w kodzie jest Handheld.Vibrate.
        // Ta gałąź nigdy się nie wykonuje – jest tylko po to, żeby uprawnienie się pojawiło.
        static bool neverTrue;
        public static void KeepPermission() { if (neverTrue) Handheld.Vibrate(); }

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void FQ_HapticTap();
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject vibrator, effect;
        static int sdk;
        static bool failed;

        static void AndroidTap()
        {
            if (failed) return;
            try
            {
                if (vibrator == null)
                {
                    using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                        sdk = version.GetStatic<int>("SDK_INT");
                    using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                        vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                    if (vibrator == null) { failed = true; return; }

                    // Efekt tworzymy raz i trzymamy – kliknięcia nie tworzą nowych obiektów Javy.
                    // VibrationEffect jest dopiero od Androida 8 (API 26) – na starszych nawet go nie szukamy.
                    if (sdk >= 26)
                    using (var ve = new AndroidJavaClass("android.os.VibrationEffect"))
                    {
                        if (sdk >= 29) effect = ve.CallStatic<AndroidJavaObject>("createPredefined", 2);          // EFFECT_TICK – najlżejszy
                        else if (sdk >= 26) effect = ve.CallStatic<AndroidJavaObject>("createOneShot", 12L, 60); // 12 ms, słabo
                    }
                }
                if (effect != null) vibrator.Call("vibrate", effect);
                else vibrator.Call("vibrate", 12L);   // stare Androidy: bez regulacji siły, więc bardzo krótko
            }
            catch (System.Exception)
            {
                failed = true;   // telefon bez wibracji albo bez uprawnienia – po prostu przestajemy próbować
            }
        }
#endif
    }
}

using UnityEditor;
using UnityEngine;

namespace FinanceQuest.EditorTools
{
    // Szybki powrót do „czystego” stanu przy testach w Editorze.
    public static class FinanceQuestMenu
    {
        [MenuItem("Finance Quest/Resetuj postęp")]
        static void ResetProgress()
        {
            PlayerPrefs.DeleteKey(ProgressStore.PrefsKey);
            PlayerPrefs.Save();
            Debug.Log("Finance Quest: postęp wyzerowany.");
        }

        // Przy następnym Play ankieta pojawi się znowu, a polecane kursy znikną.
        [MenuItem("Finance Quest/Resetuj ankietę")]
        static void ResetSurvey()
        {
            PlayerPrefs.DeleteKey(Recommendations.DoneKey);
            PlayerPrefs.DeleteKey(Recommendations.ListKey);
            PlayerPrefs.Save();
            Debug.Log("Finance Quest: ankieta wyzerowana – pokaże się przy następnym uruchomieniu.");
        }
    }
}

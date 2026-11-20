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
    }
}

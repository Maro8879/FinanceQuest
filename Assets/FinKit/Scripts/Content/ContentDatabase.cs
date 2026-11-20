using System.Collections.Generic;
using UnityEngine;

namespace FinanceQuest
{
    // Cała treść w jednym assecie – składa go importer z plików JSON (Assets/FinKit/Content).
    // Leży w Resources, więc gra wczytuje go jednym wywołaniem, bez parsowania JSON na telefonie.
    public class ContentDatabase : ScriptableObject
    {
        public const string ResourcePath = "Content/ContentDatabase";
        public List<CourseBundle> courses = new List<CourseBundle>();
    }
}

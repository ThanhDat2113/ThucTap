using System.Collections.Generic;
using UnityEngine;

namespace Luan.Dialogue
{
    [CreateAssetMenu(fileName = "Chapter1_Dialogue", menuName = "Luan/Dialogue Story Data")]
    public class DialogueStoryData : ScriptableObject
    {
        [Tooltip("Tên chương hoặc mô tả")]
        public string chapterTitle = "Chapter 1: Concrete Dreams";

        [Tooltip("Danh sách các câu thoại trong chương")]
        public List<DialogueLine> lines = new List<DialogueLine>();
    }
}


using System;
using System.Collections.Generic;
using UnityEngine;

namespace Luan.Dialogue
{
    [CreateAssetMenu(fileName = "Chapter1_Dialogue", menuName = "Luan/Dialogue Story Data")]
    public class DialogueStoryData : ScriptableObject
    {
        [Header("Thông Tin Chapter")]
        public int chapterNumber = 1;
        public string chapterTitle = "Chapter 1: Concrete Dreams";

        [Tooltip("Hình ảnh Logo Chapter (để trống nếu chưa có hình, bạn có thể kéo ảnh vào sau)")]
        public Sprite chapterLogo;

        [Header("Giới Thiệu Nhân Vật (Story Intro)")]
        public string characterStoryTitle = "Câu chuyện của Raze";

        [TextArea(6, 16)]
        public string characterStory = "";

        [Tooltip("Hình ảnh nhân vật trong phần giới thiệu")]
        public Sprite characterIntroArt;

        [Header("Lời Thoại Trước Trận Đấu (Pre-Battle)")]
        public List<DialogueLine> preBattleLines = new List<DialogueLine>();

        [Header("Lời Thoại Sau Trận Đấu (Post-Battle)")]
        public List<DialogueLine> postBattleLines = new List<DialogueLine>();

        // Thuộc tính tương thích ngược cho code cũ
        public List<DialogueLine> lines
        {
            get
            {
                if (preBattleLines != null && preBattleLines.Count > 0)
                    return preBattleLines;
                return _legacyLines;
            }
            set => preBattleLines = value;
        }

        [SerializeField, HideInInspector]
        private List<DialogueLine> _legacyLines = new List<DialogueLine>();
    }
}

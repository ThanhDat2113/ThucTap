using System;
using System.Collections.Generic;
using UnityEngine;

namespace Luan.Dialogue
{
    [System.Serializable]
    public class BackstorySlide
    {
        [Tooltip("Ảnh minh họa toàn cảnh / storyboard cutscene")]
        public Sprite illustration;
        [Tooltip("Tên người nói / phân vai (RAZE, NGƯỜI CHƠI, DẪN CHUYỆN)")]
        public string speakerName = "DẪN CHUYỆN";
        [Tooltip("Màu chữ tên người nói")]
        public Color nameColor = new Color(0.2f, 0.95f, 1f, 1f);
        [Tooltip("Mô tả hành động của nhân vật theo thoại hoặc cốt truyện")]
        public string actionDescription = "";
        [TextArea(2, 6)]
        [Tooltip("Nội dung lời thoại hoặc lời dẫn câu chuyện")]
        public string storyText = "";
    }

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

        [Header("Minh Họa Backstory & Thoại Đầu (Illustrated Cutscenes)")]
        [Tooltip("Danh sách các khung hình minh họa backstory kèm hành động và lời thoại")]
        public List<BackstorySlide> backstorySlides = new List<BackstorySlide>();

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

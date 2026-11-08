using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

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
        [Header("1. THÔNG TIN CHAPTER")]
        public int chapterNumber = 1;
        public string chapterTitle = "Chapter 1: Concrete Dreams";

        [Header("2. INTRO VIDEO CHO CHAPTER (~8s - THAY CHO LOGO)")]
        [Tooltip("Kéo file video intro vào đây. Video này sẽ phát đầu tiên thay thế cho logo chapter.")]
        public VideoClip introVideoClip;
        [Tooltip("Thời lượng tối đa của video intro (giây)")]
        public float introVideoDuration = 8f;

        [Header("3. DANH SÁCH ẢNH BACKSTORY (ĐÃ CÓ THOẠI TRÊN ẢNH)")]
        [Tooltip("Chỉ cần kéo ảnh vào đây! Ảnh 1 là câu chuyện nhân vật (bấm để qua), các ảnh sau là thoại (3s tự qua).")]
        public List<Sprite> backstoryImages = new List<Sprite>();

        [Header("4. THOẠI KHI THẮNG (WIN)")]
        [Tooltip("Hội thoại xuất hiện sau khi người chơi CHIẾN THẮNG bài hát")]
        public List<DialogueLine> winDialogueLines = new List<DialogueLine>();

        [Header("5. THOẠI KHI THUA (LOSE)")]
        [Tooltip("Hội thoại xuất hiện sau khi người chơi THẤT BẠI (hết máu / Fail)")]
        public List<DialogueLine> loseDialogueLines = new List<DialogueLine>();

        /// <summary>
        /// Lấy danh sách ảnh Backstory (ưu tiên backstoryImages, fallback sang backstorySlides cũ nếu có)
        /// </summary>
        public List<Sprite> GetBackstorySprites()
        {
            var imgList = new List<Sprite>();
            if (backstoryImages != null)
            {
                foreach (var img in backstoryImages)
                {
                    if (img != null) imgList.Add(img);
                }
            }

            // Ưu tiên tuyệt đối danh sách backstoryImages được cấu hình trên Inspector
            if (imgList.Count > 0)
                return imgList;

            var slideList = new List<Sprite>();
            if (backstorySlides != null)
            {
                foreach (var s in backstorySlides)
                {
                    if (s != null && s.illustration != null)
                        slideList.Add(s.illustration);
                }
            }
            return slideList;
        }

        public List<DialogueLine> GetWinLines()
        {
            if (winDialogueLines != null && winDialogueLines.Count > 0)
                return winDialogueLines;
            if (postBattleLines != null && postBattleLines.Count > 0)
                return postBattleLines;
            return null;
        }

        public List<DialogueLine> GetLoseLines()
        {
            if (loseDialogueLines != null && loseDialogueLines.Count > 0)
                return loseDialogueLines;
            return null;
        }

        // ==========================================
        // ẨN CÁC TRƯỜNG CŨ ĐỂ INSPECTOR GỌN GÀNG, DỄ DÙNG
        // ==========================================
        [HideInInspector] public Sprite chapterLogo;
        [HideInInspector] public string characterStoryTitle = "";
        [TextArea(3, 8), HideInInspector] public string characterStory = "";
        [HideInInspector] public Sprite characterIntroArt;
        [HideInInspector] public string introVideoUrl = "";
        [HideInInspector] public List<BackstorySlide> backstorySlides = new List<BackstorySlide>();
        [HideInInspector] public List<DialogueLine> preBattleLines = new List<DialogueLine>();
        [HideInInspector] public List<DialogueLine> postBattleLines = new List<DialogueLine>();

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

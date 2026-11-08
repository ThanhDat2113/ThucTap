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
        [Header("Thông Tin Chapter")]
        public int chapterNumber = 1;
        public string chapterTitle = "Chapter 1: Concrete Dreams";

        [Tooltip("Hình ảnh Logo Chapter (để trống nếu chưa có hình, bạn có thể kéo ảnh vào sau)")]
        public Sprite chapterLogo;

        [Header("Intro Video Cho Chapter (~8s)")]
        [Tooltip("Video Clip intro cho chapter (kéo file video .mp4/.webm vào đây)")]
        public VideoClip introVideoClip;
        [Tooltip("Đường dẫn file video hoặc URL (nếu dùng StreamingAssets hoặc URL)")]
        public string introVideoUrl = "";
        [Tooltip("Thời lượng tối đa của video intro (giây)")]
        public float introVideoDuration = 8f;

        [Header("Giới Thiệu Nhân Vật (Story Intro)")]
        public string characterStoryTitle = "Câu chuyện của Raze";

        [TextArea(6, 16)]
        public string characterStory = "";

        [Tooltip("Hình ảnh nhân vật trong phần giới thiệu")]
        public Sprite characterIntroArt;

        [Header("Minh Họa Backstory & Thoại Đầu (Thay Cho Dialog Box Thoại Đầu)")]
        [Tooltip("Danh sách các slide minh họa backstory & thoại đầu")]
        public List<BackstorySlide> backstorySlides = new List<BackstorySlide>();

        [Header("Lời Thoại Trước Trận Đấu (Dự phòng nếu không có Backstory Slides)")]
        public List<DialogueLine> preBattleLines = new List<DialogueLine>();

        [Header("Lời Thoại Sau Trận Đấu - CHIẾN THẮNG (Win)")]
        [Tooltip("Hội thoại xuất hiện sau khi người chơi CHIẾN THẮNG (vượt qua màn chơi)")]
        public List<DialogueLine> winDialogueLines = new List<DialogueLine>();

        [Header("Lời Thoại Sau Trận Đấu - THẤT BẠI (Lose)")]
        [Tooltip("Hội thoại xuất hiện sau khi người chơi THẤT BẠI (hết máu / Fail)")]
        public List<DialogueLine> loseDialogueLines = new List<DialogueLine>();

        [Header("Lời Thoại Sau Trận Đấu Cũ (Fallback)")]
        [Tooltip("Dùng khi winDialogueLines rỗng")]
        public List<DialogueLine> postBattleLines = new List<DialogueLine>();

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

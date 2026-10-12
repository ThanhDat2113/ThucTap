using UnityEngine;

namespace RhythmGame
{
    /// <summary>
    /// Giữ nguyên màu receptor đã đặt trong scene (không đụng tới
    /// thiết lập hiện tại); khi người chơi đang giữ phím lane
    /// (nhấn) thì receptor đổi sang màu xám (pressedColor).
    /// </summary>
    public class ReceptorFlasher : MonoBehaviour
    {
        public SpriteRenderer[] receptors;
        public InputHandler input;

        [Tooltip("Màu receptor khi đang giữ phím (nhấn).")]
        public Color pressedColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        // Màu receptor lúc bắt đầu play — lấy đúng màu đang đặt
        // trong scene để giữ nguyên khi chưa nhấn.
        Color[] idleColors;

        void Start()
        {
            idleColors = new Color[receptors != null ? receptors.Length : 0];
            for (int i = 0; i < idleColors.Length; i++)
                idleColors[i] = receptors[i] != null ? receptors[i].color : Color.white;
        }

        void Update()
        {
            if (input == null) return;
            for (int l = 0; l < idleColors.Length; l++)
            {
                if (receptors[l] == null) continue;
                receptors[l].color = input.IsHeld(l) ? pressedColor : idleColors[l];
            }
        }
    }
}

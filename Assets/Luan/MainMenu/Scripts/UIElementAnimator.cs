using UnityEngine;
using UnityEngine.UI;

namespace Luan.MainMenu
{
    /// <summary>
    /// Script tạo chuyển động hoạt họa nhẹ nhàng cho các thành phần UI trong Canvas.
    /// Có thể dùng cho: Nhân vật (thở), Loa (đập bass), Nốt nhạc (bay lơ lửng), Logo (nhịp beat).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class UIElementAnimator : MonoBehaviour
    {
        public enum AnimationType
        {
            Breathing,    // Thân nhân vật nhấp nhô hít thở
            Floating,     // Nốt nhạc, mũi tên bay bổng lơ lửng
            BeatPunch,    // Loa đập bass / Logo nhún theo nhịp
            NeonGlow      // Viền neon nhấp nháy chớp tắt
        }

        [Header("Animation Settings")]
        [SerializeField] private AnimationType animType = AnimationType.Breathing;
        [SerializeField] private float speed = 2.0f;          // Tốc độ nhịp
        [SerializeField] private float intensity = 0.05f;     // Biên độ co giãn / di chuyển
        [SerializeField] private float timeOffset = 0f;       // Độ lệch pha giữa các vật thể

        private RectTransform rectTransform;
        private Vector3 initialScale;
        private Vector2 initialAnchoredPos;
        private Graphic graphicComponent;
        private Color initialColor;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            initialScale = rectTransform.localScale;
            initialAnchoredPos = rectTransform.anchoredPosition;
            graphicComponent = GetComponent<Graphic>();
            if (graphicComponent != null)
            {
                initialColor = graphicComponent.color;
            }
        }

        private void Update()
        {
            float time = Time.time * speed + timeOffset;

            switch (animType)
            {
                case AnimationType.Breathing:
                    // Co giãn nhẹ theo trục Y (nhịp thở tự nhiên)
                    float breathY = 1.0f + Mathf.Sin(time) * intensity;
                    float breathX = 1.0f + Mathf.Cos(time) * (intensity * 0.3f);
                    rectTransform.localScale = new Vector3(initialScale.x * breathX, initialScale.y * breathY, initialScale.z);
                    break;

                case AnimationType.Floating:
                    // Bay lơ lửng lên xuống hình sin
                    float floatY = Mathf.Sin(time) * (intensity * 100f);
                    float rotZ = Mathf.Cos(time * 0.7f) * 4f;
                    rectTransform.anchoredPosition = initialAnchoredPos + new Vector2(0, floatY);
                    rectTransform.localEulerAngles = new Vector3(0, 0, rotZ);
                    break;

                case AnimationType.BeatPunch:
                    // Nhịp đập bass: đập mạnh nảy ra rồi hồi phục nhanh
                    float beat = Mathf.Sin(time);
                    float kick = Mathf.Max(0, beat);
                    kick = Mathf.Pow(kick, 3f); // Tạo lực nảy dứt khoát
                    float scalePunch = 1.0f + kick * intensity;
                    rectTransform.localScale = initialScale * scalePunch;
                    break;

                case AnimationType.NeonGlow:
                    // Chớp tắt độ sáng viền neon
                    if (graphicComponent != null)
                    {
                        float alphaPulse = 0.7f + (Mathf.Sin(time * 2f) * 0.5f + 0.5f) * 0.3f;
                        graphicComponent.color = new Color(initialColor.r, initialColor.g, initialColor.b, initialColor.a * alphaPulse);
                    }
                    break;
            }
        }
    }
}


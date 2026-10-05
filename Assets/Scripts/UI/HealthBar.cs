using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RhythmGame
{
    /// <summary>
    /// Thanh máu hiển thị trên UI. Nhận giá trị chuẩn hoá (0..1) từ
    /// HealthSystem.OnHealthChanged.
    ///
    /// Thanh co giãn bằng cách đổi anchorMax.x (neo bám cạnh trái) nên máu
    /// thu dần từ phải sang trái. Không dùng Image.Type.Filled vì sprite trắng
    /// tạo bằng code không có border nên fill không vẽ được.
    ///
    /// Có 2 lớp: "ghost" (trắng mờ) đứng sau, hiện phần máu vừa mất rồi mới
    /// tụt xuống — giúp người chơi nhìn thấy rõ vừa bị trừ bao nhiêu.
    /// </summary>
    public class HealthBar : MonoBehaviour
    {
        [Header("Refs")]
        public HealthSystem healthSystem;
        public RectTransform ghostRect;
        public RectTransform fillRect;
        public Image fillImage;
        public TMP_Text healthText;

        [Header("Màu")]
        public Color highColor = new Color(0.25f, 0.90f, 0.35f);
        public Color midColor = new Color(1.00f, 0.80f, 0.10f);
        public Color lowColor = new Color(1.00f, 0.20f, 0.20f);
        [Range(0f, 1f)] public float midThreshold = 0.5f;
        [Range(0f, 1f)] public float lowThreshold = 0.3f;

        [Header("Hiệu ứng")]
        [Tooltip("Tốc độ thanh máu chính bắt kịp giá trị mới (cao = nhanh).")]
        public float fillSpeed = 9f;
        [Tooltip("Tốc độ thanh ghost tụt xuống (thấp = chậm hơn, dễ thấy hơn).")]
        public float ghostSpeed = 2.2f;
        [Tooltip("Máu thấp hơn ngưỡng này sẽ nhấp nháy cảnh báo.")]
        [Range(0f, 1f)] public float dangerThreshold = 0.25f;
        public float pulseSpeed = 6f;

        float current;      // giá trị thanh chính đang hiển thị
        float ghost;        // giá trị thanh ghost đang hiển thị
        float target = 1f;  // giá trị đích
        float pulseTimer;

        void Awake()
        {
            current = ghost = target = 1f;
            Apply(current);
            ApplyGhost(ghost);
        }

        void OnEnable()
        {
            if (healthSystem != null) healthSystem.OnHealthChanged.AddListener(HandleHealthChanged);
        }

        void OnDisable()
        {
            if (healthSystem != null) healthSystem.OnHealthChanged.RemoveListener(HandleHealthChanged);
        }

        /// <summary>
        /// Gán reference sau khi AddComponent (Awake đã chạy trước lúc gán nên
        /// không thể khởi tạo trong Awake). Dùng khi dựng UI bằng code.
        /// </summary>
        public void Bind(HealthSystem system, RectTransform ghostTransform, RectTransform fillTransform, Image fillImg, TMP_Text text)
        {
            if (healthSystem != null && isActiveAndEnabled)
                healthSystem.OnHealthChanged.RemoveListener(HandleHealthChanged);

            healthSystem = system;
            ghostRect = ghostTransform;
            fillRect = fillTransform;
            fillImage = fillImg;
            healthText = text;

            if (healthSystem != null)
            {
                // Lưu ý: tham số không được trùng tên field float (ghost/fill) vì sẽ bị che.
                current = ghost = target = healthSystem.NormalizedHealth;
                if (isActiveAndEnabled) healthSystem.OnHealthChanged.AddListener(HandleHealthChanged);
            }

            Apply(current);
            ApplyGhost(ghost);
            RefreshLabel();
        }

        public void HandleHealthChanged(float normalized)
        {
            target = Mathf.Clamp01(normalized);

            // Tăng máu thì ghost bám sát luôn, không cần hiệu ứng tụt.
            if (target >= ghost) ghost = target;

            RefreshLabel();
        }

        void RefreshLabel()
        {
            if (healthText == null || healthSystem == null) return;
            healthText.text = Mathf.RoundToInt(healthSystem.CurrentHealth) + " / " + Mathf.RoundToInt(healthSystem.MaxHealth);
        }

        void Update()
        {
            current = Mathf.MoveTowards(current, target, fillSpeed * Time.deltaTime);
            ghost = Mathf.MoveTowards(ghost, target, ghostSpeed * Time.deltaTime);

            Apply(current);
            ApplyGhost(ghost);

            // Nhấp nháy khi máu thấp để cảnh báo người chơi.
            Color c = ColorFor(current);
            if (current <= dangerThreshold && current > 0f)
            {
                pulseTimer += Time.deltaTime * pulseSpeed;
                float pulse = (Mathf.Sin(pulseTimer * Mathf.PI * 2f) + 1f) * 0.5f;
                c = Color.Lerp(c, Color.white, pulse * 0.6f);
            }
            else
            {
                pulseTimer = 0f;
            }
            if (fillImage != null) fillImage.color = c;
        }

        void Apply(float v)
        {
            if (fillRect != null)
            {
                var max = fillRect.anchorMax;
                max.x = Mathf.Clamp01(v);
                fillRect.anchorMax = max;
            }
            else if (fillImage != null)
            {
                fillImage.fillAmount = v;
            }
        }

        void ApplyGhost(float v)
        {
            if (ghostRect != null)
            {
                var max = ghostRect.anchorMax;
                max.x = Mathf.Clamp01(v);
                ghostRect.anchorMax = max;
            }
        }

        Color ColorFor(float v)
        {
            if (v <= lowThreshold) return lowColor;
            if (v <= midThreshold) return Color.Lerp(lowColor, midColor, (v - lowThreshold) / Mathf.Max(0.0001f, midThreshold - lowThreshold));
            return Color.Lerp(midColor, highColor, (v - midThreshold) / Mathf.Max(0.0001f, 1f - midThreshold));
        }
    }
}
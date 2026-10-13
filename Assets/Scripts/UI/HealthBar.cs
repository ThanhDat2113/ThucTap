using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RhythmGame
{
    /// <summary>
    /// Thanh máu hiển thị trên UI. Nhận giá trị chuẩn hoá (0..1) từ
    /// HealthSystem.OnHealthChanged.
    ///
    /// Code chỉ làm một việc: RÚT NGẮN hình ảnh theo HP. Mọi thứ khác
    /// (sprite, màu, vị trí, kích thước, anchor, pivot) đều do bạn đặt
    /// trong scene. Code tự đo trạng thái "đầy" (HP=100%) lúc Awake/Bind
    /// rồi rút ngắn bề rộng, giữ nguyên CẠNH TRÁI nên máu luôn thu dần
    /// TỪ PHẢI SANG TRÁI. HP = 0 -> bề rộng = 0 -> hình ảnh thu lại hết.
    ///
    /// Cách rút:
    ///   - Có fillRect: đổi sizeDelta.x (kèm bù anchoredPosition.x theo
    ///     pivot để cạnh trái đứng yên). Hoạt động với mọi kiểu
    ///     anchor/pivot đã đặt trong scene, không ghi đè lên chúng.
    ///   - Chỉ có fillImage (không có fillRect): dùng fillAmount. Khi đó
    ///     đặt Image Type = Filled, Fill Method = Horizontal,
    ///     Fill Origin = Left để thanh rút từ phải sang trái.
    ///
    /// Có 2 lớp: "ghost" (trắng mờ) đứng sau, hiện phần máu vừa mất rồi
    /// mới tụt xuống — giúp người chơi nhìn thấy rõ vừa bị trừ bao nhiêu.
    /// Ghost rút bằng cơ chế như lớp fill.
    /// </summary>
    public class HealthBar : MonoBehaviour
    {
        [Header("Refs")]
        public HealthSystem healthSystem;
        [Tooltip("RectTransform lớp ghost. Để trống thì tắt hiệu ứng ghost.")]
        public RectTransform ghostRect;
        [Tooltip("RectTransform lớp máu chính. Code chỉ rút ngắn bề rộng " +
                 "(sizeDelta.x) và giữ cạnh trái đứng yên -> máu rút từ " +
                 "phải sang trái; HP = 0 thì hình ảnh thu lại hết.")]
        public RectTransform fillRect;
        [Tooltip("Image lớp máu chính (đổi màu theo HP). Chỉ dùng khi không " +
                 "có fillRect: lúc đó đặt Image Type = Filled, Fill Method " +
                 "= Horizontal, Fill Origin = Left.")]
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

        // Trạng thái "đầy" (HP = 100%) đo được lúc Awake/Bind.
        bool fillCaptured, ghostCaptured;
        float fillFullWidth, fillBaseX, fillPivotX;
        float ghostFullWidth, ghostBaseX, ghostPivotX;

        void Awake()
        {
            current = ghost = target = 1f;
            CaptureRects();
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

            CaptureRects();

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

        /// <summary>
        /// Đo trạng thái "đầy" của thanh (bề rộng, vị trí, pivot).
        /// Anchor X dạng giãn (anchorMin.x != anchorMax.x) được chuyển về
        /// dạng điểm ở cạnh trái, giữ nguyên vị trí hiển thị, để việc
        /// rút ngắn sau này chỉ cần đổi sizeDelta.x.
        /// </summary>
        void CaptureRects()
        {
            fillCaptured = Capture(fillRect, out fillFullWidth, out fillBaseX, out fillPivotX);
            ghostCaptured = Capture(ghostRect, out ghostFullWidth, out ghostBaseX, out ghostPivotX);
        }

        bool Capture(RectTransform r, out float fullWidth, out float baseX, out float pivotX)
        {
            fullWidth = 0f;
            baseX = 0f;
            pivotX = 0.5f;
            if (r == null) return false;

            float spread = r.anchorMax.x - r.anchorMin.x;
            if (spread > 0.0001f)
            {
                // Neo giãn -> neo điểm ở cạnh trái hiện tại; bù sizeDelta
                // và anchoredPosition để hình ảnh không nhảy chỗ.
                float pw = ParentWidth(r);
                r.anchorMax = new Vector2(r.anchorMin.x, r.anchorMax.y);
                r.sizeDelta = new Vector2(r.sizeDelta.x + spread * pw, r.sizeDelta.y);
                r.anchoredPosition = new Vector2(
                    r.anchoredPosition.x + r.pivot.x * spread * pw,
                    r.anchoredPosition.y);
            }

            fullWidth = r.rect.width;
            baseX = r.anchoredPosition.x;
            pivotX = r.pivot.x;
            return fullWidth > 0f;
        }

        static float ParentWidth(RectTransform r)
        {
            RectTransform p = r.parent as RectTransform;
            return p != null ? p.rect.width : 0f;
        }

        /// <summary>
        /// Rút ngắn hình ảnh: bề rộng = fullWidth * HP. Cạnh trái đứng
        /// yên (bù anchoredPosition theo pivot) nên máu rút từ phải sang
        /// trái; HP = 0 -> bề rộng bằng 0 -> hình ảnh thu lại hết.
        /// </summary>
        void Apply(float v)
        {
            v = Mathf.Clamp01(v);
            if (fillCaptured)
            {
                Shorten(fillRect, v, fillFullWidth, fillBaseX, fillPivotX);
            }
            else if (fillImage != null)
            {
                fillImage.fillAmount = v;
            }
        }

        void ApplyGhost(float v)
        {
            if (ghostCaptured)
            {
                Shorten(ghostRect, v, ghostFullWidth, ghostBaseX, ghostPivotX);
            }
        }

        static void Shorten(RectTransform r, float v, float fullWidth, float baseX, float pivotX)
        {
            float w = fullWidth * v;
            r.sizeDelta = new Vector2(w, r.sizeDelta.y);
            // Bù vị trí theo pivot để cạnh trái của ảnh không dịch chuyển.
            r.anchoredPosition = new Vector2(baseX + pivotX * (w - fullWidth), r.anchoredPosition.y);
        }

        Color ColorFor(float v)
        {
            if (v <= lowThreshold) return lowColor;
            if (v <= midThreshold) return Color.Lerp(lowColor, midColor, (v - lowThreshold) / Mathf.Max(0.0001f, midThreshold - lowThreshold));
            return Color.Lerp(midColor, highColor, (v - midThreshold) / Mathf.Max(0.0001f, 1f - midThreshold));
        }
    }
}

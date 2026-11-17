using UnityEngine;

namespace RhythmGame
{
    /// <summary>
    /// Quản lý thanh Flow (gauge combo).
    /// - Hit note liên tục (combo) làm tăng flow.
    /// - Khi flow đạt max -> vào trạng thái "In the Flow".
    /// - Trong trạng thái In the Flow: flow GIỮ NGUYÊN (không tiêu hao theo thời gian).
    /// - Miss note -> thoát In the Flow NGAY LẬP TỨC (flow về 0).
    /// Không tác động đến tốc độ nhạc hay chart.
    /// </summary>
    public class FlowSystem : MonoBehaviour
    {
        [Header("Flow Settings")]
        [Tooltip("Lượng flow tăng mỗi lần hit note thành công.")]
        public float flowBuildPerHit = 10f;
        [Tooltip("Lượng flow tăng thêm mỗi cấp combo (cộng vào flowBuildPerHit).")]
        public float flowBuildPerCombo = 2f;
        [Tooltip("Giới hạn tối đa của thanh flow (0-100).")]
        public float maxFlow = 100f;
        [Tooltip("Hệ số điểm khi đang In the Flow (1.0 = không thay đổi).")]
        public float flowScoreMultiplier = 1.5f;
        [Tooltip("Lượng flow mất khi miss trong lúc CHƯA In the Flow. Khi đang In the Flow, miss sẽ thoát state ngay (không cần trừ).")]
        public float flowLossPerMiss = 30f;

        // Field cũ — không còn dùng. Giữ lại để prefab cũ không mất dữ liệu serialize.
        [HideInInspector] public float flowDrainRate = 0f;

        [Header("Events")]
        public System.Action<float> OnFlowAmountChanged;
        public System.Action<bool> OnFlowStateChanged;

        float _flowAmount;
        bool _isFlowActive;
        int _currentCombo;
        bool _flowJustDeactivated;

        public float FlowAmount => _flowAmount;
        public float NormalizedFlow => maxFlow > 0f ? Mathf.Clamp01(_flowAmount / maxFlow) : 0f;
        public bool IsFlowActive => _isFlowActive;
        public float FlowScoreMultiplier => _isFlowActive ? flowScoreMultiplier : 1f;

        void Awake()
        {
            _flowAmount = 0f;
            _isFlowActive = false;
            _currentCombo = 0;
            _flowJustDeactivated = false;
            OnFlowAmountChanged?.Invoke(NormalizedFlow);
            OnFlowStateChanged?.Invoke(_isFlowActive);
        }

        /// <summary>
        /// Gọi khi hit note thành công. Tăng flow, kiểm tra chuyển sang In the Flow.
        /// </summary>
        public void OnNoteJudged(bool isHit, int combo)
        {
            if (!isHit)
            {
                OnMiss();
                return;
            }

            _currentCombo = combo;

            // Đang In the Flow: flow đã max, giữ nguyên — không cộng thêm, cũng không drain
            if (_isFlowActive) return;

            float build;
            if (_flowJustDeactivated)
            {
                build = flowBuildPerHit;
            }
            else
            {
                build = flowBuildPerHit + (flowBuildPerCombo * Mathf.Max(0, _currentCombo - 1));
            }
            _flowAmount = Mathf.Clamp(_flowAmount + build, 0f, maxFlow);

            if (_flowAmount >= maxFlow)
            {
                _flowJustDeactivated = false;
                ActivateFlow();
            }

            OnFlowAmountChanged?.Invoke(NormalizedFlow);
        }

        /// <summary>
        /// Gọi khi miss note.
        /// - Đang In the Flow: THOÁT ngay lập tức (flow về 0). Không trừ dần.
        /// - Chưa In the Flow: giảm một lượng nhỏ flow đang tích.
        /// </summary>
        public void OnMiss()
        {
            _currentCombo = 0;

            if (_isFlowActive)
            {
                // ★ Miss = kết thúc In the Flow ngay
                DeactivateFlow();
                _flowJustDeactivated = true;
                return;
            }

            _flowAmount = Mathf.Max(_flowAmount - flowLossPerMiss, 0f);
            OnFlowAmountChanged?.Invoke(NormalizedFlow);
        }

        void ActivateFlow()
        {
            _isFlowActive = true;
            _flowAmount = maxFlow;
            OnFlowAmountChanged?.Invoke(NormalizedFlow);
            OnFlowStateChanged?.Invoke(true);
        }

        void DeactivateFlow()
        {
            _isFlowActive = false;
            _flowAmount = 0f;
            _currentCombo = 0;
            OnFlowAmountChanged?.Invoke(NormalizedFlow);
            OnFlowStateChanged?.Invoke(false);
        }

        public void ResetFlow()
        {
            _flowAmount = 0f;
            _currentCombo = 0;
            _flowJustDeactivated = false;
            if (_isFlowActive)
            {
                DeactivateFlow();
            }
            else
            {
                OnFlowAmountChanged?.Invoke(NormalizedFlow);
            }
        }

        public void Reset()
        {
            _flowAmount = 0f;
            _isFlowActive = false;
            _currentCombo = 0;
            _flowJustDeactivated = false;
            OnFlowAmountChanged?.Invoke(NormalizedFlow);
            OnFlowStateChanged?.Invoke(false);
        }

        // Không còn Update() drain theo thời gian.
        // Flow chỉ kết thúc khi có sự kiện miss (hoặc Reset).
    }
}

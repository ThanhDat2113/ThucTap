using UnityEngine;

namespace RhythmGame
{
    /// <summary>
    /// Quản lý thanh Flow (gauge combo).
    /// - Hit note liên tục (combo) làm tăng flow.
    /// - Khi flow đạt max -> vào trạng thái "In the Flow".
    /// - Trong trạng thái In the Flow: flow tiêu hao dần theo thời gian, điểm nhan duoc duoc nhan theo he so.
    /// - Miss hoac het flow -> thoat trang thai In the Flow, flow ve 0.
    /// Khong tac dong den toc do nhac hay chart.
    /// </summary>
    public class FlowSystem : MonoBehaviour
    {
        [Header("Flow Settings")]
        [Tooltip("Luong flow tang mot lan hit note thanh cong.")]
        public float flowBuildPerHit = 10f;
        [Tooltip("Luong flow tang them moi cap combo (them vao flowBuildPerHit).")]
        public float flowBuildPerCombo = 2f;
        [Tooltip("Gioi han toi da cua thanh flow (0-100).")]
        public float maxFlow = 100f;
        [Tooltip("Toc do tieu hao flow khi dang o trang thai In the Flow (don vi flow / giay).")]
        public float flowDrainRate = 20f;
        [Tooltip("He so diem khi dang o trang thai In the Flow (1.0 = khong thay doi).")]
        public float flowScoreMultiplier = 1.5f;

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
        /// Goi khi hit note thanh cong. Tang flow, kiem tra chuyen sang trang thai In the Flow.
        /// </summary>
        public void OnNoteJudged(bool isHit, int combo)
        {
            if (!isHit)
            {
                ResetFlow();
                return;
            }

            _currentCombo = combo;

            // Khi dang o trang thai In the Flow, flow tiep tuc tut xuong, khong tang len nua
            if (_isFlowActive) return;

            // Khi flow vua tut xuong het, cho phep build flow lai chi khi combo moi bat dau (combo == 1)
            if (_flowJustDeactivated)
            {
                if (combo <= 1)
                {
                    _flowJustDeactivated = false;
                }
                else
                {
                    return;
                }
            }

            float build = flowBuildPerHit + (flowBuildPerCombo * Mathf.Max(0, _currentCombo - 1));
            _flowAmount = Mathf.Clamp(_flowAmount + build, 0f, maxFlow);

            if (!_isFlowActive && _flowAmount >= maxFlow)
            {
                ActivateFlow();
            }

            OnFlowAmountChanged?.Invoke(NormalizedFlow);
        }

        /// <summary>
        /// Goi khi miss note. Reset flow ve 0 va thoat trang thai In the Flow.
        /// </summary>
        public void OnMiss()
        {
            // Miss khong reset flow ve 0, chi giam mot luong nho
            _currentCombo = 0;
            if (_isFlowActive)
            {
                // Dang In the Flow: khong deactivates, chi giam flow
                _flowAmount = Mathf.Max(_flowAmount - flowBuildPerHit * 2f, 0f);
                OnFlowAmountChanged?.Invoke(NormalizedFlow);
                if (_flowAmount <= 0f)
                {
                    DeactivateFlow();
                    _flowJustDeactivated = true;
                }
            }
            else
            {
                // Chua active: giam flow mot luong, khong reset ve 0
                _flowAmount = Mathf.Max(_flowAmount - flowBuildPerHit * 3f, 0f);
                OnFlowAmountChanged?.Invoke(NormalizedFlow);
            }
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
            _flowJustDeactivated = true;
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

        void Update()
        {
            if (!_isFlowActive) return;

            _flowAmount = Mathf.Max(_flowAmount - flowDrainRate * Time.deltaTime, 0f);
            OnFlowAmountChanged?.Invoke(NormalizedFlow);

            if (_flowAmount <= 0f)
            {
                DeactivateFlow();
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
    }
}

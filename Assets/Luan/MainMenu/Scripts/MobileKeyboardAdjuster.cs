using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Luan.MainMenu
{
    /// <summary>
    /// Script tự động nâng/đẩy Panel (Login, Register...) lên cao khi mở bàn phím ảo trên điện thoại (Android / iOS).
    /// Khắc phục triệt để lỗi cộng dồn độ cao khi chuyển nhanh giữa Username và Password.
    /// Tương thích hoàn toàn với New Input System (không gây lỗi InvalidOperationException).
    /// </summary>
    [AddComponentMenu("UI/Luan MainMenu/Mobile Keyboard Adjuster")]
    public class MobileKeyboardAdjuster : MonoBehaviour
    {
        [Header("--- Mục Tiêu Nâng (Target Transform) ---")]
        [Tooltip("RectTransform của Panel cần đẩy lên (để trống script sẽ tự lấy Panel này)")]
        [SerializeField] private RectTransform targetToLift;

        [Header("--- Cấu Hình Độ Nâng (Lift Settings) ---")]
        [Tooltip("Khoảng cách nâng panel lên theo trục Y khi bàn phím mở (pixel Canvas). Màn hình ngang khuyến nghị 180 - 260px")]
        [SerializeField] private float liftOffset = 220f;

        [Tooltip("Thời gian chuyển động trượt lên / xuống (giây)")]
        [Range(0.1f, 0.6f)]
        [SerializeField] private float transitionDuration = 0.25f;

        [Tooltip("Đường cong gia tốc chuyển động mượt mà")]
        [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("--- Tùy Chọn Di Động (Mobile Options) ---")]
        [Tooltip("Ẩn thanh nhập chữ phụ mặc định của hệ điều hành để người chơi gõ và nhìn trực tiếp vào ô trong game")]
        [SerializeField] private bool hideNativeInputBar = true;

        [Tooltip("Cho phép bấm vào vùng trống bên ngoài Panel để tự động ẩn bàn phím và hạ panel về vị trí cũ")]
        [SerializeField] private bool dismissOnTapOutside = true;

        [Header("--- Thử Nghiệm Trong Editor ---")]
        [Tooltip("Bật chế độ mô phỏng nâng panel ngay trong Unity Editor khi bấm vào ô nhập")]
        [SerializeField] private bool testInEditor = true;

        // TỌA ĐỘ GỐC BẤT BIẾN - Ghi nhớ một lần duy nhất lúc khởi động, KHÔNG BAO GIỜ bị ghi đè khi panel đang nâng
        private Vector2 _basePosition;
        private bool _hasBasePosition = false;

        private bool _isLifted = false;
        private Coroutine _moveRoutine;
        private Coroutine _checkDeselectRoutine;

        private readonly List<TMP_InputField> _tmpInputs = new List<TMP_InputField>();
        private readonly List<InputField> _legacyInputs = new List<InputField>();

        public bool IsLifted => _isLifted;
        public float LiftOffset => liftOffset;
        public Vector2 BasePosition => _basePosition;

        private void Awake()
        {
            InitTargetAndBasePosition();
            CollectAndBindInputFields();
        }

        private void Start()
        {
            InitTargetAndBasePosition();
        }

        /// <summary>
        /// Khởi tạo và khóa cứng tọa độ gốc chuẩn của Panel.
        /// </summary>
        private void InitTargetAndBasePosition()
        {
            if (targetToLift == null)
            {
                targetToLift = GetComponent<RectTransform>();
            }

            // CHỈ lưu tọa độ gốc 1 lần duy nhất lúc ban đầu
            if (targetToLift != null && !_hasBasePosition)
            {
                _basePosition = targetToLift.anchoredPosition;
                _hasBasePosition = true;
            }
        }

        private void OnEnable()
        {
            CollectAndBindInputFields();

            // Nếu panel đang tắt mở lại trong trạng thái bình thường (không nâng), cập nhật tọa độ gốc nếu cần
            if (!_isLifted && targetToLift != null)
            {
                _basePosition = targetToLift.anchoredPosition;
                _hasBasePosition = true;
            }
        }

        private void OnDisable()
        {
            ResetImmediate();
        }

        private void Update()
        {
#if UNITY_ANDROID || UNITY_IOS
            bool isMobileKbVisible = TouchScreenKeyboard.visible;

            if (isMobileKbVisible && !_isLifted)
            {
                LiftPanel();
            }
            else if (!isMobileKbVisible && _isLifted && !IsAnyInputFocused())
            {
                LowerPanel();
            }
#endif

            // Kiểm tra chạm/click ra vùng trống bên ngoài để đóng bàn phím
            if (dismissOnTapOutside && _isLifted)
            {
                CheckPointerDownTapOutside();
            }
        }

        /// <summary>
        /// Phát hiện chạm/click bằng New Input System hoặc Legacy Input an toàn, không ném ngoại lệ.
        /// </summary>
        private void CheckPointerDownTapOutside()
        {
            bool pointerDown = false;
            Vector2 pointerPos = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
            {
                pointerDown = true;
                pointerPos = Pointer.current.position.ReadValue();
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetMouseButtonDown(0))
            {
                pointerDown = true;
                pointerPos = Input.mousePosition;
            }
#endif

            if (pointerDown)
            {
                // Nếu điểm chạm KHÔNG nằm trong Panel (chạm ra khoảng trống nền bên ngoài)
                if (!IsPointerOverPanel(pointerPos))
                {
                    DismissKeyboard();
                }
            }
        }

        /// <summary>
        /// Thu thập tất cả các ô nhập liệu trong Panel và gán sự kiện focus/unfocus.
        /// </summary>
        public void CollectAndBindInputFields()
        {
            _tmpInputs.Clear();
            _legacyInputs.Clear();

            GetComponentsInChildren(true, _tmpInputs);
            GetComponentsInChildren(true, _legacyInputs);

            foreach (var tmp in _tmpInputs)
            {
                if (tmp == null) continue;

                tmp.onSelect.RemoveListener(OnInputSelected);
                tmp.onSelect.AddListener(OnInputSelected);

                tmp.onDeselect.RemoveListener(OnInputDeselected);
                tmp.onDeselect.AddListener(OnInputDeselected);

                if (hideNativeInputBar)
                {
                    tmp.shouldHideMobileInput = true;
                }
            }

            foreach (var legacy in _legacyInputs)
            {
                if (legacy == null) continue;

                if (hideNativeInputBar)
                {
                    legacy.shouldHideMobileInput = true;
                }
            }
        }

        private void OnInputSelected(string _)
        {
            HandleFocusChange(true);
        }

        private void OnInputDeselected(string _)
        {
            HandleFocusChange(false);
        }

        private void HandleFocusChange(bool hasFocus)
        {
            if (hasFocus)
            {
                // Người chơi đang focus vào 1 ô nhập -> hủy ngay coroutine kiểm tra hạ panel
                if (_checkDeselectRoutine != null)
                {
                    StopCoroutine(_checkDeselectRoutine);
                    _checkDeselectRoutine = null;
                }

                bool canLift = Application.isMobilePlatform || testInEditor;
                if (canLift)
                {
                    LiftPanel();
                }
            }
            else
            {
                // Người chơi rời khỏi ô nhập: Chờ 0.15s xem có phải họ bấm chuyển sang ô khác trong cùng Panel không
                if (_checkDeselectRoutine != null)
                {
                    StopCoroutine(_checkDeselectRoutine);
                }
                _checkDeselectRoutine = StartCoroutine(CheckDeselectRoutine());
            }
        }

        private IEnumerator CheckDeselectRoutine()
        {
            // Đợi 0.15s để hệ điều hành và EventSystem kịp chuyển focus sang ô tiếp theo nếu bấm từ Username sang Password
            yield return new WaitForSecondsRealtime(0.15f);

            bool anyFocused = IsAnyInputFocused();
            bool mobileKbActive = false;

#if UNITY_ANDROID || UNITY_IOS
            mobileKbActive = TouchScreenKeyboard.visible;
#endif

            // Chỉ hạ panel khi THỰC SỰ không còn ô nào trong panel được chọn và bàn phím đã đóng
            if (!anyFocused && !mobileKbActive && _isLifted)
            {
                LowerPanel();
            }

            _checkDeselectRoutine = null;
        }

        private bool IsAnyInputFocused()
        {
            foreach (var tmp in _tmpInputs)
            {
                if (tmp != null && tmp.isFocused) return true;
            }

            foreach (var legacy in _legacyInputs)
            {
                if (legacy != null && legacy.isFocused) return true;
            }

            return false;
        }

        /// <summary>
        /// Kiểm tra xem con trỏ chuột / điểm chạm ngón tay có đang nằm trên Panel hoặc bất kỳ phần tử con nào của Panel không.
        /// </summary>
        private bool IsPointerOverPanel(Vector2 screenPosition)
        {
            if (EventSystem.current == null || targetToLift == null) return false;

            PointerEventData eventData = new PointerEventData(EventSystem.current)
            {
                position = screenPosition
            };

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            foreach (var r in results)
            {
                if (r.gameObject == null) continue;

                // Nếu click vào chính Panel hoặc bất kỳ phần tử con nào (ô Username, Password, mắt xem pass, checkbox...)
                if (r.gameObject.transform == targetToLift.transform ||
                    r.gameObject.transform.IsChildOf(targetToLift.transform))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Đóng bàn phím ảo và hủy chọn ô nhập hiện tại.
        /// </summary>
        public void DismissKeyboard()
        {
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

#if UNITY_ANDROID || UNITY_IOS
            if (TouchScreenKeyboard.visible)
            {
                TouchScreenKeyboard.hideInput = true;
            }
#endif

            if (_isLifted)
            {
                LowerPanel();
            }
        }

        /// <summary>
        /// Nâng panel lên một khoảng liftOffset theo trục Y (Tính chuẩn từ vị trí gốc _basePosition).
        /// TUYỆT ĐỐI CỐ ĐỊNH, KHÔNG CỘNG DỒN TỌA ĐỘ.
        /// </summary>
        public void LiftPanel()
        {
            if (targetToLift == null) return;

            InitTargetAndBasePosition();

            _isLifted = true;

            // Đích đến LUÔN LUÔN là _basePosition + liftOffset (Cố định, không phụ thuộc vào vị trí hiện tại)
            Vector2 targetPos = new Vector2(_basePosition.x, _basePosition.y + liftOffset);
            AnimateToPosition(targetPos);
        }

        /// <summary>
        /// Hạ panel trở về đúng vị trí gốc ban đầu (_basePosition).
        /// </summary>
        public void LowerPanel()
        {
            if (targetToLift == null) return;

            InitTargetAndBasePosition();

            _isLifted = false;

            // Đích đến LUÔN LUÔN là _basePosition gốc
            Vector2 targetPos = _basePosition;
            AnimateToPosition(targetPos);
        }

        /// <summary>
        /// Đặt lại ngay lập tức về vị trí gốc không có hiệu ứng (dùng khi đóng panel hoặc chuyển tab).
        /// </summary>
        public void ResetImmediate()
        {
            if (_moveRoutine != null)
            {
                StopCoroutine(_moveRoutine);
                _moveRoutine = null;
            }

            if (_checkDeselectRoutine != null)
            {
                StopCoroutine(_checkDeselectRoutine);
                _checkDeselectRoutine = null;
            }

            if (targetToLift != null && _hasBasePosition)
            {
                targetToLift.anchoredPosition = _basePosition;
            }

            _isLifted = false;
        }

        /// <summary>
        /// Cài đặt độ nâng mới từ script bên ngoài.
        /// </summary>
        public void SetLiftOffset(float newOffset)
        {
            liftOffset = newOffset;
            if (_isLifted && targetToLift != null && _hasBasePosition)
            {
                targetToLift.anchoredPosition = new Vector2(_basePosition.x, _basePosition.y + liftOffset);
            }
        }

        /// <summary>
        /// Đặt lại tọa độ gốc nếu người dùng di chuyển panel trong Unity Editor.
        /// </summary>
        public void SetBasePosition(Vector2 pos)
        {
            _basePosition = pos;
            _hasBasePosition = true;
        }

        private void AnimateToPosition(Vector2 targetPos)
        {
            // Nếu đã ở đúng vị trí (sai số < 0.1px) thì không cần chạy Coroutine
            if ((targetToLift.anchoredPosition - targetPos).sqrMagnitude < 0.01f)
            {
                targetToLift.anchoredPosition = targetPos;
                return;
            }

            if (_moveRoutine != null)
            {
                StopCoroutine(_moveRoutine);
            }
            _moveRoutine = StartCoroutine(MoveRoutine(targetPos));
        }

        private IEnumerator MoveRoutine(Vector2 targetPos)
        {
            Vector2 startPos = targetToLift.anchoredPosition;
            float elapsed = 0f;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / transitionDuration);
                float curveT = transitionCurve != null ? transitionCurve.Evaluate(t) : Mathf.SmoothStep(0f, 1f, t);
                targetToLift.anchoredPosition = Vector2.LerpUnclamped(startPos, targetPos, curveT);
                yield return null;
            }

            targetToLift.anchoredPosition = targetPos;
            _moveRoutine = null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                _hasBasePosition = false;
                InitTargetAndBasePosition();
            }
        }

        [ContextMenu("▶ Test: Nâng Panel Lên (Lift)")]
        public void ContextLift()
        {
            if (!Application.isPlaying)
            {
                InitTargetAndBasePosition();
                targetToLift.anchoredPosition = new Vector2(_basePosition.x, _basePosition.y + liftOffset);
                _isLifted = true;
            }
            else
            {
                LiftPanel();
            }
        }

        [ContextMenu("◀ Test: Hạ Panel Xuống (Lower)")]
        public void ContextLower()
        {
            if (!Application.isPlaying)
            {
                InitTargetAndBasePosition();
                targetToLift.anchoredPosition = _basePosition;
                _isLifted = false;
            }
            else
            {
                LowerPanel();
            }
        }
#endif
    }
}

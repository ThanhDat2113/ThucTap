using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Luan.MainMenu
{
    /// <summary>
    /// Script điều khiển animation ẩn/hiện mượt mà cho các Panel UI trong Unity Canvas.
    /// Hoạt động 100% bằng Coroutine và CanvasGroup thuần Unity (không phụ thuộc plugin ngoài).
    /// Hỗ trợ nhiều hiệu ứng: Fade, Pop Scale (nhún nảy), Slide (cả panel), và StaggeredSlide (trượt từng nút lần lượt từ trái sang phải).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    [AddComponentMenu("UI/Luan MainMenu/UI Panel Animator")]
    public class UIPanelAnimator : MonoBehaviour
    {
        public enum TransitionType
        {
            FadeOnly,        // Chỉ mờ dần / rõ dần
            PopScale,        // Phóng to thu nhỏ nảy nhẹ (hợp phong cách anime/game âm nhạc)
            SlideHorizontal, // Trượt ngang (trái <-> phải) nguyên cả panel
            SlideVertical,   // Trượt dọc (trên <-> dưới) nguyên cả panel
            FadeAndScale,    // Kết hợp mờ dần và phóng to/thu nhỏ
            StaggeredSlide   // Trượt lần lượt từng nút con từ trái sang phải (so le nhau)
        }

        public enum SlideDirection
        {
            FromLeft,
            FromRight,
            FromTop,
            FromBottom
        }

        [Header("--- Cấu hình Hiệu Ứng (Animation Settings) ---")]
        [Tooltip("Kiểu hiệu ứng ẩn hiện")]
        [SerializeField] private TransitionType transitionType = TransitionType.FadeAndScale;

        [Tooltip("Hướng trượt (áp dụng khi chọn Slide hoặc StaggeredSlide)")]
        [SerializeField] private SlideDirection slideDirection = SlideDirection.FromLeft;

        [Tooltip("Thời gian chạy animation của mỗi phần tử (giây)")]
        [Range(0.1f, 1.5f)]
        [SerializeField] private float duration = 0.4f;

        [Tooltip("Đường cong gia tốc mượt mà (Ease curve)")]
        [SerializeField] private AnimationCurve animCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("--- Cấu Hình Trượt Lần Lượt (Staggered Slide Settings) ---")]
        [Tooltip("Thời gian trễ giữa mỗi nút trượt vào (ví dụ: 0.1s - nút 1 vào, 0.1s sau nút 2 vào, tiếp nút 3)")]
        [Range(0.02f, 0.5f)]
        [SerializeField] private float staggerDelay = 0.1f;

        [Tooltip("Khoảng cách trượt từ ngoài màn hình vào (Pixel), mặc định 800px")]
        [SerializeField] private float slideDistance = 800f;

        [Tooltip("Danh sách các nút trượt lần lượt (Kéo 3 nút Bắt Đầu, Cài Đặt, Thành Tích vào đây; nếu để trống script sẽ tự lấy các con)")]
        [SerializeField] private List<RectTransform> staggeredElements = new List<RectTransform>();

        [Header("--- Trạng Thái Ban Đầu (Initial State) ---")]
        [Tooltip("Tự động chạy Animation Hiện ngay khi bấm Play game (BẬT Ô NÀY ĐỂ TEST NGAY HIỆU ỨNG TRƯỢT 3 NÚT)")]
        [SerializeField] private bool playAnimOnStart = false;

        [Tooltip("Panel có hiển thị ngay khi Start game không (hiện sẵn không chạy anim)?")]
        [SerializeField] private bool startVisible = false;

        [Tooltip("Tắt GameObject (SetActive = false) khi ẩn xong để tối ưu hiệu năng draw call")]
        [SerializeField] private bool deactivateOnHide = true;

        [Header("--- Sự Kiện (Events) ---")]
        public UnityEvent onShowStarted;
        public UnityEvent onShowFinished;
        public UnityEvent onHideStarted;
        public UnityEvent onHideFinished;

        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private Coroutine _currentAnimRoutine;

        private Vector2 _originalPosition;
        private Vector3 _originalScale;
        private List<Vector2> _originalElementPositions = new List<Vector2>();
        private bool _isInitialized = false;
        private bool _isOpen = false;

        public bool IsOpen => _isOpen;
        public CanvasGroup CanvasGroup => _canvasGroup;
        public RectTransform RectTransform => _rectTransform;

        private void Awake()
        {
            InitIfNeeded();
        }

        private void Start()
        {
            if (playAnimOnStart)
            {
                // Bắt đầu từ trạng thái ẩn rồi lướt vào mượt mà để bạn xem thử animation!
                HideImmediate();
                Show();
            }
            else if (startVisible)
            {
                ShowImmediate();
            }
            else
            {
                HideImmediate();
            }
        }

        [ContextMenu("▶ TEST SHOW (Chạy animation Hiện)")]
        public void ContextTestShow()
        {
            Show();
        }

        [ContextMenu("◀ TEST HIDE (Chạy animation Ẩn)")]
        public void ContextTestHide()
        {
            Hide();
        }

        private void InitIfNeeded()
        {
            if (_isInitialized) return;

            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();

            _originalPosition = _rectTransform.anchoredPosition;
            _originalScale = _rectTransform.localScale;

            // Nếu danh sách staggeredElements chưa có, tự động lấy các RectTransform con trực tiếp
            if (staggeredElements == null || staggeredElements.Count == 0)
            {
                staggeredElements = new List<RectTransform>();
                for (int i = 0; i < transform.childCount; i++)
                {
                    RectTransform childRect = transform.GetChild(i) as RectTransform;
                    if (childRect != null)
                    {
                        staggeredElements.Add(childRect);
                    }
                }
            }

            // Lưu tọa độ ban đầu của từng nút con
            _originalElementPositions.Clear();
            foreach (RectTransform elem in staggeredElements)
            {
                if (elem != null)
                {
                    _originalElementPositions.Add(elem.anchoredPosition);
                }
                else
                {
                    _originalElementPositions.Add(Vector2.zero);
                }
            }

            // Khởi tạo đường cong mượt mà có nảy nhẹ nếu chưa có
            if (animCurve == null || animCurve.length == 0)
            {
                // Keyframe có độ overshoot nhẹ tạo cảm giác nảy nhịp điệu (beat) cực đẹp
                Keyframe k0 = new Keyframe(0f, 0f, 0f, 2f);
                Keyframe k1 = new Keyframe(0.7f, 1.05f, 0.5f, 0f);
                Keyframe k2 = new Keyframe(1f, 1f, 0f, 0f);
                animCurve = new AnimationCurve(k0, k1, k2);
            }

            _isInitialized = true;
        }

        #region Public Controls

        /// <summary>
        /// Mở panel kèm hiệu ứng hoạt họa mượt mà.
        /// </summary>
        public void Show(Action onComplete = null)
        {
            InitIfNeeded();

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            if (_currentAnimRoutine != null)
            {
                StopCoroutine(_currentAnimRoutine);
            }

            _isOpen = true;
            onShowStarted?.Invoke();

            if (transitionType == TransitionType.StaggeredSlide)
            {
                _currentAnimRoutine = StartCoroutine(AnimateStaggeredRoutine(true, onComplete));
            }
            else
            {
                _currentAnimRoutine = StartCoroutine(AnimatePanelRoutine(true, onComplete));
            }
        }

        /// <summary>
        /// Đóng panel kèm hiệu ứng hoạt họa mượt mà.
        /// </summary>
        public void Hide(Action onComplete = null)
        {
            InitIfNeeded();

            if (!gameObject.activeSelf && !_isOpen)
            {
                onComplete?.Invoke();
                return;
            }

            if (_currentAnimRoutine != null)
            {
                StopCoroutine(_currentAnimRoutine);
            }

            _isOpen = false;
            onHideStarted?.Invoke();

            Action finishAction = () =>
            {
                if (deactivateOnHide)
                {
                    gameObject.SetActive(false);
                }
                onComplete?.Invoke();
            };

            if (transitionType == TransitionType.StaggeredSlide)
            {
                _currentAnimRoutine = StartCoroutine(AnimateStaggeredRoutine(false, finishAction));
            }
            else
            {
                _currentAnimRoutine = StartCoroutine(AnimatePanelRoutine(false, finishAction));
            }
        }

        /// <summary>
        /// Đảo ngược trạng thái hiện/ẩn.
        /// </summary>
        public void Toggle()
        {
            if (_isOpen)
                Hide();
            else
                Show();
        }

        /// <summary>
        /// Hiển thị ngay lập tức (không chạy animation).
        /// </summary>
        public void ShowImmediate()
        {
            InitIfNeeded();
            if (_currentAnimRoutine != null) StopCoroutine(_currentAnimRoutine);

            gameObject.SetActive(true);
            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
            _rectTransform.anchoredPosition = _originalPosition;
            _rectTransform.localScale = _originalScale;

            // Đặt tất cả nút con về vị trí chuẩn
            if (transitionType == TransitionType.StaggeredSlide)
            {
                for (int i = 0; i < staggeredElements.Count; i++)
                {
                    if (staggeredElements[i] != null && i < _originalElementPositions.Count)
                    {
                        staggeredElements[i].anchoredPosition = _originalElementPositions[i];
                    }
                }
            }

            _isOpen = true;
        }

        /// <summary>
        /// Ẩn ngay lập tức (không chạy animation).
        /// </summary>
        public void HideImmediate()
        {
            InitIfNeeded();
            if (_currentAnimRoutine != null) StopCoroutine(_currentAnimRoutine);

            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            _rectTransform.anchoredPosition = _originalPosition;
            _rectTransform.localScale = _originalScale;

            // Đặt tất cả nút con ra ngoài màn hình
            if (transitionType == TransitionType.StaggeredSlide)
            {
                Vector2 offset = CalculateStaggeredOffset();
                for (int i = 0; i < staggeredElements.Count; i++)
                {
                    if (staggeredElements[i] != null && i < _originalElementPositions.Count)
                    {
                        staggeredElements[i].anchoredPosition = _originalElementPositions[i] + offset;
                    }
                }
            }

            _isOpen = false;

            if (deactivateOnHide)
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Chuyển mượt từ panel này sang panel khác.
        /// </summary>
        public void SwitchTo(UIPanelAnimator targetPanel, Action onComplete = null)
        {
            if (targetPanel == null)
            {
                Debug.LogWarning($"[UIPanelAnimator] TargetPanel để chuyển sang là NULL trên {gameObject.name}");
                return;
            }

            Hide();
            targetPanel.Show(onComplete);
        }

        #endregion

        #region Animation Coroutine (Chung)

        private IEnumerator AnimatePanelRoutine(bool isShowing, Action onComplete)
        {
            _canvasGroup.interactable = false;

            Vector2 startPos = _originalPosition;
            Vector2 targetPos = _originalPosition;

            Vector3 startScale = _originalScale;
            Vector3 targetScale = _originalScale;

            float startAlpha = _canvasGroup.alpha;
            float targetAlpha = isShowing ? 1f : 0f;

            switch (transitionType)
            {
                case TransitionType.FadeOnly:
                    break;

                case TransitionType.PopScale:
                    if (isShowing)
                    {
                        startScale = _originalScale * 0.75f;
                        targetScale = _originalScale;
                    }
                    else
                    {
                        startScale = _rectTransform.localScale;
                        targetScale = _originalScale * 0.75f;
                    }
                    break;

                case TransitionType.FadeAndScale:
                    if (isShowing)
                    {
                        startScale = _originalScale * 0.85f;
                        targetScale = _originalScale;
                    }
                    else
                    {
                        startScale = _rectTransform.localScale;
                        targetScale = _originalScale * 0.85f;
                    }
                    break;

                case TransitionType.SlideHorizontal:
                case TransitionType.SlideVertical:
                    Vector2 offset = CalculateSlideOffset();
                    if (isShowing)
                    {
                        startPos = _originalPosition + offset;
                        targetPos = _originalPosition;
                        _rectTransform.anchoredPosition = startPos;
                    }
                    else
                    {
                        startPos = _rectTransform.anchoredPosition;
                        targetPos = _originalPosition + offset;
                    }
                    break;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float curveValue = animCurve.Evaluate(t);

                _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, curveValue);

                if (transitionType == TransitionType.PopScale || transitionType == TransitionType.FadeAndScale)
                {
                    _rectTransform.localScale = Vector3.LerpUnclamped(startScale, targetScale, curveValue);
                }

                if (transitionType == TransitionType.SlideHorizontal || transitionType == TransitionType.SlideVertical)
                {
                    _rectTransform.anchoredPosition = Vector2.LerpUnclamped(startPos, targetPos, curveValue);
                }

                yield return null;
            }

            _canvasGroup.alpha = targetAlpha;
            _rectTransform.anchoredPosition = targetPos;
            _rectTransform.localScale = targetScale;

            _canvasGroup.interactable = isShowing;
            _canvasGroup.blocksRaycasts = isShowing;

            _currentAnimRoutine = null;

            if (isShowing)
                onShowFinished?.Invoke();
            else
                onHideFinished?.Invoke();

            onComplete?.Invoke();
        }

        #endregion

        #region Staggered Slide Coroutine (Trượt lần lượt từng nút từ trái sang phải)

        private IEnumerator AnimateStaggeredRoutine(bool isShowing, Action onComplete)
        {
            _canvasGroup.interactable = false;

            int count = staggeredElements.Count;
            if (count == 0)
            {
                _canvasGroup.alpha = isShowing ? 1f : 0f;
                _canvasGroup.interactable = isShowing;
                _canvasGroup.blocksRaycasts = isShowing;
                onComplete?.Invoke();
                yield break;
            }

            Vector2 offset = CalculateStaggeredOffset();

            // Thiết lập vị trí bắt đầu và đích cho từng nút con
            Vector2[] startPositions = new Vector2[count];
            Vector2[] targetPositions = new Vector2[count];

            for (int i = 0; i < count; i++)
            {
                Vector2 orig = (i < _originalElementPositions.Count) ? _originalElementPositions[i] : (staggeredElements[i] != null ? staggeredElements[i].anchoredPosition : Vector2.zero);

                if (isShowing)
                {
                    startPositions[i] = orig + offset;
                    targetPositions[i] = orig;
                    if (staggeredElements[i] != null)
                    {
                        staggeredElements[i].anchoredPosition = startPositions[i];
                    }
                }
                else
                {
                    startPositions[i] = (staggeredElements[i] != null) ? staggeredElements[i].anchoredPosition : orig;
                    targetPositions[i] = orig + offset;
                }
            }

            float startAlpha = _canvasGroup.alpha;
            float targetAlpha = isShowing ? 1f : 0f;

            // Tổng thời gian = thời gian chạy của 1 nút + độ trễ tích lũy
            float totalDuration = duration + (count - 1) * staggerDelay;
            float elapsed = 0f;

            while (elapsed < totalDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                // Fade alpha của cả panel mượt mà
                float alphaT = Mathf.Clamp01(elapsed / (duration * 0.8f));
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, alphaT);

                // Cập nhật từng nút con theo thời điểm trượt riêng của nó
                for (int i = 0; i < count; i++)
                {
                    if (staggeredElements[i] == null) continue;

                    float elementStartTime = i * staggerDelay;
                    float elementElapsed = elapsed - elementStartTime;

                    if (elementElapsed > 0f)
                    {
                        float t = Mathf.Clamp01(elementElapsed / duration);
                        float curveVal = animCurve.Evaluate(t);
                        staggeredElements[i].anchoredPosition = Vector2.LerpUnclamped(startPositions[i], targetPositions[i], curveVal);
                    }
                    else
                    {
                        // Chưa đến lượt thì giữ nguyên vị trí bắt đầu
                        staggeredElements[i].anchoredPosition = startPositions[i];
                    }
                }

                yield return null;
            }

            // Đảm bảo đưa chính xác về vị trí đích tuyệt đối
            for (int i = 0; i < count; i++)
            {
                if (staggeredElements[i] != null)
                {
                    staggeredElements[i].anchoredPosition = targetPositions[i];
                }
            }

            _canvasGroup.alpha = targetAlpha;
            _canvasGroup.interactable = isShowing;
            _canvasGroup.blocksRaycasts = isShowing;

            _currentAnimRoutine = null;

            if (isShowing)
                onShowFinished?.Invoke();
            else
                onHideFinished?.Invoke();

            onComplete?.Invoke();
        }

        private Vector2 CalculateStaggeredOffset()
        {
            switch (slideDirection)
            {
                case SlideDirection.FromLeft:
                    return new Vector2(-slideDistance, 0f);
                case SlideDirection.FromRight:
                    return new Vector2(slideDistance, 0f);
                case SlideDirection.FromTop:
                    return new Vector2(0f, slideDistance);
                case SlideDirection.FromBottom:
                    return new Vector2(0f, -slideDistance);
                default:
                    return new Vector2(-slideDistance, 0f);
            }
        }

        private Vector2 CalculateSlideOffset()
        {
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            if (_rectTransform != null)
            {
                float rectW = _rectTransform.rect.width > 0 ? _rectTransform.rect.width : screenWidth;
                float rectH = _rectTransform.rect.height > 0 ? _rectTransform.rect.height : screenHeight;
                screenWidth = Mathf.Max(screenWidth, rectW);
                screenHeight = Mathf.Max(screenHeight, rectH);
            }

            switch (slideDirection)
            {
                case SlideDirection.FromLeft:
                    return new Vector2(-screenWidth, 0f);
                case SlideDirection.FromRight:
                    return new Vector2(screenWidth, 0f);
                case SlideDirection.FromTop:
                    return new Vector2(0f, screenHeight);
                case SlideDirection.FromBottom:
                    return new Vector2(0f, -screenHeight);
                default:
                    return new Vector2(-screenWidth, 0f);
            }
        }

        #endregion
    }
}

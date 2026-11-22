using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Luan.NapTien
{
    public class UICardAnimation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("--- References ---")]
        [SerializeField] private RectTransform iconTransform;
        [SerializeField] private RectTransform auraTransform;
        [SerializeField] private Image hoverFrame;
        [SerializeField] private RectTransform shimmerStreak;

        [Header("--- Floating Motion ---")]
        [SerializeField] private float floatSpeed = 2.4f;
        [SerializeField] private float floatAmplitude = 7.0f;
        [SerializeField] private float phaseOffset = 0f;

        [Header("--- Breathing Aura ---")]
        [SerializeField] private float auraPulseSpeed = 2.0f;
        [SerializeField] private float auraPulseScale = 0.12f;

        [Header("--- Shimmer Sweep ---")]
        [SerializeField] private float shimmerInterval = 4.0f;
        [SerializeField] private float shimmerSpeed = 600f;

        private Vector2 _iconBasePos;
        private Vector3 _auraBaseScale;
        private Vector3 _cardBaseScale;
        private bool _isHovered = false;
        private bool _isPressed = false;
        private Coroutine _shimmerRoutine;
        private RectTransform _cardRt;

        private void Awake()
        {
            _cardRt = GetComponent<RectTransform>();
            _cardBaseScale = _cardRt != null ? _cardRt.localScale : Vector3.one;

            if (iconTransform != null) _iconBasePos = iconTransform.anchoredPosition;
            if (auraTransform != null) _auraBaseScale = auraTransform.localScale;

            if (hoverFrame != null)
            {
                hoverFrame.gameObject.SetActive(false);
            }

            phaseOffset = Random.Range(0f, Mathf.PI * 2f);
            shimmerInterval = Random.Range(3.5f, 6.0f);
        }

        private void OnEnable()
        {
            if (shimmerStreak != null)
            {
                _shimmerRoutine = StartCoroutine(ShimmerLoopRoutine());
            }
        }

        private void OnDisable()
        {
            if (_shimmerRoutine != null)
            {
                StopCoroutine(_shimmerRoutine);
                _shimmerRoutine = null;
            }
            if (_cardRt != null) _cardRt.localScale = _cardBaseScale;
            if (hoverFrame != null) hoverFrame.gameObject.SetActive(false);
            _isHovered = false;
            _isPressed = false;
        }

        private void Update()
        {
            float time = Time.unscaledTime;

            // 1. Icon Gentle Floating (Bobbing)
            if (iconTransform != null)
            {
                float dy = Mathf.Sin(time * floatSpeed + phaseOffset) * floatAmplitude;
                iconTransform.anchoredPosition = _iconBasePos + new Vector2(0f, dy);
            }

            // 2. Aura Breathing Glow
            if (auraTransform != null)
            {
                float s = 1.0f + Mathf.Sin(time * auraPulseSpeed + phaseOffset) * auraPulseScale;
                auraTransform.localScale = _auraBaseScale * s;
            }

            // 3. Smooth Card Scale towards target
            if (_cardRt != null)
            {
                Vector3 targetScale = _cardBaseScale;
                if (_isPressed)
                {
                    targetScale = _cardBaseScale * 0.96f;
                }
                else if (_isHovered)
                {
                    targetScale = _cardBaseScale * 1.045f;
                }
                _cardRt.localScale = Vector3.Lerp(_cardRt.localScale, targetScale, Time.unscaledDeltaTime * 14f);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            if (hoverFrame != null) hoverFrame.gameObject.SetActive(true);

            if (Luan.MainMenu.AudioManager.Instance != null)
            {
                Luan.MainMenu.AudioManager.Instance.PlaySelectSound();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            _isPressed = false;
            if (hoverFrame != null) hoverFrame.gameObject.SetActive(false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isPressed = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isPressed = false;
        }

        private IEnumerator ShimmerLoopRoutine()
        {
            yield return new WaitForSecondsRealtime(Random.Range(0.5f, 2.5f));

            while (true)
            {
                if (shimmerStreak != null && _cardRt != null)
                {
                    float cardWidth = _cardRt.rect.width;
                    float startX = -cardWidth * 0.9f;
                    float endX = cardWidth * 0.9f;
                    float x = startX;

                    shimmerStreak.gameObject.SetActive(true);
                    while (x < endX)
                    {
                        x += shimmerSpeed * Time.unscaledDeltaTime;
                        shimmerStreak.anchoredPosition = new Vector2(x, 0);
                        yield return null;
                    }
                    shimmerStreak.gameObject.SetActive(false);
                }

                yield return new WaitForSecondsRealtime(shimmerInterval);
            }
        }

        public void PlayCascadeAppear(float delay)
        {
            StartCoroutine(CascadeAppearRoutine(delay));
        }

        private IEnumerator CascadeAppearRoutine(float delay)
        {
            if (_cardRt == null) yield break;

            _cardRt.localScale = Vector3.zero;
            if (delay > 0) yield return new WaitForSecondsRealtime(delay);

            float elapsed = 0f;
            float duration = 0.28f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Ease out back
                float c1 = 1.6f;
                float c3 = c1 + 1f;
                float eased = 1f + c3 * Mathf.Pow(t - 1f, 3) + c1 * Mathf.Pow(t - 1f, 2);

                _cardRt.localScale = _cardBaseScale * eased;
                yield return null;
            }
            _cardRt.localScale = _cardBaseScale;
        }
    }
}

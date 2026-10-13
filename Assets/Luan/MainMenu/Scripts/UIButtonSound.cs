using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Luan.MainMenu
{
    public enum ButtonSoundType
    {
        Select,     // Âm thanh chọn thông thường (select_001.wav)
        Confirm,    // Âm thanh xác nhận / bắt đầu (confirmation_001.wav)
        Close,      // Âm thanh đóng / hủy (rollover2.wav)
        Custom      // Âm thanh tùy chỉnh
    }

    /// <summary>
    /// Component gắn vào bất kỳ Button nào trong Canvas để tự động phát âm thanh khi click hoặc hover.
    /// Tự động kết nối với AudioManager để âm lượng được điều chỉnh theo thanh SFX trong Cài Đặt.
    /// </summary>
    [AddComponentMenu("UI/Luan MainMenu/UI Button Sound")]
    [RequireComponent(typeof(Button))]
    public class UIButtonSound : MonoBehaviour, IPointerEnterHandler
    {
        [Header("--- Kiểu Âm Thanh Khi Click ---")]
        [Tooltip("Loại âm thanh phát ra khi bấm nút: Select (thường), Confirm (xác nhận/chơi), Close (đóng/quay lại)")]
        [SerializeField] private ButtonSoundType soundType = ButtonSoundType.Select;

        [Tooltip("Audio clip tùy chỉnh nếu chọn Custom")]
        [SerializeField] private AudioClip customClickClip;

        [Header("--- Âm Thanh Khi Rê Chuột (Hover - Tùy chọn) ---")]
        [Tooltip("Có phát âm thanh khi rê chuột vào nút không?")]
        [SerializeField] private bool playOnHover = false;

        [Tooltip("Âm thanh khi rê chuột (mặc định lấy rollover2 nếu để trống)")]
        [SerializeField] private AudioClip hoverClip;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (_button != null)
            {
                _button.onClick.AddListener(OnClick);
            }
        }

        public void SetSoundType(ButtonSoundType type)
        {
            soundType = type;
        }

        public void SetCustomClip(AudioClip clip)
        {
            customClickClip = clip;
            soundType = ButtonSoundType.Custom;
        }

        private void OnClick()
        {
            if (SFXManager.Instance != null && AudioManager.Instance != null)
            {
                switch (soundType)
                {
                    case ButtonSoundType.Select:
                        if (AudioManager.Instance.ButtonSelectClip != null)
                            SFXManager.Instance.PlaySFX(AudioManager.Instance.ButtonSelectClip);
                        else
                            AudioManager.Instance.PlaySelectSound();
                        break;
                    case ButtonSoundType.Confirm:
                        if (AudioManager.Instance.ButtonConfirmClip != null)
                            SFXManager.Instance.PlaySFX(AudioManager.Instance.ButtonConfirmClip);
                        else
                            AudioManager.Instance.PlayConfirmSound();
                        break;
                    case ButtonSoundType.Close:
                        if (AudioManager.Instance.ButtonCloseClip != null)
                            SFXManager.Instance.PlaySFX(AudioManager.Instance.ButtonCloseClip);
                        else
                            AudioManager.Instance.PlayCloseSound();
                        break;
                    case ButtonSoundType.Custom:
                        if (customClickClip != null)
                            SFXManager.Instance.PlaySFX(customClickClip);
                        break;
                }
                return;
            }

            if (AudioManager.Instance != null)
            {
                switch (soundType)
                {
                    case ButtonSoundType.Select:
                        AudioManager.Instance.PlaySelectSound();
                        break;
                    case ButtonSoundType.Confirm:
                        AudioManager.Instance.PlayConfirmSound();
                        break;
                    case ButtonSoundType.Close:
                        AudioManager.Instance.PlayCloseSound();
                        break;
                    case ButtonSoundType.Custom:
                        if (customClickClip != null)
                        {
                            AudioManager.Instance.PlaySFX(customClickClip);
                        }
                        break;
                }
            }
            else if (SFXManager.Instance != null && customClickClip != null)
            {
                SFXManager.Instance.PlaySFX(customClickClip);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!playOnHover || _button == null || !_button.interactable) return;

            if (hoverClip != null)
            {
                if (SFXManager.Instance != null)
                {
                    SFXManager.Instance.PlaySFX(hoverClip, 0.6f);
                }
                else if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX(hoverClip, 0.6f);
                }
            }
            else if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayCloseSound(0.5f);
            }
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Luan.MainMenu
{
    /// <summary>
    /// Script điều khiển giao diện Cài Đặt (Settings Panel).
    /// Kết nối trực tiếp 3 Slider (Âm lượng Tổng, Âm lượng Nhạc, Âm lượng SFX) với AudioManager.
    /// Hỗ trợ hiển thị phần trăm (ví dụ: 100%, 75%), nút Đóng mượt mà qua UIPanelAnimator, và nút Khôi phục mặc định.
    /// </summary>
    [AddComponentMenu("UI/Luan MainMenu/Settings Manager")]
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }

        [Header("--- Panel Hoạt Họa (UIPanelAnimator) ---")]
        [Tooltip("Kéo chính UIPanelAnimator của SettingsPanel vào đây (để mở/đóng mượt)")]
        [SerializeField] private UIPanelAnimator settingsPanelAnimator;

        [Header("--- 1. Slider Âm Lượng Tổng (Master Volume) ---")]
        [Tooltip("Slider điều khiển Âm lượng Tổng")]
        [SerializeField] private Slider masterSlider;
        [Tooltip("Image Fill của Master (nếu dùng Image Type = Filled để tránh co giãn méo hình)")]
        [SerializeField] private Image masterFillImage;
        [Tooltip("Text hiển thị phần trăm (TMP, ví dụ: 100%) - Tùy chọn")]
        [SerializeField] private TMP_Text masterPercentTMP;
        [SerializeField] private Text masterPercentLegacy;

        [Header("--- 2. Slider Âm Lượng Nhạc (Music / BGM) ---")]
        [Tooltip("Slider điều khiển Âm lượng Nhạc nền")]
        [SerializeField] private Slider musicSlider;
        [Tooltip("Image Fill của Music (nếu dùng Image Type = Filled để tránh co giãn méo hình)")]
        [SerializeField] private Image musicFillImage;
        [Tooltip("Text hiển thị phần trăm (TMP) - Tùy chọn")]
        [SerializeField] private TMP_Text musicPercentTMP;
        [SerializeField] private Text musicPercentLegacy;

        [Header("--- 3. Slider Âm Lượng Hiệu Ứng (SFX) ---")]
        [Tooltip("Slider điều khiển Âm lượng Hiệu ứng (SFX)")]
        [SerializeField] private Slider sfxSlider;
        [Tooltip("Image Fill của SFX (nếu dùng Image Type = Filled để tránh co giãn méo hình)")]
        [SerializeField] private Image sfxFillImage;
        [Tooltip("Text hiển thị phần trăm (TMP) - Tùy chọn")]
        [SerializeField] private TMP_Text sfxPercentTMP;
        [SerializeField] private Text sfxPercentLegacy;

        [Header("--- Các Nút Bấm Khác (Buttons) ---")]
        [Tooltip("Nút Đóng bảng Cài đặt quay về MainMenu")]
        [SerializeField] private Button closeButton;

        [Tooltip("Nút Khôi phục âm lượng về mặc định (Tùy chọn)")]
        [SerializeField] private Button resetDefaultButton;

        [Header("--- Âm Thanh Khi Test (Tùy chọn) ---")]
        [Tooltip("Âm thanh test khi kéo thanh SFX Slider")]
        [SerializeField] private AudioClip sfxTestClip;
        private float _lastSfxTestTime = 0f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            if (settingsPanelAnimator == null)
            {
                settingsPanelAnimator = GetComponent<UIPanelAnimator>();
            }

            // Gắn sự kiện cho các Slider
            if (masterSlider != null)
            {
                masterSlider.onValueChanged.AddListener(OnMasterSliderChanged);
            }

            if (musicSlider != null)
            {
                musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
            }

            if (sfxSlider != null)
            {
                sfxSlider.onValueChanged.AddListener(OnSFXSliderChanged);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseSettings);
            }

            if (resetDefaultButton != null)
            {
                resetDefaultButton.onClick.AddListener(OnResetDefaultClicked);
            }
        }

        private void Start()
        {
            SyncSlidersFromAudioManager();
        }

        private void OnEnable()
        {
            SyncSlidersFromAudioManager();
        }

        /// <summary>
        /// Đồng bộ giá trị từ AudioManager lên các Slider và Text hiển thị.
        /// </summary>
        public void SyncSlidersFromAudioManager()
        {
            if (AudioManager.Instance != null)
            {
                float master = AudioManager.Instance.MasterVolume;
                float music = AudioManager.Instance.MusicVolume;
                float sfx = AudioManager.Instance.SFXVolume;

                if (masterSlider != null)
                {
                    masterSlider.SetValueWithoutNotify(master);
                    if (masterFillImage != null) masterFillImage.fillAmount = master;
                    UpdatePercentText(masterPercentTMP, masterPercentLegacy, master);
                }

                if (musicSlider != null)
                {
                    musicSlider.SetValueWithoutNotify(music);
                    if (musicFillImage != null) musicFillImage.fillAmount = music;
                    UpdatePercentText(musicPercentTMP, musicPercentLegacy, music);
                }

                if (sfxSlider != null)
                {
                    sfxSlider.SetValueWithoutNotify(sfx);
                    if (sfxFillImage != null) sfxFillImage.fillAmount = sfx;
                    UpdatePercentText(sfxPercentTMP, sfxPercentLegacy, sfx);
                }
            }
            else
            {
                // Fallback nếu chưa có AudioManager trong scene (lấy từ PlayerPrefs)
                float master = PlayerPrefs.GetFloat("SETTINGS_MASTER_VOL", 1f);
                float music = PlayerPrefs.GetFloat("SETTINGS_MUSIC_VOL", 0.8f);
                float sfx = PlayerPrefs.GetFloat("SETTINGS_SFX_VOL", 1f);

                if (masterSlider != null)
                {
                    masterSlider.SetValueWithoutNotify(master);
                    if (masterFillImage != null) masterFillImage.fillAmount = master;
                    UpdatePercentText(masterPercentTMP, masterPercentLegacy, master);
                }

                if (musicSlider != null)
                {
                    musicSlider.SetValueWithoutNotify(music);
                    if (musicFillImage != null) musicFillImage.fillAmount = music;
                    UpdatePercentText(musicPercentTMP, musicPercentLegacy, music);
                }

                if (sfxSlider != null)
                {
                    sfxSlider.SetValueWithoutNotify(sfx);
                    if (sfxFillImage != null) sfxFillImage.fillAmount = sfx;
                    UpdatePercentText(sfxPercentTMP, sfxPercentLegacy, sfx);
                }
            }
        }

        #region Slider Callbacks

        private void OnMasterSliderChanged(float value)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMasterVolume(value);
            }
            else
            {
                AudioListener.volume = value;
                PlayerPrefs.SetFloat("SETTINGS_MASTER_VOL", value);
            }

            if (masterFillImage != null) masterFillImage.fillAmount = value;
            UpdatePercentText(masterPercentTMP, masterPercentLegacy, value);
        }

        private void OnMusicSliderChanged(float value)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMusicVolume(value);
            }
            else
            {
                PlayerPrefs.SetFloat("SETTINGS_MUSIC_VOL", value);
            }

            if (musicFillImage != null) musicFillImage.fillAmount = value;
            UpdatePercentText(musicPercentTMP, musicPercentLegacy, value);
        }

        private void OnSFXSliderChanged(float value)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetSFXVolume(value);

                // Giới hạn tần suất phát âm thanh test khi rê chuột kéo slider (0.15s / lần)
                if (sfxTestClip != null && Time.unscaledTime - _lastSfxTestTime > 0.15f)
                {
                    _lastSfxTestTime = Time.unscaledTime;
                    AudioManager.Instance.PlaySFX(sfxTestClip);
                }
            }
            else
            {
                PlayerPrefs.SetFloat("SETTINGS_SFX_VOL", value);
            }

            if (sfxFillImage != null) sfxFillImage.fillAmount = value;
            UpdatePercentText(sfxPercentTMP, sfxPercentLegacy, value);
        }

        private void UpdatePercentText(TMP_Text tmp, Text legacy, float value)
        {
            int percent = Mathf.RoundToInt(Mathf.Clamp01(value) * 100f);
            string text = $"{percent}%";

            if (tmp != null) tmp.text = text;
            if (legacy != null) legacy.text = text;
        }

        private void OnResetDefaultClicked()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.ResetToDefault();
            }
            SyncSlidersFromAudioManager();
        }

        #endregion

        #region Mở / Đóng Bảng Settings

        /// <summary>
        /// Mở bảng Cài đặt kèm animation mượt mà.
        /// </summary>
        public void OpenSettings()
        {
            SyncSlidersFromAudioManager();

            if (settingsPanelAnimator != null)
            {
                settingsPanelAnimator.Show();
            }
            else
            {
                gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Đóng bảng Cài đặt quay lại MainMenu (tự động lưu và hiện lại 3 nút MainMenu).
        /// </summary>
        public void CloseSettings()
        {
            PlayerPrefs.Save(); // Đảm bảo lưu dữ liệu tuyệt đối vào máy

            if (settingsPanelAnimator != null)
            {
                settingsPanelAnimator.Hide();
            }
            else
            {
                gameObject.SetActive(false);
            }

            // Hiện lại 3 nút ngoài Main Menu
            if (MainMenuManager.Instance != null)
            {
                MainMenuManager.Instance.BackToMainMenu();
            }
        }

        #endregion
    }
}

using System;
using UnityEngine;

namespace Luan.MainMenu
{
    /// <summary>
    /// Hệ thống quản lý Âm thanh tập trung toàn bộ Game (Master, Music/BGM, SFX).
    /// Tự động lưu và khôi phục cài đặt âm lượng qua PlayerPrefs.
    /// Điều khiển AudioListener.volume để Master Volume tác động lên toàn bộ game.
    /// </summary>
    [AddComponentMenu("UI/Luan MainMenu/Audio Manager")]
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private const string PrefsMasterVolKey = "SETTINGS_MASTER_VOL";
        private const string PrefsMusicVolKey = "SETTINGS_MUSIC_VOL";
        private const string PrefsSfxVolKey = "SETTINGS_SFX_VOL";

        [Header("--- Kênh Phát Âm Thanh (Audio Sources) ---")]
        [Tooltip("AudioSource phát nhạc nền BGM (tự động lặp Loop)")]
        [SerializeField] private AudioSource musicSource;

        [Tooltip("AudioSource phát hiệu ứng âm thanh SFX")]
        [SerializeField] private AudioSource sfxSource;

        [Header("--- Mức Âm Lượng Mặc Định (0.0 đến 1.0) ---")]
        [Range(0f, 1f)] [SerializeField] private float defaultMasterVolume = 1f;
        [Range(0f, 1f)] [SerializeField] private float defaultMusicVolume = 0.8f;
        [Range(0f, 1f)] [SerializeField] private float defaultSfxVolume = 1f;

        private float _masterVolume = 1f;
        private float _musicVolume = 1f;
        private float _sfxVolume = 1f;

        public float MasterVolume => _masterVolume;
        public float MusicVolume => _musicVolume;
        public float SFXVolume => _sfxVolume;

        // Sự kiện thông báo khi âm lượng thay đổi (để UI hoặc các hệ thống khác cập nhật)
        public static event Action<float, float, float> OnVolumeChanged;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            InitAudioSources();
            LoadVolumeSettings();
        }

        private void InitAudioSources()
        {
            // Tự động tạo AudioSource nếu chưa kéo trong Inspector
            if (musicSource == null)
            {
                GameObject musicObj = new GameObject("MusicSource_BGM");
                musicObj.transform.SetParent(transform);
                musicSource = musicObj.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
            }

            if (sfxSource == null)
            {
                GameObject sfxObj = new GameObject("SFXSource");
                sfxObj.transform.SetParent(transform);
                sfxSource = sfxObj.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
            }
        }

        private void LoadVolumeSettings()
        {
            _masterVolume = PlayerPrefs.GetFloat(PrefsMasterVolKey, defaultMasterVolume);
            _musicVolume = PlayerPrefs.GetFloat(PrefsMusicVolKey, defaultMusicVolume);
            _sfxVolume = PlayerPrefs.GetFloat(PrefsSfxVolKey, defaultSfxVolume);

            ApplyAllVolumes();
        }

        private void ApplyAllVolumes()
        {
            // 1. Master Volume tác động lên toàn bộ âm thanh trong Unity qua AudioListener
            AudioListener.volume = Mathf.Clamp01(_masterVolume);

            // 2. Music Volume
            if (musicSource != null)
            {
                musicSource.volume = Mathf.Clamp01(_musicVolume);
            }

            // 3. SFX Volume
            if (sfxSource != null)
            {
                sfxSource.volume = Mathf.Clamp01(_sfxVolume);
            }

            OnVolumeChanged?.Invoke(_masterVolume, _musicVolume, _sfxVolume);
        }

        #region Public Volume Setters (Dành cho Sliders)

        /// <summary>
        /// Chỉnh Âm lượng Tổng (Master: 0.0 - 1.0)
        /// </summary>
        public void SetMasterVolume(float value)
        {
            _masterVolume = Mathf.Clamp01(value);
            AudioListener.volume = _masterVolume;
            PlayerPrefs.SetFloat(PrefsMasterVolKey, _masterVolume);
            PlayerPrefs.Save();

            OnVolumeChanged?.Invoke(_masterVolume, _musicVolume, _sfxVolume);
        }

        /// <summary>
        /// Chỉnh Âm lượng Nhạc nền (Music / BGM: 0.0 - 1.0)
        /// </summary>
        public void SetMusicVolume(float value)
        {
            _musicVolume = Mathf.Clamp01(value);
            if (musicSource != null)
            {
                musicSource.volume = _musicVolume;
            }
            PlayerPrefs.SetFloat(PrefsMusicVolKey, _musicVolume);
            PlayerPrefs.Save();

            OnVolumeChanged?.Invoke(_masterVolume, _musicVolume, _sfxVolume);
        }

        /// <summary>
        /// Chỉnh Âm lượng Hiệu ứng (SFX: 0.0 - 1.0)
        /// </summary>
        public void SetSFXVolume(float value)
        {
            _sfxVolume = Mathf.Clamp01(value);
            if (sfxSource != null)
            {
                sfxSource.volume = _sfxVolume;
            }
            PlayerPrefs.SetFloat(PrefsSfxVolKey, _sfxVolume);
            PlayerPrefs.Save();

            OnVolumeChanged?.Invoke(_masterVolume, _musicVolume, _sfxVolume);
        }

        /// <summary>
        /// Khôi phục mức âm lượng về mặc định
        /// </summary>
        public void ResetToDefault()
        {
            SetMasterVolume(defaultMasterVolume);
            SetMusicVolume(defaultMusicVolume);
            SetSFXVolume(defaultSfxVolume);
        }

        #endregion

        #region Play Audio Helpers

        /// <summary>
        /// Phát nhạc nền BGM (lặp vô tận)
        /// </summary>
        public void PlayBGM(AudioClip clip)
        {
            if (musicSource == null || clip == null) return;

            if (musicSource.clip == clip && musicSource.isPlaying) return;

            musicSource.clip = clip;
            musicSource.volume = _musicVolume;
            musicSource.loop = true;
            musicSource.Play();
        }

        /// <summary>
        /// Dừng nhạc nền BGM
        /// </summary>
        public void StopBGM()
        {
            if (musicSource != null)
            {
                musicSource.Stop();
            }
        }

        /// <summary>
        /// Phát hiệu ứng âm thanh SFX (bấm nút, thông báo, tiếng game...)
        /// </summary>
        public void PlaySFX(AudioClip clip, float volumeScale = 1f)
        {
            if (sfxSource == null || clip == null) return;

            sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale * _sfxVolume));
        }

        #endregion
    }
}

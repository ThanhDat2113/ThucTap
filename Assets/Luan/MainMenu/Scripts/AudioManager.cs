using System;
using UnityEngine;

namespace Luan.MainMenu
{
    /// <summary>
    /// Hệ thống quản lý Âm thanh tập trung toàn bộ Game (Master, Music/BGM, SFX).
    /// Tự động lưu và khôi phục cài đặt âm lượng qua PlayerPrefs.
    /// Điều khiển AudioListener.volume để Master Volume tác động lên toàn bộ game.
    /// Tự động phát nhạc nền BGM (menu.wav) và cung cấp các hàm phát âm thanh nút bấm tiện lợi.
    /// </summary>
    [AddComponentMenu("UI/Luan MainMenu/Audio Manager")]
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager _instance;
        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<AudioManager>();
                    if (_instance == null)
                    {
                        GameObject audioObj = new GameObject("AudioManager");
                        _instance = audioObj.AddComponent<AudioManager>();
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        private const string PrefsMasterVolKey = "SETTINGS_MASTER_VOL";
        private const string PrefsMusicVolKey = "SETTINGS_MUSIC_VOL";
        private const string PrefsSfxVolKey = "SETTINGS_SFX_VOL";

        [Header("--- Kênh Phát Âm Thanh (Audio Sources) ---")]
        [Tooltip("AudioSource phát nhạc nền BGM (tự động lặp Loop)")]
        [SerializeField] private AudioSource musicSource;

        [Tooltip("AudioSource phát hiệu ứng âm thanh SFX")]
        [SerializeField] private AudioSource sfxSource;

        [Header("--- Audio Clips Mặc Định ---")]
        [Tooltip("Nhạc nền BGM chính (menu.wav) - tự động phát lặp khi vào game")]
        [SerializeField] private AudioClip bgmClip;

        [Tooltip("Âm thanh bấm nút chọn thường (select_001.wav)")]
        [SerializeField] private AudioClip buttonSelectClip;

        [Tooltip("Âm thanh xác nhận / Bắt đầu chơi (confirmation_001.wav)")]
        [SerializeField] private AudioClip buttonConfirmClip;

        [Tooltip("Âm thanh đóng / quay lại / rê chuột (rollover2.wav)")]
        [SerializeField] private AudioClip buttonCloseClip;

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

        public AudioClip BGMClip => bgmClip;
        public AudioClip ButtonSelectClip => buttonSelectClip;
        public AudioClip ButtonConfirmClip => buttonConfirmClip;
        public AudioClip ButtonCloseClip => buttonCloseClip;

        // Sự kiện thông báo khi âm lượng thay đổi (để UI hoặc các hệ thống khác cập nhật)
        public static event Action<float, float, float> OnVolumeChanged;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            InitDefaultClips();
            InitAudioSources();
            LoadVolumeSettings();
        }

        private void Start()
        {
            InitDefaultClips();

            // Tự động phát nhạc nền BGM nếu có clip cấu hình
            if (bgmClip != null)
            {
                PlayBGM(bgmClip);
            }
        }

        private void InitDefaultClips()
        {
            if (buttonSelectClip == null)
            {
                buttonSelectClip = Resources.Load<AudioClip>("Audio/select_001");
#if UNITY_EDITOR
                if (buttonSelectClip == null)
                    buttonSelectClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Thuan/Audio/SFX/select_001.wav");
#endif
            }

            if (buttonConfirmClip == null)
            {
                buttonConfirmClip = Resources.Load<AudioClip>("Audio/confirmation_001");
#if UNITY_EDITOR
                if (buttonConfirmClip == null)
                    buttonConfirmClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Thuan/Audio/SFX/confirmation_001.wav");
#endif
            }

            if (buttonCloseClip == null)
            {
                buttonCloseClip = Resources.Load<AudioClip>("Audio/rollover2");
#if UNITY_EDITOR
                if (buttonCloseClip == null)
                    buttonCloseClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Thuan/Audio/SFX/rollover2.wav");
#endif
            }

            if (bgmClip == null)
            {
                bgmClip = Resources.Load<AudioClip>("Audio/menu");
#if UNITY_EDITOR
                if (bgmClip == null)
                    bgmClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Thuan/Audio/BGM/menu.wav");
#endif
            }
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
            if (MusicManager.Instance != null)
            {
                MusicManager.Instance.SetVolume(_musicVolume);
            }
            else if (musicSource != null)
            {
                musicSource.volume = Mathf.Clamp01(_musicVolume);
            }

            // 3. SFX Volume
            if (SFXManager.Instance != null)
            {
                SFXManager.Instance.SetVolume(_sfxVolume);
            }
            else if (sfxSource != null)
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

            if (MusicManager.Instance != null)
            {
                MusicManager.Instance.SetVolume(_musicVolume);
            }
            else if (musicSource != null)
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

            if (SFXManager.Instance != null)
            {
                SFXManager.Instance.SetVolume(_sfxVolume);
            }
            else if (sfxSource != null)
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
        /// Phát nhạc nền BGM (lặp vô tận). Nếu truyền clip = null sẽ phát bgmClip mặc định.
        /// </summary>
        public void PlayBGM(AudioClip clip = null)
        {
            AudioClip target = clip != null ? clip : bgmClip;
            if (target == null) return;

            if (MusicManager.Instance != null)
            {
                MusicManager.Instance.PlayMusic(target, true);
                return;
            }

            InitAudioSources();

            if (musicSource.clip == target && musicSource.isPlaying) return;

            musicSource.clip = target;
            musicSource.volume = Mathf.Clamp01(_musicVolume);
            musicSource.loop = true;
            musicSource.Play();
        }

        /// <summary>
        /// Dừng nhạc nền BGM
        /// </summary>
        public void StopBGM()
        {
            if (MusicManager.Instance != null && MusicManager.Instance.GetComponent<AudioSource>() != null)
            {
                MusicManager.Instance.GetComponent<AudioSource>().Stop();
            }

            if (musicSource != null)
            {
                musicSource.Stop();
            }
        }

        /// <summary>
        /// Phát âm thanh chọn nút bấm thông thường (select_001.wav).
        /// </summary>
        public void PlaySelectSound(float volumeScale = 1f)
        {
            if (buttonSelectClip != null)
            {
                PlaySFX(buttonSelectClip, volumeScale);
            }
        }

        /// <summary>
        /// Phát âm thanh xác nhận / Bắt đầu game (confirmation_001.wav).
        /// </summary>
        public void PlayConfirmSound(float volumeScale = 1f)
        {
            if (buttonConfirmClip != null)
            {
                PlaySFX(buttonConfirmClip, volumeScale);
            }
        }

        /// <summary>
        /// Phát âm thanh đóng / quay lại / rê chuột (rollover2.wav).
        /// </summary>
        public void PlayCloseSound(float volumeScale = 1f)
        {
            if (buttonCloseClip != null)
            {
                PlaySFX(buttonCloseClip, volumeScale);
            }
        }

        /// <summary>
        /// Phát hiệu ứng âm thanh SFX bất kỳ.
        /// </summary>
        public void PlaySFX(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null) return;

            if (SFXManager.Instance != null)
            {
                SFXManager.Instance.PlaySFX(clip, volumeScale);
                return;
            }

            InitAudioSources();

            sfxSource.volume = Mathf.Clamp01(_sfxVolume);
            sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
        }

        #endregion
    }
}

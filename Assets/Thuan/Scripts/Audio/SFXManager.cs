using UnityEngine;

public class SFXManager : MonoBehaviour
{
    private static SFXManager _instance;
    public static SFXManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<SFXManager>();
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [SerializeField] private AudioSource sfxSource;

    private const string PrefsSfxVolKey = "SETTINGS_SFX_VOL";
    private float _volume = 1f;
    public float Volume => _volume;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            // Phá hủy cả object gốc AudioManagers nếu đã có instance tồn tại từ trước
            if (transform.root != null && transform.root.gameObject != gameObject)
            {
                Destroy(transform.root.gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
            return;
        }

        _instance = this;
        // Giữ cả object gốc (AudioManagers) qua các scene
        if (transform.parent == null)
        {
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            DontDestroyOnLoad(transform.root.gameObject);
        }

        if (sfxSource == null)
            sfxSource = GetComponent<AudioSource>();

        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();

        // Tải mức âm lượng đã lưu từ PlayerPrefs
        _volume = PlayerPrefs.GetFloat(PrefsSfxVolKey, 1f);
        if (sfxSource != null)
        {
            sfxSource.volume = _volume;
        }
    }

    /// <summary>
    /// Điều chỉnh âm lượng Hiệu ứng (SFX: 0.0 đến 1.0) và tự động lưu vào PlayerPrefs
    /// </summary>
    public void SetVolume(float volume)
    {
        _volume = Mathf.Clamp01(volume);
        if (sfxSource != null)
        {
            sfxSource.volume = _volume;
        }
        PlayerPrefs.SetFloat(PrefsSfxVolKey, _volume);
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;

        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        }

        sfxSource.volume = _volume;
        sfxSource.PlayOneShot(clip);
    }

    public void PlaySFX(AudioClip clip, float volumeScale)
    {
        if (clip == null) return;

        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        }

        sfxSource.volume = _volume;
        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }
}

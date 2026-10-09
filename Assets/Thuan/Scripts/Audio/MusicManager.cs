using UnityEngine;

public class MusicManager : MonoBehaviour
{
    private static MusicManager _instance;
    public static MusicManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<MusicManager>();
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [SerializeField] private AudioSource audioSource;

    private const string PrefsMusicVolKey = "SETTINGS_MUSIC_VOL";
    private const string SelectedSongKey = "SelectedSongName";

    private float _volume = 0.8f;
    public float Volume => _volume;

    public SongData CurrentSong { get; private set; }

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

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Tải mức âm lượng đã lưu từ PlayerPrefs
        _volume = PlayerPrefs.GetFloat(PrefsMusicVolKey, 0.8f);
        if (audioSource != null)
        {
            audioSource.volume = _volume;
        }
    }

    /// <summary>
    /// Điều chỉnh âm lượng Nhạc nền (0.0 đến 1.0) và tự động lưu vào PlayerPrefs
    /// </summary>
    public void SetVolume(float volume)
    {
        _volume = Mathf.Clamp01(volume);
        if (audioSource != null)
        {
            audioSource.volume = _volume;
        }
        PlayerPrefs.SetFloat(PrefsMusicVolKey, _volume);
    }

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null) return;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (audioSource.clip == clip && audioSource.isPlaying) return;

        audioSource.clip = clip;
        audioSource.loop = loop;
        audioSource.volume = _volume;
        audioSource.Play();
    }

    public void Play(SongData song)
    {
        if (song == null || song.clip == null) return;

        CurrentSong = song;
        PlayMusic(song.clip, true);
        PlayerPrefs.SetString(SelectedSongKey, song.name);
    }

    // Chon bai (luu lai) nhung KHONG phat nhac - dung o man Song List
    public void SelectSong(SongData song)
    {
        if (song == null) return;
        CurrentSong = song;
        PlayerPrefs.SetString(SelectedSongKey, song.name);
    }

    // Phat bai tu dau - dung khi bat dau man choi
    public void PlaySongFromStart(SongData song)
    {
        if (song == null || song.clip == null) return;
        SelectSong(song);

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.Stop();
        audioSource.clip = song.clip;
        audioSource.loop = false;
        audioSource.time = 0f;
        audioSource.volume = _volume;
        audioSource.Play();
    }

    public string GetSavedSongName()
    {
        return PlayerPrefs.GetString(SelectedSongKey, string.Empty);
    }
}

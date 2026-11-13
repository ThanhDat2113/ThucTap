using System.Collections;
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
        PlayerPrefs.Save();
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

        StopFade();
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

    // Bo chon bai (man chon nhac Free Play moi vao)
    public void ClearSelection()
    {
        CurrentSong = null;
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

        StopFade();
        audioSource.Stop();
        audioSource.clip = song.clip;
        audioSource.loop = false;
        audioSource.time = 0f;
        audioSource.volume = _volume;
        audioSource.Play();
    }

    // ===== Nghe thu bai hat (man chon nhac Free Play) =====
    private Coroutine fadeRoutine;

    private void StopFade()
    {
        if (fadeRoutine != null) { StopCoroutine(fadeRoutine); fadeRoutine = null; }
        if (audioSource != null) audioSource.volume = _volume;
    }

    // Phat thu 1 bai: giam dan bai dang phat -> chuyen sang bai moi tu doan nghe thu -> to dan len
    public void PlayPreview(SongData song, float fadeTime = 0.35f)
    {
        if (song == null || song.clip == null) return;
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        }
        if (audioSource.clip == song.clip && audioSource.isPlaying) return;
        float start = song.previewStart >= 0f ? song.previewStart : song.clip.length * 0.3f;
        CrossFade(song.clip, Mathf.Clamp(start, 0f, Mathf.Max(0f, song.clip.length - 1f)), true, fadeTime);
    }

    // Chuyen nhac co giam/tang am luong cho em tai
    public void CrossFade(AudioClip clip, float startTime, bool loop, float fadeTime = 0.35f)
    {
        if (clip == null) return;
        if (audioSource != null && audioSource.clip == clip && audioSource.isPlaying && fadeRoutine == null) return;
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(CrossFadeRoutine(clip, startTime, loop, fadeTime));
    }

    private IEnumerator CrossFadeRoutine(AudioClip clip, float startTime, bool loop, float fadeTime)
    {
        float from = audioSource.isPlaying ? audioSource.volume : 0f;
        for (float t = 0f; t < fadeTime && audioSource.isPlaying; t += Time.unscaledDeltaTime)
        {
            audioSource.volume = Mathf.Lerp(from, 0f, t / fadeTime);
            yield return null;
        }
        if (clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData();
        while (clip.loadState == AudioDataLoadState.Loading) yield return null;
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.loop = loop;
        audioSource.time = Mathf.Clamp(startTime, 0f, Mathf.Max(0f, clip.length - 0.1f));
        audioSource.volume = 0f;
        audioSource.Play();
        for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
        {
            audioSource.volume = Mathf.Lerp(0f, _volume, t / fadeTime);
            yield return null;
        }
        audioSource.volume = _volume;
        fadeRoutine = null;
    }

    public string GetSavedSongName()
    {
        return PlayerPrefs.GetString(SelectedSongKey, string.Empty);
    }

    /// <summary>
    /// Dừng nhạc nền hiện tại.
    /// </summary>
    public void StopMusic()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
}

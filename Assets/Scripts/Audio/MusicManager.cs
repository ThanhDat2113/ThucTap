using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [SerializeField] private AudioSource audioSource;

    private const string SelectedSongKey = "SelectedSongName";

    public SongData CurrentSong { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // Giu ca object goc (AudioManagers) qua cac scene - DontDestroyOnLoad chi co tac dung voi object goc
        DontDestroyOnLoad(transform.root.gameObject);

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null) return;
        if (audioSource.clip == clip && audioSource.isPlaying) return;

        audioSource.clip = clip;
        audioSource.loop = loop;
        audioSource.Play();
    }

    public void Play(SongData song)
    {
        if (song == null || song.clip == null) return;

        CurrentSong = song;
        PlayMusic(song.clip, true);
        PlayerPrefs.SetString(SelectedSongKey, song.name);
    }

    public string GetSavedSongName()
    {
        return PlayerPrefs.GetString(SelectedSongKey, string.Empty);
    }
}

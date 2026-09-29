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
        DontDestroyOnLoad(gameObject);

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public void Play(SongData song)
    {
        if (song == null || song.clip == null) return;

        CurrentSong = song;
        audioSource.clip = song.clip;
        audioSource.loop = true;
        audioSource.Play();

        PlayerPrefs.SetString(SelectedSongKey, song.name);
    }

    public string GetSavedSongName()
    {
        return PlayerPrefs.GetString(SelectedSongKey, string.Empty);
    }
}
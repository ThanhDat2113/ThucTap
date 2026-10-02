using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SongListController : MonoBehaviour
{
    [SerializeField] private Transform content;
    [SerializeField] private SongItemUI itemPrefab;
    [SerializeField] private List<SongData> songs;
    [SerializeField] private MusicManager musicManager;
    [SerializeField] private AudioClip ambientBgm;

    [Header("Start game")]
    [SerializeField] private Button playButton;
    [SerializeField] private string gameplaySceneName = "Gameplay";

    private readonly List<SongItemUI> spawnedItems = new List<SongItemUI>();

    private void Start()
    {
        // MusicManager duoc giu tu scene truoc (DontDestroyOnLoad) -> dung ban dang song
        if (MusicManager.Instance != null)
            musicManager = MusicManager.Instance;

        if (playButton != null)
        {
            playButton.interactable = false;
            playButton.onClick.AddListener(StartGame);
        }

        BuildList();
    }

    private void BuildList()
    {
        foreach (Transform child in content)
            Destroy(child.gameObject);
        spawnedItems.Clear();

        string savedName = musicManager.GetSavedSongName();
        bool foundSaved = false;

        foreach (SongData song in songs)
        {
            SongItemUI item = Instantiate(itemPrefab, content);
            item.Setup(song, this);
            spawnedItems.Add(item);

            if (!foundSaved && song.name == savedName)
            {
                SelectSong(song, item);
                foundSaved = true;
            }
        }

        if (!foundSaved)
            musicManager.PlayMusic(ambientBgm);
    }

    public void SelectSong(SongData song, SongItemUI selectedItem)
    {
        foreach (SongItemUI item in spawnedItems)
            item.SetSelected(item == selectedItem);

        musicManager.Play(song);

        if (playButton != null)
            playButton.interactable = musicManager.CurrentSong != null;
    }

    public void StartGame()
    {
        if (musicManager.CurrentSong == null) return;
        SceneManager.LoadScene(gameplaySceneName);
    }
}

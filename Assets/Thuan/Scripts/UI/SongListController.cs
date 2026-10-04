using System.Collections;
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

    [Header("Detail + start game")]
    [SerializeField] private SongDetailPanel detailPanel;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Button playButton;
    [SerializeField] private string gameplaySceneName = "Thuan_Gameplay";

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
        int selectedIndex = -1;

        for (int i = 0; i < songs.Count; i++)
        {
            SongData song = songs[i];
            SongItemUI item = Instantiate(itemPrefab, content);
            item.Setup(song, this, i);
            spawnedItems.Add(item);

            if (selectedIndex < 0 && song.name == savedName)
            {
                SelectSong(song, item);
                selectedIndex = i;
            }
        }

        // Man chon bai luon phat nhac nen; bai hat chi phat khi bam PLAY
        musicManager.PlayMusic(ambientBgm);

        if (selectedIndex < 0)
        {
            if (detailPanel != null) detailPanel.ShowEmpty();
        }
        else
        {
            StartCoroutine(ScrollToIndex(selectedIndex));
        }
    }

    public void SelectSong(SongData song, SongItemUI selectedItem)
    {
        foreach (SongItemUI item in spawnedItems)
            item.SetSelected(item == selectedItem);

        musicManager.SelectSong(song);

        if (detailPanel != null)
            detailPanel.Show(song);

        if (playButton != null)
            playButton.interactable = musicManager.CurrentSong != null;
    }

    public void StartGame()
    {
        SongData song = musicManager.CurrentSong;
        if (song == null) return;

        GameSession.SelectedSong = song;
        SongStats.IncrementPlayCount(song);
        SceneManager.LoadScene(gameplaySceneName);
    }

    // Cuon danh sach toi bai da chon lan truoc
    private IEnumerator ScrollToIndex(int index)
    {
        yield return null;
        if (scrollRect == null || spawnedItems.Count <= 1) yield break;
        Canvas.ForceUpdateCanvases();
        float contentH = scrollRect.content.rect.height;
        float viewH = scrollRect.viewport.rect.height;
        if (contentH <= viewH) yield break;
        var itemRt = (RectTransform)spawnedItems[index].transform;
        float itemCenter = -itemRt.anchoredPosition.y;
        float target = Mathf.Clamp(itemCenter - viewH * 0.5f, 0f, contentH - viewH);
        scrollRect.verticalNormalizedPosition = 1f - target / (contentH - viewH);
    }
}

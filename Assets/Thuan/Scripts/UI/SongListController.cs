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
    private bool built;
    private bool leavingScene;
    private bool freePlayVisible;

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
        built = true;
        freePlayVisible = IsFreePlayVisible();
    }

    private bool IsFreePlayVisible()
    {
        return detailPanel != null && detailPanel.gameObject.activeInHierarchy;
    }

    // Theo doi doi tab: moi lan vao Free Play deu bat dau chua chon bai nao + nhac nen
    private void Update()
    {
        if (!built || leavingScene) return;
        bool visible = IsFreePlayVisible();
        if (visible == freePlayVisible) return;
        freePlayVisible = visible;
        if (visible) ClearSelection();
        if (musicManager != null && ambientBgm != null) musicManager.CrossFade(ambientBgm, 0f, true);
    }

    // Bo chon tat ca: khung chi tiet trong, nut PLAY tat
    private void ClearSelection()
    {
        foreach (SongItemUI item in spawnedItems)
            item.SetSelected(false);
        if (musicManager != null) musicManager.ClearSelection();
        if (detailPanel != null) detailPanel.ShowEmpty();
        if (playButton != null) playButton.interactable = false;
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;
    }

    private void BuildList()
    {
        foreach (Transform child in content)
            Destroy(child.gameObject);
        spawnedItems.Clear();

        for (int i = 0; i < songs.Count; i++)
        {
            SongData song = songs[i];
            SongItemUI item = Instantiate(itemPrefab, content);
            item.Setup(song, this, i);
            spawnedItems.Add(item);
        }

        // Vao man chua chon bai nao: phat nhac nen, nguoi choi bam bai nao thi moi nghe thu bai do
        ClearSelection();
        if (musicManager != null && ambientBgm != null)
            musicManager.PlayMusic(ambientBgm);
    }

    public void SelectSong(SongData song, SongItemUI selectedItem)
    {
        foreach (SongItemUI item in spawnedItems)
            item.SetSelected(item == selectedItem);

        if (musicManager != null)
        {
            musicManager.SelectSong(song);
            // bam vao bai nao thi nhac nen chuyen sang nghe thu bai do
            musicManager.PlayPreview(song);
        }

        if (detailPanel != null)
            detailPanel.Show(song);

        if (playButton != null)
            playButton.interactable = musicManager != null && musicManager.CurrentSong != null;
    }

    public void StartGame()
    {
        SongData song = musicManager != null ? musicManager.CurrentSong : null;
        if (song == null) return;

        // Tắt nhạc nền của scene Song List trước khi chuyển sang Gameplay
        if (MusicManager.Instance != null)
            MusicManager.Instance.StopMusic();

        StorySession.End();
        leavingScene = true;
        GameSession.SelectedSong = song;
        SongStats.IncrementPlayCount(song);
        SceneManager.LoadScene(gameplaySceneName);
    }
}

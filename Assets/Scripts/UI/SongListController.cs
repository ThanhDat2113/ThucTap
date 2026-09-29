using System.Collections.Generic;
using UnityEngine;

public class SongListController : MonoBehaviour
{
    [SerializeField] private Transform content;
    [SerializeField] private SongItemUI itemPrefab;
    [SerializeField] private List<SongData> songs;
    [SerializeField] private MusicManager musicManager;

    private readonly List<SongItemUI> spawnedItems = new List<SongItemUI>();

    private void Start()
    {
        BuildList();
    }

    private void BuildList()
    {
        foreach (Transform child in content)
            Destroy(child.gameObject);
        spawnedItems.Clear();

        string savedName = musicManager.GetSavedSongName();

        foreach (SongData song in songs)
        {
            SongItemUI item = Instantiate(itemPrefab, content);
            item.Setup(song, this);
            spawnedItems.Add(item);

            if (song.name == savedName)
                SelectSong(song, item);
        }
    }

    public void SelectSong(SongData song, SongItemUI selectedItem)
    {
        foreach (SongItemUI item in spawnedItems)
            item.SetSelected(item == selectedItem);

        musicManager.Play(song);
    }
}
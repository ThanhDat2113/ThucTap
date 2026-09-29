using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SongItemUI : MonoBehaviour
{
    [SerializeField] private TMP_Text songNameText;
    [SerializeField] private TMP_Text difficultyText;
    [SerializeField] private Image coverImage;
    [SerializeField] private Image cardBackground;
    [SerializeField] private GameObject selectedMark;
    [SerializeField] private Button button;

    [Header("Selected look")]
    [SerializeField] private Color normalColor = new Color(0.12f, 0.12f, 0.18f);
    [SerializeField] private Color selectedColor = new Color(0.9f, 0.22f, 0.18f);
    [SerializeField] private float selectedScale = 1.06f;

    private SongData song;
    private SongListController controller;

    public void Setup(SongData songData, SongListController listController)
    {
        song = songData;
        controller = listController;

        songNameText.text = song.songName;

        if (difficultyText != null)
            difficultyText.text = $"Độ khó: {song.difficulty}/5";

        if (coverImage != null && song.coverArt != null)
            coverImage.sprite = song.coverArt;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);

        SetSelected(false);
    }

    private void OnClick()
    {
        controller.SelectSong(song, this);
    }

    public void SetSelected(bool isSelected)
    {
        if (selectedMark != null)
            selectedMark.SetActive(isSelected);

        if (cardBackground != null)
            cardBackground.color = isSelected ? selectedColor : normalColor;

        transform.localScale = Vector3.one * (isSelected ? selectedScale : 1f);
    }
}
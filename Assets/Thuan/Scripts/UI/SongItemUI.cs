using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SongItemUI : MonoBehaviour
{
    [SerializeField] private TMP_Text numberText;
    [SerializeField] private TMP_Text songNameText;
    [SerializeField] private TMP_Text artistText;
    [SerializeField] private Image coverImage;
    [SerializeField] private Image rowBackground;
    [SerializeField] private GameObject selectedMark;
    [SerializeField] private Image[] diamonds = new Image[3];
    [SerializeField] private Button button;
    [SerializeField] private AudioClip selectClip;

    [Header("Look")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite selectedSprite;
    [SerializeField] private Color numberNormalColor = new Color(0.25f, 0.85f, 1f);
    [SerializeField] private Color numberSelectedColor = Color.white;
    [SerializeField] private Color diamondOnColor = new Color(0.2f, 0.92f, 1f);
    [SerializeField] private Color diamondHardColor = new Color(1f, 0.18f, 0.55f);
    [SerializeField] private Color diamondOffColor = new Color(0.36f, 0.36f, 0.42f);

    private SongData song;
    private SongListController controller;

    public SongData Song { get { return song; } }

    public void Setup(SongData songData, SongListController listController, int index)
    {
        song = songData;
        controller = listController;

        if (numberText != null) numberText.text = (index + 1).ToString("00");
        songNameText.text = song.songName;
        if (artistText != null) artistText.text = string.IsNullOrEmpty(song.artist) ? "Unknown artist" : song.artist;

        if (coverImage != null && song.coverArt != null)
            coverImage.sprite = song.coverArt;

        SetDiamonds(DiamondCount(song.difficulty));

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);

        SetSelected(false);
    }

    // Do kho 1-5 cua bai -> 1-3 kim cuong
    public static int DiamondCount(int difficulty)
    {
        if (difficulty <= 2) return 1;
        if (difficulty == 3) return 2;
        return 3;
    }

    private void SetDiamonds(int count)
    {
        for (int i = 0; i < diamonds.Length; i++)
        {
            if (diamonds[i] == null) continue;
            bool on = i < count;
            diamonds[i].color = !on ? diamondOffColor : (count == 3 && i == 2 ? diamondHardColor : diamondOnColor);
        }
    }

    private void OnClick()
    {
        if (SFXManager.Instance != null) SFXManager.Instance.PlaySFX(selectClip);
        controller.SelectSong(song, this);
    }

    public void SetSelected(bool isSelected)
    {
        if (selectedMark != null) selectedMark.SetActive(isSelected);
        if (rowBackground != null && normalSprite != null && selectedSprite != null)
            rowBackground.sprite = isSelected ? selectedSprite : normalSprite;
        if (numberText != null) numberText.color = isSelected ? numberSelectedColor : numberNormalColor;
    }
}

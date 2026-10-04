using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Khung thong tin bai dang chon (ben phai man Song List)
public class SongDetailPanel : MonoBehaviour
{
    [SerializeField] private Image coverImage;
    [SerializeField] private TMP_Text songNameText;
    [SerializeField] private TMP_Text artistText;
    [SerializeField] private TMP_Text bpmText;
    [SerializeField] private TMP_Text durationText;

    [Header("Difficulty: 0 Easy, 1 Normal, 2 Hard")]
    [SerializeField] private Button[] difficultyButtons = new Button[3];
    [SerializeField] private Sprite[] difficultyOnSprites = new Sprite[3];
    [SerializeField] private Sprite[] difficultyOffSprites = new Sprite[3];
    [SerializeField] private TMP_Text[] difficultyLabels = new TMP_Text[3];
    [SerializeField] private AudioClip difficultyClip;

    [Header("Stats")]
    [SerializeField] private TMP_Text bestScoreText;
    [SerializeField] private TMP_Text bestComboText;
    [SerializeField] private TMP_Text accuracyText;
    [SerializeField] private TMP_Text playCountText;

    [Header("Shown when nothing is selected")]
    [SerializeField] private GameObject contentRoot;
    [SerializeField] private GameObject emptyHint;

    private void Awake()
    {
        for (int i = 0; i < difficultyButtons.Length; i++)
        {
            if (difficultyButtons[i] == null) continue;
            int index = i;
            difficultyButtons[i].onClick.AddListener(() => SelectDifficulty(index));
        }
        RefreshDifficulty();
    }

    public void ShowEmpty()
    {
        if (contentRoot != null) contentRoot.SetActive(false);
        if (emptyHint != null) emptyHint.SetActive(true);
    }

    public void Show(SongData song)
    {
        if (song == null) { ShowEmpty(); return; }
        if (contentRoot != null) contentRoot.SetActive(true);
        if (emptyHint != null) emptyHint.SetActive(false);

        if (coverImage != null) { coverImage.sprite = song.coverArt; coverImage.enabled = song.coverArt != null; }
        songNameText.text = song.songName;
        if (artistText != null) artistText.text = string.IsNullOrEmpty(song.artist) ? "Unknown artist" : song.artist;
        if (bpmText != null) bpmText.text = song.bpm > 0 ? song.bpm.ToString() : "--";
        if (durationText != null) durationText.text = song.clip != null ? FormatTime(song.clip.length) : "--";

        int best = SongStats.GetBestScore(song);
        float acc = SongStats.GetBestAccuracy(song);
        if (bestScoreText != null) bestScoreText.text = best > 0 ? best.ToString() : "--";
        if (bestComboText != null) bestComboText.text = best > 0 ? SongStats.GetBestCombo(song) + " COMBO" : "";
        if (accuracyText != null) accuracyText.text = acc >= 0f ? acc.ToString("0.0") + "%" : "--";
        if (playCountText != null) playCountText.text = SongStats.GetPlayCount(song).ToString();

        RefreshDifficulty();
    }

    private void SelectDifficulty(int index)
    {
        GameSession.SelectedDifficulty = (ChartDifficulty)index;
        if (SFXManager.Instance != null) SFXManager.Instance.PlaySFX(difficultyClip);
        RefreshDifficulty();
    }

    private void RefreshDifficulty()
    {
        int selected = (int)GameSession.SelectedDifficulty;
        for (int i = 0; i < difficultyButtons.Length; i++)
        {
            if (difficultyButtons[i] == null) continue;
            var img = difficultyButtons[i].targetGraphic as Image;
            if (img != null) img.sprite = i == selected ? difficultyOnSprites[i] : difficultyOffSprites[i];
            if (difficultyLabels[i] != null) difficultyLabels[i].alpha = i == selected ? 1f : 0.55f;
        }
    }

    private static string FormatTime(float seconds)
    {
        int s = Mathf.RoundToInt(seconds);
        return (s / 60) + ":" + (s % 60).ToString("00");
    }
}

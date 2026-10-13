using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Man choi TAM: hien bai da chon. Thay bang gameplay that (not nhac, cham diem) sau.
public class GameplayScreen : MonoBehaviour
{
    [SerializeField] private TMP_Text songNameText;
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private Image coverImage;

    private void Start()
    {
        SongData song = GameSession.SelectedSong;
        if (song == null && MusicManager.Instance != null)
            song = MusicManager.Instance.CurrentSong;

        // Bam PLAY o man chon bai -> bai hat bat dau phat tu dau tai day
        if (song != null && MusicManager.Instance != null)
            MusicManager.Instance.PlaySongFromStart(song);

        if (song == null)
        {
            songNameText.text = "Chưa chọn bài";
            if (coverImage != null) coverImage.enabled = false;
            return;
        }

        songNameText.text = song.songName;

        if (infoText != null)
            infoText.text = "Độ khó " + song.difficulty + "/5";

        if (coverImage != null && song.coverArt != null)
            coverImage.sprite = song.coverArt;
    }
}

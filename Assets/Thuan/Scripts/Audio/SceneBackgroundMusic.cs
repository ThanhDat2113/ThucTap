using UnityEngine;

public class SceneBackgroundMusic : MonoBehaviour
{
    [SerializeField] private AudioClip bgmClip;

    private void Start()
    {
        if (MusicManager.Instance != null && bgmClip != null)
        {
            MusicManager.Instance.PlayMusic(bgmClip);
        }
    }
}

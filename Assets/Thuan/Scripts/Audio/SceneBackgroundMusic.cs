using UnityEngine;

public class SceneBackgroundMusic : MonoBehaviour
{
    [SerializeField] private AudioClip bgmClip;

    private void Start()
    {
        MusicManager.Instance.PlayMusic(bgmClip);
    }
}

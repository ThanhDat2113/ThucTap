using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class ButtonSFX : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private AudioClip hoverClip;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(PlayClick);
    }

    private void PlayClick()
    {
        if (clickClip == null && Luan.MainMenu.AudioManager.Instance != null)
            clickClip = Luan.MainMenu.AudioManager.Instance.ButtonSelectClip;

        if (clickClip == null) return;
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySFX(clickClip);
        else if (Luan.MainMenu.AudioManager.Instance != null)
            Luan.MainMenu.AudioManager.Instance.PlaySFX(clickClip);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverClip == null) return;
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySFX(hoverClip);
        else if (Luan.MainMenu.AudioManager.Instance != null)
            Luan.MainMenu.AudioManager.Instance.PlaySFX(hoverClip);
    }
}

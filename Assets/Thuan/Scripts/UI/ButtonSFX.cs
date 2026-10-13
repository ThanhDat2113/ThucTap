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
        SFXManager.Instance.PlaySFX(clickClip);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SFXManager.Instance.PlaySFX(hoverClip);
    }
}

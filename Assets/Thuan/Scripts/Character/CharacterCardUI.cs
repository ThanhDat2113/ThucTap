using System;
using UnityEngine;
using UnityEngine.UI;

// 1 o trong luoi chon nhan vat
public class CharacterCardUI : MonoBehaviour
{
    [SerializeField] private Image art;
    [SerializeField] private GameObject selectedOverlay;
    [SerializeField] private Button button;

    public CharacterData Data { get; private set; }
    public bool IsLocked { get { return Data == null || !Data.unlocked; } }

    private RectTransform rt;
    private bool focused;
    private float scaleVel;

    public void Setup(CharacterData data, Sprite lockedSprite, Action<CharacterCardUI> onClick)
    {
        rt = (RectTransform)transform;
        Data = data;
        art.sprite = data != null && data.unlocked ? data.cardSprite : lockedSprite;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick(this));
        SetSelected(false);
        SetFocused(false);
    }

    public void SetSelected(bool on)
    {
        if (selectedOverlay != null) selectedOverlay.SetActive(on);
    }

    public void SetFocused(bool on)
    {
        focused = on;
        art.color = on || IsLocked ? Color.white : new Color(0.78f, 0.78f, 0.85f, 1f);
        if (on) transform.SetAsLastSibling();
    }

    private void Update()
    {
        float target = focused ? 1.07f + 0.015f * Mathf.Sin(Time.unscaledTime * 4f) : 1f;
        float s = Mathf.SmoothDamp(rt.localScale.x, target, ref scaleVel, 0.08f, Mathf.Infinity, Time.unscaledDeltaTime);
        rt.localScale = new Vector3(s, s, 1f);
    }
}

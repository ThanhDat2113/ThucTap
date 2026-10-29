using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 1 o vat pham trong luoi tui do
public class InventoryItemCardUI : MonoBehaviour
{
    [SerializeField] private Image frame;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image rarityBadge;
    [SerializeField] private TMP_Text rarityText;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private GameObject equippedMark;
    [SerializeField] private Button button;

    public InventoryItem Item { get; private set; }

    private Sprite normalFrame, selectedFrame;
    private RectTransform rt;
    private float pulse;
    private bool selected;

    public void Setup(InventoryItem item, Sprite normal, Sprite selectedSprite, Sprite badge, Action<InventoryItemCardUI> onClick)
    {
        rt = (RectTransform)transform;
        Item = item;
        normalFrame = normal;
        selectedFrame = selectedSprite;
        icon.sprite = item.icon;
        icon.preserveAspect = true;
        nameText.text = item.displayName;
        countText.text = "x" + item.count;
        rarityBadge.sprite = badge;
        rarityBadge.color = InventoryItem.RarityColor(item.rarity);
        rarityText.text = item.rarity.ToString().ToUpperInvariant();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick(this));
        SetSelected(false);
    }

    public void SetSelected(bool on)
    {
        selected = on;
        frame.sprite = on ? selectedFrame : normalFrame;
        if (on) pulse = 1f;
    }

    public void SetEquipped(bool on)
    {
        if (equippedMark != null) equippedMark.SetActive(on);
    }

    private void Update()
    {
        if (rt == null) return;
        pulse = Mathf.MoveTowards(pulse, 0f, Time.unscaledDeltaTime * 5f);
        float s = 1f + 0.06f * pulse * pulse + (selected ? 0.012f * Mathf.Sin(Time.unscaledTime * 4f) : 0f);
        rt.localScale = new Vector3(s, s, 1f);
    }
}

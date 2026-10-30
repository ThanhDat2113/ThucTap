using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// Man TUI DO: danh muc ben trai, luoi vat pham o giua, chi tiet + nut trang bi ben phai.
public class InventoryScreen : MonoBehaviour
{
    [Header("Vat pham trong tui (hien tai 1 mon de test)")]
    [SerializeField] private List<InventoryItem> items = new List<InventoryItem>();

    [Header("Danh muc (theo thu tu InventoryCategory)")]
    [SerializeField] private Button[] categoryButtons = new Button[6];
    [SerializeField] private Sprite[] categoryNormal = new Sprite[6];
    [SerializeField] private Sprite[] categorySelected = new Sprite[6];
    [SerializeField] private GameObject[] categoryArrows = new GameObject[6];

    [Header("Luoi")]
    [SerializeField] private InventoryItemCardUI cardTemplate;
    [SerializeField] private RectTransform[] slots = new RectTransform[12];
    [SerializeField] private GameObject emptyHint;
    [SerializeField] private Sprite cardNormal;
    [SerializeField] private Sprite cardSelected;
    [SerializeField] private Sprite rarityBadge;

    [Header("Chi tiet")]
    [SerializeField] private GameObject detailRoot;
    [SerializeField] private TMP_Text detailName;
    [SerializeField] private Image detailArt;
    [SerializeField] private Image detailRarity;
    [SerializeField] private TMP_Text detailRarityText;
    [SerializeField] private TMP_Text detailDescription;
    [SerializeField] private TMP_Text detailCount;
    [SerializeField] private Button equipButton;
    [SerializeField] private Image equipImage;
    [SerializeField] private Sprite equippedSprite;
    [SerializeField] private Sprite useSprite;
    [SerializeField] private GameObject detailEmpty;

    [Header("Vi tien")]
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private TMP_Text gemText;

    [Header("Khac")]
    [SerializeField] private Button backButton;
    [SerializeField] private string backScene = "Thuan_MainMenu";
    [SerializeField] private AudioClip selectClip;
    [SerializeField] private AudioClip equipClip;

    private readonly List<InventoryItemCardUI> cards = new List<InventoryItemCardUI>();
    private InventoryItemCardUI selected;
    private Vector2 artBasePos;

    private void Awake()
    {
        for (int i = 0; i < categoryButtons.Length; i++)
        {
            if (categoryButtons[i] == null) continue;
            var c = (InventoryCategory)i;
            categoryButtons[i].onClick.AddListener(() => { Play(selectClip); ShowCategory(c); });
        }
        if (equipButton != null) equipButton.onClick.AddListener(ToggleEquip);
        if (backButton != null) backButton.onClick.AddListener(() => SceneManager.LoadScene(backScene));
        if (detailArt != null) artBasePos = detailArt.rectTransform.anchoredPosition;
        if (cardTemplate != null) cardTemplate.gameObject.SetActive(false);
    }

    private void OnEnable() { Wallet.Changed += RefreshWallet; }
    private void OnDisable() { Wallet.Changed -= RefreshWallet; }

    private void Start()
    {
        RefreshWallet();
        ShowCategory(InventoryCategory.All);
    }

    public void ShowCategory(InventoryCategory c)
    {
        for (int i = 0; i < categoryButtons.Length; i++)
        {
            if (categoryButtons[i] == null) continue;
            ((Image)categoryButtons[i].targetGraphic).sprite = i == (int)c ? categorySelected[i] : categoryNormal[i];
            if (categoryArrows != null && i < categoryArrows.Length && categoryArrows[i] != null)
                categoryArrows[i].SetActive(i == (int)c);
        }

        foreach (var card in cards) Destroy(card.gameObject);
        cards.Clear();
        selected = null;

        int slot = 0;
        foreach (var item in items)
        {
            if (item == null || slot >= slots.Length) continue;
            if (c != InventoryCategory.All && item.category != c) continue;
            var card = Instantiate(cardTemplate, slots[slot].parent);
            card.gameObject.SetActive(true);
            card.name = "Item_" + item.id;
            ((RectTransform)card.transform).anchoredPosition = slots[slot].anchoredPosition;
            card.Setup(item, cardNormal, cardSelected, rarityBadge, OnCardClicked);
            card.SetEquipped(item.equippable && item.IsEquipped);
            cards.Add(card);
            slot++;
        }

        if (emptyHint != null) emptyHint.SetActive(cards.Count == 0);
        if (cards.Count > 0) Select(cards[0], false);
        else ShowDetail(null, false);
    }

    private void OnCardClicked(InventoryItemCardUI card)
    {
        if (card == selected) return;
        Play(selectClip);
        Select(card, true);
    }

    private void Select(InventoryItemCardUI card, bool animate)
    {
        if (selected != null) selected.SetSelected(false);
        selected = card;
        selected.SetSelected(true);
        ShowDetail(card.Item, animate);
    }

    private void ShowDetail(InventoryItem item, bool animate)
    {
        if (detailRoot != null) detailRoot.SetActive(item != null);
        if (detailEmpty != null) detailEmpty.SetActive(item == null);
        if (item == null) return;

        detailName.text = item.displayName.ToUpperInvariant();
        detailArt.sprite = item.art != null ? item.art : item.icon;
        detailArt.preserveAspect = item.art == null;
        detailRarity.color = InventoryItem.RarityColor(item.rarity);
        detailRarityText.text = item.rarity.ToString().ToUpperInvariant();
        detailDescription.text = item.description;
        if (detailCount != null) detailCount.text = "OWNED  x" + item.count;
        RefreshEquip();
        if (animate) { StopAllCoroutines(); StartCoroutine(SlideArt()); }
    }

    private void RefreshEquip()
    {
        if (selected == null || equipButton == null) return;
        var item = selected.Item;
        equipButton.gameObject.SetActive(item.equippable);
        if (equipImage != null) equipImage.sprite = item.IsEquipped ? equippedSprite : useSprite;
        foreach (var c in cards) c.SetEquipped(c.Item.equippable && c.Item.IsEquipped);
    }

    // Bam USE -> trang bi; bam EQUIPPED -> thao ra
    private void ToggleEquip()
    {
        if (selected == null) return;
        var item = selected.Item;
        item.SetEquipped(!item.IsEquipped);
        Play(equipClip);
        RefreshEquip();
        StartCoroutine(Punch((RectTransform)equipButton.transform));
    }

    private void RefreshWallet()
    {
        if (coinText != null) coinText.text = Wallet.Coins.ToString("N0");
        if (gemText != null) gemText.text = Wallet.Gems.ToString("N0");
    }

    private IEnumerator SlideArt()
    {
        var rt = detailArt.rectTransform;
        float t = 0f;
        while (t < 0.22f)
        {
            t += Time.unscaledDeltaTime;
            float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / 0.22f), 3f);
            rt.anchoredPosition = artBasePos + new Vector2(50f * (1f - e), 0f);
            detailArt.color = new Color(1f, 1f, 1f, e);
            yield return null;
        }
        rt.anchoredPosition = artBasePos;
        detailArt.color = Color.white;
    }

    private IEnumerator Punch(RectTransform rt)
    {
        float t = 0f;
        while (t < 0.25f)
        {
            t += Time.unscaledDeltaTime;
            float s = 1f + 0.12f * Mathf.Sin(Mathf.Clamp01(t / 0.25f) * Mathf.PI);
            rt.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    private static void Play(AudioClip clip)
    {
        if (clip != null && SFXManager.Instance != null) SFXManager.Instance.PlaySFX(clip);
    }
}

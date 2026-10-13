using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopController : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private List<ShopItemData> items = new List<ShopItemData>();
    [SerializeField] private ShopItemCard cardPrefab;
    [SerializeField] private Transform grid;
    [SerializeField] private GameObject emptyHint;

    [Header("Categories (theo thu tu enum ShopCategory)")]
    [SerializeField] private Button[] categoryButtons = new Button[6];
    [SerializeField] private Sprite[] categoryNormal = new Sprite[6];
    [SerializeField] private Sprite[] categorySelected = new Sprite[6];
    [SerializeField] private GameObject[] categoryArrows = new GameObject[6];

    [Header("Wallet")]
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private TMP_Text gemText;
    [SerializeField] private Button coinPlusButton;
    [SerializeField] private Button gemPlusButton;

    [Header("Detail")]
    [SerializeField] private GameObject detailContent;
    [SerializeField] private TMP_Text detailName;
    [SerializeField] private Image detailArt;
    [SerializeField] private TMP_Text detailDescription;
    [SerializeField] private TMP_Text detailPrice;
    [SerializeField] private GameObject detailGemBadge;
    [SerializeField] private Button buyButton;
    [SerializeField] private GameObject ownedLabel;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private ShopConfirmDialog confirmDialog;

    [Header("Audio")]
    [SerializeField] private AudioClip buyClip;
    [SerializeField] private AudioClip failClip;
    [SerializeField] private AudioClip tabClip;

    private readonly List<ShopItemCard> cards = new List<ShopItemCard>();
    private ShopItemCard selected;
    private ShopCategory currentCategory = ShopCategory.Characters;
    private Coroutine messageRoutine;

    private void Awake()
    {
        for (int i = 0; i < categoryButtons.Length; i++)
        {
            if (categoryButtons[i] == null) continue;
            var cat = (ShopCategory)i;
            categoryButtons[i].onClick.AddListener(() => { PlayClip(tabClip); ShowCategory(cat); });
        }
        buyButton.onClick.AddListener(BuySelected);
        if (coinPlusButton != null) coinPlusButton.onClick.AddListener(() => ShowCategory(ShopCategory.CurrencyPacks));
        if (gemPlusButton != null) gemPlusButton.onClick.AddListener(() => ShowCategory(ShopCategory.CurrencyPacks));
    }

    private void OnEnable() { Wallet.Changed += RefreshWallet; }
    private void OnDisable() { Wallet.Changed -= RefreshWallet; }

    private void Start()
    {
        if (messageText != null) messageText.gameObject.SetActive(false);
        RefreshWallet();
        ShowCategory(ShopCategory.Characters);
    }

    public void ShowCategory(ShopCategory category)
    {
        currentCategory = category;
        for (int i = 0; i < categoryButtons.Length; i++)
        {
            if (categoryButtons[i] == null) continue;
            bool on = i == (int)category;
            ((Image)categoryButtons[i].targetGraphic).sprite = on ? categorySelected[i] : categoryNormal[i];
            if (categoryArrows[i] != null) categoryArrows[i].SetActive(on);
        }

        foreach (Transform child in grid) Destroy(child.gameObject);
        cards.Clear();
        selected = null;

        foreach (var item in items)
        {
            if (item == null || item.category != category) continue;
            var card = Instantiate(cardPrefab, grid);
            card.Setup(item, this);
            cards.Add(card);
        }

        if (emptyHint != null) emptyHint.SetActive(cards.Count == 0);
        if (cards.Count > 0) SelectItem(cards[0]);
        else ShowDetail(null);
    }

    public void SelectItem(ShopItemCard card)
    {
        selected = card;
        foreach (var c in cards) c.SetSelected(c == card);
        ShowDetail(card != null ? card.Item : null);
    }

    // Bam BUY / gio hang: kiem tra tien roi hien hop xac nhan
    public void BuySelected()
    {
        if (selected == null) return;
        var item = selected.Item;
        if (Wallet.IsOwned(item.Id)) return;

        if (Wallet.Get(item.currency) < item.price)
        {
            PlayClip(failClip);
            ShowMessage(item.currency == CurrencyType.Coin ? "NOT ENOUGH COINS" : "NOT ENOUGH GEMS", new Color(1f, 0.3f, 0.45f));
            return;
        }

        if (confirmDialog != null) confirmDialog.Show(item, () => CompletePurchase(item));
        else CompletePurchase(item);
    }

    private void CompletePurchase(ShopItemData item)
    {
        if (Wallet.TryBuy(item))
        {
            PlayClip(buyClip);
            ShowMessage("PURCHASED!", new Color(0.35f, 0.95f, 1f));
        }
        else
        {
            PlayClip(failClip);
            ShowMessage(item.currency == CurrencyType.Coin ? "NOT ENOUGH COINS" : "NOT ENOUGH GEMS", new Color(1f, 0.3f, 0.45f));
        }
        foreach (var c in cards) c.Refresh();
        if (selected != null) ShowDetail(selected.Item);
    }

    private void ShowDetail(ShopItemData item)
    {
        detailContent.SetActive(item != null);
        if (item == null) return;

        detailName.text = item.displayName;
        detailArt.sprite = item.artSprite;
        detailDescription.text = item.description;
        detailPrice.text = item.price.ToString("N0");
        detailGemBadge.SetActive(item.currency == CurrencyType.Gem);

        bool owned = Wallet.IsOwned(item.Id);
        buyButton.gameObject.SetActive(!owned);
        ownedLabel.SetActive(owned);
    }

    private void RefreshWallet()
    {
        coinText.text = Wallet.Coins.ToString("N0");
        gemText.text = Wallet.Gems.ToString("N0");
    }

    private void ShowMessage(string text, Color color)
    {
        if (messageText == null) return;
        if (messageRoutine != null) StopCoroutine(messageRoutine);
        messageRoutine = StartCoroutine(MessageRoutine(text, color));
    }

    private IEnumerator MessageRoutine(string text, Color color)
    {
        messageText.text = text;
        messageText.color = color;
        messageText.gameObject.SetActive(true);
        yield return new WaitForSeconds(1.6f);
        messageText.gameObject.SetActive(false);
    }

    private static void PlayClip(AudioClip clip)
    {
        if (SFXManager.Instance != null) SFXManager.Instance.PlaySFX(clip);
    }
}

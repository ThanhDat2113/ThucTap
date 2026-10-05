using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemCard : MonoBehaviour
{
    [SerializeField] private Image cardImage;
    [SerializeField] private Button cardButton;
    [SerializeField] private RectTransform priceArea;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button cartButton;
    [SerializeField] private Image ownedCart;
    [SerializeField] private GameObject selectFrame;
    [SerializeField] private GameObject hotBadge;
    [SerializeField] private AudioClip selectClip;

    private ShopItemData item;
    private ShopController controller;

    public ShopItemData Item { get { return item; } }

    public void Setup(ShopItemData data, ShopController shop)
    {
        item = data;
        controller = shop;

        cardImage.sprite = item.cardSprite;
        var fitter = cardImage.GetComponent<AspectRatioFitter>();
        if (fitter != null && item.cardSprite != null) fitter.aspectRatio = item.cardSprite.rect.width / item.cardSprite.rect.height;
        SetRect(priceArea, item.priceRect);
        SetRect((RectTransform)cartButton.transform, item.cartRect);
        SetRect((RectTransform)ownedCart.transform, item.cartRect);
        if (hotBadge != null) hotBadge.SetActive(item.isHot);

        cardButton.onClick.RemoveAllListeners();
        cardButton.onClick.AddListener(OnSelect);
        cartButton.onClick.RemoveAllListeners();
        cartButton.onClick.AddListener(OnCart);

        Refresh();
        SetSelected(false);
    }

    public void Refresh()
    {
        bool owned = Wallet.IsOwned(item.Id);
        priceText.text = owned ? "OWNED" : item.price.ToString("N0");
        priceText.color = owned ? new Color(0.35f, 0.95f, 1f) : Color.white;
        // dung the co gio hang xam (khop dung vi tri) thay vi de hinh len tren
        cardImage.sprite = owned && item.cardOwnedSprite != null ? item.cardOwnedSprite : item.cardSprite;
        ownedCart.gameObject.SetActive(owned && item.cardOwnedSprite == null);
        cartButton.gameObject.SetActive(!owned);
    }

    public void SetSelected(bool selected)
    {
        if (selectFrame != null) selectFrame.SetActive(selected);
        transform.localScale = Vector3.one * (selected ? 1.03f : 1f);
    }

    private void OnSelect()
    {
        if (SFXManager.Instance != null) SFXManager.Instance.PlaySFX(selectClip);
        controller.SelectItem(this);
    }

    private void OnCart()
    {
        controller.SelectItem(this);
        controller.BuySelected();
    }

    private static void SetRect(RectTransform rt, Vector4 r)
    {
        rt.anchorMin = new Vector2(r.x, r.y);
        rt.anchorMax = new Vector2(r.z, r.w);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}

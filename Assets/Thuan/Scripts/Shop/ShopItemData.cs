using UnityEngine;

public enum ShopCategory { Characters = 0, Skins = 1, Icons = 2, Notes = 3, Backgrounds = 4, CurrencyPacks = 5 }
public enum CurrencyType { Coin = 0, Gem = 1 }

[CreateAssetMenu(fileName = "NewShopItem", menuName = "Shop/Shop Item")]
public class ShopItemData : ScriptableObject
{
    public string displayName;
    [TextArea(2, 4)] public string description;
    public ShopCategory category = ShopCategory.Characters;
    public CurrencyType currency = CurrencyType.Coin;
    public int price = 100;
    public bool isHot;

    [Header("Art")]
    public Sprite cardSprite;
    [Tooltip("The giong cardSprite nhung gio hang mau xam - hien khi da mua")]
    public Sprite cardOwnedSprite;
    public Sprite artSprite;

    [Header("Vi tri tren the (0-1, goc duoi trai) - x=xMin, y=yMin, z=xMax, w=yMax")]
    public Vector4 priceRect = new Vector4(0.31f, 0.05f, 0.63f, 0.23f);
    public Vector4 cartRect = new Vector4(0.67f, 0.05f, 0.93f, 0.23f);

    public string Id { get { return name; } }
}

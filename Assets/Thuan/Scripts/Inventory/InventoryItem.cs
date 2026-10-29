using System;
using UnityEngine;

public enum InventoryCategory { All = 0, Characters = 1, Skins = 2, Icons = 3, Notes = 4, Backgrounds = 5 }
public enum ItemRarity { Common = 0, Rare = 1, Epic = 2 }

[Serializable]
public class InventoryItem
{
    public string id = "neon_beat_girl";
    public string displayName = "Neon Beat Girl";
    [TextArea(2, 4)] public string description;
    public InventoryCategory category = InventoryCategory.Characters;
    public ItemRarity rarity = ItemRarity.Epic;
    public int count = 1;
    [Tooltip("Hinh nho tren o vat pham")]
    public Sprite icon;
    [Tooltip("Hinh lon o khung chi tiet (de trong = dung icon)")]
    public Sprite art;
    [Tooltip("Chi do trang bi duoc (nhan vat, skin...) moi co nut USE / EQUIPPED")]
    public bool equippable = true;

    public static Color RarityColor(ItemRarity r)
    {
        switch (r)
        {
            case ItemRarity.Common: return new Color(0.62f, 0.66f, 0.78f);
            case ItemRarity.Rare: return new Color(0.45f, 0.75f, 1.6f);
            default: return Color.white; // sprite goc da la mau tim EPIC
        }
    }

    // ---- Trang bi (moi danh muc chi trang bi 1 mon) ----
    private string EquipKey { get { return "inventory_equipped_" + category; } }
    public bool IsEquipped { get { return PlayerPrefs.GetString(EquipKey, "") == id; } }

    public void SetEquipped(bool on)
    {
        if (on) PlayerPrefs.SetString(EquipKey, id);
        else if (IsEquipped) PlayerPrefs.DeleteKey(EquipKey);
        PlayerPrefs.Save();
    }
}

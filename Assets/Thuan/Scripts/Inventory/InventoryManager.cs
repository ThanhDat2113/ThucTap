using System.Collections.Generic;
using UnityEngine;

// Quản lý đồ đã sở hữu của người chơi.
// Nguồn sự thật duy nhất: Wallet (key "owned_"). Key cũ "inv_owned_" được migrate sang.
public static class InventoryManager
{
    private const string LegacyPrefix = "inv_owned_";
    public static System.Action<string> OnItemOwnedChanged;

    public static bool IsOwned(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return false;

        // Migration: bản cũ dùng key "inv_owned_" → chuyển sang key Wallet "owned_"
        if (PlayerPrefs.GetInt(LegacyPrefix + itemId, 0) == 1)
        {
            if (!Wallet.IsOwned(itemId)) Wallet.GrantItem(itemId);
            PlayerPrefs.DeleteKey(LegacyPrefix + itemId);
            PlayerPrefs.Save();
        }
        return Wallet.IsOwned(itemId);
    }

    public static void SetOwned(string itemId, bool owned)
    {
        if (string.IsNullOrEmpty(itemId)) return;
        if (IsOwned(itemId) == owned) return;

        if (owned) Wallet.GrantItem(itemId);
        else       Wallet.RevokeItem(itemId);

        OnItemOwnedChanged?.Invoke(itemId);
    }

    public static void GrantItem(string itemId) => SetOwned(itemId, true);
    public static void RevokeItem(string itemId) => SetOwned(itemId, false);

    // Xoá toàn bộ trạng thái "đã sở hữu" (dùng khi reset debug / fix dữ liệu bị grant nhầm)
    public static void ClearAllOwned(List<ShopItemData> allShopItems)
    {
        if (allShopItems == null) return;
        foreach (var item in allShopItems)
        {
            if (item == null) continue;

            string legacyKey = LegacyPrefix + item.Id;
            string walletKey = "owned_" + item.Id;

            if (PlayerPrefs.HasKey(legacyKey)) PlayerPrefs.DeleteKey(legacyKey);
            if (PlayerPrefs.HasKey(walletKey)) PlayerPrefs.DeleteKey(walletKey);
        }
        PlayerPrefs.Save();
        Debug.Log("[InventoryManager] Đã xoá toàn bộ trạng thái owned (legacy + wallet).");
    }

    public static List<ShopItemData> GetOwnedShopItems(List<ShopItemData> allShopItems)
    {
        var owned = new List<ShopItemData>();
        if (allShopItems == null) return owned;
        foreach (var item in allShopItems)
            if (item != null && IsOwned(item.Id)) owned.Add(item);
        return owned;
    }
}

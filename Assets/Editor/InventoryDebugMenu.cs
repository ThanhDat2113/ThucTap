#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public static class InventoryDebugMenu
{
    [MenuItem("Tools/Inventory/Clear All Owned (Debug)")]
    public static void ClearAllOwned()
    {
        var guids = AssetDatabase.FindAssets("t:ShopItemData");
        int count = 0;
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var item = AssetDatabase.LoadAssetAtPath<ShopItemData>(path);
            if (item == null) continue;

            string legacyKey = "inv_owned_" + item.Id;
            string walletKey = "owned_" + item.Id;
            if (PlayerPrefs.HasKey(legacyKey)) { PlayerPrefs.DeleteKey(legacyKey); count++; }
            if (PlayerPrefs.HasKey(walletKey)) { PlayerPrefs.DeleteKey(walletKey); count++; }
        }
        PlayerPrefs.Save();
        Debug.Log($"[InventoryDebug] Đã xoá {count} key ownership. Play lại để test mua sạch.");
    }

    [MenuItem("Tools/Inventory/List Owned (Debug)")]
    public static void ListOwned()
    {
        var guids = AssetDatabase.FindAssets("t:ShopItemData");
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var item = AssetDatabase.LoadAssetAtPath<ShopItemData>(path);
            if (item == null) continue;
            bool legacy = PlayerPrefs.GetInt("inv_owned_" + item.Id, 0) == 1;
            bool wallet = PlayerPrefs.GetInt("owned_" + item.Id, 0) == 1;
            if (legacy || wallet)
                Debug.Log($"[Owned] {item.name} — legacy={legacy}, wallet={wallet}");
        }
    }
}
#endif

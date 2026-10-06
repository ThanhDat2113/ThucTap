using UnityEditor;
using UnityEngine;

// Menu test cho Shop: Unity -> Thuan -> Shop
public static class ShopDebugMenu
{
    [MenuItem("Thuan/Shop/Reset do da mua")]
    public static void ResetOwned()
    {
        int n = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:ShopItemData"))
        {
            var item = AssetDatabase.LoadAssetAtPath<ShopItemData>(AssetDatabase.GUIDToAssetPath(guid));
            if (item == null) continue;
            PlayerPrefs.DeleteKey("owned_" + item.Id);
            n++;
        }
        PlayerPrefs.Save();
        Debug.Log("[Shop] Da reset " + n + " vat pham ve chua mua.");
    }

    [MenuItem("Thuan/Shop/Reset vi tien ve mac dinh")]
    public static void ResetWallet()
    {
        PlayerPrefs.DeleteKey("wallet_coins");
        PlayerPrefs.DeleteKey("wallet_gems");
        PlayerPrefs.Save();
        Debug.Log("[Shop] Vi tien ve mac dinh: " + Wallet.StartCoins + " xu, " + Wallet.StartGems + " kim cuong.");
    }

    [MenuItem("Thuan/Shop/Reset toan bo Shop")]
    public static void ResetAll()
    {
        ResetOwned();
        ResetWallet();
    }
}

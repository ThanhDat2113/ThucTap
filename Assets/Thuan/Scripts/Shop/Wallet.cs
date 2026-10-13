using System;
using UnityEngine;

// Vi tien cua nguoi choi + do da mua, luu bang PlayerPrefs
public static class Wallet
{
    public const int StartCoins = 0;
    public const int StartGems = 0;

    private const string CoinKey = "wallet_coins";
    private const string GemKey = "wallet_gems";
    private const string OwnedPrefix = "owned_";
    private const string TicketKey = "wallet_tickets";

    public static event Action Changed;

    public static int Coins { get { return PlayerPrefs.GetInt(CoinKey, StartCoins); } }
    public static int Gems { get { return PlayerPrefs.GetInt(GemKey, StartGems); } }

    public static int Get(CurrencyType type) { return type == CurrencyType.Coin ? Coins : Gems; }

    public static void Add(CurrencyType type, int amount)
    {
        PlayerPrefs.SetInt(type == CurrencyType.Coin ? CoinKey : GemKey, Mathf.Max(0, Get(type) + amount));
        PlayerPrefs.Save();
        if (Changed != null) Changed();
    }

    public static int Tickets { get { return PlayerPrefs.GetInt(TicketKey, 0); } }

    public static void AddTickets(int amount)
    {
        PlayerPrefs.SetInt(TicketKey, Mathf.Max(0, Tickets + amount));
        PlayerPrefs.Save();
        if (Changed != null) Changed();
    }

    // Tang do mien phi (vd: qua diem danh)
    public static void GrantItem(string itemId)
    {
        PlayerPrefs.SetInt(OwnedPrefix + itemId, 1);
        PlayerPrefs.Save();
        if (Changed != null) Changed();
    }

    public static bool IsOwned(string itemId) { return PlayerPrefs.GetInt(OwnedPrefix + itemId, 0) == 1; }

    public static bool TryBuy(ShopItemData item)
    {
        if (item == null || IsOwned(item.Id)) return false;
        if (Get(item.currency) < item.price) return false;
        PlayerPrefs.SetInt(item.currency == CurrencyType.Coin ? CoinKey : GemKey, Get(item.currency) - item.price);
        PlayerPrefs.SetInt(OwnedPrefix + item.Id, 1);
        PlayerPrefs.Save();
        if (Changed != null) Changed();
        return true;
    }
}

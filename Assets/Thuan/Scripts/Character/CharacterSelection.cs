using System;
using UnityEngine;

// Nhan vat dang duoc chon, luu bang PlayerPrefs
public static class CharacterSelection
{
    public const string DefaultId = "kai";
    private const string Key = "selected_character";

    public static event Action Changed;

    public static string SelectedId
    {
        get { return PlayerPrefs.GetString(Key, DefaultId); }
        set
        {
            PlayerPrefs.SetString(Key, value);
            PlayerPrefs.Save();
            if (Changed != null) Changed();
        }
    }

    public static void Reset()
    {
        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();
        if (Changed != null) Changed();
    }
}

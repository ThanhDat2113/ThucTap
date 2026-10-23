using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Tab STORY MODE / FREE PLAY trong man chon nhac (nho tab lan cuoi)
public class SongListTabs : MonoBehaviour
{
    [SerializeField] private Button[] tabs = new Button[2];
    [SerializeField] private GameObject[] roots = new GameObject[2];
    [SerializeField] private TMP_Text[] labels = new TMP_Text[2];
    [SerializeField] private Image[] icons = new Image[2];
    [SerializeField] private Sprite selectedSprite;
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Color selectedText = new Color(1f, 0.25f, 0.75f);
    [SerializeField] private Color normalText = new Color(0.85f, 0.95f, 1f, 0.85f);
    [SerializeField] private AudioClip tabClip;

    private const string Key = "songlist_tab";
    private int current = -1;

    private void Awake()
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            int idx = i;
            if (tabs[i] != null) tabs[i].onClick.AddListener(() => { if (idx != current && tabClip != null && SFXManager.Instance != null) SFXManager.Instance.PlaySFX(tabClip); Show(idx); });
        }
        Show(PlayerPrefs.GetInt(Key, 0));
    }

    public void Show(int index)
    {
        index = Mathf.Clamp(index, 0, tabs.Length - 1);
        current = index;
        PlayerPrefs.SetInt(Key, index);
        for (int i = 0; i < tabs.Length; i++)
        {
            bool on = i == index;
            if (roots[i] != null) roots[i].SetActive(on);
            if (tabs[i] != null) ((Image)tabs[i].targetGraphic).sprite = on ? selectedSprite : normalSprite;
            if (labels[i] != null) labels[i].color = on ? selectedText : normalText;
            if (icons[i] != null) icons[i].color = on ? selectedText : normalText;
        }
    }
}

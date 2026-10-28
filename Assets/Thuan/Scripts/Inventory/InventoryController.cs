using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryController : MonoBehaviour
{
    [Header("Dữ liệu")]
    [Tooltip("Danh sách ShopItemData (kéo asset thật vào, hoặc tick Auto Load bên dưới).")]
    [SerializeField] private List<ShopItemData> allShopItems;
    [Tooltip("Tự load tất cả ShopItemData trong thư mục Resources/ShopItems/.")]
    [SerializeField] private bool autoLoadFromResources = true;
    [Tooltip("CHỈ bật khi cần test — sẽ tặng FREE toàn bộ item trong list.")]
    [SerializeField] private bool grantAllForDebug = false;

    Canvas canvas;
    RectTransform panel;
    RectTransform grid;
    GameObject emptyHint;

    void Start()
    {
        Debug.Log("[Inventory] Start");
        EnsureItemList();
        EnsureUI();
        if (grantAllForDebug)
        {
            foreach (var it in allShopItems)
                if (it != null) InventoryManager.GrantItem(it.Id);
        }
        Refresh();
    }

    void OnEnable()  { Wallet.Changed += Refresh; }
    void OnDisable() { Wallet.Changed -= Refresh; }

    void EnsureItemList()
    {
        if (allShopItems == null) allShopItems = new List<ShopItemData>();

        // Nếu list rỗng và có bật auto load -> lấy toàn bộ asset trong Resources/ShopItems/
        if (allShopItems.Count == 0 && autoLoadFromResources)
        {
            var loaded = Resources.LoadAll<ShopItemData>("ShopItems");
            if (loaded != null && loaded.Length > 0)
            {
                allShopItems.AddRange(loaded);
                Debug.Log($"[Inventory] Auto load được {loaded.Length} ShopItemData từ Resources/ShopItems/");
            }
        }

        if (allShopItems.Count == 0)
            Debug.LogWarning("[Inventory] Chưa có ShopItemData nào. " +
                             "Hãy kéo asset vào list hoặc đặt asset dưới Resources/ShopItems/.");
    }

    void EnsureUI()
    {
        var existing = FindObjectOfType<Canvas>();
        if (existing != null) canvas = existing;
        if (canvas == null)
        {
            var go = new GameObject("InventoryCanvas", typeof(Canvas));
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            if (go.GetComponent<GraphicRaycaster>() == null) go.AddComponent<GraphicRaycaster>();
        }

        if (panel == null)
        {
            var go = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel = go.GetComponent<RectTransform>();
            panel.SetParent(canvas.transform, false);
            panel.anchorMin = Vector2.zero; panel.anchorMax = Vector2.one;
            panel.offsetMin = Vector2.zero; panel.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        }

        if (grid == null)
        {
            var go = new GameObject("Grid", typeof(RectTransform), typeof(GridLayoutGroup));
            grid = go.GetComponent<RectTransform>();
            grid.SetParent(panel.transform, false);
            grid.anchorMin = Vector2.zero; grid.anchorMax = Vector2.one;
            grid.offsetMin = Vector2.zero; grid.offsetMax = Vector2.zero;
            var glg = go.GetComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(120, 120);
            glg.spacing = new Vector2(15, 15);
            glg.childAlignment = TextAnchor.UpperLeft;
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 5;
            glg.padding = new RectOffset(20, 20, 20, 20);
        }

        if (emptyHint == null)
        {
            var go = new GameObject("EmptyHint", typeof(RectTransform));
            emptyHint = go;
            var erh = emptyHint.GetComponent<RectTransform>();
            erh.SetParent(panel.transform, false);
            erh.anchorMin = new Vector2(0.5f, 0.5f); erh.anchorMax = new Vector2(0.5f, 0.5f);
            erh.anchoredPosition = Vector2.zero; erh.sizeDelta = new Vector2(200, 40);
            var txtGo = new GameObject("Text", typeof(TextMeshProUGUI));
            txtGo.GetComponent<RectTransform>().SetParent(erh, false);
            var txt = txtGo.GetComponent<TextMeshProUGUI>();
            txt.text = "Kho đồ trống.";
            txt.fontSize = 20; txt.color = Color.white; txt.alignment = TMPro.TextAlignmentOptions.Center;
            emptyHint.gameObject.SetActive(false);
        }
    }

    public void Refresh()
    {
        if (grid == null) return;

        foreach (Transform child in grid) if (child != null) Destroy(child.gameObject);

        var owned = InventoryManager.GetOwnedShopItems(allShopItems);
        Debug.Log("[Inventory] Owned=" + owned.Count + " / Total=" + allShopItems.Count);

        if (emptyHint != null) emptyHint.SetActive(owned.Count == 0);

        foreach (var item in owned)
        {
            var go = new GameObject("Item_" + item.Id, typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(grid.transform, false); rt.sizeDelta = new Vector2(100, 100);
            var img = go.GetComponent<Image>(); img.color = new Color(0.25f, 0.25f, 0.3f, 1f);
            var btn = go.GetComponent<Button>(); btn.targetGraphic = img;

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            var irt = iconGo.GetComponent<RectTransform>();
            irt.SetParent(rt.transform, false); irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one;
            irt.offsetMin = Vector2.zero; irt.offsetMax = Vector2.zero;
            var iimg = iconGo.GetComponent<Image>();
            iimg.sprite = item.cardSprite != null ? item.cardSprite : item.artSprite;
            iimg.color = Color.white;

            var nameGo = new GameObject("Name", typeof(TextMeshProUGUI));
            var nrt = nameGo.GetComponent<RectTransform>();
            nrt.SetParent(rt.transform, false); nrt.anchorMin = new Vector2(0, 0); nrt.anchorMax = new Vector2(1, 0);
            nrt.offsetMin = new Vector2(2, 2); nrt.offsetMax = new Vector2(-2, 20);
            var ntxt = nameGo.GetComponent<TextMeshProUGUI>();
            ntxt.text = item.displayName; ntxt.fontSize = 14; ntxt.color = Color.white;
            ntxt.alignment = TMPro.TextAlignmentOptions.Center; ntxt.enableWordWrapping = true;
        }
    }
}

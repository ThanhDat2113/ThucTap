using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class InventoryMenuButton : MonoBehaviour
{
    [SerializeField] private string inventorySceneName = "Inventory";
    [SerializeField] private string buttonLabel = "Kho Đồ";

    void Awake()
    {
        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[InventoryMenuButton] Không tìm thấy Canvas trong scene.");
            return;
        }

        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        var btnGo = new GameObject("Btn_KhoDo", typeof(RectTransform), typeof(Button), typeof(Image));
        var rt = btnGo.GetComponent<RectTransform>();
        rt.SetParent(canvas.transform, false);
        rt.anchorMin = new Vector2(1, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.anchoredPosition = new Vector2(-20f, -20f);
        rt.sizeDelta = new Vector2(160f, 56f);

        var img = btnGo.GetComponent<Image>();
        img.color = new Color(0.2f, 0.6f, 0.9f, 1f);
        img.raycastTarget = true;

        var btn = btnGo.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.interactable = true;

        var textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        var tr = textGo.GetComponent<RectTransform>();
        tr.SetParent(rt, false);
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;
        var tmp = textGo.GetComponent<TextMeshProUGUI>();
        tmp.text = buttonLabel;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.fontSize = 20;
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        btn.onClick.AddListener(() =>
        {
            Debug.Log("[InventoryMenuButton] Load " + inventorySceneName);
            if (Application.CanStreamedLevelBeLoaded(inventorySceneName))
                SceneManager.LoadScene(inventorySceneName);
            else
                Debug.LogWarning("[InventoryMenuButton] Scene chưa có trong Build Settings.");
        });

        Debug.Log("[InventoryMenuButton] Đã tạo nút Kho Đồ trên Canvas đã có.");
    }
}

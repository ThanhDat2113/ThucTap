using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Tạm dừng game: dừng thời gian (Time.timeScale = 0) + dừng toàn bộ âm thanh (AudioListener.pause),
/// hiện menu PAUSE GAME với các nút RESUME / RESTART / LOBBY / MAIN MENU.
/// Nút ở góc màn hình đổi thành nút "quay lại gameplay" (►) khi đang pause.
/// Tự dựng UI bằng code, không cần tạo Canvas. Chữ và ảnh nút đều đổi được trong Inspector.
///
/// CÁCH DÙNG: tạo 1 GameObject rỗng trong scene gameplay, gắn script này vào.
/// Phím tắt: Esc hoặc P (Esc cũng là nút Back trên Android).
/// Nút LOBBY / MAIN MENU: điền tên scene (hoặc gắn sự kiện) trong Inspector để nút hoạt động.
/// </summary>
public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }
    public static bool IsPaused { get; private set; }

    [Header("Tuỳ chọn")]
    [Tooltip("Cho phép tạm dừng. GameManager tự tắt khi game kết thúc / thua")]
    public bool canPause = true;
    [Tooltip("Số giây đếm ngược trước khi chơi tiếp (0 = chơi tiếp ngay)")]
    [Range(0, 5)] public int countdownSeconds = 0;
    public bool showPauseButton = true;
    [Tooltip("Vị trí nút góc, tính từ góc trên phải. Y càng âm thì nút càng xuống thấp (tránh đè lên thanh máu)")]
    public Vector2 pauseButtonPosition = new Vector2(-30f, -65f);
    [Tooltip("Tự tạm dừng khi thoát app / mất focus (chỉ chạy trên bản build, không chạy trong Editor)")]
    public bool pauseOnFocusLost = true;

    [Header("Quay về sảnh / menu chính")]
    [Tooltip("Tên scene sảnh (chọn bài). Phải có trong File > Build Settings > Scenes In Build. Trống = bấm nút sẽ chỉ báo cảnh báo trong Console")]
    public string lobbySceneName = "";
    [Tooltip("Tên scene menu chính (màn hình tiêu đề). Trống = bấm nút sẽ chỉ báo cảnh báo trong Console")]
    public string mainMenuSceneName = "";
    [Tooltip("Hiện nút LOBBY / MAIN MENU trong menu pause (tắt nếu game bạn không cần)")]
    public bool showLobbyButton = true;
    public bool showMainMenuButton = true;
    [Tooltip("Nếu sảnh/menu của bạn là 1 panel trong cùng scene (không phải scene riêng): gắn việc cần làm vào đây. Có gắn thì nút vẫn hiện dù không điền tên scene")]
    public UnityEvent onGoLobby;
    public UnityEvent onGoMainMenu;

    [Header("Chữ hiển thị")]
    public string titleText = "PAUSE GAME";
    public string resumeText = "RESUME";
    public string restartText = "RESTART";
    public string lobbyText = "LOBBY";
    public string mainMenuText = "MAIN MENU";

    [Header("Ảnh nút (để trống = dùng nút màu mặc định)")]
    [Tooltip("Nút ở góc khi đang chơi (bấm để pause)")]
    public Sprite pauseIconSprite;
    [Tooltip("Nút ở góc khi đang pause (bấm để quay lại gameplay). Trống = hiện ký hiệu ►")]
    public Sprite resumeIconSprite;
    [Tooltip("Nút RESUME lớn trong menu")]
    public Sprite resumeButtonSprite;
    public Sprite restartButtonSprite;
    public Sprite lobbyButtonSprite;
    public Sprite mainMenuButtonSprite;
    [Tooltip("Bật = có ảnh thì ẩn chữ (dùng khi ảnh đã có sẵn chữ/icon). Tắt = chữ hiện đè lên ảnh (dùng khi ảnh chỉ là khung nền trống)")]
    public bool hideLabelWhenSprite = false;

    [Header("Kích thước nút")]
    [Tooltip("Chiều cao nút ở góc. Có ảnh thì chiều rộng tự theo tỉ lệ ảnh")]
    public float pauseButtonHeight = 90f;
    [Tooltip("Chiều cao các nút trong menu. Có ảnh thì chiều rộng tự theo tỉ lệ ảnh")]
    public float menuButtonHeight = 110f;
    [Tooltip("Khoảng cách giữa các nút trong menu")]
    public float menuButtonGap = 20f;

    [Header("Giao diện")]
    [Tooltip("Kéo font của bạn vào đây cho khớp với Score/Combo. Để trống = font hệ thống")]
    public Font font;
    public Color dimColor = new Color(0f, 0f, 0f, 0.7f);
    public Color buttonColor = new Color(0.95f, 0.25f, 0.65f, 1f);
    public Color textColor = Color.white;

    [Header("Sự kiện (gắn thêm việc cần làm khi pause / tiếp tục)")]
    public UnityEvent onPause;
    public UnityEvent onResume;

    /// <summary>Game đăng ký cách restart riêng (GameManager.Restart). Trống = load lại scene.</summary>
    public System.Action RestartHandler;

    Font uiFont;
    GameObject panel;
    Button pauseButton;
    Image pauseImage;
    Text pauseLabel;
    Text countdownText;
    float prevTimeScale = 1f;
    bool counting;

    bool HasLobby => showLobbyButton;
    bool HasMainMenu => showMainMenuButton;

    void Awake()
    {
        Instance = this;
        ResetState();
        BuildUI();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            ResetState(); // đảm bảo không bị kẹt timeScale = 0 khi đổi scene
            Instance = null;
        }
    }

    void Update()
    {
        if (!canPause) return;
        if (KeyPressed()) Toggle();
    }

    bool KeyPressed()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        var kb = Keyboard.current;
        return kb != null && (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P);
#endif
    }

#if !UNITY_EDITOR
    void OnApplicationPause(bool paused) { if (paused && pauseOnFocusLost && canPause) Pause(); }
    void OnApplicationFocus(bool focus) { if (!focus && pauseOnFocusLost && canPause) Pause(); }
#endif

    // ---------- Điều khiển ----------
    public void Toggle()
    {
        if (IsPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (IsPaused || !canPause) return;
        IsPaused = true;
        prevTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;
        AudioListener.pause = true;

        panel.SetActive(true);
        RefreshPauseButton(true); // nút góc đổi thành nút quay lại gameplay
        onPause.Invoke();
    }

    public void Resume()
    {
        if (!IsPaused || counting) return;
        StartCoroutine(ResumeRoutine());
    }

    IEnumerator ResumeRoutine()
    {
        counting = true;
        panel.SetActive(false);

        if (countdownSeconds > 0)
        {
            pauseButton.gameObject.SetActive(false);
            countdownText.gameObject.SetActive(true);
            for (int i = countdownSeconds; i > 0; i--)
            {
                countdownText.text = i.ToString();
                yield return new WaitForSecondsRealtime(1f); // thời gian thật, không bị timeScale = 0 chặn
            }
            countdownText.gameObject.SetActive(false);
        }

        Time.timeScale = prevTimeScale;
        AudioListener.pause = false;
        IsPaused = false;
        counting = false;
        RefreshPauseButton(false);
        pauseButton.gameObject.SetActive(showPauseButton);
        onResume.Invoke();
    }

    public void Restart()
    {
        if (RestartHandler != null)
        {
            ForceUnpause();
            RestartHandler();
            return;
        }
        ResetState();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToLobby() { LeaveGameplay(lobbySceneName, onGoLobby); }
    public void GoToMainMenu() { LeaveGameplay(mainMenuSceneName, onGoMainMenu); }

    // Rời gameplay: trả timeScale + âm thanh về bình thường, chạy sự kiện (nếu có), rồi load scene (nếu có tên)
    void LeaveGameplay(string sceneName, UnityEvent customEvent)
    {
        bool hasEvent = customEvent != null && customEvent.GetPersistentEventCount() > 0;
        if (string.IsNullOrEmpty(sceneName) && !hasEvent)
        {
            Debug.LogWarning("PauseManager: nút này chưa có đích đến. Điền tên scene (Lobby Scene Name / Main Menu Scene Name) " +
                             "hoặc gắn sự kiện On Go Lobby / On Go Main Menu trong Inspector.");
            return;
        }
        ForceUnpause();
        if (customEvent != null) customEvent.Invoke();
        if (!string.IsNullOrEmpty(sceneName)) SceneManager.LoadScene(sceneName);
    }

    /// <summary>Bỏ pause ngay lập tức, không đếm ngược (dùng khi restart / rời gameplay).</summary>
    public void ForceUnpause()
    {
        StopAllCoroutines();
        counting = false;
        if (countdownText) countdownText.gameObject.SetActive(false);
        if (panel) panel.SetActive(false);
        ResetState();
        if (pauseButton)
        {
            RefreshPauseButton(false);
            pauseButton.gameObject.SetActive(showPauseButton);
        }
    }

    static void ResetState()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        IsPaused = false;
    }

    // ---------- Dựng UI bằng code ----------
    void BuildUI()
    {
        uiFont = font;
        if (!uiFont) uiFont = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Roboto", "Helvetica" }, 32);

        if (EventSystem.current == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }

        var canvasGO = new GameObject("PauseCanvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Màn hình tạm dừng
        var panelRt = NewRect("PausePanel", canvasGO.transform);
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = panelRt.offsetMax = Vector2.zero;
        var dim = panelRt.gameObject.AddComponent<Image>();
        dim.color = dimColor; // vừa làm nền tối vừa chặn bấm xuyên xuống gameplay
        panel = panelRt.gameObject;

        // Danh sách nút trong menu (nút nào không có đích đến thì không hiện)
        var items = new List<MenuItem>();
        items.Add(new MenuItem(resumeText, resumeButtonSprite, new UnityAction(Resume)));
        items.Add(new MenuItem(restartText, restartButtonSprite, new UnityAction(Restart)));
        if (HasLobby) items.Add(new MenuItem(lobbyText, lobbyButtonSprite, new UnityAction(GoToLobby)));
        if (HasMainMenu) items.Add(new MenuItem(mainMenuText, mainMenuButtonSprite, new UnityAction(GoToMainMenu)));

        // Xếp dọc, cả cụm nằm giữa màn hình (hơi lệch xuống để chừa chỗ cho tiêu đề)
        float spacing = menuButtonHeight + menuButtonGap;
        float startY = (items.Count - 1) * spacing * 0.5f - 20f;
        for (int i = 0; i < items.Count; i++)
            MakeMenuButton(panelRt, items[i], new Vector2(0f, startY - i * spacing));

        float titleY = startY + menuButtonHeight * 0.5f + 110f;
        MakeText(panelRt, titleText, 96, new Vector2(0f, titleY), new Vector2(1400f, 200f));
        panel.SetActive(false);

        // Số đếm ngược
        countdownText = MakeText(canvasGO.transform, "", 260, Vector2.zero, new Vector2(600f, 400f));
        countdownText.gameObject.SetActive(false);

        // Nút ở góc (tạo SAU CÙNG để luôn nằm trên màn hình pause -> vẫn bấm được khi đang pause)
        pauseButton = MakeButton(canvasGO.transform, Vector2.zero, Toggle, out pauseImage, out pauseLabel);
        var prt = (RectTransform)pauseButton.transform;
        prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(1f, 1f);
        prt.anchoredPosition = pauseButtonPosition;
        RefreshPauseButton(false);
        pauseButton.gameObject.SetActive(showPauseButton);
    }

    struct MenuItem
    {
        public string label;
        public Sprite sprite;
        public UnityAction action;
        public MenuItem(string l, Sprite s, UnityAction a) { label = l; sprite = s; action = a; }
    }

    // Nút góc: đang chơi = icon pause (II), đang pause = icon quay lại (►)
    void RefreshPauseButton(bool paused)
    {
        if (pauseButton == null) return;
        ApplyLook(pauseButton, pauseImage, pauseLabel,
            paused ? resumeIconSprite : pauseIconSprite,
            paused ? "\u25BA" : "II", 44,
            new Vector2(pauseButtonHeight, pauseButtonHeight), pauseButtonHeight);
    }

    void MakeMenuButton(Transform parent, MenuItem item, Vector2 pos)
    {
        var btn = MakeButton(parent, pos, item.action, out Image img, out Text txt);
        ApplyLook(btn, img, txt, item.sprite, item.label, 52,
            new Vector2(560f, menuButtonHeight), menuButtonHeight);
    }

    // Có ảnh: dùng ảnh (rộng theo tỉ lệ ảnh, không méo). Không có: nút màu + chữ.
    void ApplyLook(Button btn, Image img, Text label, Sprite sprite, string text, int fontSize,
                   Vector2 fallbackSize, float spriteHeight)
    {
        var rt = (RectTransform)btn.transform;
        img.sprite = sprite;
        img.color = sprite != null ? Color.white : buttonColor;
        rt.sizeDelta = sprite != null
            ? new Vector2(spriteHeight * sprite.rect.width / Mathf.Max(1f, sprite.rect.height), spriteHeight)
            : fallbackSize;
        label.text = text;
        label.fontSize = fontSize;
        label.resizeTextForBestFit = true;   // chữ tự thu nhỏ cho vừa khung nút
        label.resizeTextMinSize = 16;
        label.resizeTextMaxSize = fontSize;
        label.gameObject.SetActive(sprite == null || !hideLabelWhenSprite);
    }

    RectTransform NewRect(string objName, Transform parent)
    {
        var go = new GameObject(objName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    Text MakeText(Transform parent, string content, int size, Vector2 pos, Vector2 box)
    {
        var rt = NewRect("Text", parent);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = box;
        rt.anchoredPosition = pos;
        var t = rt.gameObject.AddComponent<Text>();
        t.font = uiFont;
        t.text = content;
        t.fontSize = size;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = textColor;
        t.raycastTarget = false;
        return t;
    }

    Button MakeButton(Transform parent, Vector2 pos, UnityAction action, out Image img, out Text label)
    {
        var rt = NewRect("Button", parent);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        img = rt.gameObject.AddComponent<Image>();
        var btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(action);

        // Chữ trên nút: giãn kín nút, tự theo kích thước nút
        var lrt = NewRect("Label", rt);
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = new Vector2(16f, 8f);   // chừa viền để chữ không đè lên cạnh khung
        lrt.offsetMax = new Vector2(-16f, -8f);
        label = lrt.gameObject.AddComponent<Text>();
        label.font = uiFont;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = textColor;
        label.raycastTarget = false;
        return btn;
    }
}
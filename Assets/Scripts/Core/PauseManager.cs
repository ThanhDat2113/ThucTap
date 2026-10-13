using System.Collections;
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
/// hiện menu Tiếp tục / Chơi lại / Menu, đếm ngược 3-2-1 trước khi chơi tiếp.
/// Tự dựng UI bằng code, không cần tạo Canvas.
///
/// CÁCH DÙNG: tạo 1 GameObject rỗng trong scene gameplay, gắn script này vào.
/// Phím tắt: Esc hoặc P (Esc cũng là nút Back trên Android).
/// Trong code nhận input / chấm điểm nốt, thêm dòng đầu:  if (PauseManager.IsPaused) return;
/// </summary>
public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }
    public static bool IsPaused { get; private set; }

    [Header("Tuỳ chọn")]
    [Tooltip("Cho phép tạm dừng. Tắt (từ code) khi game kết thúc / đang ở màn hình kết quả")]
    public bool canPause = true;
    [Tooltip("Số giây đếm ngược trước khi chơi tiếp (0 = chơi tiếp ngay)")]
    [Range(0, 5)] public int countdownSeconds = 0;
    public bool showPauseButton = true;
    [Tooltip("Vị trí nút pause tính từ góc trên phải")]
    public Vector2 pauseButtonPosition = new Vector2(-30f, -30f);
    [Tooltip("Tự tạm dừng khi thoát app / mất focus (chỉ chạy trên bản build, không chạy trong Editor)")]
    public bool pauseOnFocusLost = true;
    [Tooltip("Tên scene menu (phải thêm vào Build Settings). Để trống = ẩn nút Menu")]
    public string menuSceneName = "";

    [Header("Giao diện")]
    [Tooltip("Kéo font của bạn vào đây cho khớp với Score/Combo. Để trống = font hệ thống")]
    public Font font;
    public Color dimColor = new Color(0f, 0f, 0f, 0.7f);
    public Color buttonColor = new Color(0.95f, 0.25f, 0.65f, 1f);
    public Color textColor = Color.white;

    [Header("Sự kiện (gắn thêm việc cần làm khi pause / tiếp tục)")]
    public UnityEvent onPause;
    public UnityEvent onResume;

    Font uiFont;
    GameObject panel;
    Button pauseButton;
    Text countdownText;
    float prevTimeScale = 1f;
    bool counting;

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
        pauseButton.gameObject.SetActive(false);
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
        pauseButton.gameObject.SetActive(showPauseButton);
        onResume.Invoke();
    }

    /// <summary>
    /// Nếu game đã đăng ký cách restart riêng (GameManager.Restart) thì dùng nó,
    /// không thì load lại cả scene.
    /// </summary>
    public System.Action RestartHandler;

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

    /// <summary>Bỏ pause ngay lập tức, không đếm ngược (dùng khi restart).</summary>
    public void ForceUnpause()
    {
        StopAllCoroutines();
        counting = false;
        if (countdownText) countdownText.gameObject.SetActive(false);
        if (panel) panel.SetActive(false);
        ResetState();
        if (pauseButton) pauseButton.gameObject.SetActive(showPauseButton);
    }

    public void GoToMenu()
    {
        ResetState();
        SceneManager.LoadScene(menuSceneName);
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

        // Nút pause (góc trên phải)
        pauseButton = MakeButton(canvasGO.transform, "II", Vector2.zero, new Vector2(90f, 90f), Pause, 44);
        var prt = (RectTransform)pauseButton.transform;
        prt.anchorMin = prt.anchorMax = prt.pivot = new Vector2(1f, 1f);
        prt.anchoredPosition = pauseButtonPosition;
        pauseButton.gameObject.SetActive(showPauseButton);

        // Màn hình tạm dừng
        var panelRt = NewRect("PausePanel", canvasGO.transform);
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = panelRt.offsetMax = Vector2.zero;
        var dim = panelRt.gameObject.AddComponent<Image>();
        dim.color = dimColor; // vừa làm nền tối vừa chặn bấm xuyên xuống gameplay
        panel = panelRt.gameObject;

        MakeText(panelRt, "TẠM DỪNG", 96, new Vector2(0f, 230f), new Vector2(1200f, 200f));
        Vector2 size = new Vector2(560f, 110f);
        MakeButton(panelRt, "TIẾP TỤC", new Vector2(0f, 60f), size, Resume, 52);
        MakeButton(panelRt, "CHƠI LẠI", new Vector2(0f, -70f), size, Restart, 52);
        if (!string.IsNullOrEmpty(menuSceneName))
            MakeButton(panelRt, "MENU", new Vector2(0f, -200f), size, GoToMenu, 52);
        panel.SetActive(false);

        // Số đếm ngược
        countdownText = MakeText(canvasGO.transform, "", 260, Vector2.zero, new Vector2(600f, 400f));
        countdownText.gameObject.SetActive(false);
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

    Button MakeButton(Transform parent, string label, Vector2 pos, Vector2 size, UnityAction action, int fontSize)
    {
        var rt = NewRect(label + "Button", parent);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        var img = rt.gameObject.AddComponent<Image>();
        img.color = buttonColor;
        var btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(action);
        MakeText(rt, label, fontSize, Vector2.zero, size);
        return btn;
    }
}
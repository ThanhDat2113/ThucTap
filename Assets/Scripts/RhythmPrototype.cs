using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Prototype game nhịp điệu kiểu FNF: 4 lane, màn hình ngang, note rơi từ trên xuống.
/// Cách dùng: tạo Empty GameObject trong scene rỗng, gắn script này, bấm Play.
/// Phím: D F J K hoặc các phím mũi tên (Trái, Xuống, Lên, Phải). R để chơi lại.
/// </summary>
public class RhythmPrototype : MonoBehaviour
{
    // ---------------- Cài đặt (chỉnh trong Inspector) ----------------
    [Header("Layout")]
    public float laneSpacing = 1.8f;
    public float hitLineY = -3.3f;
    public float scrollSpeed = 7f;          // đơn vị world / giây

    [Header("Timing window (giây)")]
    public float perfectWindow = 0.045f;
    public float goodWindow = 0.09f;
    public float missWindow = 0.14f;        // trễ quá mốc này thì tính MISS

    [Header("Chart thử nghiệm")]
    public float bpm = 120f;
    public int noteCount = 50;
    public float startTime = 0.5f;          // thời điểm note đầu tiên (giây)
    public int seed = 1;

    // ---------------- Dữ liệu ----------------
    class Note
    {
        public float time;                  // thời điểm phải bấm (giây)
        public int lane;
        public Transform tf;
        public SpriteRenderer sr;
        public bool judged;                 // đã hit hoặc đã miss
        public Note(float t, int l) { time = t; lane = l; }
    }

    enum State { Countdown, Playing, Finished }

    static readonly Color[] LaneColors =
    {
        new Color(0.76f, 0.29f, 0.60f),     // trái  - tím
        new Color(0.00f, 1.00f, 1.00f),     // xuống - xanh lơ
        new Color(0.07f, 0.98f, 0.02f),     // lên   - xanh lá
        new Color(0.98f, 0.22f, 0.25f),     // phải  - đỏ
    };

    State state;
    float countdown;
    float songTime;                         // "đồng hồ bài hát"; sau này sẽ lấy từ audio
    float playElapsed;
    float travelTime;                       // thời gian note rơi từ mép trên tới vạch bấm
    float spawnY;

    readonly List<Note> chart = new List<Note>();
    readonly List<Note> active = new List<Note>();
    int nextSpawn;

    Sprite whiteSprite;
    SpriteRenderer[] receptors = new SpriteRenderer[4];
    Vector2 noteSize;

    int score, combo, maxCombo;
    string judgeText = "";
    Color judgeColor = Color.white;
    float judgeTimer;

    GUIStyle bigStyle, judgeStyle, hudStyle;

    // ---------------- Khởi tạo ----------------
    void Start()
    {
        SetupCamera();
        whiteSprite = MakeWhiteSprite();
        noteSize = new Vector2(laneSpacing * 0.85f, 0.5f);

        spawnY = Camera.main.orthographicSize + 0.5f;
        travelTime = (spawnY - hitLineY) / scrollSpeed;

        BuildLanes();
        StartRun();
    }

    void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            cam = go.AddComponent<Camera>();
        }
        cam.transform.position = new Vector3(0, 0, -10);
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.07f, 0.07f, 0.11f);
    }

    Sprite MakeWhiteSprite()
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        // pixelsPerUnit = 1 nên localScale chính là kích thước world
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }

    SpriteRenderer MakeQuad(string objName, Vector2 pos, Vector2 size, Color color, int order)
    {
        var go = new GameObject(objName);
        go.transform.SetParent(transform, false);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = whiteSprite;
        sr.color = color;
        sr.sortingOrder = order;
        return sr;
    }

    void BuildLanes()
    {
        float height = Camera.main.orthographicSize * 2f + 2f;
        for (int l = 0; l < 4; l++)
        {
            // nền lane
            MakeQuad("LaneBG_" + l, new Vector2(LaneX(l), 0), new Vector2(laneSpacing * 0.95f, height),
                     new Color(1, 1, 1, 0.05f), 0);
            // vạch bấm (receptor)
            receptors[l] = MakeQuad("Receptor_" + l, new Vector2(LaneX(l), hitLineY), noteSize,
                                    Color.white, 1);
        }
    }

    float LaneX(int lane) { return (lane - 1.5f) * laneSpacing; }

    // ---------------- Chart ----------------
    void BuildChart()
    {
        chart.Clear();
        var rng = new System.Random(seed);
        float beat = 60f / bpm;
        float t = startTime;
        int prev = -1;

        for (int i = 0; i < noteCount; i++)
        {
            int lane;
            do { lane = rng.Next(0, 4); } while (lane == prev);   // không lặp lane liên tiếp
            prev = lane;

            chart.Add(new Note(t, lane));
            t += rng.NextDouble() < 0.3 ? beat * 0.5f : beat;     // thỉnh thoảng có nốt dày hơn
        }

        // Muốn tự viết chart tay thì thay đoạn trên bằng:
        // chart.Add(new Note(1.0f, 0));
        // chart.Add(new Note(1.5f, 2));
        // ... (nhớ giữ danh sách theo thứ tự thời gian)
    }

    void StartRun()
    {
        foreach (var n in active)
            if (n.tf != null) Destroy(n.tf.gameObject);
        active.Clear();

        BuildChart();
        nextSpawn = 0;
        score = combo = maxCombo = 0;
        judgeText = "";
        countdown = 3f;
        state = State.Countdown;
    }

    // ---------------- Vòng lặp chính ----------------
    void Update()
    {
        judgeTimer -= Time.deltaTime;

        switch (state)
        {
            case State.Countdown: UpdateCountdown(); break;
            case State.Playing:   UpdatePlaying();   break;
        }

        if (RestartPressed()) StartRun();

        // receptor sáng lên khi giữ phím
        for (int l = 0; l < 4; l++)
        {
            Color dim = LaneColors[l] * 0.35f; dim.a = 1f;
            receptors[l].color = Color.Lerp(dim, LaneColors[l], LaneHeld(l) ? 1f : 0f);
        }
    }

    void UpdateCountdown()
    {
        countdown -= Time.deltaTime;
        if (countdown > 0f) return;

        state = State.Playing;
        playElapsed = 0f;
        // Cho note đầu tiên xuất hiện ngay ở mép trên khi vào game.
        // Khi có nhạc: nhạc bắt đầu phát ở songTime = 0.
        songTime = chart[0].time - travelTime;
    }

    void UpdatePlaying()
    {
        float dt = Time.deltaTime;
        songTime += dt;                     // prototype: sau này đổi sang thời gian audio (dspTime)
        playElapsed += dt;

        // 1) Spawn note sắp tới
        while (nextSpawn < chart.Count && chart[nextSpawn].time - songTime <= travelTime)
            Spawn(chart[nextSpawn++]);

        // 2) Xử lý input
        for (int l = 0; l < 4; l++)
            if (LaneDown(l)) TryHit(l);

        // 3) Cập nhật vị trí, xử lý miss, dọn note ra khỏi màn hình
        float killY = -Camera.main.orthographicSize - 1f;
        for (int i = active.Count - 1; i >= 0; i--)
        {
            Note n = active[i];

            if (!n.judged && songTime - n.time > missWindow)
            {
                n.judged = true;
                n.sr.color = new Color(0.4f, 0.4f, 0.4f, 0.6f);
                Judge("MISS", Color.red, 0, false);
            }

            float y = hitLineY + (n.time - songTime) * scrollSpeed;
            n.tf.position = new Vector3(LaneX(n.lane), y, 0);

            if (y < killY)
            {
                Destroy(n.tf.gameObject);
                active.RemoveAt(i);
            }
        }

        // 4) Hết bài
        if (nextSpawn >= chart.Count && active.Count == 0)
            state = State.Finished;
    }

    void Spawn(Note n)
    {
        n.sr = MakeQuad("Note", new Vector2(LaneX(n.lane), spawnY), noteSize, LaneColors[n.lane], 2);
        n.tf = n.sr.transform;
        active.Add(n);
    }

    // ---------------- Hit detection ----------------
    void TryHit(int lane)
    {
        // Lấy note chưa xử lý, sớm nhất trong lane này
        Note best = null;
        foreach (var n in active)
            if (n.lane == lane && !n.judged && (best == null || n.time < best.time))
                best = n;

        if (best == null) return;

        float diff = Mathf.Abs(songTime - best.time);
        if (diff > missWindow) return;      // bấm quá sớm: bỏ qua (ghost tap, không bị phạt)

        best.judged = true;
        Destroy(best.tf.gameObject);        // note biến mất
        active.Remove(best);

        if (diff <= perfectWindow)   Judge("PERFECT", Color.yellow, 300, true);
        else if (diff <= goodWindow) Judge("GOOD", Color.green, 100, true);
        else                         Judge("BAD", new Color(1f, 0.6f, 0.2f), 50, true);
    }

    void Judge(string text, Color color, int points, bool hit)
    {
        judgeText = text;
        judgeColor = color;
        judgeTimer = 0.5f;
        score += points;
        if (hit) { combo++; if (combo > maxCombo) maxCombo = combo; }
        else combo = 0;
    }

    // ---------------- Input (hỗ trợ cả Input cũ và Input System mới) ----------------
    bool LaneDown(int lane) { return Query(lane, true); }
    bool LaneHeld(int lane) { return Query(lane, false); }

    bool Query(int lane, bool pressedThisFrame)
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb == null) return false;
        var a = lane switch { 0 => kb.dKey, 1 => kb.fKey, 2 => kb.jKey, _ => kb.kKey };
        var b = lane switch { 0 => kb.leftArrowKey, 1 => kb.downArrowKey, 2 => kb.upArrowKey, _ => kb.rightArrowKey };
        return pressedThisFrame ? (a.wasPressedThisFrame || b.wasPressedThisFrame)
                                : (a.isPressed || b.isPressed);
#else
        KeyCode a = lane == 0 ? KeyCode.D : lane == 1 ? KeyCode.F : lane == 2 ? KeyCode.J : KeyCode.K;
        KeyCode b = lane == 0 ? KeyCode.LeftArrow : lane == 1 ? KeyCode.DownArrow
                  : lane == 2 ? KeyCode.UpArrow : KeyCode.RightArrow;
        return pressedThisFrame ? (Input.GetKeyDown(a) || Input.GetKeyDown(b))
                                : (Input.GetKey(a) || Input.GetKey(b));
#endif
    }

    bool RestartPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.R);
#endif
    }

    // ---------------- UI (IMGUI, không cần Canvas) ----------------
    void OnGUI()
    {
        if (bigStyle == null)
        {
            bigStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            judgeStyle = new GUIStyle(bigStyle);
            hudStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperLeft, fontStyle = FontStyle.Bold };
        }
        bigStyle.fontSize = Screen.height / 5;
        judgeStyle.fontSize = Screen.height / 14;
        hudStyle.fontSize = Screen.height / 28;

        var full = new Rect(0, 0, Screen.width, Screen.height);

        // Đếm ngược 3 2 1 và chữ GO!
        if (state == State.Countdown)
        {
            bigStyle.normal.textColor = Color.white;
            GUI.Label(full, Mathf.CeilToInt(countdown).ToString(), bigStyle);
        }
        else if (state == State.Playing && playElapsed < 0.6f)
        {
            bigStyle.normal.textColor = Color.yellow;
            GUI.Label(full, "GO!", bigStyle);
        }
        else if (state == State.Finished)
        {
            bigStyle.fontSize = Screen.height / 9;
            bigStyle.normal.textColor = Color.white;
            GUI.Label(full, "XONG!\nNhan R de choi lai", bigStyle);
        }

        // Judgement
        if (judgeTimer > 0f && state != State.Countdown)
        {
            var c = judgeColor; c.a = Mathf.Clamp01(judgeTimer / 0.3f);
            judgeStyle.normal.textColor = c;
            GUI.Label(new Rect(0, Screen.height * 0.30f, Screen.width, Screen.height * 0.15f), judgeText, judgeStyle);
        }

        // HUD
        hudStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(20, 10, 600, Screen.height / 4f),
                  "Score: " + score + "\nCombo: " + combo + "  (max " + maxCombo + ")", hudStyle);
    }
}
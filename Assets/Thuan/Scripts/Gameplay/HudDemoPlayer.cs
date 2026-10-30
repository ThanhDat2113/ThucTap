using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// CHE DO CHOI THU de test UI gameplay (not tu sinh theo BPM, khong khop nhac that).
// Khi gameplay that (NoteSpawner/JudgementSystem...) duoc gop vao: tat component nay
// va goi cac ham public cua GameplayHUD tu gameplay that.
public class HudDemoPlayer : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameplayHUD hud;
    [SerializeField] private RectTransform noteLayer;
    [SerializeField] private RectTransform[] laneTops = new RectTransform[4];
    [SerializeField] private Sprite[] noteSprites = new Sprite[4];
    [SerializeField] private float[] noteRotations = { 0f, 90f, -90f, 180f };
    [SerializeField] private Vector2 noteSize = new Vector2(150f, 128f);

    [Header("Not giu (not dai)")]
    [SerializeField] private Sprite holdTailSprite;
    [SerializeField] private Color[] laneColors = { new Color(0.76f, 0.24f, 1f), new Color(0.13f, 0.67f, 1f), new Color(0.16f, 0.9f, 0.4f), new Color(1f, 0.23f, 0.36f) };
    [SerializeField] private float holdTailWidth = 64f;
    [Tooltip("Ty le not giu theo do kho: Easy / Normal / Hard")]
    [SerializeField] private Vector3 holdChance = new Vector3(0.12f, 0.18f, 0.22f);
    [Tooltip("Nha phim som hon cuoi duoi bao nhieu giay thi van tinh la giu du")]
    [SerializeField] private float releaseGrace = 0.12f;

    [Header("Bai hat")]
    [SerializeField] private SongData fallbackSong;
    [SerializeField] private float fallbackLength = 90f;
    [SerializeField] private string songListScene = "Thuan_SongList";
    [Tooltip("Tu dong bam dung nhip (de xem thu hieu ung PERFECT / combo)")]
    [SerializeField] private bool autoPlay;

    [Header("Timing")]
    [SerializeField] private float approachTime = 1.45f;
    [SerializeField] private float firstNoteTime = 2.5f;
    [SerializeField] private float perfectWindow = 0.06f;
    [SerializeField] private float goodWindow = 0.12f;
    [SerializeField] private float badWindow = 0.18f;

    [Header("HP")]
    [SerializeField] private float missDamage = 0.08f;
    [SerializeField] private float perfectHeal = 0.02f;
    [SerializeField] private float opponentDamage = 0.012f;

    private class Note
    {
        public float time, duration;
        public int lane;
        public RectTransform rt;
        public Image img;
        public Image tail;
        public bool done;      // dau not da xu ly (trung hoac truot)
        public bool holding;   // dang giu
        public bool broken;    // truot / nha som -> mo dan
        public float tickTimer;
        public bool IsHold { get { return duration > 0f; } }
        public float End { get { return time + duration; } }
    }

    private readonly List<Note> chart = new List<Note>();
    private readonly List<Note> active = new List<Note>();
    private readonly Stack<Image> pool = new Stack<Image>();
    private readonly Stack<Image> tailPool = new Stack<Image>();
    private RectTransform tailLayer;
    private readonly float[] laneBusyUntil = new float[4];
    private int nextIndex;
    private AudioSource audioSource;
    private float clock, songLength;
    private bool started, finished;
    private int score, combo, maxCombo;
    private int judged;
    private int perfectCount, goodCount, badCount, missCount;
    private float accuracyPoints;
    private float playerHp = 1f, opponentHp = 1f;
    private readonly bool[] held = new bool[4];
    private readonly bool[] pointerHeld = new bool[4];
    private readonly float[] autoHold = new float[4];

    private void Start()
    {
        SongData song = GameSession.SelectedSong != null ? GameSession.SelectedSong : fallbackSong;
        GameSession.SelectedSong = song; // de man tong ket / luu diem biet dang choi bai nao
        if (song != null && MusicManager.Instance != null)
        {
            MusicManager.Instance.PlaySongFromStart(song);
            audioSource = MusicManager.Instance.GetComponent<AudioSource>();
        }
        songLength = song != null && song.clip != null ? song.clip.length : fallbackLength;

        string diff = GameSession.SelectedDifficulty.ToString();
        string artist = song != null ? song.artist : "Rhythm Battle";
        if (StorySession.Active) artist += "   |   WEEK " + StorySession.Week.weekNumber + "  " + (StorySession.TrackIndex + 1) + "/" + StorySession.TrackCount;
        hud.SetSongInfo(song != null ? song.songName : "Demo Track", artist, diff);
        hud.PauseChanged += OnPause;
        hud.RestartRequested += () => SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        hud.QuitRequested += () => { if (audioSource != null) audioSource.Stop(); SceneManager.LoadScene(songListScene); };
        foreach (var r in hud.Receptors) if (r != null) r.PointerPress += (lane, down) => pointerHeld[lane] = down;

        CreateTailLayer();
        BuildChart(song);
        started = true;
    }

    private void BuildChart(SongData song)
    {
        float bpm = song != null && song.bpm > 0 ? song.bpm : 110f;
        float beat = 60f / bpm;
        var rnd = new System.Random(song != null && song.songName != null ? song.songName.GetHashCode() : 7);
        var d = GameSession.SelectedDifficulty;
        float density = d == ChartDifficulty.Easy ? 0.45f : d == ChartDifficulty.Normal ? 0.8f : 1f;
        bool halfBeats = d == ChartDifficulty.Hard;
        float holdP = d == ChartDifficulty.Easy ? holdChance.x : d == ChartDifficulty.Normal ? holdChance.y : holdChance.z;
        int lastLane = -1;
        for (float t = firstNoteTime; t < songLength - 4f; t += beat)
        {
            if (rnd.NextDouble() < density) AddNote(t, ref lastLane, rnd, holdP, beat);
            if (halfBeats && rnd.NextDouble() < 0.3) AddNote(t + beat * 0.5f, ref lastLane, rnd, 0f, beat);
        }
    }

    private void AddNote(float t, ref int lastLane, System.Random rnd, float holdP, float beat)
    {
        // chon lan con trong (khong dang co not giu), uu tien khac lan truoc
        var free = new List<int>();
        for (int l = 0; l < 4; l++) if (laneBusyUntil[l] + 0.15f <= t && l != lastLane) free.Add(l);
        if (free.Count == 0) return;
        int lane = free[rnd.Next(free.Count)];
        lastLane = lane;
        float dur = 0f;
        if (rnd.NextDouble() < holdP)
        {
            float[] beats = { 1f, 1.5f, 2f, 3f };
            dur = beats[rnd.Next(beats.Length)] * beat;
        }
        laneBusyUntil[lane] = t + dur;
        chart.Add(new Note { time = t, lane = lane, duration = dur });
    }

    // Lay thoi gian theo nhac neu nhac dang chay; neu nhac tam ngung bat thuong thi chay tiep bang dong ho rieng
    private float SongTime
    {
        get
        {
            if (audioSource != null && audioSource.clip != null && audioSource.isPlaying && audioSource.time > 0f) clock = audioSource.time;
            return clock;
        }
    }

    private void OnPause(bool paused)
    {
        if (audioSource == null) return;
        if (paused) audioSource.Pause(); else audioSource.UnPause();
    }

    private void Update()
    {
        if (!started || finished) return;
        if (EscapePressed()) hud.SetPaused(!hud.IsPaused);
        if (hud.IsPaused) return;

        clock += Time.deltaTime;
        float now = SongTime;
        hud.SetProgress(now, songLength);

        while (nextIndex < chart.Count && chart[nextIndex].time - now <= approachTime)
        {
            var n = chart[nextIndex++];
            n.img = GetImage(n.lane);
            n.rt = n.img.rectTransform;
            if (n.IsHold) n.tail = GetTail(n.lane);
            active.Add(n);
        }

        if (autoPlay)
        {
            foreach (var n in active)
                if (!n.done && n.time - now <= 0.01f) { autoHold[n.lane] = n.IsHold ? n.duration + 0.05f : 0.1f; held[n.lane] = true; Press(n.lane, n.time); }
            for (int l = 0; l < 4; l++) autoHold[l] -= Time.deltaTime;
        }

        for (int lane = 0; lane < 4; lane++)
        {
            bool down = KeyHeld(lane) || pointerHeld[lane] || autoHold[lane] > 0f;
            if (down && !held[lane]) Press(lane, now);
            held[lane] = down;
            hud.SetLanePressed(lane, down);
        }

        for (int i = active.Count - 1; i >= 0; i--)
        {
            var n = active[i];
            float dt = n.time - now;
            if (!n.done && dt < -badWindow)
            {
                n.done = true; n.broken = true;
                Judge(HudJudgement.Miss, n.lane); if (finished) return;
            }
            if (n.holding)
            {
                bool down = held[n.lane];
                if (now >= n.End)
                {
                    n.holding = false; // giu du -> thuong them 1 lan cham
                    var c = n.img.color; c.a = 0f; n.img.color = c;
                    Judge(HudJudgement.Perfect, n.lane); if (finished) return;
                }
                else if (!down && now < n.End - releaseGrace)
                {
                    n.holding = false; n.broken = true; // nha som
                    Judge(HudJudgement.Miss, n.lane); if (finished) return;
                }
                else
                {
                    n.tickTimer -= Time.deltaTime;
                    if (n.tickTimer <= 0f) { n.tickTimer = 0.12f; score += 20; hud.SetScore(score); hud.PlayLaneHit(n.lane); }
                }
            }
            float t = n.holding ? 1f : 1f - dt / approachTime;
            bool gone = n.IsHold
                ? (n.done && !n.holding && (n.broken ? n.img.color.a <= 0.01f && (n.tail == null || n.tail.color.a <= 0.01f) : true)) || 1f - (n.End - now) / approachTime > 1.3f
                : t > 1.25f || (n.done && n.img.color.a <= 0.01f);
            if (gone) { Recycle(n); active.RemoveAt(i); continue; }
            Place(n, t, now);
        }

        if (now >= songLength - 0.1f || (nextIndex >= chart.Count && active.Count == 0 && now > songLength - 2f)) Finish(true);
    }

    private Vector3 LanePoint(int lane, float t, out float scale)
    {
        Vector3 top = noteLayer.InverseTransformPoint(laneTops[lane].position);
        Vector3 hit = noteLayer.InverseTransformPoint(hud.Receptors[lane].transform.position);
        float p = Mathf.Pow(Mathf.Max(0f, t), 1.6f);
        scale = Mathf.LerpUnclamped(0.3f, 1f, p);
        return Vector3.LerpUnclamped(top, hit, p);
    }

    private void Place(Note n, float t, float now)
    {
        float s;
        Vector3 pos = LanePoint(n.lane, t, out s);
        n.rt.anchoredPosition = pos;
        n.rt.localScale = new Vector3(s, s, 1f);
        float a = Mathf.Clamp01(t / 0.12f);
        if (n.done && !n.holding) a = Mathf.Clamp01(n.img.color.a - Time.deltaTime * (n.broken ? 3f : 6f));
        var c = n.img.color; c.a = a; n.img.color = c;
        if (n.holding) n.img.color = new Color(1.2f, 1.2f, 1.2f, 1f);

        if (n.tail == null) return;
        // duoi not: tu dau not keo len diem ket thuc
        float tEnd = Mathf.Max(0f, 1f - (n.End - now) / approachTime);
        float se;
        Vector3 endPos = LanePoint(n.lane, tEnd, out se);
        Vector3 a0 = tailLayer.InverseTransformPoint(noteLayer.TransformPoint(pos));
        Vector3 a1 = tailLayer.InverseTransformPoint(noteLayer.TransformPoint(endPos));
        Vector3 d = a1 - a0;
        var trt = n.tail.rectTransform;
        trt.anchoredPosition = a0;
        trt.sizeDelta = new Vector2(holdTailWidth * (s + se) * 0.5f, Mathf.Max(0f, d.magnitude));
        trt.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f);
        Color lc = laneColors[n.lane];
        float ta;
        if (n.broken) { lc = Color.Lerp(lc, Color.gray, 0.7f); ta = Mathf.Clamp01(n.tail.color.a - Time.deltaTime * 3f); }
        else ta = Mathf.Clamp01(t / 0.12f);
        if (n.holding) lc = Color.Lerp(lc, Color.white, 0.35f + 0.15f * Mathf.Sin(Time.time * 25f));
        lc.a = ta;
        n.tail.color = lc;
    }

    private void Press(int lane, float now)
    {
        Note best = null; float bestAbs = float.MaxValue;
        foreach (var n in active)
        {
            if (n.done || n.lane != lane) continue;
            float d = Mathf.Abs(n.time - now);
            if (d < bestAbs) { bestAbs = d; best = n; }
        }
        if (best == null || bestAbs > badWindow) return;
        best.done = true;
        if (best.IsHold) { best.holding = true; best.tickTimer = 0.12f; }
        else { var col = best.img.color; col.a = 0f; best.img.color = col; }
        HudJudgement j = bestAbs <= perfectWindow ? HudJudgement.Perfect : bestAbs <= goodWindow ? HudJudgement.Good : HudJudgement.Bad;
        Judge(j, lane);
    }

    private void Judge(HudJudgement j, int lane)
    {
        judged++;
        if (j == HudJudgement.Perfect) perfectCount++;
        else if (j == HudJudgement.Good) goodCount++;
        else if (j == HudJudgement.Bad) badCount++;
        else missCount++;
        accuracyPoints += j == HudJudgement.Perfect ? 1f : j == HudJudgement.Good ? 0.7f : j == HudJudgement.Bad ? 0.3f : 0f;
        if (j == HudJudgement.Miss)
        {
            combo = 0;
            playerHp -= missDamage;
        }
        else
        {
            combo++;
            maxCombo = Mathf.Max(maxCombo, combo);
            int basePts = j == HudJudgement.Perfect ? 300 : j == HudJudgement.Good ? 150 : 50;
            score += basePts + Mathf.Min(combo, 100) * 5;
            if (j == HudJudgement.Perfect) playerHp += perfectHeal;
            opponentHp -= j == HudJudgement.Perfect ? opponentDamage : j == HudJudgement.Good ? opponentDamage * 0.6f : opponentDamage * 0.2f;
            hud.PlayLaneHit(lane);
        }
        playerHp = Mathf.Clamp01(playerHp);
        opponentHp = Mathf.Clamp(opponentHp, 0.05f, 1f);
        hud.SetScore(score);
        hud.SetCombo(combo);
        hud.ShowJudgement(j, lane);
        hud.SetPlayerHealth(playerHp);
        hud.SetOpponentHealth(opponentHp);
        if (playerHp <= 0f) Finish(false);
    }

    private void Finish(bool cleared)
    {
        if (finished) return;
        finished = true;
        if (!cleared && audioSource != null) audioSource.Stop();
        foreach (var n in active) Recycle(n);
        active.Clear();
        for (int i = 0; i < 4; i++) hud.SetLanePressed(i, false);
        float accuracy = judged > 0 ? accuracyPoints / judged * 100f : -1f;
        hud.ShowResult(cleared, score, maxCombo, accuracy, perfectCount, goodCount, badCount, missCount);
    }

    private Image GetImage(int lane)
    {
        Image img = pool.Count > 0 ? pool.Pop() : CreateImage();
        img.gameObject.SetActive(true);
        img.sprite = noteSprites[lane];
        img.rectTransform.localEulerAngles = new Vector3(0, 0, noteRotations[lane]);
        img.color = new Color(1, 1, 1, 0);
        return img;
    }

    private Image CreateImage()
    {
        var go = new GameObject("Note", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(noteLayer, false);
        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        img.preserveAspect = true;
        img.rectTransform.sizeDelta = noteSize;
        return img;
    }

    private void Recycle(Note n)
    {
        if (n.tail != null) { n.tail.gameObject.SetActive(false); tailPool.Push(n.tail); n.tail = null; }
        if (n.img == null) return;
        n.img.gameObject.SetActive(false);
        pool.Push(n.img);
        n.img = null;
    }

    private void CreateTailLayer()
    {
        var go = new GameObject("HoldTailLayer", typeof(RectTransform));
        tailLayer = go.GetComponent<RectTransform>();
        tailLayer.SetParent(noteLayer.parent, false);
        tailLayer.anchorMin = noteLayer.anchorMin; tailLayer.anchorMax = noteLayer.anchorMax;
        tailLayer.pivot = noteLayer.pivot; tailLayer.anchoredPosition = noteLayer.anchoredPosition; tailLayer.sizeDelta = noteLayer.sizeDelta;
        tailLayer.SetSiblingIndex(noteLayer.GetSiblingIndex()); // nam duoi dau not
    }

    private Image GetTail(int lane)
    {
        Image img;
        if (tailPool.Count > 0) img = tailPool.Pop();
        else
        {
            var go = new GameObject("HoldTail", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(tailLayer, false);
            img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.sprite = holdTailSprite;
            img.rectTransform.pivot = new Vector2(0.5f, 0f);
        }
        img.gameObject.SetActive(true);
        var c = laneColors[lane]; c.a = 0f; img.color = c;
        return img;
    }

    private static bool KeyHeld(int lane)
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current; if (kb == null) return false;
        switch (lane)
        {
            case 0: return kb.dKey.isPressed || kb.leftArrowKey.isPressed;
            case 1: return kb.fKey.isPressed || kb.downArrowKey.isPressed;
            case 2: return kb.jKey.isPressed || kb.upArrowKey.isPressed;
            default: return kb.kKey.isPressed || kb.rightArrowKey.isPressed;
        }
#else
        switch (lane)
        {
            case 0: return Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.LeftArrow);
            case 1: return Input.GetKey(KeyCode.F) || Input.GetKey(KeyCode.DownArrow);
            case 2: return Input.GetKey(KeyCode.J) || Input.GetKey(KeyCode.UpArrow);
            default: return Input.GetKey(KeyCode.K) || Input.GetKey(KeyCode.RightArrow);
        }
#endif
    }

    private static bool EscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }
}

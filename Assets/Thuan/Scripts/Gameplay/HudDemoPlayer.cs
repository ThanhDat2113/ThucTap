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

    private class Note { public float time; public int lane; public RectTransform rt; public Image img; public bool done; }

    private readonly List<Note> chart = new List<Note>();
    private readonly List<Note> active = new List<Note>();
    private readonly Stack<Image> pool = new Stack<Image>();
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
        int lastLane = -1;
        for (float t = firstNoteTime; t < songLength - 3f; t += beat)
        {
            if (rnd.NextDouble() < density) AddNote(t, ref lastLane, rnd);
            if (halfBeats && rnd.NextDouble() < 0.3) AddNote(t + beat * 0.5f, ref lastLane, rnd);
        }
    }

    private void AddNote(float t, ref int lastLane, System.Random rnd)
    {
        int lane = rnd.Next(4);
        if (lane == lastLane) lane = (lane + 1 + rnd.Next(3)) % 4;
        lastLane = lane;
        chart.Add(new Note { time = t, lane = lane });
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
            active.Add(n);
        }

        if (autoPlay)
        {
            foreach (var n in active)
                if (!n.done && n.time - now <= 0.01f) { autoHold[n.lane] = 0.1f; held[n.lane] = true; Press(n.lane, n.time); }
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
            if (!n.done && dt < -badWindow) { n.done = true; Judge(HudJudgement.Miss, n.lane); if (finished) return; }
            float t = 1f - dt / approachTime;
            if (t > 1.25f || (n.done && n.img.color.a <= 0.01f)) { Recycle(n); active.RemoveAt(i); continue; }
            Place(n, t);
        }

        if (now >= songLength - 0.1f || (nextIndex >= chart.Count && active.Count == 0 && now > songLength - 2f)) Finish(true);
    }

    private void Place(Note n, float t)
    {
        Vector3 top = noteLayer.InverseTransformPoint(laneTops[n.lane].position);
        Vector3 hit = noteLayer.InverseTransformPoint(hud.Receptors[n.lane].transform.position);
        float p = Mathf.Pow(Mathf.Max(0f, t), 1.6f);
        Vector3 pos = Vector3.LerpUnclamped(top, hit, p);
        float s = Mathf.LerpUnclamped(0.3f, 1f, p);
        n.rt.anchoredPosition = pos;
        n.rt.localScale = new Vector3(s, s, 1f);
        float a = Mathf.Clamp01(t / 0.12f);
        if (n.done) a = Mathf.Clamp01(n.img.color.a - Time.deltaTime * 6f);
        var c = n.img.color; c.a = a; n.img.color = c;
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
        var col = best.img.color; col.a = 0f; best.img.color = col;
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
        if (n.img == null) return;
        n.img.gameObject.SetActive(false);
        pool.Push(n.img);
        n.img = null;
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

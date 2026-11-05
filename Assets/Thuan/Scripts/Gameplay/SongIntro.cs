using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Man THONG TIN BAI HAT hien truoc khi vao gameplay:
// bia bai + khung, ten bai tren bang ron, tac gia ben duoi, huy hieu do kho.
// Gameplay goi: yield return SongIntro.Run(song);  (khong co SongIntro trong scene thi bo qua)
public class SongIntro : MonoBehaviour
{
    public static SongIntro Instance { get; private set; }
    public bool IsPlaying { get; private set; }

    [Header("Khung 1920x1080 (tu co gian phu kin man hinh)")]
    [SerializeField] private RectTransform stage;
    [SerializeField] private CanvasGroup group;
    [SerializeField] private RectTransform background;

    [Header("Bia bai")]
    [SerializeField] private RectTransform coverRoot;
    [SerializeField] private Image cover;
    [SerializeField] private Sprite defaultCover;

    [Header("Ten bai + tac gia")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text creditsText;

    [Header("Huy hieu do kho")]
    [SerializeField] private RectTransform badge;
    [SerializeField] private TMP_Text badgeText;

    [Header("Not nhac / vuong mien bay lo lung")]
    [SerializeField] private RectTransform[] floaters;

    [Header("Thoi gian (giay)")]
    [SerializeField] private float holdTime = 2.4f;
    [SerializeField] private float minTimeBeforeSkip = 0.8f;
    [SerializeField] private AudioClip whooshClip;
    [SerializeField] private AudioClip stampClip;

    private Vector2[] floaterBase;
    private bool skipRequested;
    private bool used;

    private void Awake()
    {
        Instance = this;
        KeepOnTop();
        // che man ngay tu frame dau (khong de lo gameplay khi scene vua load)
        if (stage != null) stage.gameObject.SetActive(true);
        if (group != null) { group.alpha = 1f; group.blocksRaycasts = true; }
        HideElements();
        FitStage();
    }

    private void Start()
    {
        // scene nao co SongIntro nhung gameplay khong goi Run() thi tu tat de khong che man
        StartCoroutine(AutoHideIfUnused());
    }

    private IEnumerator AutoHideIfUnused()
    {
        yield return null;
        yield return null;
        if (!IsPlaying && !used) { stage.gameObject.SetActive(false); group.blocksRaycasts = false; }
    }

    private void HideElements()
    {
        SetScale(background, 1.08f);
        SetScale(coverRoot, 0.55f);
        SetAlpha(coverRoot, 0f);
        SetAlpha(titleText, 0f);
        SetAlpha(creditsText, 0f);
        SetAlpha(badge, 0f);
        if (floaters != null) foreach (var f in floaters) SetAlpha(f, 0f);
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }

    // Canvas co the bi doi sang Screen Space - Camera (AspectRatioEnforcer),
    // nen dat len sorting layer cao nhat de che ca nhan vat (SpriteRenderer)
    private void KeepOnTop()
    {
        var canvas = GetComponent<Canvas>();
        if (canvas == null) return;
        var layers = SortingLayer.layers;
        if (layers.Length > 0) canvas.sortingLayerID = layers[layers.Length - 1].id;
        canvas.sortingOrder = 500;
    }

    // Goi tu gameplay: neu scene khong co SongIntro thi tra ve ngay
    public static IEnumerator Run(SongData song)
    {
        if (Instance == null) yield break;
        yield return Instance.Play(song);
    }

    public IEnumerator Play(SongData song)
    {
        IsPlaying = true;
        used = true;
        skipRequested = false;
        Fill(song);
        KeepOnTop();
        stage.gameObject.SetActive(true);
        group.blocksRaycasts = true;
        FitStage();

        floaterBase = new Vector2[floaters != null ? floaters.Length : 0];
        for (int i = 0; i < floaterBase.Length; i++) if (floaters[i] != null) floaterBase[i] = floaters[i].anchoredPosition;

        // trang thai bat dau: nen hien san, cac phan tu an
        group.alpha = 1f;
        HideElements();
        var titleBase = titleText.rectTransform.anchoredPosition;
        var creditBase = creditsText.rectTransform.anchoredPosition;
        titleText.maxVisibleCharacters = 0;

        // nap san nhac de luc vao gameplay khong bi khung
        if (song != null && song.clip != null && song.clip.loadState == AudioDataLoadState.Unloaded) song.clip.LoadAudioData();

        // cho scene load xong han (frame on dinh) roi moi chay animation, tranh bi giat mat hieu ung
        float waited = 0f;
        int stable = 0;
        while (stable < 4 && waited < 2f)
        {
            float d = Time.unscaledDeltaTime;
            waited += d;
            stable = d < 0.05f ? stable + 1 : 0;
            yield return null;
        }

        Play(whooshClip);
        float t = 0f;
        bool stamped = false;
        float total = 1.1f + holdTime;
        while (t < total)
        {
            // gioi han buoc thoi gian moi frame: frame nao bi giat thi animation cham lai chu khong nhay coc
            t += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
            if (t > minTimeBeforeSkip && SkipPressed()) skipRequested = true;
            if (skipRequested) break;

            SetScale(background, Mathf.Lerp(1.08f, 1f, EaseOut(t / 0.9f)));

            // bia bat ra (vuot qua roi ve 1)
            float c = Mathf.Clamp01((t - 0.15f) / 0.45f);
            SetScale(coverRoot, Mathf.LerpUnclamped(0.55f, 1f, EaseOutBack(c)));
            SetAlpha(coverRoot, Mathf.Clamp01(c * 2.5f));

            // ten bai truot vao + hien tung chu
            float ti = Mathf.Clamp01((t - 0.4f) / 0.4f);
            titleText.rectTransform.anchoredPosition = titleBase + new Vector2(-90f * (1f - EaseOut(ti)), 0f);
            SetAlpha(titleText, ti);
            titleText.maxVisibleCharacters = Mathf.CeilToInt(titleText.textInfo.characterCount * EaseOut(Mathf.Clamp01((t - 0.4f) / 0.5f))) + (ti >= 1f ? 999 : 0);

            // tac gia hien tu duoi len
            float cr = Mathf.Clamp01((t - 0.65f) / 0.35f);
            creditsText.rectTransform.anchoredPosition = creditBase + new Vector2(0f, -25f * (1f - EaseOut(cr)));
            SetAlpha(creditsText, cr);

            // huy hieu dong dau
            float b = Mathf.Clamp01((t - 0.8f) / 0.22f);
            SetAlpha(badge, b);
            SetScale(badge, Mathf.Lerp(2.2f, 1f, EaseOut(b)));
            if (b >= 1f && !stamped) { stamped = true; Play(stampClip); StartCoroutine(Shake(coverRoot, 0.18f, 7f)); }

            // not nhac lo lung
            for (int i = 0; i < floaterBase.Length; i++)
            {
                if (floaters[i] == null) continue;
                float fa = Mathf.Clamp01((t - 0.3f - i * 0.07f) / 0.35f);
                SetAlpha(floaters[i], fa);
                floaters[i].anchoredPosition = floaterBase[i] + new Vector2(0f, Mathf.Sin(t * 2.2f + i * 1.1f) * 10f + (1f - EaseOut(fa)) * -30f);
                floaters[i].localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(t * 1.6f + i) * 6f);
            }
            yield return null;
        }

        // hien day du truoc khi tat (truong hop bo qua giua chung)
        titleText.maxVisibleCharacters = 99999;
        titleText.rectTransform.anchoredPosition = titleBase;
        creditsText.rectTransform.anchoredPosition = creditBase;

        // mo dan + phong to nhe
        float o = 0f;
        float startAlpha = group.alpha;
        while (o < 0.4f)
        {
            o += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
            float k = Mathf.Clamp01(o / 0.4f);
            group.alpha = Mathf.Lerp(startAlpha, 0f, k);
            SetScale(stage, StageScale() * (1f + 0.06f * k));
            yield return null;
        }
        for (int i = 0; i < floaterBase.Length; i++) if (floaters[i] != null) floaters[i].anchoredPosition = floaterBase[i];
        group.blocksRaycasts = false;
        stage.gameObject.SetActive(false);
        IsPlaying = false;
    }

    private void Fill(SongData song)
    {
        string name = song != null && !string.IsNullOrEmpty(song.songName) ? song.songName : "Demo Track";
        titleText.text = name;
        titleText.ForceMeshUpdate();
        cover.sprite = song != null && song.coverArt != null ? song.coverArt : defaultCover;
        cover.enabled = cover.sprite != null;

        var sb = new System.Text.StringBuilder();
        if (song != null)
        {
            if (!string.IsNullOrEmpty(song.lyricist)) sb.Append("LYRICS : ").Append(song.lyricist).Append('\n');
            if (!string.IsNullOrEmpty(song.composer)) sb.Append("MUSIC : ").Append(song.composer).Append('\n');
            if (!string.IsNullOrEmpty(song.arranger)) sb.Append("ARRANGE : ").Append(song.arranger).Append('\n');
            if (sb.Length == 0 && !string.IsNullOrEmpty(song.artist)) sb.Append("ARTIST : ").Append(song.artist);
        }
        creditsText.text = sb.ToString().TrimEnd('\n');

        badgeText.text = StorySession.Active ? "STORY" : GameSession.SelectedDifficulty.ToString().ToUpperInvariant();
    }

    private void Update()
    {
        if (stage != null && stage.gameObject.activeSelf && (IsPlaying || !used)) FitStage();
    }

    // Phu kin man hinh (giong "Envelope"), giu ti le 16:9 cua anh mau
    private float StageScale()
    {
        var parent = stage.parent as RectTransform;
        if (parent == null) return 1f;
        var size = parent.rect.size;
        if (size.x < 2f || size.y < 2f) return 1f;
        return Mathf.Max(size.x / 1920f, size.y / 1080f);
    }

    private void FitStage()
    {
        if (stage == null) return;
        float s = StageScale();
        if (stage.localScale.x < s * 0.999f || stage.localScale.x > s * 1.07f) stage.localScale = new Vector3(s, s, 1f);
    }

    private static bool SkipPressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
        return false;
#else
        return Input.anyKeyDown;
#endif
    }

    private IEnumerator Shake(RectTransform rt, float time, float power)
    {
        var basePos = rt.anchoredPosition;
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            float k = 1f - t / time;
            rt.anchoredPosition = basePos + Random.insideUnitCircle * power * k;
            yield return null;
        }
        rt.anchoredPosition = basePos;
    }

    private static float EaseOut(float x) { x = Mathf.Clamp01(x); return 1f - Mathf.Pow(1f - x, 3f); }
    private static float EaseOutBack(float x)
    {
        x = Mathf.Clamp01(x);
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    private static void SetScale(Transform t, float s) { if (t != null) t.localScale = new Vector3(s, s, 1f); }

    private static void SetAlpha(Component c, float a)
    {
        if (c == null) return;
        var cg = c.GetComponent<CanvasGroup>();
        if (cg != null) { cg.alpha = a; return; }
        var g = c.GetComponent<Graphic>();
        if (g != null) { var col = g.color; col.a = a; g.color = col; }
    }

    private static void Play(AudioClip clip)
    {
        if (clip != null && SFXManager.Instance != null) SFXManager.Instance.PlaySFX(clip);
    }
}

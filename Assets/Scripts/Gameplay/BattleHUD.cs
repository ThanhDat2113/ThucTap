using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD gameplay dựng hoàn toàn bằng code (không cần sprite), theo style neon hồng/cyan của màn Song List.
/// Gắn vào 1 object stretch full dưới Canvas, rồi gọi SetHealth / SetFlow / SetScore / SetCombo / SetAccuracy.
/// Mọi phần tử đều tắt Raycast Target nên không chặn thao tác chạm trên Android.
/// </summary>
public class BattleHUD : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset displayFont;

    [Header("Vị trí (hệ 1920x1080)")]
    [Tooltip("Khoảng cách từ góc trên-trái màn hình tới cụm HP / Flow / Điểm / Combo / Hạng")]
    [SerializeField] private Vector2 hudOrigin = new Vector2(40f, 30f);

    [Header("Hiệu ứng")]
    [SerializeField] private float smooth = 10f;
    [SerializeField] private float trailDelay = 0.35f;
    [SerializeField, Range(0f, 1f)] private float lowHealth = 0.25f;
    [Tooltip("Vệt trắng mờ hiện phần máu vừa mất")]
    [SerializeField] private bool showDamageTrail = false;
    [Tooltip("Vạch sáng nhỏ ở đầu thanh HP / FLOW")]
    [SerializeField] private bool showBarTip = true;

    private static readonly Color Pink = Hex("FF2E88");
    private static readonly Color Cyan = Hex("22D9FF");
    private static readonly Color Yellow = Hex("FFD21C");
    private static readonly Color Red = Hex("FF365D");
    private static readonly Color Purple = Hex("B455FF");
    private static readonly Color Dark = new Color(0.06f, 0.015f, 0.14f, 0.90f); // tím đậm, hợp nền synthwave

    private const float SlantRatio = 0.30f; // độ nghiêng cố định theo chiều cao -> mọi khung song song nhau
    private readonly Dictionary<string, Sprite> slantCache = new Dictionary<string, Sprite>();
    private RectTransform root, comboRoot, rankRoot;
    private CanvasGroup comboGroup;
    private Image healthFill, healthTrail, healthTip, flowFill, flowTip;
    private Sprite hpSprite, flowNormalSprite, flowGoldSprite;
    private TMP_Text flowLabel, scoreText, comboText, rankText, healthValue;
    private int shownHpInt = -1;

    private float targetHealth = 1f, shownHealth = 1f, trailHealth = 1f, trailTimer;
    private float targetFlow, shownFlow;
    private bool flowActive;
    private float targetScore, shownScore;
    private int shownScoreInt = -1;
    private int combo, lastRankPop;
    private float comboPop, rankPop;
    private string rank = "";

    private void Awake()
    {
        if (displayFont == null) displayFont = TMP_Settings.defaultFontAsset;
        hpSprite = MakeBarSprite(Hex("C2105F"), Pink, Hex("FF8AB5"));                 // máu: hồng đậm -> hồng sáng
        flowNormalSprite = MakeBarSprite(Cyan, Hex("7B6CFF"), Hex("D455FF"));         // flow: cyan -> tím -> hồng tím
        flowGoldSprite = MakeBarSprite(Hex("FF8A00"), Yellow, Hex("FFF3A0"));         // đang Flow: cam -> vàng
        Build();
        ResetHUD();
    }

    // ================= API =================

    /// <summary>0..1</summary>
    public void SetHealth(float value01)
    {
        value01 = Mathf.Clamp01(value01);
        if (value01 < targetHealth) trailTimer = trailDelay; // mất máu: thanh trắng đuổi theo chậm
        targetHealth = value01;
    }

    /// <summary>0..1; active = đang trong trạng thái Flow (thanh đổi màu vàng, nhấp nháy)</summary>
    public void SetFlow(float value01, bool active = false)
    {
        targetFlow = Mathf.Clamp01(value01);
        flowActive = active;
    }

    public void SetScore(int score) { targetScore = Mathf.Max(0, score); }

    public void SetCombo(int value)
    {
        if (value > combo && value >= 2) comboPop = 1f;
        combo = Mathf.Max(0, value);
        comboText.text = combo.ToString();
    }

    /// <summary>Độ chính xác theo %, dùng cùng thang hạng với màn kết quả. Truyền số âm để ẩn hạng.</summary>
    public void SetAccuracy(float percent)
    {
        string newRank = percent < 0f ? "" : GameplayResultScreen.RankOf(true, percent);
        if (newRank != rank)
        {
            rank = newRank;
            if (rank != "") rankPop = 1f;
        }
        rankRoot.gameObject.SetActive(rank != "");
        rankText.text = rank;
        rankText.color = RankColor(rank);
    }

    public void SetVisible(bool visible) { if (root != null) root.gameObject.SetActive(visible); }

    public void ResetHUD()
    {
        targetHealth = shownHealth = trailHealth = 1f;
        targetFlow = shownFlow = 0f;
        flowActive = false;
        targetScore = shownScore = 0f;
        shownScoreInt = -1;
        combo = 0;
        comboText.text = "0";
        comboGroup.alpha = 0f;
        SetAccuracy(-1f);
    }

    // ================= Update =================

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        float k = 1f - Mathf.Exp(-smooth * dt);

        // --- Máu ---
        shownHealth = Mathf.Lerp(shownHealth, targetHealth, k);
        if (trailTimer > 0f) trailTimer -= dt;
        else trailHealth = Mathf.Lerp(trailHealth, shownHealth, k);
        if (trailHealth < shownHealth) trailHealth = shownHealth;
        SetFill(healthFill, shownHealth);
        if (healthTrail != null) SetFill(healthTrail, trailHealth);
        SetTip(healthTip, shownHealth);
        healthFill.color = targetHealth <= lowHealth
            ? Color.Lerp(Color.white, new Color(1f, 0.4f, 0.45f), 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 9f))
            : Color.white;
        int hpInt = Mathf.CeilToInt(shownHealth * 100f);
        if (hpInt != shownHpInt)
        {
            shownHpInt = hpInt;
            healthValue.text = hpInt.ToString();
        }

        // --- Flow ---
        shownFlow = Mathf.Lerp(shownFlow, targetFlow, k);
        SetFill(flowFill, shownFlow);
        SetTip(flowTip, shownFlow);
        bool full = shownFlow >= 0.995f;
        Sprite wantSprite = flowActive ? flowGoldSprite : flowNormalSprite;
        if (flowFill.sprite != wantSprite) flowFill.sprite = wantSprite;
        if (flowActive)
            flowFill.color = Color.Lerp(Color.white, new Color(0.8f, 0.8f, 0.8f), 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 12f));
        else if (full)
            flowFill.color = Color.Lerp(Color.white, new Color(0.8f, 0.8f, 0.8f), 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 7f));
        else
            flowFill.color = Color.white;
        string wantLabel = flowActive ? "FLOW!" : (full ? "READY" : "FLOW");
        if (flowLabel.text != wantLabel) { flowLabel.text = wantLabel; flowLabel.color = flowActive ? Yellow : Cyan; }

        // --- Điểm (đếm lên) ---
        shownScore = Mathf.Lerp(shownScore, targetScore, 1f - Mathf.Exp(-14f * dt));
        if (Mathf.Abs(targetScore - shownScore) < 1f) shownScore = targetScore;
        int s = Mathf.RoundToInt(shownScore);
        if (s != shownScoreInt)
        {
            shownScoreInt = s;
            scoreText.text = s.ToString("N0");
        }

        // --- Combo ---
        float wantAlpha = combo >= 2 ? 1f : 0f;
        comboGroup.alpha = Mathf.MoveTowards(comboGroup.alpha, wantAlpha, dt * (wantAlpha > 0f ? 8f : 4f));
        comboPop = Mathf.MoveTowards(comboPop, 0f, dt * 5f);
        comboRoot.localScale = Vector3.one * (1f + 0.3f * comboPop * comboPop);

        // --- Hạng ---
        rankPop = Mathf.MoveTowards(rankPop, 0f, dt * 4f);
        rankRoot.localScale = Vector3.one * (1f + 0.4f * rankPop);
    }

    private static void SetFill(Image fill, float value)
    {
        value = Mathf.Clamp01(value);
        if (fill.type == Image.Type.Filled) fill.fillAmount = value;
        else fill.rectTransform.anchorMax = new Vector2(value, 1f);
    }

    // Vạch sáng ở đầu thanh, chạy theo giá trị
    private void SetTip(Image tip, float value)
    {
        bool show = showBarTip && value > 0.01f;
        if (tip.gameObject.activeSelf != show) tip.gameObject.SetActive(show);
        if (!show) return;
        RectTransform r = tip.rectTransform;
        r.anchorMin = r.anchorMax = new Vector2(Mathf.Clamp01(value), 0.5f);
        r.anchoredPosition = Vector2.zero;
    }

    // ================= Dựng UI =================

    private void Build()
    {
        root = CreateNode("HUDRoot", transform);
        Stretch(root, 0f);

        float x = hudOrigin.x, y = hudOrigin.y;
        Vector2 tl = new Vector2(0f, 1f);
        Vector2 mid = new Vector2(.5f, .5f);

        // ----- Máu -----
        RectTransform hp = CreateNode("Health", root);
        Place(hp, tl, tl, new Vector2(600f, 50f), new Vector2(x, -y));
        BuildBadge(hp, "HP", Pink, new Vector2(112f, 50f), Vector2.zero);
        Vector2 hpSize = new Vector2(480f, 50f);
        RectTransform hpBar = BuildBar(hp, hpSize, new Vector2(120f, 0f), hpSprite, showDamageTrail, 0, out healthFill, out healthTrail, out healthTip);
        healthValue = CreateText("HealthValue", hpBar, "100", 30, Color.white, TextAlignmentOptions.Right);
        Place(healthValue.rectTransform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(120f, hpSize.y), new Vector2(-28f, 0f));
        healthValue.fontStyle = FontStyles.Bold | FontStyles.Italic;
        healthValue.outlineWidth = 0.22f;
        healthValue.outlineColor = new Color32(40, 0, 70, 255);

        // ----- Flow (ngay dưới máu) -----
        RectTransform flow = CreateNode("Flow", root);
        Place(flow, tl, tl, new Vector2(600f, 36f), new Vector2(x, -(y + 62f)));
        flowLabel = BuildBadge(flow, "FLOW", Cyan, new Vector2(112f, 36f), Vector2.zero);
        BuildBar(flow, new Vector2(400f, 36f), new Vector2(120f, 0f), flowNormalSprite, false, 0, out flowFill, out _, out flowTip);

        // ----- Điểm (dưới flow) -----
        RectTransform sp = CreateNode("ScorePanel", root);
        Place(sp, tl, tl, new Vector2(440f, 80f), new Vector2(x, -(y + 112f)));
        AddGlow(sp, new Vector2(440f, 80f), Purple);
        Image sFrame = CreateImage("Frame", sp, Slant(new Vector2(440f, 80f), true));
        Stretch(sFrame.rectTransform, 0f);
        sFrame.color = Color.white;
        Image sInner = CreateImage("Inner", sp, Slant(new Vector2(432f, 72f)));
        Stretch(sInner.rectTransform, 4f);
        sInner.color = Dark;

        TMP_Text scoreLabel = CreateText("ScoreLabel", sp, "SCORE", 26, Pink, TextAlignmentOptions.Left);
        Place(scoreLabel.rectTransform, new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(120f, 40f), new Vector2(36f, 0f));
        scoreLabel.fontStyle = FontStyles.Bold | FontStyles.Italic;

        scoreText = CreateText("ScoreValue", sp, "0", 58, Yellow, TextAlignmentOptions.Right);
        Place(scoreText.rectTransform, new Vector2(1f, .5f), new Vector2(1f, .5f), new Vector2(300f, 74f), new Vector2(-28f, 0f));
        scoreText.fontStyle = FontStyles.Bold | FontStyles.Italic;
        scoreText.enableAutoSizing = true;
        scoreText.fontSizeMin = 30f;
        scoreText.fontSizeMax = 58f;

        // ----- Hàng dưới điểm: Hạng (trái) + Combo (phải) -----
        float rowY = -(y + 212f + 46f);

        rankRoot = CreateNode("Rank", root);
        Place(rankRoot, tl, mid, new Vector2(92f, 92f), new Vector2(x + 46f, rowY));
        Image rOuter = CreateImage("Frame", rankRoot, null);
        Place(rOuter.rectTransform, mid, mid, new Vector2(66f, 66f), Vector2.zero);
        rOuter.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
        rOuter.color = Cyan;
        Image rInner = CreateImage("Inner", rankRoot, null);
        Place(rInner.rectTransform, mid, mid, new Vector2(56f, 56f), Vector2.zero);
        rInner.rectTransform.localEulerAngles = new Vector3(0f, 0f, 45f);
        rInner.color = new Color(0.12f, 0.02f, 0.18f, 0.95f);
        rankText = CreateText("RankValue", rankRoot, "S", 54, Yellow, TextAlignmentOptions.Center);
        Place(rankText.rectTransform, mid, mid, new Vector2(80f, 80f), new Vector2(0f, -2f));
        rankText.fontStyle = FontStyles.Bold | FontStyles.Italic;

        comboRoot = CreateNode("Combo", root);
        Place(comboRoot, tl, mid, new Vector2(290f, 120f), new Vector2(x + 92f + 20f + 145f, rowY));
        comboRoot.localEulerAngles = new Vector3(0f, 0f, 3f);
        comboGroup = comboRoot.gameObject.AddComponent<CanvasGroup>();
        comboGroup.blocksRaycasts = false;
        comboGroup.interactable = false;

        comboText = CreateText("ComboValue", comboRoot, "0", 96, Color.white, TextAlignmentOptions.Center);
        Place(comboText.rectTransform, mid, mid, new Vector2(290f, 90f), new Vector2(0f, 18f));
        comboText.fontStyle = FontStyles.Bold | FontStyles.Italic;
        comboText.outlineWidth = 0.25f;
        comboText.outlineColor = (Color32)Pink;

        TMP_Text comboLabel = CreateText("ComboLabel", comboRoot, "COMBO", 34, Cyan, TextAlignmentOptions.Center);
        Place(comboLabel.rectTransform, mid, mid, new Vector2(290f, 40f), new Vector2(0f, -44f));
        comboLabel.fontStyle = FontStyles.Bold | FontStyles.Italic;
    }

    private RectTransform BuildBar(Transform parent, Vector2 size, Vector2 pos, Sprite fillSprite, bool withTrail,
        int segments, out Image fill, out Image trail, out Image tip)
    {
        RectTransform bar = CreateNode("Bar", parent);
        Place(bar, new Vector2(0f, .5f), new Vector2(0f, .5f), size, pos);

        AddGlow(bar, size, Purple);
        Image frame = CreateImage("Frame", bar, Slant(size, true));
        Stretch(frame.rectTransform, 0f);
        frame.color = Color.white;

        Image inner = CreateImage("Inner", bar, Slant(size - new Vector2(8f, 8f)));
        Stretch(inner.rectTransform, 4f);
        inner.color = Dark;
        inner.gameObject.AddComponent<Mask>().showMaskGraphic = true;

        // vệt "máu vừa mất" (trắng mờ) nằm sau lớp chính
        trail = withTrail ? BarFill("Trail", inner.transform, new Color(1f, 1f, 1f, .55f)) : null;

        // lớp chính: sprite gradient + kiểu Filled nên màu không bị co theo giá trị
        fill = BarFill("Fill", inner.transform, Color.white);
        fill.sprite = fillSprite;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        fill.fillAmount = 0f;

        // vạch chia, nghiêng song song với khung
        float angle = -Mathf.Atan(SlantRatio) * Mathf.Rad2Deg;
        for (int i = 1; i < segments; i++)
        {
            Image tick = CreateImage("Tick" + i, inner.transform, null);
            RectTransform tr = tick.rectTransform;
            tr.anchorMin = tr.anchorMax = new Vector2((float)i / segments, .5f);
            tr.pivot = new Vector2(.5f, .5f);
            tr.sizeDelta = new Vector2(2f, size.y + 20f);
            tr.anchoredPosition = Vector2.zero;
            tr.localEulerAngles = new Vector3(0f, 0f, angle);
            tick.color = new Color(0.05f, 0f, 0.12f, 0.5f);
        }

        // đầu thanh phát sáng
        tip = CreateImage("Tip", inner.transform, null);
        RectTransform trt = tip.rectTransform;
        trt.pivot = new Vector2(.5f, .5f);
        trt.sizeDelta = new Vector2(4f, size.y + 10f);
        tip.color = new Color(1f, 1f, 1f, 0.9f);
        Image tipGlow = CreateImage("TipGlow", tip.transform, null);
        Place(tipGlow.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(18f, size.y + 10f), Vector2.zero);
        tipGlow.color = new Color(1f, 1f, 1f, 0.22f);
        tip.gameObject.SetActive(false);

        return bar;
    }

    private Image BarFill(string name, Transform parent, Color color)
    {
        Image img = CreateImage(name, parent, null);
        RectTransform r = img.rectTransform;
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
        img.color = color;
        return img;
    }

    /// <summary>Nhãn HP / FLOW: cùng kiểu khung (viền chuyển màu + glow + nền tối) với thanh bên cạnh.</summary>
    private TMP_Text BuildBadge(Transform parent, string label, Color textColor, Vector2 size, Vector2 pos)
    {
        RectTransform badge = CreateNode(label + "Badge", parent);
        Place(badge, new Vector2(0f, .5f), new Vector2(0f, .5f), size, pos);

        AddGlow(badge, size, Purple);
        Image frame = CreateImage("Frame", badge, Slant(size, true));
        Stretch(frame.rectTransform, 0f);
        Image inner = CreateImage("Inner", badge, Slant(size - new Vector2(8f, 8f)));
        Stretch(inner.rectTransform, 4f);
        inner.color = Dark;

        TMP_Text t = CreateText(label + "Label", badge, label, size.y * 0.62f, textColor, TextAlignmentOptions.Center);
        Stretch(t.rectTransform, 0f);
        t.fontStyle = FontStyles.Bold | FontStyles.Italic;
        t.characterSpacing = 3f;
        return t;
    }

    // ================= Helper =================

    private TMP_Text CreateText(string name, Transform parent, string value, float size, Color color, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TMP_Text t = go.GetComponent<TMP_Text>();
        t.text = value;
        t.font = displayFont;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.raycastTarget = false;
        t.overflowMode = TextOverflowModes.Overflow;
        return t;
    }

    private static Image CreateImage(string name, Transform parent, Sprite sprite)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = Color.white;
        img.raycastTarget = false;
        return img;
    }

    private static RectTransform CreateNode(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static void Stretch(RectTransform r, float inset)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(inset, inset);
        r.offsetMax = new Vector2(-inset, -inset);
    }

    private static void Place(RectTransform r, Vector2 anchor, Vector2 pivot, Vector2 size, Vector2 pos)
    {
        r.anchorMin = anchor;
        r.anchorMax = anchor;
        r.pivot = pivot;
        r.sizeDelta = size;
        r.anchoredPosition = pos;
    }

    /// <summary>Sprite hình bình hành theo đúng kích thước khung, độ nghiêng cố định (có cache).</summary>
    private Sprite Slant(Vector2 size, bool gradient = false)
    {
        int w = Mathf.Max(8, Mathf.RoundToInt(size.x));
        int h = Mathf.Max(8, Mathf.RoundToInt(size.y));
        string key = w + "x" + h + (gradient ? "g" : "");
        Sprite sp;
        if (slantCache.TryGetValue(key, out sp)) return sp;
        sp = MakeSlantSprite(w * 2, h * 2, h * 2 * SlantRatio, gradient);
        slantCache[key] = sp;
        return sp;
    }

    private static Sprite MakeSlantSprite(int w, int h, float slantPx, bool gradient)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        Color32[] px = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            float shift = slantPx * y / (h - 1);
            float left = shift;
            float right = w - slantPx + shift;
            for (int x = 0; x < w; x++)
            {
                float cx = x + 0.5f;
                float a = Mathf.Clamp01(Mathf.Min(cx - left, right - cx) + 0.5f);
                Color32 col = gradient ? (Color32)GradientAt((x + 0.5f) / w) : new Color32(255, 255, 255, 255);
                col.a = (byte)(a * 255f);
                px[y * w + x] = col;
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(.5f, .5f), 100f);
    }

    /// <summary>Sprite gradient ngang 3 màu, có đổ bóng dọc + vệt bóng nhẹ nửa trên để thanh trông nổi khối.</summary>
    private static Sprite MakeBarSprite(Color left, Color mid, Color right)
    {
        const int w = 256, h = 32;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        Color32[] px = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            float v = y / (float)(h - 1);
            float shade = Mathf.Lerp(0.72f, 1.08f, v);
            float gloss = v > 0.5f ? 0.16f : 0f;
            for (int x = 0; x < w; x++)
            {
                float t = x / (float)(w - 1);
                Color c = t < 0.5f ? Color.Lerp(left, mid, t * 2f) : Color.Lerp(mid, right, (t - 0.5f) * 2f);
                c = new Color(Mathf.Clamp01(c.r * shade), Mathf.Clamp01(c.g * shade), Mathf.Clamp01(c.b * shade), 1f);
                c = Color.Lerp(c, Color.white, gloss);
                px[y * w + x] = (Color32)c;
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(.5f, .5f), 100f);
    }

    private static Color GradientAt(float t)
    {
        return t < 0.5f ? Color.Lerp(Pink, Purple, t * 2f) : Color.Lerp(Purple, Cyan, (t - 0.5f) * 2f);
    }

    /// <summary>Quầng sáng mờ phía sau khung (2 lớp chồng nhau).</summary>
    private void AddGlow(Transform parent, Vector2 size, Color c)
    {
        Image outer = CreateImage("GlowOuter", parent, Slant(size + new Vector2(20f, 20f)));
        Stretch(outer.rectTransform, -10f);
        outer.color = new Color(c.r, c.g, c.b, 0.14f);
        Image inner = CreateImage("GlowInner", parent, Slant(size + new Vector2(10f, 10f)));
        Stretch(inner.rectTransform, -5f);
        inner.color = new Color(c.r, c.g, c.b, 0.28f);
    }

    private static Color RankColor(string r)
    {
        switch (r)
        {
            case "S": return Yellow;
            case "A": return Pink;
            case "B": return Cyan;
            case "C": return Purple;
            case "F": return Red;
            default: return Color.white;
        }
    }

    private static Color Hex(string value)
    {
        ColorUtility.TryParseHtmlString("#" + value, out Color c);
        return c;
    }
}
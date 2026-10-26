using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameplayResultScreen : MonoBehaviour
{
    public event Action RetryRequested;
    public event Action ContinueRequested;
    public event Action MenuRequested;

    private readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
    [SerializeField] private TMP_FontAsset displayFont;
    private RectTransform content;
    private Sprite stageBackground;
    private Sprite clearLogoSprite;
    private Sprite failedLogoSprite;
    private Image resultLogo;
    private Image coverArt;
    private TMP_Text songText;
    private TMP_Text artistText;
    private TMP_Text difficultyText;
    private TMP_Text perfectText;
    private TMP_Text goodText;
    private TMP_Text badText;
    private TMP_Text shitText;
    private GameObject nextButton;
    private RectTransform retryButton, menuButton;
    private TMP_Text missText;
    private TMP_Text comboText;
    private TMP_Text scoreText;
    private TMP_Text rankText;
    private TMP_Text newBestText;

    private static readonly Color Yellow = Hex("FFD21C");
    private static readonly Color Cyan = Hex("22D9FF");
    private static readonly Color Red = Hex("FF365D");
    private const float ScaleX = 1920f / 1672f;
    private const float ScaleY = 1080f / 941f;

    private void Awake()
    {
        if (displayFont == null) displayFont = TMP_Settings.defaultFontAsset;
        stageBackground = Resources.Load<Sprite>("Result/result_stage_background");
        clearLogoSprite = Resources.Load<Sprite>("Result/stage_clear");
        failedLogoSprite = Resources.Load<Sprite>("Result/stage_failed");
        foreach (Sprite sprite in Resources.LoadAll<Sprite>("Result/result_assets")) sprites[sprite.name] = sprite;
        foreach (Transform child in transform) child.gameObject.SetActive(false);
        Build();
    }

    public void Show(bool cleared, SongData song, Sprite performer, int score, int maxCombo, float accuracy,
        int perfect, int good, int bad, int miss, bool newBest, bool hasNext)
    {
        gameObject.SetActive(true);
        resultLogo.sprite = cleared ? clearLogoSprite : failedLogoSprite;
        songText.text = song != null ? song.songName : "RHYTHM BATTLE";
        artistText.text = song != null && !string.IsNullOrWhiteSpace(song.artist) ? song.artist : "UNKNOWN ARTIST";
        difficultyText.text = GameSession.SelectedDifficulty.ToString().ToUpperInvariant();
        coverArt.sprite = song != null ? song.coverArt : null;
        coverArt.enabled = coverArt.sprite != null;
        perfectText.text = perfect.ToString("N0");
        goodText.text = good.ToString("N0");
        badText.text = bad.ToString("N0");
        shitText.text = "0";
        missText.text = miss.ToString("N0");
        comboText.text = maxCombo.ToString("N0");
        scoreText.text = score.ToString("N0");
        rankText.text = RankOf(cleared, accuracy);
        rankText.color = RankColor(rankText.text);
        newBestText.gameObject.SetActive(newBest);
        // NEXT chi hien khi con bai tiep theo trong Story Mode
        if (nextButton != null) nextButton.SetActive(hasNext);
        // khong co NEXT thi dua RETRY va MENU vao giua
        if (retryButton != null) retryButton.anchoredPosition = new Vector2((hasNext ? 670f : 815f) * ScaleX - 960f, retryButton.anchoredPosition.y);
        if (menuButton != null) menuButton.anchoredPosition = new Vector2((hasNext ? 1325f : 1165f) * ScaleX - 960f, menuButton.anchoredPosition.y);
        content.localScale = Vector3.one * 0.92f;
        StopAllCoroutines();
        StartCoroutine(Pop());
    }

    private void Build()
    {
        Image background = CreateImage("StageBackground", transform, stageBackground);
        Stretch(background.rectTransform);
        background.color = stageBackground != null ? new Color(.67f, .67f, .72f, 1f) : new Color(0.018f, 0.02f, 0.045f, 1f);
        background.preserveAspect = false;
        content = CreateNode("ResultContent", transform);
        Stretch(content);

        resultLogo = CreateImage("ResultLogo", content, clearLogoSprite);
        SetRect(resultLogo.rectTransform, new Vector2(720f * ScaleX, 231f * ScaleY), new Vector2(-546.6f, 407.6f));
        resultLogo.raycastTarget = false;
        resultLogo.rectTransform.sizeDelta *= 0.96f;
        resultLogo.rectTransform.anchoredPosition += new Vector2(50f, -6f);
        resultLogo.preserveAspect = false;
        Image character = CreateImage("Character", content, Resources.Load<Sprite>("Result/result_character"));
        SetRect(character.rectTransform, new Vector2(720f * ScaleX, 660f * ScaleY), new Vector2(-546.6f, 5.3f));
        character.raycastTarget = false;
        character.rectTransform.sizeDelta *= 1.28f;
        character.rectTransform.anchoredPosition += new Vector2(20f, -185f);
        Image songPanel = PlaceSprite("SongPanel", "song_panel", new Rect(720, 710, 850, 231));
        Image statsPanel = PlaceSprite("StatsPanel", "stats_panel", new Rect(705, 315, 570, 400));
        Image rankPanel = PlaceSprite("RankPanel", "rank_panel", new Rect(1270, 285, 402, 440));
        Image scoreBar = PlaceSprite("ScoreBar", "score_bar", new Rect(700, 175, 630, 145));

        // Noi dung dat theo o ben trong khung (khung hoi nghieng len ben phai)
        RectTransform songContent = CreateNode("SongContent", songPanel.transform);
        SetRect(songContent, new Vector2(880f, 170f), new Vector2(4f, -4f));
        songContent.localEulerAngles = new Vector3(0f, 0f, 2.5f);

        Image coverFrame = CreateImage("SongCoverFrame", songContent, null);
        SetRect(coverFrame.rectTransform, new Vector2(146f, 146f), new Vector2(-358f, 0f));
        coverFrame.color = new Color(1f, .16f, .5f, 1f);
        coverFrame.raycastTarget = false;
        coverArt = CreateImage("SongCover", songContent, null);
        SetRect(coverArt.rectTransform, new Vector2(138f, 138f), new Vector2(-358f, 0f));
        coverArt.preserveAspect = false;
        coverArt.raycastTarget = false;

        songText = CreateText("SongName", songContent, "SONG TITLE", 40, Color.white, TextAlignmentOptions.Left);
        SetRect(songText.rectTransform, new Vector2(560f, 54f), new Vector2(5f, 38f));
        songText.fontStyle = FontStyles.Bold;
        songText.enableAutoSizing = true;
        songText.fontSizeMin = 22f;
        songText.fontSizeMax = 40f;
        artistText = CreateText("Artist", songContent, "ARTIST", 26, new Color(.7f, .7f, .76f), TextAlignmentOptions.Left);
        SetRect(artistText.rectTransform, new Vector2(560f, 36f), new Vector2(5f, -6f));
        artistText.enableAutoSizing = true;
        artistText.fontSizeMin = 16f;
        artistText.fontSizeMax = 26f;
        Image difficultyBadge = CreateImage("DifficultyBadge", songContent, null);
        SetRect(difficultyBadge.rectTransform, new Vector2(130f, 38f), new Vector2(-210f, -50f));
        difficultyBadge.color = new Color(1f, .08f, .4f, 1f);
        difficultyBadge.raycastTarget = false;
        difficultyText = CreateText("Difficulty", songContent, "HARD", 24, Color.black, TextAlignmentOptions.Center);
        SetRect(difficultyText.rectTransform, new Vector2(126f, 36f), new Vector2(-210f, -50f));
        difficultyText.fontStyle = FontStyles.Bold;

        // canh dung 5 dong cua khung: PERFECT, GOOD, BAD, SHIT, MISSES + MAX COMBO
        perfectText = ValueText("PerfectValue", statsPanel.transform, 155f);
        goodText = ValueText("GoodValue", statsPanel.transform, 91f);
        badText = ValueText("BadValue", statsPanel.transform, 31f);
        shitText = ValueText("ShitValue", statsPanel.transform, -30f);
        missText = ValueText("MissValue", statsPanel.transform, -93f);
        comboText = ValueText("ComboValue", statsPanel.transform, -175f);

        rankText = CreateText("RankValue", rankPanel.transform, "S", 205, Yellow, TextAlignmentOptions.Center);
        SetRect(rankText.rectTransform, new Vector2(300f, 265f), new Vector2(6f, -5f));
        rankText.fontStyle = FontStyles.Bold | FontStyles.Italic;
        rankText.outlineWidth = 0.22f;
        rankText.outlineColor = new Color32(25, 0, 20, 255);
        newBestText = CreateText("NewRecord", rankPanel.transform, "NEW RECORD!", 27, Color.white, TextAlignmentOptions.Center);
        SetRect(newBestText.rectTransform, new Vector2(260f, 50f), new Vector2(10f, -153f));
        newBestText.fontStyle = FontStyles.Bold | FontStyles.Italic;

        scoreText = CreateText("ScoreValue", scoreBar.transform, "0", 62, Yellow, TextAlignmentOptions.Right);
        SetRect(scoreText.rectTransform, new Vector2(420f, 92f), new Vector2(84f, 0f));
        scoreText.fontStyle = FontStyles.Bold;

        retryButton = (RectTransform)CreateButton("RetryButton", "retry_button", new Rect(500, 60, 340, 120), () => RetryRequested?.Invoke()).transform;
        nextButton = CreateButton("NextButton", "next_button", new Rect(810, 60, 355, 120), () => ContinueRequested?.Invoke());
        menuButton = (RectTransform)CreateButton("MenuButton", "menu_button", new Rect(1120, 60, 410, 120), () => MenuRequested?.Invoke()).transform;
    }

    private TMP_Text ValueText(string name, Transform parent, float y)
    {
        TMP_Text value = CreateText(name, parent, "0", 34, Color.white, TextAlignmentOptions.Right);
        SetRect(value.rectTransform, new Vector2(150f, 52f), new Vector2(166f, y));
        value.fontStyle = FontStyles.Bold;
        return value;
    }

    private Image PlaceSprite(string objectName, string spriteName, Rect sourceRect)
    {
        Sprite sprite;
        sprites.TryGetValue(spriteName, out sprite);
        Image image = CreateImage(objectName, content, sprite);
        float width = sourceRect.width * ScaleX;
        float height = sourceRect.height * ScaleY;
        float x = (sourceRect.x + sourceRect.width * .5f) * ScaleX - 960f;
        float y = (sourceRect.y + sourceRect.height * .5f) * ScaleY - 540f;
        SetRect(image.rectTransform, new Vector2(width, height), new Vector2(x, y));
        image.raycastTarget = false;
        return image;
    }

    private GameObject CreateButton(string objectName, string spriteName, Rect sourceRect, UnityEngine.Events.UnityAction action)
    {
        Image image = PlaceSprite(objectName, spriteName, sourceRect);
        image.raycastTarget = true;
        Button button = image.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1f, 1f, 1f, .82f);
        colors.pressedColor = new Color(.72f, .72f, .72f, 1f);
        button.colors = colors;
        button.onClick.AddListener(action);
        return image.gameObject;
    }

    private TMP_Text CreateText(string name, Transform parent, string value, float size, Color color, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TMP_Text text = go.GetComponent<TMP_Text>();
        text.text = value;
        text.font = displayFont;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }

    private static Image CreateImage(string name, Transform parent, Sprite sprite)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = Color.white;
        return image;
    }

    private static RectTransform CreateNode(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetRect(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = new Vector2(.5f, .5f);
        rect.anchorMax = new Vector2(.5f, .5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private IEnumerator Pop()
    {
        float elapsed = 0f;
        while (elapsed < .28f)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / .28f);
            content.localScale = Vector3.one * Mathf.Lerp(.92f, 1f, 1f - Mathf.Pow(1f - progress, 3f));
            yield return null;
        }
        content.localScale = Vector3.one;
    }

    // Thang hang dung chung cho ca game (man tong ket, Free Play, Story Mode)
    public static string RankOf(bool cleared, float accuracy)
    {
        if (!cleared) return "F";
        if (accuracy < 0f) return "";
        if (accuracy >= 95f) return "S";
        if (accuracy >= 90f) return "A";
        if (accuracy >= 80f) return "B";
        if (accuracy >= 70f) return "C";
        return "D";
    }

    private static Color RankColor(string rank)
    {
        if (rank == "S" || rank == "A") return Yellow;
        if (rank == "B") return Cyan;
        if (rank == "F") return Color.white;
        return Color.white;
    }

    private static Color Hex(string value)
    {
        ColorUtility.TryParseHtmlString("#" + value, out Color color);
        return color;
    }
}

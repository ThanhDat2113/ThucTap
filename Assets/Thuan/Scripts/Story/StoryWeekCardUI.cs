using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 1 the tuan ben trai man Story Mode
public class StoryWeekCardUI : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text weekText;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text[] trackTexts = new TMP_Text[3];
    [SerializeField] private Image[] trackCrowns = new Image[3];
    [SerializeField] private Image lockShade;
    [SerializeField] private GameObject lockRoot;
    [SerializeField] private TMP_Text lockText;
    [SerializeField] private Color crownDone = new Color(1f, 0.82f, 0.25f);
    [SerializeField] private Color crownTodo = new Color(0.6f, 0.6f, 0.75f, 0.7f);

    public StoryWeekData Week { get; private set; }
    public bool Locked { get; private set; }

    private RectTransform rt;
    private Vector2 basePos;
    private float offset, offsetVel;
    private bool selected, inited;

    public void Setup(StoryWeekData week, bool unlocked, string lockMessage, Action<StoryWeekCardUI> onClick)
    {
        rt = (RectTransform)transform;
        if (!inited) { basePos = rt.anchoredPosition; inited = true; }
        Week = week;
        Locked = !unlocked;

        weekText.text = "WEEK " + week.weekNumber;
        titleText.text = week.title;
        for (int i = 0; i < trackTexts.Length; i++)
        {
            bool has = week.tracks != null && i < week.tracks.Length && week.tracks[i] != null;
            if (trackTexts[i] != null) trackTexts[i].text = has ? (i + 1) + ".  " + week.tracks[i].DisplayTitle : "";
            if (trackCrowns[i] != null)
            {
                bool done = has && week.tracks[i].song != null && SongStats.GetBestScore(week.tracks[i].song) > 0;
                trackCrowns[i].color = done ? crownDone : crownTodo;
                trackCrowns[i].enabled = has;
            }
        }
        if (lockRoot != null) lockRoot.SetActive(Locked);
        if (lockShade != null) { lockShade.sprite = week.cardNormal; lockShade.enabled = Locked; }
        if (lockText != null) lockText.text = lockMessage;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick(this));
        SetSelected(false, true);
    }

    public void SetSelected(bool on, bool instant = false)
    {
        selected = on;
        background.sprite = on ? Week.cardSelected : Week.cardNormal;
        if (lockShade != null) lockShade.sprite = background.sprite;
        weekText.colorGradient = on
            ? new VertexGradient(new Color(1f, 0.55f, 0.9f), new Color(1f, 0.55f, 0.9f), new Color(1f, 0.15f, 0.65f), new Color(1f, 0.15f, 0.65f))
            : new VertexGradient(new Color(0.75f, 1f, 1f), new Color(0.75f, 1f, 1f), new Color(0.25f, 0.8f, 1f), new Color(0.25f, 0.8f, 1f));
        if (instant) { offset = on ? 18f : 0f; Apply(); }
    }

    private void Update()
    {
        offset = Mathf.SmoothDamp(offset, selected ? 18f : 0f, ref offsetVel, 0.08f, Mathf.Infinity, Time.unscaledDeltaTime);
        Apply();
    }

    private void Apply() { if (rt != null) rt.anchoredPosition = basePos + new Vector2(offset, 0f); }
}

using UnityEngine;
using TMPro;
using RhythmGame;   // để dùng enum Judgement từ JudgementSystem.cs

/// <summary>
/// Chữ PERFECT / GOOD / BAD / MISS: nảy to rồi thu về, giữ một lúc, sau đó mờ đi và trôi lên.
/// Dùng chung enum Judgement với JudgementSystem.cs, không cần enum HitJudgement riêng nữa.
/// Gắn vào object có TextMeshPro (cả TextMeshPro world lẫn TextMeshPro UI trong Canvas đều được).
/// </summary>
public class JudgementPopup : MonoBehaviour
{
    [System.Serializable]
    public class Style
    {
        public string text = "PERFECT";
        public Color topColor = new Color(0.75f, 0.85f, 1f);
        public Color bottomColor = new Color(1f, 0.35f, 0.85f);
    }

    [Tooltip("Để trống sẽ tự lấy TextMeshPro trên chính object này")]
    public TMP_Text label;

    public Style perfect = new Style { text = "PERFECT" };
    public Style good = new Style { text = "GOOD", topColor = new Color(1f, 0.6f, 0.9f), bottomColor = new Color(0.6f, 0.3f, 1f) };
    public Style bad = new Style { text = "BAD", topColor = new Color(0.6f, 1f, 0.8f), bottomColor = new Color(0.2f, 0.7f, 1f) };
    public Style miss = new Style { text = "MISS", topColor = new Color(0.8f, 0.8f, 0.85f), bottomColor = new Color(0.45f, 0.45f, 0.55f) };

    [Header("Animation")]
    public float popScale = 1.4f;
    public float popDuration = 0.12f;
    public float holdTime = 0.35f;
    public float fadeTime = 0.15f;
    [Tooltip("Khoảng trôi lên. Trong Canvas tự nhân 100 (thành pixel)")]
    public float riseDistance = 0.25f;

    float t = 999f;
    Vector3 basePos;
    Vector3 baseScale;
    float unit = 1f;

    void Awake()
    {
        if (!label) label = GetComponent<TMP_Text>();
        if (!label)
        {
            Debug.LogError("JudgementPopup: không tìm thấy TextMeshPro. Hãy kéo vào ô Label.", this);
            enabled = false;
            return;
        }

        label.enableVertexGradient = true;
        basePos = label.transform.localPosition;
        baseScale = label.transform.localScale;
        unit = label.GetComponentInParent<Canvas>() != null ? 100f : 1f;
        label.alpha = 0f;
    }

    /// <summary>Gọi hàm này từ JudgementSystem.OnJudged.</summary>
    public void Show(Judgement j)
    {
        if (!label) return;

        Style s;
        switch (j)
        {
            case Judgement.Perfect: s = perfect; break;
            case Judgement.Good:    s = good;    break;
            case Judgement.Bad:     s = bad;     break;
            default:                s = miss;    break;
        }

        label.text = s.text;
        label.colorGradient = new VertexGradient(s.topColor, s.topColor, s.bottomColor, s.bottomColor);
        t = 0f;
    }

    void Update()
    {
        float total = popDuration + holdTime + fadeTime;
        if (t > total)
        {
            label.alpha = 0f;
            return;
        }

        t += Time.unscaledDeltaTime;

        float scale = 1f;
        if (t < popDuration)
        {
            float k = 1f - Mathf.Pow(1f - t / popDuration, 3f);
            scale = Mathf.Lerp(popScale, 1f, k);
        }
        label.transform.localScale = baseScale * scale;

        float fadeStart = popDuration + holdTime;
        float a = 1f, rise = 0f;
        if (t > fadeStart)
        {
            float f = Mathf.Clamp01((t - fadeStart) / fadeTime);
            a = 1f - f;
            rise = f * riseDistance * unit;
        }
        label.alpha = a;
        label.transform.localPosition = basePos + Vector3.up * rise;
    }
}
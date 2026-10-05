using UnityEngine;

/// <summary>
/// Làm object (nền, logo, nhân vật) "đập" theo nhịp nhạc.
/// Đồng bộ bằng AudioSettings.dspTime nên không bị lệch như dùng Time.time.
/// </summary>
public class BeatPulse : MonoBehaviour
{
    [Header("Nhạc")]
    public float bpm = 120f;
    public float firstBeatOffset = 0f;     // giây, độ trễ từ lúc nhạc bắt đầu tới beat đầu tiên
    public double songStartDspTime;        // gán khi bắt đầu phát nhạc: AudioSettings.dspTime

    [Header("Hiệu ứng")]
    public float pulseScale = 1.15f;       // phóng to bao nhiêu khi có beat
    public float returnSpeed = 8f;         // tốc độ thu về
    public SpriteRenderer glowSprite;      // tuỳ chọn: sprite nháy sáng theo beat
    public Color flashColor = new Color(1f, 0.4f, 0.8f, 1f);

    Vector3 baseScale;
    Color baseColor;
    int lastBeat = -1;

    void Start()
    {
        baseScale = transform.localScale;
        if (glowSprite) baseColor = glowSprite.color;
        if (songStartDspTime == 0) songStartDspTime = AudioSettings.dspTime;
    }

    void Update()
    {
        double songTime = AudioSettings.dspTime - songStartDspTime - firstBeatOffset;
        if (songTime >= 0)
        {
            int beat = Mathf.FloorToInt((float)(songTime / (60.0 / bpm)));
            if (beat != lastBeat)
            {
                lastBeat = beat;
                OnBeat();
            }
        }

        // Thu về kích thước / màu ban đầu một cách mượt mà
        transform.localScale = Vector3.Lerp(transform.localScale, baseScale, Time.deltaTime * returnSpeed);
        if (glowSprite)
            glowSprite.color = Color.Lerp(glowSprite.color, baseColor, Time.deltaTime * returnSpeed);
    }

    void OnBeat()
    {
        transform.localScale = baseScale * pulseScale;
        if (glowSprite) glowSprite.color = flashColor;
    }
}

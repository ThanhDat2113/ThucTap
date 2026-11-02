using UnityEngine;

/// <summary>
/// Visualizer: tạo một hàng thanh (sprite) nhảy theo phổ tần số của AudioSource.
/// Gắn vào 1 GameObject rỗng, kéo AudioSource và sprite của thanh vào.
/// </summary>
public class AudioSpectrumBars : MonoBehaviour
{
    public AudioSource source;
    public SpriteRenderer barPrefab;       // sprite hình chữ nhật, pivot ở dưới (Bottom)
    public int barCount = 32;
    public float spacing = 0.3f;
    public float maxHeight = 4f;
    public float sensitivity = 60f;
    public float smooth = 12f;
    public Gradient colorByHeight;

    Transform[] bars;
    SpriteRenderer[] renderers;
    float[] spectrum = new float[512];

    void Start()
    {
        bars = new Transform[barCount];
        renderers = new SpriteRenderer[barCount];
        float startX = -(barCount - 1) * spacing * 0.5f;

        for (int i = 0; i < barCount; i++)
        {
            var b = Instantiate(barPrefab, transform);
            b.transform.localPosition = new Vector3(startX + i * spacing, 0f, 0f);
            bars[i] = b.transform;
            renderers[i] = b;
        }
    }

    void Update()
    {
        if (!source) return;
        source.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

        // Chia phổ theo thang log để dải bass/treble hiển thị cân đối
        for (int i = 0; i < barCount; i++)
        {
            float t0 = Mathf.Pow((float)i / barCount, 2f);
            float t1 = Mathf.Pow((float)(i + 1) / barCount, 2f);
            int from = Mathf.Clamp((int)(t0 * spectrum.Length), 0, spectrum.Length - 1);
            int to = Mathf.Clamp((int)(t1 * spectrum.Length), from + 1, spectrum.Length);

            float sum = 0f;
            for (int k = from; k < to; k++) sum += spectrum[k];
            float value = Mathf.Clamp01(sum / (to - from) * sensitivity);

            float targetH = Mathf.Max(0.05f, value * maxHeight);
            Vector3 s = bars[i].localScale;
            s.y = Mathf.Lerp(s.y, targetH, Time.deltaTime * smooth);
            bars[i].localScale = s;

            if (colorByHeight != null)
                renderers[i].color = colorByHeight.Evaluate(value);
        }
    }
}

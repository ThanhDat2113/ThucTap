using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Trang tri dong cho man chon nhac (dung chung Story Mode + Free Play):
// vet sang nhap nho theo nhip, hat sang bay len, cot equalizer o day, sticker lac lu.
public class SongListDecor : MonoBehaviour
{
    [Header("Nhip (BPM) cho hieu ung dap theo nhac")]
    [SerializeField] private float bpm = 110f;

    [Header("Vet sang nen (nhap nho)")]
    [SerializeField] private Image[] glows;
    [SerializeField] private float glowMinAlpha = 0.18f;
    [SerializeField] private float glowMaxAlpha = 0.38f;

    [Header("Sticker lac lu nhe")]
    [SerializeField] private RectTransform[] floaters;
    [SerializeField] private float floatAmount = 8f;
    [SerializeField] private float tiltAmount = 4f;

    [Header("Sticker dap theo nhip")]
    [SerializeField] private RectTransform[] beaters;
    [SerializeField] private float beatScale = 0.08f;

    [Header("Hat sang bay len")]
    [SerializeField] private RectTransform particleArea;
    [SerializeField] private Sprite particleSprite;
    [SerializeField] private int particleCount = 28;
    [SerializeField] private Color[] particleColors = { new Color(1f, 0.3f, 0.8f), new Color(0.3f, 0.9f, 1f), new Color(0.75f, 0.45f, 1f) };

    [Header("Equalizer o day man hinh")]
    [SerializeField] private RectTransform equalizer;
    [SerializeField] private int barCount = 48;
    [SerializeField] private float barMaxHeight = 110f;
    [SerializeField] private Color barLeft = new Color(1f, 0.25f, 0.75f, 0.35f);
    [SerializeField] private Color barRight = new Color(0.25f, 0.85f, 1f, 0.35f);

    private class Particle { public RectTransform rt; public Image img; public float speed, sway, phase, life, age, size; }

    private readonly List<Particle> particles = new List<Particle>();
    private readonly List<RectTransform> bars = new List<RectTransform>();
    private Vector2[] floaterBase;
    private Vector3[] beaterBase;
    private float[] glowAlpha;

    private void Start()
    {
        floaterBase = new Vector2[floaters != null ? floaters.Length : 0];
        for (int i = 0; i < floaterBase.Length; i++) if (floaters[i] != null) floaterBase[i] = floaters[i].anchoredPosition;
        beaterBase = new Vector3[beaters != null ? beaters.Length : 0];
        for (int i = 0; i < beaterBase.Length; i++) if (beaters[i] != null) beaterBase[i] = beaters[i].localScale;
        glowAlpha = new float[glows != null ? glows.Length : 0];
        for (int i = 0; i < glowAlpha.Length; i++) if (glows[i] != null) glowAlpha[i] = glows[i].color.a;

        BuildParticles();
        BuildEqualizer();
    }

    private void BuildParticles()
    {
        if (particleArea == null || particleSprite == null) return;
        for (int i = 0; i < particleCount; i++)
        {
            var go = new GameObject("Spark", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(particleArea, false);
            var p = new Particle { rt = (RectTransform)go.transform, img = go.GetComponent<Image>() };
            p.img.sprite = particleSprite;
            p.img.raycastTarget = false;
            p.rt.anchorMin = p.rt.anchorMax = new Vector2(0f, 0f);
            Respawn(p, true);
            particles.Add(p);
        }
    }

    private void Respawn(Particle p, bool anywhere)
    {
        var size = particleArea.rect.size;
        p.size = Random.Range(12f, 30f);
        p.rt.sizeDelta = new Vector2(p.size, p.size);
        p.speed = Random.Range(25f, 70f);
        p.sway = Random.Range(10f, 35f);
        p.phase = Random.Range(0f, 10f);
        p.life = size.y / p.speed;
        p.age = anywhere ? Random.Range(0f, p.life) : 0f;
        p.rt.anchoredPosition = new Vector2(Random.Range(0f, size.x), 0f);
        var c = particleColors[Random.Range(0, particleColors.Length)];
        p.img.color = c;
    }

    private void BuildEqualizer()
    {
        if (equalizer == null) return;
        float w = equalizer.rect.width / barCount;
        for (int i = 0; i < barCount; i++)
        {
            var go = new GameObject("Bar" + i, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(equalizer, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(w * (i + 0.5f), 0f);
            rt.sizeDelta = new Vector2(w * 0.62f, 10f);
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.color = Color.Lerp(barLeft, barRight, barCount > 1 ? i / (float)(barCount - 1) : 0f);
            bars.Add(rt);
        }
    }

    private void Update()
    {
        float t = Time.unscaledTime;
        float dt = Time.unscaledDeltaTime;
        float beat = t * bpm / 60f;
        // xung nhip: dap manh ngay dau nhip roi tat dan
        float pulse = Mathf.Pow(1f - (beat - Mathf.Floor(beat)), 4f);

        for (int i = 0; i < glowAlpha.Length; i++)
        {
            if (glows[i] == null) continue;
            float wave = 0.5f + 0.5f * Mathf.Sin(t * 0.8f + i * 1.7f);
            var c = glows[i].color;
            c.a = Mathf.Lerp(glowMinAlpha, glowMaxAlpha, wave * 0.7f + pulse * 0.3f);
            glows[i].color = c;
        }

        for (int i = 0; i < floaterBase.Length; i++)
        {
            if (floaters[i] == null) continue;
            float ph = i * 1.3f;
            floaters[i].anchoredPosition = floaterBase[i] + new Vector2(0f, Mathf.Sin(t * 1.4f + ph) * floatAmount);
            floaters[i].localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(t * 0.9f + ph) * tiltAmount);
        }

        for (int i = 0; i < beaterBase.Length; i++)
            if (beaters[i] != null) beaters[i].localScale = beaterBase[i] * (1f + beatScale * pulse);

        if (particleArea != null)
        {
            float h = particleArea.rect.height;
            foreach (var p in particles)
            {
                p.age += dt;
                if (p.age >= p.life) Respawn(p, false);
                var pos = p.rt.anchoredPosition;
                pos.y = p.age * p.speed;
                pos.x += Mathf.Sin(t * 1.2f + p.phase) * p.sway * dt;
                p.rt.anchoredPosition = pos;
                float k = pos.y / h;
                var c = p.img.color;
                c.a = Mathf.Sin(Mathf.Clamp01(k) * Mathf.PI) * 0.95f;
                p.img.color = c;
            }
        }

        for (int i = 0; i < bars.Count; i++)
        {
            float n = Mathf.PerlinNoise(i * 0.35f, t * 1.6f);
            float hgt = Mathf.Lerp(8f, barMaxHeight, n * (0.65f + 0.35f * pulse));
            var s = bars[i].sizeDelta;
            s.y = Mathf.Lerp(s.y, hgt, 1f - Mathf.Exp(-12f * dt));
            bars[i].sizeDelta = s;
        }
    }
}

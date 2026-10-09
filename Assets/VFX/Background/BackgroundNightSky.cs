using UnityEngine;

/// <summary>
/// Bầu trời đêm: trăng + sao nhấp nháy + sao băng thỉnh thoảng vụt qua.
/// Gắn vào Main Camera. Kích thước tính theo % chiều cao màn hình.
/// </summary>
public class BackgroundNightSky : MonoBehaviour
{
    [Header("Mặt trăng")]
    public bool showMoon = true;
    public Color moonColor = new Color(1f, 0.97f, 0.85f, 1f);
    public Color moonGlowColor = new Color(0.8f, 0.85f, 1f, 0.35f);
    [Tooltip("Đường kính trăng = % chiều cao màn hình")]
    [Range(0.05f, 1f)] public float moonSize = 0.22f;
    [Range(-1f, 1f)] public float moonHorizontal = 0.65f;
    [Range(-1f, 1f)] public float moonVertical = 0.5f;
    [Tooltip("Bật = trăng lưỡi liềm, tắt = trăng tròn")]
    public bool crescent = true;
    public float moonDistance = 48f;

    [Header("Sao")]
    public bool showStars = true;
    public int starCount = 80;
    public Color starColor = new Color(1f, 1f, 0.95f, 0.9f);
    public Vector2 starSizeRange = new Vector2(0.004f, 0.013f);
    public Vector2 twinkleSpeedRange = new Vector2(1f, 3.5f);
    public float starDistance = 50f;

    [Header("Sao băng")]
    public bool showShootingStars = true;
    public Vector2 shootingIntervalRange = new Vector2(3f, 8f);
    [Tooltip("Độ dài vệt = % chiều cao màn hình")]
    public float shootingLength = 0.18f;
    [Tooltip("Tốc độ = % chiều rộng màn hình mỗi giây")]
    public float shootingSpeed = 1.1f;
    public float shootingDistance = 46f;

    Camera cam;
    float viewHeight, viewWidth;

    SpriteRenderer[] stars;
    float[] twinkleSpeeds, twinklePhases, starBaseAlpha;

    Transform shoot;
    SpriteRenderer shootSr;
    Vector2 shootDir;
    float shootTimer, shootLife, shootAge;
    bool shootActive;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (!cam) cam = Camera.main;
        if (!cam) { Debug.LogError("BackgroundNightSky: không tìm thấy Camera."); return; }

        if (showMoon) BuildMoon();
        if (showStars) BuildStars();
        if (showShootingStars) BuildShootingStar();
    }

    float ViewHeightAt(float d)
    {
        return cam.orthographic ? cam.orthographicSize * 2f
            : 2f * d * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
    }

    // ---------- Mặt trăng ----------
    void BuildMoon()
    {
        float h = ViewHeightAt(moonDistance);
        float w = h * cam.aspect;

        var go = new GameObject("Moon");
        go.transform.SetParent(cam.transform, false);
        go.transform.localPosition = new Vector3(moonHorizontal * w * 0.5f, moonVertical * h * 0.5f, moonDistance);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = BuildMoonSprite();
        sr.sortingOrder = -90;
        float target = h * moonSize * 1.8f; // sprite gồm cả glow nên to hơn đĩa 1.8 lần
        go.transform.localScale = Vector3.one * (target / sr.sprite.bounds.size.x);
    }

    Sprite BuildMoonSprite()
    {
        int n = 256;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float discR = 0.5f / 1.8f;
        Vector2 shadowCenter = new Vector2(discR * 0.45f, discR * 0.2f);
        float shadowR = discR * 0.9f;

        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float u = x / (n - 1f) - 0.5f, v = y / (n - 1f) - 0.5f;
            float d = Mathf.Sqrt(u * u + v * v);
            Color c;
            if (d <= discR)
            {
                c = moonColor;
                c.a = Mathf.Clamp01((discR - d) * n * 0.5f);
                if (crescent)
                {
                    float sd = Vector2.Distance(new Vector2(u, v), shadowCenter);
                    float cut = Mathf.Clamp01((sd - shadowR) * n * 0.5f); // 0 = trong bóng
                    c.a *= cut;
                }
            }
            else
            {
                float g = Mathf.Clamp01(1f - (d - discR) / (0.5f - discR));
                c = moonGlowColor;
                c.a = moonGlowColor.a * g * g;
            }
            tex.SetPixel(x, y, c);
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 1f);
    }

    // ---------- Sao ----------
    void BuildStars()
    {
        viewHeight = ViewHeightAt(starDistance);
        viewWidth = viewHeight * cam.aspect;

        var sprite = BuildDotSprite();
        stars = new SpriteRenderer[starCount];
        twinkleSpeeds = new float[starCount];
        twinklePhases = new float[starCount];
        starBaseAlpha = new float[starCount];

        for (int i = 0; i < starCount; i++)
        {
            var go = new GameObject("Star_" + i);
            go.transform.SetParent(cam.transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = -92;
            sr.color = starColor;

            float size = viewHeight * Random.Range(starSizeRange.x, starSizeRange.y);
            go.transform.localScale = Vector3.one * (size / sprite.bounds.size.x);
            go.transform.localPosition = new Vector3(
                Random.Range(-viewWidth * 0.5f, viewWidth * 0.5f),
                Random.Range(-viewHeight * 0.5f, viewHeight * 0.5f),
                starDistance);

            stars[i] = sr;
            twinkleSpeeds[i] = Random.Range(twinkleSpeedRange.x, twinkleSpeedRange.y);
            twinklePhases[i] = Random.Range(0f, Mathf.PI * 2f);
            starBaseAlpha[i] = starColor.a * Random.Range(0.5f, 1f);
        }
    }

    Sprite BuildDotSprite()
    {
        int n = 32;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float u = (x / (n - 1f) - 0.5f) * 2f, v = (y / (n - 1f) - 0.5f) * 2f;
            float a = Mathf.Clamp01(1f - Mathf.Sqrt(u * u + v * v));
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 1f);
    }

    // ---------- Sao băng ----------
    void BuildShootingStar()
    {
        float h = ViewHeightAt(shootingDistance);
        var go = new GameObject("ShootingStar");
        go.transform.SetParent(cam.transform, false);
        shootSr = go.AddComponent<SpriteRenderer>();
        shootSr.sprite = BuildStreakSprite();
        shootSr.sortingOrder = -88;
        shootSr.color = Color.clear;
        go.transform.localScale = new Vector3(
            h * shootingLength / shootSr.sprite.bounds.size.x,
            h * 0.012f / shootSr.sprite.bounds.size.y, 1f);
        shoot = go.transform;
        shootActive = false;
        shootTimer = Random.Range(shootingIntervalRange.x, shootingIntervalRange.y);
    }

    // Đầu sao băng nằm bên phải (x = 1), đuôi mờ dần về bên trái
    Sprite BuildStreakSprite()
    {
        int w = 128, hgt = 8;
        var tex = new Texture2D(w, hgt, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < hgt; y++)
        for (int x = 0; x < w; x++)
        {
            float t = x / (w - 1f);
            float v = Mathf.Abs((y / (hgt - 1f) - 0.5f) * 2f);
            float a = t * t * (1f - v * v);
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(a)));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, hgt), new Vector2(0.5f, 0.5f), 1f);
    }

    void SpawnShootingStar()
    {
        float h = ViewHeightAt(shootingDistance);
        float w = h * cam.aspect;
        float angle = Random.Range(200f, 230f); // bay xuống góc trái
        shootDir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        shoot.localRotation = Quaternion.Euler(0, 0, angle);
        shoot.localPosition = new Vector3(
            Random.Range(-0.1f, 0.5f) * w,
            Random.Range(0.1f, 0.5f) * h,
            shootingDistance);
        shootAge = 0f;
        shootLife = Random.Range(0.7f, 1.2f);
        shootActive = true;
    }

    void Update()
    {
        // Sao nhấp nháy
        if (stars != null)
        {
            float t = Time.time;
            for (int i = 0; i < stars.Length; i++)
            {
                Color c = starColor;
                c.a = starBaseAlpha[i] * (0.35f + 0.65f * (0.5f + 0.5f * Mathf.Sin(t * twinkleSpeeds[i] + twinklePhases[i])));
                stars[i].color = c;
            }
        }

        // Sao băng
        if (shoot == null) return;
        if (!shootActive)
        {
            shootTimer -= Time.deltaTime;
            if (shootTimer <= 0f) SpawnShootingStar();
            return;
        }

        shootAge += Time.deltaTime;
        float p = shootAge / shootLife;
        if (p >= 1f)
        {
            shootActive = false;
            shootSr.color = Color.clear;
            shootTimer = Random.Range(shootingIntervalRange.x, shootingIntervalRange.y);
            return;
        }

        float w = ViewHeightAt(shootingDistance) * cam.aspect;
        shoot.localPosition += (Vector3)(shootDir * (w * shootingSpeed * Time.deltaTime));
        Color sc = Color.white;
        sc.a = Mathf.Sin(p * Mathf.PI); // hiện dần rồi tắt dần
        shootSr.color = sc;
    }
}

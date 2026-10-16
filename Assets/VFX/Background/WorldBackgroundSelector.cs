using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BgScene
{
    None,
    Synthwave,
    Sunset,
    Dream,
    Aurora,
    Rainy,
    Blossom,
    Night,
    Day,
    Winter,
    Forest,
    Autumn,
    Equalizer,
    SynthBars
}

/// <summary>
/// Gộp tất cả background vào 1 chỗ: chọn cảnh trong dropdown, script tự thêm gradient + hiệu ứng phù hợp.
///
/// CÁCH DÙNG: gắn script này vào Main Camera, chọn "Scene". Đổi dropdown lúc đang Play cũng được.
/// Đổi cảnh bằng code:  GetComponent<WorldBackgroundSelector>().SetScene(BgScene.Rainy);
///
/// YÊU CẦU: trong project phải có đủ các script: WorldBackgroundGradientPro, BackgroundExtras,
/// BackgroundNightSky, BackgroundClouds, BackgroundSnow, BackgroundFireflies, BackgroundSynthwave,
/// BackgroundMountains, BackgroundAurora, BackgroundRain, BackgroundPetals, BackgroundBokeh,
/// BackgroundEqualizer, BackgroundBeatReactive.
/// KHÔNG gắn riêng các script đó lên Camera nữa (và gỡ WorldBackgroundGradient cũ) để khỏi bị trùng.
/// </summary>
public class WorldBackgroundSelector : MonoBehaviour
{
    [Header("Chọn cảnh nền")]
    public BgScene scene = BgScene.Synthwave;
    [Tooltip("Tắt nếu bạn muốn tự dùng nền gradient riêng, chỉ lấy hiệu ứng")]
    public bool includeGradient = true;
    [Tooltip("Tự xoá các script/object background cũ còn sót trên Camera (gắn tay từ trước) để khỏi bị trùng hiệu ứng")]
    public bool cleanLeftovers = true;
    [Tooltip("Bật = mọi cảnh đều động: đập theo nhịp, phản ứng theo nhạc và combo, chớp khi Miss / Perfect")]
    public bool dynamicBackground = true;

    BgScene applied = (BgScene)(-1);
    Transform camTf;
    readonly List<Component> added = new List<Component>();
    readonly List<GameObject> spawned = new List<GameObject>();
    HashSet<Transform> pendingBefore;

    void Start()
    {
        var cam = GetComponent<Camera>();
        if (!cam) cam = Camera.main;
        if (!cam) { Debug.LogError("WorldBackgroundSelector: không tìm thấy Camera."); enabled = false; return; }
        camTf = cam.transform;
        Apply(scene);
    }

    void Update()
    {
        if (scene != applied) Apply(scene); // đổi dropdown lúc đang chạy
    }

    // ---------- API gọi từ code / UnityEvent ----------
    public void SetScene(BgScene s) { scene = s; if (camTf) Apply(s); }
    public void SetScene(int index) { SetScene((BgScene)index); }
    public void NextScene()
    {
        int count = System.Enum.GetValues(typeof(BgScene)).Length;
        SetScene((BgScene)(((int)scene + 1) % count));
    }

    // ---------- Quản lý thêm / dọn ----------
    void Apply(BgScene s)
    {
        Clear();
        if (cleanLeftovers) PurgeLeftovers();
        applied = s;
        if (s == BgScene.None) return;

        pendingBefore = new HashSet<Transform>();
        foreach (Transform c in camTf) pendingBefore.Add(c);

        Build(s);
        StartCoroutine(CollectNextFrame());
    }

    // Các script con tạo object con của Camera trong Start() -> ghi nhận để dọn khi đổi cảnh
    IEnumerator CollectNextFrame()
    {
        yield return null;
        CollectNow();
    }

    void CollectNow()
    {
        if (pendingBefore == null) return;
        foreach (Transform c in camTf)
            if (!pendingBefore.Contains(c)) spawned.Add(c.gameObject);
        pendingBefore = null;
    }

    void Clear()
    {
        if (camTf) CollectNow();
        StopAllCoroutines();
        foreach (var c in added) if (c) Destroy(c);
        foreach (var go in spawned) if (go) Destroy(go);
        added.Clear();
        spawned.Clear();
    }

    // ---------- Dọn đồ cũ còn sót (gắn tay từ trước, không phải do Selector tạo) ----------
    static readonly HashSet<string> BgScriptNames = new HashSet<string>
    {
        "WorldBackgroundGradient", "WorldBackgroundGradientPro", "WorldBackgroundNightGradient",
        "WorldBackgroundSkyGradient", "WorldBackgroundWinterGradient", "WorldBackgroundForestGradient",
        "BackgroundExtras", "BackgroundNightSky", "BackgroundClouds", "BackgroundSnow",
        "BackgroundFireflies", "BackgroundSynthwave", "BackgroundMountains", "BackgroundAurora",
        "BackgroundRain", "BackgroundPetals", "BackgroundBokeh", "BackgroundEqualizer"
    };

    static readonly HashSet<string> BgObjectNames = new HashSet<string>
    {
        "Sun", "Moon", "ShootingStar", "SynthSun", "SynthFloor", "HorizonLine", "LightningFlash",
        "WorldBackgroundGradient", "WorldBackgroundGradientPro", "WorldBackgroundNightGradient",
        "WorldBackgroundSkyGradient", "WorldBackgroundWinterGradient", "WorldBackgroundForestGradient"
    };

    static readonly string[] BgObjectPrefixes =
    {
        "Dust_", "Star_", "Cloud_", "Snow_", "Firefly_", "GridV_", "GridH_",
        "Mountain_", "Aurora_", "Rain_", "Petal_", "Bokeh_", "EqBar_", "EqRefl_"
    };

    void PurgeLeftovers()
    {
        PurgeComponents(gameObject);
        if (camTf.gameObject != gameObject) PurgeComponents(camTf.gameObject);

        for (int i = camTf.childCount - 1; i >= 0; i--)
        {
            var child = camTf.GetChild(i).gameObject;
            if (IsBgObjectName(child.name)) Destroy(child);
        }
    }

    void PurgeComponents(GameObject go)
    {
        foreach (var mb in go.GetComponents<MonoBehaviour>())
        {
            if (!mb) continue;
            string n = mb.GetType().Name;
            // BackgroundBeatReactive chỉ bị dọn khi Selector tự thêm bản của nó (Dynamic Background bật)
            if (BgScriptNames.Contains(n) || (dynamicBackground && n == "BackgroundBeatReactive")) Destroy(mb);
        }
    }

    bool IsBgObjectName(string n)
    {
        if (BgObjectNames.Contains(n)) return true;
        if (dynamicBackground && (n == "BeatFlash" || n.StartsWith("BeatRing_"))) return true;
        foreach (var p in BgObjectPrefixes) if (n.StartsWith(p)) return true;
        return false;
    }

    T Add<T>() where T : Component
    {
        var c = gameObject.AddComponent<T>();
        added.Add(c);
        return c;
    }

    WorldBackgroundGradientPro Gradient(BgGradientPreset p)
    {
        if (!includeGradient) return null;
        var g = Add<WorldBackgroundGradientPro>();
        g.preset = p;
        return g;
    }

    // ---------- Công thức từng cảnh ----------
    void Build(BgScene s)
    {
        switch (s)
        {
            case BgScene.Synthwave:
            {
                Gradient(BgGradientPreset.Synthwave);
                Add<BackgroundSynthwave>();
                var sky = Add<BackgroundNightSky>();
                sky.showMoon = false;
                sky.starCount = 60;
                break;
            }
            case BgScene.Sunset:
            {
                Gradient(BgGradientPreset.Sunset);
                Add<BackgroundMountains>();
                var sun = Add<BackgroundExtras>();
                sun.sunHorizontal = 0.35f;
                sun.sunVertical = 0.1f;
                break;
            }
            case BgScene.Dream:
            {
                Gradient(BgGradientPreset.Dream);
                Add<BackgroundBokeh>();
                var ex = Add<BackgroundExtras>();
                ex.showSun = false;
                ex.particleColor = new Color(1f, 0.8f, 0.95f, 0.7f);
                break;
            }
            case BgScene.Aurora:
            {
                Gradient(BgGradientPreset.Aurora);
                Add<BackgroundAurora>();
                var sky = Add<BackgroundNightSky>();
                sky.showMoon = false;
                sky.showShootingStars = true;
                break;
            }
            case BgScene.Rainy:
            {
                Gradient(BgGradientPreset.Rainy);
                var clouds = Add<BackgroundClouds>();
                clouds.cloudColor = new Color(0.45f, 0.50f, 0.58f, 0.7f);
                clouds.cloudCount = 10;
                clouds.driftSpeedRange = new Vector2(0.4f, 1.4f);
                clouds.verticalRange = new Vector2(0.3f, 0.95f);
                Add<BackgroundRain>();
                break;
            }
            case BgScene.Blossom:
            {
                Gradient(BgGradientPreset.Blossom);
                var clouds = Add<BackgroundClouds>();
                clouds.cloudColor = new Color(1f, 1f, 1f, 0.45f);
                clouds.cloudCount = 5;
                Add<BackgroundPetals>();
                break;
            }
            case BgScene.Night:
            {
                Gradient(BgGradientPreset.Night);
                Add<BackgroundNightSky>();
                break;
            }
            case BgScene.Day:
            {
                Gradient(BgGradientPreset.Day);
                Add<BackgroundClouds>();
                var sun = Add<BackgroundExtras>();
                sun.showParticles = false;
                sun.sunHorizontal = -0.6f;
                sun.sunVertical = 0.55f;
                sun.sunTopColor = new Color(1f, 0.95f, 0.6f, 1f);
                sun.sunBottomColor = new Color(1f, 0.8f, 0.4f, 1f);
                sun.glowColor = new Color(1f, 0.9f, 0.6f, 0.4f);
                break;
            }
            case BgScene.Winter:
            {
                Gradient(BgGradientPreset.Winter);
                var clouds = Add<BackgroundClouds>();
                clouds.cloudColor = new Color(0.8f, 0.88f, 0.95f, 0.5f);
                clouds.cloudCount = 6;
                Add<BackgroundSnow>();
                break;
            }
            case BgScene.Forest:
            {
                Gradient(BgGradientPreset.Forest);
                var mt = Add<BackgroundMountains>();
                mt.farColor = new Color(0.08f, 0.22f, 0.20f, 1f);
                mt.nearColor = new Color(0.02f, 0.08f, 0.07f, 1f);
                mt.seed = 3;
                Add<BackgroundFireflies>();
                break;
            }
            case BgScene.Autumn:
            {
                var g = Gradient(BgGradientPreset.Custom);
                if (g != null)
                {
                    g.topColor = new Color(0.35f, 0.18f, 0.30f);
                    g.middleColor = new Color(0.85f, 0.42f, 0.22f);
                    g.bottomColor = new Color(1.00f, 0.72f, 0.35f);
                    g.middlePosition = 0.45f;
                }
                var mt = Add<BackgroundMountains>();
                mt.farColor = new Color(0.75f, 0.45f, 0.35f, 1f);
                mt.nearColor = new Color(0.25f, 0.10f, 0.10f, 1f);
                mt.seed = 11;
                var leaves = Add<BackgroundPetals>();
                leaves.petalCount = 35;
                leaves.colorA = new Color(1f, 0.60f, 0.15f, 0.95f);
                leaves.colorB = new Color(0.80f, 0.20f, 0.10f, 0.95f);
                break;
            }
            case BgScene.Equalizer:
            {
                Gradient(BgGradientPreset.Dream);
                var eq = Add<BackgroundEqualizer>();
                eq.baseline = -0.35f;
                var bokeh = Add<BackgroundBokeh>();
                bokeh.count = 14;
                break;
            }
            case BgScene.SynthBars:
            {
                Gradient(BgGradientPreset.Synthwave);
                Add<BackgroundSynthwave>();
                var sky = Add<BackgroundNightSky>();
                sky.showMoon = false;
                sky.starCount = 60;
                var eq = Add<BackgroundEqualizer>();
                eq.baseline = 0f;     // đứng trên đường chân trời của Synthwave
                eq.maxHeight = 0.3f;
                break;
            }
        }

        // Làm cảnh vừa dựng "sống": đập theo nhịp / nhạc / combo
        if (dynamicBackground)
        {
            var react = Add<BackgroundBeatReactive>();
            react.beatRings = s == BgScene.Synthwave || s == BgScene.SynthBars || s == BgScene.Sunset
                           || s == BgScene.Dream || s == BgScene.Night || s == BgScene.Aurora
                           || s == BgScene.Equalizer;
        }
    }
}
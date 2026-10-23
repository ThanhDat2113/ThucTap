using UnityEngine;

/// <summary>
/// Viền neon sáng 2 mép đường, đầu xa mờ dần.
/// Chỉ dùng sprite nền của từng làn (con trực tiếp của "Lanes") và CHỐT vị trí 1 lần lúc Start,
/// nên hiệu ứng bấm phím (tia sáng, hit effect...) không làm viền bị méo/mất.
/// CÁCH DÙNG: gắn vào object bất kỳ (vd "Lanes"). Nếu đường không nằm dưới "Lanes", kéo nó vào lanesRoot.
/// </summary>
public class RoadNeonEdge : MonoBehaviour
{
    public Transform lanesRoot;
    public Color coreColor = new Color(1f, 1f, 1f, 1f);
    public Color glowColor = new Color(0.3f, 0.9f, 1f, 0.55f);
    [Range(0f, 1f)] public float farAlpha = 0f;

    [Header("Độ dày (world unit)")]
    public float coreWidth = 0.06f;
    public float glowWidth = 0.35f;

    [Header("Nhịp sáng (0 = tắt)")]
    public float pulseAmount = 0.15f;
    public float pulseSpeed = 3f;

    [Tooltip("Bật nếu đường của bạn di chuyển/co giãn khi chạy. Mặc định tắt để tránh bị ảnh hưởng bởi hiệu ứng bấm phím.")]
    public bool recomputeEveryFrame = false;

    Camera cam;
    LineRenderer[] lines = new LineRenderer[4]; // [leftGlow, leftCore, rightGlow, rightCore]
    SpriteRenderer[] baseRends;
    Material mat;
    Vector3 lNear, lFar, rNear, rFar;

    void Start()
    {
        cam = Camera.main;
        if (!lanesRoot)
        {
            var go = GameObject.Find("Lanes");
            lanesRoot = go ? go.transform : transform;
        }

        // Chỉ lấy SpriteRenderer nằm ngay trên từng làn (con trực tiếp), bỏ qua con cháu (beam, hit effect...)
        var list = new System.Collections.Generic.List<SpriteRenderer>();
        foreach (Transform child in lanesRoot)
        {
            var sr = child.GetComponent<SpriteRenderer>();
            if (sr && sr.sprite) list.Add(sr);
        }
        if (list.Count == 0) // dự phòng: cả 4 làn là con cháu
            list.AddRange(lanesRoot.GetComponentsInChildren<SpriteRenderer>(true));
        baseRends = list.ToArray();

        if (baseRends.Length == 0 || !cam)
        {
            Debug.LogError("RoadNeonEdge: không tìm thấy sprite làn hoặc Camera.");
            enabled = false; return;
        }

        mat = new Material(Shader.Find("Sprites/Default"));
        for (int i = 0; i < 4; i++)
        {
            var go = new GameObject(i < 2 ? "NeonLeft" : "NeonRight");
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 2;
            lr.material = mat;
            lr.numCapVertices = 4;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lines[i] = lr;
        }

        int maxOrder = int.MinValue;
        foreach (var r in baseRends) maxOrder = Mathf.Max(maxOrder, r.sortingOrder);
        for (int i = 0; i < 4; i++)
            lines[i].sortingOrder = maxOrder + (i % 2 == 0 ? 1 : 2);

        ComputeEdges();
    }

    void ComputeEdges()
    {
        Vector3 la = default, lb = default, ra = default, rb = default;
        float minCx = float.MaxValue, maxCx = float.MinValue;

        foreach (var r in baseRends)
        {
            if (!r || !r.sprite) continue;
            Bounds b = r.sprite.bounds;
            Vector3[] w = new Vector3[4];
            w[0] = r.transform.TransformPoint(new Vector3(b.min.x, b.min.y, 0));
            w[1] = r.transform.TransformPoint(new Vector3(b.min.x, b.max.y, 0));
            w[2] = r.transform.TransformPoint(new Vector3(b.max.x, b.min.y, 0));
            w[3] = r.transform.TransformPoint(new Vector3(b.max.x, b.max.y, 0));

            System.Array.Sort(w, (p, q) =>
                cam.transform.InverseTransformPoint(p).x.CompareTo(cam.transform.InverseTransformPoint(q).x));

            float lx = cam.transform.InverseTransformPoint(w[0]).x;
            float rx = cam.transform.InverseTransformPoint(w[3]).x;
            if (lx < minCx) { minCx = lx; la = w[0]; lb = w[1]; }
            if (rx > maxCx) { maxCx = rx; ra = w[3]; rb = w[2]; }
        }

        OrderNearFar(la, lb, out lNear, out lFar);
        OrderNearFar(ra, rb, out rNear, out rFar);
    }

    void OrderNearFar(Vector3 a, Vector3 b, out Vector3 near, out Vector3 far)
    {
        float za = cam.transform.InverseTransformPoint(a).z;
        float zb = cam.transform.InverseTransformPoint(b).z;
        if (za <= zb) { near = a; far = b; } else { near = b; far = a; }
    }

    void LateUpdate()
    {
        if (recomputeEveryFrame) ComputeEdges();
        float pulse = 1f + pulseAmount * Mathf.Sin(Time.time * pulseSpeed);
        Apply(lines[0], lNear, lFar, glowColor, glowWidth, pulse);
        Apply(lines[1], lNear, lFar, coreColor, coreWidth, pulse);
        Apply(lines[2], rNear, rFar, glowColor, glowWidth, pulse);
        Apply(lines[3], rNear, rFar, coreColor, coreWidth, pulse);
    }

    void Apply(LineRenderer lr, Vector3 a, Vector3 b, Color c, float width, float pulse)
    {
        lr.SetPosition(0, a);
        lr.SetPosition(1, b);
        lr.widthMultiplier = width;
        Color near = c; near.a = Mathf.Clamp01(c.a * pulse);
        Color far = c; far.a = near.a * farAlpha;
        lr.startColor = near;
        lr.endColor = far;
    }
}
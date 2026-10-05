using UnityEngine;

/// <summary>
/// Cột sáng hình thang trên mỗi làn khi người chơi bấm (rộng ở gần, hẹp và mờ dần ở xa),
/// kèm hiệu ứng sáng viền cho pad đánh nốt. Mesh được tạo bằng code nên không cần sprite.
/// Gọi Press() khi bấm và Release() khi thả từ script input của bạn.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class LaneBeam : MonoBehaviour
{
    [Header("Hình dạng cột sáng (đơn vị world)")]
    public float bottomWidth = 2.2f;
    public float topWidth = 0.9f;
    public float height = 8f;

    [Header("Màu & độ sáng")]
    public Color beamColor = new Color(0.65f, 1f, 0.3f, 1f);   // mỗi làn 1 màu: xanh lá, hồng, xanh dương...
    [Range(0f, 1f)] public float maxAlpha = 0.75f;
    public float fadeInSpeed = 40f;
    public float fadeOutSpeed = 5f;

    [Header("Pad đánh nốt (tuỳ chọn)")]
    public SpriteRenderer padGlow;         // sprite viền phát sáng quanh pad
    public Color padGlowColor = Color.white;
    public float padPunchScale = 1.12f;

    [Tooltip("Để trống sẽ dùng Sprites/Default. Với URP 2D nên gán material Sprite-Unlit-Default.")]
    public Material beamMaterial;

    MeshRenderer mr;
    Material mat;
    float alpha;
    bool held;
    Vector3 padBaseScale = Vector3.one;
    Color padBaseColor = Color.white;

    void Awake()
    {
        mr = GetComponent<MeshRenderer>();
        mat = beamMaterial ? new Material(beamMaterial) : new Material(Shader.Find("Sprites/Default"));
        mr.material = mat;
        GetComponent<MeshFilter>().mesh = BuildMesh();

        if (padGlow)
        {
            padBaseScale = padGlow.transform.localScale;
            padBaseColor = padGlow.color;
        }
        ApplyAlpha(0f);
    }

    Mesh BuildMesh()
    {
        float bw = bottomWidth * 0.5f, tw = topWidth * 0.5f;
        var m = new Mesh();
        m.vertices = new[]
        {
            new Vector3(-bw, 0, 0), new Vector3(bw, 0, 0),
            new Vector3(tw, height, 0), new Vector3(-tw, height, 0)
        };
        // Đáy sáng, đỉnh trong suốt -> hiệu ứng mờ dần về phía xa
        var bottom = new Color(beamColor.r, beamColor.g, beamColor.b, 1f);
        var top = new Color(beamColor.r, beamColor.g, beamColor.b, 0f);
        m.colors = new[] { bottom, bottom, top, top };
        m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        m.RecalculateBounds();
        return m;
    }

    public void Press()
    {
        held = true;
        alpha = maxAlpha;   // nháy sáng tức thì
        if (padGlow)
        {
            padGlow.transform.localScale = padBaseScale * padPunchScale;
            padGlow.color = padGlowColor;
        }
    }

    public void Release() => held = false;

    void Update()
    {
        float target = held ? maxAlpha : 0f;
        float speed = held ? fadeInSpeed : fadeOutSpeed;
        alpha = Mathf.MoveTowards(alpha, target, speed * Time.deltaTime);
        ApplyAlpha(alpha);

        if (padGlow)
        {
            padGlow.transform.localScale = Vector3.Lerp(padGlow.transform.localScale, padBaseScale, Time.deltaTime * 14f);
            padGlow.color = Color.Lerp(padGlow.color, padBaseColor, Time.deltaTime * 6f);
        }
    }

    void ApplyAlpha(float a)
    {
        var c = mat.color;
        c.a = a;
        mat.color = c;
    }

    void OnDestroy() { if (mat) Destroy(mat); }
}

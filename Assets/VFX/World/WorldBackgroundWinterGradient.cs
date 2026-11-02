using UnityEngine;

/// <summary>
/// Gradient mùa đông: xanh xám lạnh trên đỉnh, xanh băng ở giữa, trắng xanh nhạt phía dưới.
/// Tạo 1 Quad gradient 3 màu (trên -> giữa -> dưới), đặt làm con của Camera và nằm thật xa
/// phía sau mọi thứ trong scene (không phải UI Overlay nên không che làn nhạc, note...).
///
/// CÁCH DÙNG: gắn thẳng vào Main Camera. Hợp với: BackgroundSnow (tuyết rơi, để hạt tuyết trắng vẫn nổi trên nền).
/// Lưu ý: distance phải LỚN HƠN khoảng cách của các script hiệu ứng (tối đa 50) để gradient luôn nằm sau cùng.
/// </summary>
public class WorldBackgroundWinterGradient : MonoBehaviour
{
    [Header("Màu gradient (trên -> giữa -> dưới)")]
    public Color topColor = new Color(0.28f, 0.38f, 0.58f);
    public Color middleColor = new Color(0.50f, 0.66f, 0.84f);
    public Color bottomColor = new Color(0.82f, 0.92f, 0.98f);
    [Tooltip("Vị trí màu giữa tính từ đáy: 0 = đáy, 1 = đỉnh")]
    [Range(0.05f, 0.95f)] public float middlePosition = 0.45f;

    [Header("Khoảng cách đặt phía sau Camera (world units)")]
    public float distance = 60f;
    [Tooltip("Phóng to thêm so với khung nhìn vừa đủ, để phủ kín kể cả khi camera xoay nhẹ")]
    public float overscan = 1.15f;

    public int resolution = 128;

    void Start()
    {
        var cam = GetComponent<Camera>();
        if (!cam) cam = Camera.main;
        if (!cam) { Debug.LogError("WorldBackgroundWinterGradient: không tìm thấy Camera."); return; }

        var quadGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quadGO.name = "WorldBackgroundWinterGradient";
        Destroy(quadGO.GetComponent<Collider>()); // không cần va chạm

        quadGO.transform.SetParent(cam.transform, false);
        quadGO.transform.localRotation = Quaternion.identity;

        float heightWorld = cam.orthographic
            ? cam.orthographicSize * 2f
            : 2f * distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float widthWorld = heightWorld * cam.aspect;

        quadGO.transform.localPosition = new Vector3(0f, 0f, distance);
        quadGO.transform.localScale = new Vector3(widthWorld * overscan, heightWorld * overscan, 1f);

        var mr = quadGO.GetComponent<MeshRenderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.sortingOrder = -100; // luôn vẽ trước các hiệu ứng khác

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Texture");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        var mat = new Material(shader);
        mat.mainTexture = BuildGradientTexture();
        mr.material = mat;
    }

    Texture2D BuildGradientTexture()
    {
        var tex = new Texture2D(4, resolution, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < resolution; y++)
        {
            float t = (float)y / (resolution - 1); // 0 = dưới texture, 1 = trên
            Color c = t < middlePosition
                ? Color.Lerp(bottomColor, middleColor, t / middlePosition)
                : Color.Lerp(middleColor, topColor, (t - middlePosition) / (1f - middlePosition));
            for (int x = 0; x < 4; x++) tex.SetPixel(x, y, c);
        }
        tex.Apply();
        return tex;
    }
}

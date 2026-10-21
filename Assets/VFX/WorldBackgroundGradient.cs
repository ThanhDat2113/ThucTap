using UnityEngine;

namespace RhythmGame
{
    /// <summary>
    /// Tạo 1 mặt phẳng (Quad) gradient tím-hồng, đặt làm con của Camera, nằm thật xa
    /// phía sau mọi thứ trong scene. Vì nó nằm trong không gian 3D (không phải UI
    /// Overlay), Camera sẽ tự vẽ đúng thứ tự theo độ sâu — không che mất làn nhạc,
    /// note, hay bất cứ gì bạn cần bấm/nhìn thấy.
    ///
    /// CÁCH DÙNG: gắn script này thẳng vào Main Camera (không phải object rời).
    /// </summary>
    public class WorldBackgroundGradient : MonoBehaviour
    {
        [Header("Màu gradient (trên -> dưới)")]
        public Color topColor = new Color(0.45f, 0.15f, 0.55f);
        public Color bottomColor = new Color(0.95f, 0.25f, 0.65f);

        [Header("Màu khi In the Flow")]
        public Color flowTopColor = new Color(0.1f, 0f, 0.4f);
        public Color flowBottomColor = new Color(0.3f, 0f, 0.8f);

        [Header("Khoảng cách đặt phía sau Camera (world units)")]
        public float distance = 50f;
        [Tooltip("Phóng to thêm bao nhiêu % so với khung nhìn vừa đủ, để chắc chắn phủ kín kể cả khi camera xoay nhẹ")]
        public float overscan = 1.15f;

        [Header("Flow System")]
        public FlowSystem flowSystem;

        public int resolution = 128;

        MeshRenderer _mr;
        Material _mat;

        void Start()
        {
            var cam = GetComponent<Camera>();
            if (!cam) cam = Camera.main;
            if (!cam) { Debug.LogError("WorldBackgroundGradient: không tìm thấy Camera."); return; }

            if (flowSystem != null)
            {
                flowSystem.OnFlowStateChanged += OnFlowStateChanged;
            }

            BuildQuad(cam);
        }

        void OnDestroy()
        {
            if (flowSystem != null)
            {
                flowSystem.OnFlowStateChanged -= OnFlowStateChanged;
            }
        }

        void OnFlowStateChanged(bool isFlowActive)
        {
            if (_mat == null) return;
            var tex = isFlowActive ? BuildGradientTexture(flowTopColor, flowBottomColor) : BuildGradientTexture(topColor, bottomColor);
            _mat.mainTexture = tex;
        }

        void BuildQuad(Camera cam)
        {
            var quadGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quadGO.name = "WorldBackgroundGradient";
            Destroy(quadGO.GetComponent<Collider>()); // không cần va chạm

            quadGO.transform.SetParent(cam.transform, false);
            quadGO.transform.localRotation = Quaternion.identity;

            float heightWorld, widthWorld;
            if (cam.orthographic)
            {
                heightWorld = cam.orthographicSize * 2f;
            }
            else
            {
                heightWorld = 2f * distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            }
            widthWorld = heightWorld * cam.aspect;

            quadGO.transform.localPosition = new Vector3(0f, 0f, distance);
            quadGO.transform.localScale = new Vector3(widthWorld * overscan, heightWorld * overscan, 1f);

            _mr = quadGO.GetComponent<MeshRenderer>();
            _mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _mr.receiveShadows = false;
            _mr.sortingOrder = -100; // luôn vẽ trước sun / streaks / dust

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Texture");
            _mat = new Material(shader);
            _mat.mainTexture = BuildGradientTexture(topColor, bottomColor);
            _mr.material = _mat;
        }

        Texture2D BuildGradientTexture(Color top, Color bottom)
        {
            var tex = new Texture2D(4, resolution, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < resolution; y++)
            {
                float t = (float)y / (resolution - 1); // 0 = dưới texture, 1 = trên
                Color c = Color.Lerp(bottom, top, t);
                for (int x = 0; x < 4; x++) tex.SetPixel(x, y, c);
            }
            tex.Apply();
            return tex;
        }
    }
}

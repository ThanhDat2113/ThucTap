using UnityEngine;

namespace RhythmGame
{
    public class FlowAuroraEffect : MonoBehaviour
    {
        public FlowSystem flowSystem;

        [Header("Subtle defaults")]
        [Tooltip("Số hạt. Nên 10–25 để không rối mắt.")]
        public int particleCount = 18;

        [Tooltip("Màu hạt — giữ alpha thấp (0.2–0.4).")]
        public Color particleColor = new Color(1f, 0.9f, 0.4f, 0.3f);

        [Tooltip("Tốc độ bay (chậm cho dịu mắt).")]
        public float speed = 1.2f;

        public Vector3 flyDirection = Vector3.up;
        public Vector3 spawnOffset = Vector3.zero;
        public float lifetime = 3.5f;

        [Header("Layout")]
        [Tooltip("Chỉ spawn ở 2 dải mép trái/phải để tránh vùng note.")]
        public bool spawnOnSides = true;

        [Tooltip("Độ rộng mỗi dải spawn ở mỗi bên.")]
        public float sideBandWidth = 2f;

        public float spawnHalfWidth  = 8f;
        public float spawnHalfHeight = 6f;

        [Tooltip("Khoảng hở trên đỉnh trước khi tái sinh hạt.")]
        public float recycleMargin = 1f;

        [Header("Rendering")]
        [Tooltip("Sorting layer — đặt trùng tên layer của background.")]
        public string sortingLayerName = "Default";

        [Tooltip("Sorting order âm để hạt nằm sau note.")]
        public int sortingOrder = -10;

        [Tooltip("Kích thước gốc của hạt (world units).")]
        public float baseSize = 0.08f;

        [Tooltip("Biên độ nhấp nháy kích thước (0 = tắt).")]
        [Range(0f, 0.5f)] public float sizePulse = 0.15f;

        public float pulseSpeed = 2f;

        GameObject[] particles;
        float[] timers;
        float[] baseScales;

        void Start()
        {
            if (flowSystem == null)
                flowSystem = FindObjectOfType<FlowSystem>();
            if (flowSystem != null) flowSystem.OnFlowStateChanged += SetActive;
            Build();
            SetActive(flowSystem != null && flowSystem.IsFlowActive);
        }

        void OnDestroy()
        {
            if (flowSystem != null) flowSystem.OnFlowStateChanged -= SetActive;
        }

        void Build()
        {
            particles  = new GameObject[particleCount];
            timers     = new float[particleCount];
            baseScales = new float[particleCount];

            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var px = new Color[16];
            for (int i = 0; i < px.Length; i++) px[i] = Color.white;
            tex.SetPixels(px);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);

            for (int i = 0; i < particleCount; i++)
            {
                var go = new GameObject("AuroraParticle_" + i);
                go.transform.SetParent(transform, false);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = particleColor;
                sr.sortingLayerName = sortingLayerName;
                sr.sortingOrder = sortingOrder;

                float scale = baseSize * Random.Range(0.7f, 1.3f);
                baseScales[i] = scale;
                go.transform.localScale = Vector3.one * scale;

                float progress = Random.value;
                Vector3 basePos = PickSpawnPosition();
                go.transform.position = basePos + flyDirection.normalized * (progress * speed * lifetime);
                timers[i] = progress * lifetime;

                particles[i] = go;
                go.SetActive(false);
            }
        }

        Vector3 PickSpawnPosition()
        {
            float x;
            if (spawnOnSides)
            {
                bool left = Random.value < 0.5f;
                float band = Mathf.Min(sideBandWidth, spawnHalfWidth * 0.5f);
                x = left
                    ? Random.Range(-spawnHalfWidth, -spawnHalfWidth + band)
                    : Random.Range( spawnHalfWidth - band,  spawnHalfWidth);
            }
            else
            {
                x = Random.Range(-spawnHalfWidth, spawnHalfWidth);
            }
            float y = Random.Range(-spawnHalfHeight, spawnHalfHeight);
            return new Vector3(x, y, 0f) + spawnOffset;
        }

        void SetActive(bool isFlow)
        {
            if (particles == null) return;
            foreach (var p in particles) if (p) p.SetActive(isFlow);
        }

        void Update()
        {
            if (particles == null || flowSystem == null || !flowSystem.IsFlowActive) return;

            float recycleY = spawnOffset.y + spawnHalfHeight + recycleMargin;

            for (int i = 0; i < particles.Length; i++)
            {
                var p = particles[i];
                if (!p) continue;

                timers[i] += Time.deltaTime;
                p.transform.Translate(flyDirection.normalized * speed * Time.deltaTime);

                if (timers[i] >= lifetime || p.transform.position.y > recycleY)
                {
                    timers[i] = 0f;
                    Vector3 pos = PickSpawnPosition();
                    pos.z = 0f;
                    p.transform.position = pos;
                }

                float pulse = sizePulse > 0f
                    ? 1f + sizePulse * Mathf.Sin(Time.time * pulseSpeed + i * 0.7f)
                    : 1f;
                p.transform.localScale = Vector3.one * (baseScales[i] * pulse);
            }
        }
    }
}

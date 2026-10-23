using UnityEngine;
using RhythmGame;

namespace RhythmGame
{
    public class FlowAuroraEffect : MonoBehaviour
    {
        public FlowSystem flowSystem;
        public int particleCount = 60;
        public Color particleColor = new Color(1f, 0.9f, 0.4f, 0.9f);
        public float speed = 2f;
        public Vector3 flyDirection = Vector3.up;
        public Vector3 spawnOffset = Vector3.zero;
        public float lifetime = 3f;

        GameObject[] particles;

        void Start()
        {
            if (flowSystem != null) flowSystem.OnFlowStateChanged += SetActive;
            Build();
            SetActive(flowSystem != null && flowSystem.IsFlowActive);
        }

        void OnDestroy()
        {
            if (flowSystem != null) flowSystem.OnFlowStateChanged -= SetActive;
        }

float[] timers;

        void Build()
        {
            particles = new GameObject[particleCount];
            timers = new float[particleCount];
            for (int i = 0; i < particleCount; i++)
            {
                var go = new GameObject("AuroraParticle_" + i);
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.color = particleColor;
                var tex = new Texture2D(4, 4);
                for (int x = 0; x < 4; x++)
                    for (int y = 0; y < 4; y++)
                        tex.SetPixel(x, y, Color.white);
                tex.Apply();
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
                go.transform.localScale = Vector3.one * Random.Range(0.05f, 0.15f);
                float progress = Random.value;
Vector3 basePos = new Vector3(Random.Range(-8f, 8f), Random.Range(-6f, 6f), 0f) + spawnOffset;
go.transform.position = basePos + flyDirection.normalized * (progress * speed * lifetime);
timers[i] = progress * lifetime;
                particles[i] = go;
                go.SetActive(false);
            }
        }

        void SetActive(bool isFlow)
        {
            if (particles == null) return;
            foreach (var p in particles) if (p) p.SetActive(isFlow);
        }

        void Update()
        {
            if (particles == null || flowSystem == null || !flowSystem.IsFlowActive) return;
            for (int i = 0; i < particles.Length; i++)
            {
                var p = particles[i];
                if (!p) continue;
                timers[i] += Time.deltaTime;
                p.transform.Translate(flyDirection.normalized * speed * Time.deltaTime);
                if (timers[i] >= lifetime || p.transform.position.y > 5f)
                {
                    timers[i] = 0f;
                    p.transform.position = new Vector3(Random.Range(-8f, 8f), Random.Range(-6f, 6f), 0f) + spawnOffset;
                }
                float s = 1f + 0.3f * Mathf.Sin(Time.time * 3f + p.transform.position.x);
                p.transform.localScale = Vector3.one * (0.1f * s);
            }
        }
    }
}
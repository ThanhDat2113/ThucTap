using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Luan.NapTien
{
    public class UIParticleFloatingSystem : MonoBehaviour
    {
        [Header("--- Particle Settings ---")]
        [SerializeField] private int particleCount = 28;
        [SerializeField] private Sprite particleSprite;
        [SerializeField] private Vector2 sizeRange = new Vector2(10f, 26f);
        [SerializeField] private Vector2 speedRange = new Vector2(25f, 65f);
        [SerializeField] private Color[] particleColors = new Color[]
        {
            new Color(0f, 0.9f, 1f, 0.7f),     // Neon Cyan
            new Color(1f, 0.2f, 0.8f, 0.65f),  // Hot Pink
            new Color(1f, 0.85f, 0.2f, 0.75f), // Gold
            new Color(1f, 1f, 1f, 0.85f)       // White
        };

        private class ParticleData
        {
            public RectTransform rt;
            public Image img;
            public float speed;
            public float swayFrequency;
            public float swayAmplitude;
            public float phase;
            public float baseAlpha;
            public float blinkSpeed;
            public float currentX;
            public float currentY;
        }

        private readonly List<ParticleData> _particles = new List<ParticleData>();
        private RectTransform _canvasRt;
        private float _halfWidth = 960f;
        private float _halfHeight = 540f;

        private void Start()
        {
            _canvasRt = GetComponent<RectTransform>();
            if (_canvasRt != null)
            {
                _halfWidth = _canvasRt.rect.width * 0.5f;
                _halfHeight = _canvasRt.rect.height * 0.5f;
            }

            InitParticles();
        }

        private void InitParticles()
        {
            for (int i = 0; i < particleCount; i++)
            {
                var pGo = new GameObject($"Particle_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                pGo.transform.SetParent(transform, false);

                var rt = pGo.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);

                var img = pGo.GetComponent<Image>();
                if (particleSprite != null) img.sprite = particleSprite;
                img.raycastTarget = false;

                Color c = particleColors[Random.Range(0, particleColors.Length)];
                img.color = c;

                float sz = Random.Range(sizeRange.x, sizeRange.y);
                rt.sizeDelta = new Vector2(sz, sz);

                var data = new ParticleData
                {
                    rt = rt,
                    img = img,
                    speed = Random.Range(speedRange.x, speedRange.y),
                    swayFrequency = Random.Range(0.8f, 2.0f),
                    swayAmplitude = Random.Range(15f, 45f),
                    phase = Random.Range(0f, Mathf.PI * 2f),
                    baseAlpha = c.a,
                    blinkSpeed = Random.Range(1.5f, 3.5f),
                    currentX = Random.Range(-_halfWidth, _halfWidth),
                    currentY = Random.Range(-_halfHeight, _halfHeight)
                };

                rt.anchoredPosition = new Vector2(data.currentX, data.currentY);
                _particles.Add(data);
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float time = Time.unscaledTime;

            for (int i = 0; i < _particles.Count; i++)
            {
                var p = _particles[i];
                if (p.rt == null) continue;

                // Move upwards
                p.currentY += p.speed * dt;
                if (p.currentY > _halfHeight + 40f)
                {
                    p.currentY = -_halfHeight - 40f;
                    p.currentX = Random.Range(-_halfWidth, _halfWidth);
                }

                // Sway horizontally with sine
                float sway = Mathf.Sin(time * p.swayFrequency + p.phase) * p.swayAmplitude;
                p.rt.anchoredPosition = new Vector2(p.currentX + sway, p.currentY);

                // Gentle sparkle blinking
                if (p.img != null)
                {
                    float blink = 0.5f + 0.5f * Mathf.Sin(time * p.blinkSpeed + p.phase);
                    Color c = p.img.color;
                    c.a = p.baseAlpha * (0.35f + 0.65f * blink);
                    p.img.color = c;
                }
            }
        }
    }
}

using UnityEngine;

namespace RhythmGame
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class ReceptorVisuals : MonoBehaviour
    {
        [Header("Glow Effect")]
        public SpriteRenderer glowRenderer;
        public float idleGlowAlpha = 0.3f;
        public float pressGlowAlpha = 0.9f;
        public float glowPulseSpeed = 2f;
        public float glowPulseScale = 1.1f;

        [Header("Press Animation")]
        public float pressScale = 0.85f;
        public float pressSpeed = 20f;
        public float releaseSpeed = 8f;

        [Header("Hit Feedback (generated at runtime)")]
        public int burstParticleCount = 15;
        public float burstParticleSize = 0.2f;
        public float burstParticleLifetime = 0.4f;
        public float burstSpeed = 4f;

        [Header("Lane Colors")]
        public Color[] laneColors =
        {
            new Color(0.76f, 0.29f, 0.60f), // Lane 0 - Magenta
            new Color(0.00f, 1.00f, 1.00f), // Lane 1 - Cyan
            new Color(0.07f, 0.98f, 0.02f), // Lane 2 - Green
            new Color(0.98f, 0.22f, 0.25f), // Lane 3 - Red
        };

        [Header("Background Sync")]
        public bool syncWithBackground = true;
        public float bgPulseIntensity = 0.15f;

        SpriteRenderer mainRenderer;
        Material mainMat;
        Material glowMat;
        Vector3 baseScale;
        Color baseColor;
        Color glowBaseColor;
        float targetScale = 1f;
        float currentScale = 1f;
        float glowPulseTimer;
        int currentLane = 0;
        bool isPressed;
        float bgPulsePhase;

        void Awake()
        {
            mainRenderer = GetComponent<SpriteRenderer>();
            baseScale = transform.localScale;
            baseColor = mainRenderer.color;
            currentScale = 1f;

            SetupGlow();
        }

        void SetupGlow()
        {
            if (glowRenderer == null)
            {
                var glowGO = new GameObject("ReceptorGlow");
                glowGO.transform.SetParent(transform, false);
                glowRenderer = glowGO.AddComponent<SpriteRenderer>();
                glowRenderer.sprite = mainRenderer.sprite;
                glowRenderer.sortingLayerID = mainRenderer.sortingLayerID;
                glowRenderer.sortingOrder = mainRenderer.sortingOrder - 1;
            }

            glowMat = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default"));
            if (glowMat.shader == null) glowMat = new Material(Shader.Find("Sprites/Default"));
            glowRenderer.material = glowMat;
        }

        public void Initialize(int lane, Color idleColor)
        {
            currentLane = Mathf.Clamp(lane, 0, laneColors.Length - 1);
            Color laneColor = laneColors[currentLane];

            baseColor = idleColor;
            mainRenderer.color = idleColor;

            if (glowMat != null)
            {
                Color glowColor = laneColor;
                glowColor.a = idleGlowAlpha;
                glowMat.SetColor("_EmissionColor", glowColor * 2f);
                glowMat.EnableKeyword("_EMISSION");
                glowRenderer.color = glowColor;
                glowBaseColor = glowColor;
            }

            glowPulseTimer = currentLane * 1.5f;
            bgPulsePhase = currentLane * 0.5f;
        }

        void Update()
        {
            currentScale = Mathf.MoveTowards(currentScale, targetScale, (isPressed ? pressSpeed : releaseSpeed) * Time.deltaTime);
            transform.localScale = baseScale * currentScale;

            if (glowRenderer != null && glowMat != null)
            {
                glowPulseTimer += Time.deltaTime * glowPulseSpeed;

                float pulse = Mathf.Sin(glowPulseTimer) * 0.5f + 0.5f;
                float targetAlpha = isPressed ? pressGlowAlpha : idleGlowAlpha;
                float alpha = Mathf.Lerp(targetAlpha * 0.7f, targetAlpha, pulse * 0.5f);

                if (syncWithBackground)
                {
                    bgPulsePhase += Time.deltaTime * 0.5f;
                    float bgPulse = (Mathf.Sin(bgPulsePhase) + 1f) * 0.5f;
                    alpha *= Mathf.Lerp(1f - bgPulseIntensity, 1f + bgPulseIntensity, bgPulse);
                }

                Color c = glowBaseColor;
                c.a = alpha;
                glowRenderer.color = c;

                float scalePulse = Mathf.Lerp(1f, glowPulseScale, pulse * 0.3f);
                glowRenderer.transform.localScale = Vector3.one * scalePulse;
            }
        }

        public void SetPressed(bool pressed)
        {
            isPressed = pressed;
            targetScale = pressed ? pressScale : 1f;
        }

        public void PlayHitFeedback(Judgement judgement)
        {
            SpawnBurstParticles(judgement);

            if (judgement != Judgement.Miss)
            {
                StartCoroutine(HitPulse(judgement));
            }
            else
            {
                StartCoroutine(MissShake());
            }
        }

        void SpawnBurstParticles(Judgement judgement)
        {
            Color laneColor = laneColors[currentLane];
            Color startColor = judgement switch
            {
                Judgement.Perfect => Color.white,
                Judgement.Good => Color.Lerp(laneColor, Color.white, 0.5f),
                Judgement.Bad => Color.Lerp(laneColor, new Color(1f, 0.5f, 0.5f), 0.5f),
                _ => new Color(0.5f, 0.5f, 0.5f)
            };

            var go = new GameObject("ReceptorBurst");
            go.transform.position = transform.position;
            var ps = go.AddComponent<ParticleSystem>();
            
            // Ensure particle system is stopped before configuring
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            
            var main = ps.main;
            main.duration = 0.1f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(burstParticleLifetime * 0.5f, burstParticleLifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(burstSpeed * 0.5f, burstSpeed * 2f);
            main.startSize = new ParticleSystem.MinMaxCurve(burstParticleSize * 0.5f, burstParticleSize);
            main.startColor = new ParticleSystem.MinMaxGradient(startColor, laneColor);
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, burstParticleCount) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.2f;
            shape.position = Vector3.zero;
            shape.rotation = Vector3.zero;

            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.radial = new ParticleSystem.MinMaxCurve(0f, burstSpeed);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(startColor, 0f), new GradientColorKey(laneColor, 0.5f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.5f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = grad;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(1f, 0f)
            ));

            ps.Play();
            Destroy(go, burstParticleLifetime + 0.5f);
        }

        System.Collections.IEnumerator HitPulse(Judgement judgement)
        {
            Color flashColor = judgement switch
            {
                Judgement.Perfect => Color.white,
                Judgement.Good => new Color(1f, 1f, 0.8f),
                Judgement.Bad => new Color(1f, 0.6f, 0.6f),
                _ => Color.gray
            };

            float originalScale = currentScale;
            float pulseScale = 1.2f;
            float elapsed = 0f;
            float duration = 0.15f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float curved = 1f - Mathf.Pow(1f - t, 3f);
                targetScale = Mathf.Lerp(originalScale, pulseScale, curved);
                yield return null;
            }

            elapsed = 0f;
            duration = 0.2f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                targetScale = Mathf.Lerp(pulseScale, originalScale, t);
                yield return null;
            }
            targetScale = originalScale;
        }

        System.Collections.IEnumerator MissShake()
        {
            Vector3 originalPos = transform.localPosition;
            float elapsed = 0f;
            float duration = 0.15f;
            float magnitude = 0.15f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float falloff = 1f - elapsed / duration;
                Vector2 offset = Random.insideUnitCircle * magnitude * falloff;
                transform.localPosition = originalPos + (Vector3)offset;
                yield return null;
            }
            transform.localPosition = originalPos;
        }

        public void SetLaneColor(Color color)
        {
            if (glowMat != null)
            {
                Color glowColor = color;
                glowColor.a = isPressed ? pressGlowAlpha : idleGlowAlpha;
                glowMat.SetColor("_EmissionColor", glowColor * 2f);
                glowRenderer.color = glowColor;
                glowBaseColor = glowColor;
            }
        }

        void OnDestroy()
        {
            if (mainMat != null) Destroy(mainMat);
            if (glowMat != null) Destroy(glowMat);
        }
    }
}
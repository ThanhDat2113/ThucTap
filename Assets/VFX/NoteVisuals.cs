using UnityEngine;

namespace RhythmGame
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class NoteVisuals : MonoBehaviour
    {
        [Header("Glow Effect")]
        public SpriteRenderer glowRenderer;
        public float glowIntensity = 1.5f;
        public float pulseSpeed = 3f;
        public float pulseScale = 1.15f;

        [Header("Trail Effect (for hold notes)")]
        public bool enableTrail = true;
        public float trailLifetime = 0.15f;
        public float trailWidth = 0.3f;

        [Header("Hit Effects (generated at runtime)")]
        public int burstParticleCount = 20;
        public float burstParticleSize = 0.15f;
        public float burstParticleLifetime = 0.5f;
        public float burstSpeed = 5f;

        [Header("Lane Colors (matching GameBootstrapper)")]
        public Color[] laneColors =
        {
            new Color(0.76f, 0.29f, 0.60f), // Lane 0 - Magenta
            new Color(0.00f, 1.00f, 1.00f), // Lane 1 - Cyan
            new Color(0.07f, 0.98f, 0.02f), // Lane 2 - Green
            new Color(0.98f, 0.22f, 0.25f), // Lane 3 - Red
        };

        SpriteRenderer mainRenderer;
        NoteView noteView;
        Material mainMat;
        Material glowMat;
        Vector3 baseScale;
        Color baseColor;
        Color glowBaseColor;
        float pulseTimer;
        int currentLane = 0;

        void Awake()
        {
            mainRenderer = GetComponent<SpriteRenderer>();
            noteView = GetComponent<NoteView>();

            baseScale = transform.localScale;
            baseColor = mainRenderer.color;

            SetupGlow();
            SetupTrail();
        }

        void SetupGlow()
        {
            if (glowRenderer == null)
            {
                var glowGO = new GameObject("NoteGlow");
                glowGO.transform.SetParent(transform, false);
                glowRenderer = glowGO.AddComponent<SpriteRenderer>();
                glowRenderer.sprite = mainRenderer.sprite;
                glowRenderer.sortingLayerID = mainRenderer.sortingLayerID;
                glowRenderer.sortingOrder = mainRenderer.sortingOrder - 1;
            }

            glowMat = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default"));
            if (glowMat.shader == null) glowMat = new Material(Shader.Find("Sprites/Default"));
            glowRenderer.material = glowMat;
            glowRenderer.color = new Color(1f, 1f, 1f, 0f);
            glowBaseColor = glowRenderer.color;
        }

        void SetupTrail()
        {
            if (!enableTrail) return;
        }

        public void Initialize(int lane, Color color)
        {
            currentLane = Mathf.Clamp(lane, 0, laneColors.Length - 1);
            Color laneColor = laneColors[currentLane];

            baseColor = color;
            mainRenderer.color = color;

            if (glowMat != null)
            {
                Color glowColor = laneColor;
                glowColor.a = 0.6f;
                glowMat.SetColor("_EmissionColor", glowColor * glowIntensity);
                glowMat.EnableKeyword("_EMISSION");
                glowRenderer.color = glowColor;
                glowBaseColor = glowColor;
            }

            transform.localScale = baseScale;
            pulseTimer = Random.Range(0f, 6.28f);
        }

        void Update()
        {
            if (noteView == null) return;

            pulseTimer += Time.deltaTime * pulseSpeed;
            float pulse = Mathf.Sin(pulseTimer) * 0.5f + 0.5f;
            float scale = Mathf.Lerp(1f, pulseScale, pulse * 0.3f);
            transform.localScale = baseScale * scale;

            if (glowRenderer != null && glowMat != null)
            {
                float glowPulse = Mathf.Lerp(0.3f, 1f, pulse);
                Color c = glowBaseColor;
                c.a = glowBaseColor.a * glowPulse;
                glowRenderer.color = c;
                glowRenderer.transform.localScale = Vector3.one * Mathf.Lerp(1f, 1.3f, pulse * 0.5f);
            }

            if (noteView.Holding)
            {
                UpdateHoldVisuals();
            }
        }

        void UpdateHoldVisuals()
        {
            if (glowRenderer != null)
            {
                float holdPulse = (Mathf.Sin(Time.time * 8f) + 1f) * 0.5f;
                Color c = glowBaseColor;
                c.a = Mathf.Lerp(0.4f, 0.9f, holdPulse);
                glowRenderer.color = c;
            }
        }

        public void PlayHitEffect(Judgement judgement, Vector3 position)
        {
            SpawnBurstParticles(judgement, position);

            if (judgement != Judgement.Miss)
            {
                StartCoroutine(HitFlash(judgement));
            }
        }

        void SpawnBurstParticles(Judgement judgement, Vector3 position)
        {
            Color laneColor = laneColors[currentLane];
            Color startColor = judgement switch
            {
                Judgement.Perfect => Color.white,
                Judgement.Good => Color.Lerp(laneColor, Color.white, 0.5f),
                Judgement.Bad => Color.Lerp(laneColor, new Color(1f, 0.5f, 0.5f), 0.5f),
                _ => new Color(0.5f, 0.5f, 0.5f)
            };

            var go = new GameObject("HitBurst");
            go.transform.position = position;
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
            shape.radius = 0.1f;
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

        System.Collections.IEnumerator HitFlash(Judgement judgement)
        {
            Color flashColor = judgement switch
            {
                Judgement.Perfect => Color.white,
                Judgement.Good => new Color(1f, 1f, 0.8f),
                Judgement.Bad => new Color(1f, 0.6f, 0.6f),
                _ => Color.gray
            };

            float elapsed = 0f;
            float duration = 0.1f;
            Color originalColor = mainRenderer.color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                mainRenderer.color = Color.Lerp(flashColor, originalColor, t);
                yield return null;
            }
            mainRenderer.color = originalColor;
        }

        public void PlayMissEffect()
        {
            StartCoroutine(MissFade());
        }

        System.Collections.IEnumerator MissFade()
        {
            float elapsed = 0f;
            float duration = 0.3f;
            Vector3 startScale = transform.localScale;
            Color startColor = mainRenderer.color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                Color c = startColor;
                c.a = Mathf.Lerp(1f, 0f, t);
                mainRenderer.color = c;
                if (glowRenderer != null)
                {
                    Color gc = glowRenderer.color;
                    gc.a = Mathf.Lerp(glowBaseColor.a, 0f, t);
                    glowRenderer.color = gc;
                }
                yield return null;
            }
        }

        void OnDestroy()
        {
            if (mainMat != null) Destroy(mainMat);
            if (glowMat != null) Destroy(glowMat);
        }
    }
}
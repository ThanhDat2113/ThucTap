using System.Collections;
using UnityEngine;

public enum HitJudgement { Miss, Good, Great, Perfect }

/// <summary>
/// Gọi HitEffect.Instance.Play(HitJudgement.Perfect, viTriNote) khi người chơi đánh trúng nốt.
/// Mỗi mức judgement dùng 1 prefab ParticleSystem riêng (Perfect nhiều hạt, màu vàng...).
/// </summary>
public class HitEffect : MonoBehaviour
{
    public static HitEffect Instance;

    [Header("Prefab ParticleSystem theo mức đánh")]
    public ParticleSystem perfectFx;
    public ParticleSystem greatFx;
    public ParticleSystem goodFx;
    public ParticleSystem missFx;

    [Header("Camera shake")]
    public Transform cam;
    public float perfectShake = 0.08f;
    public float missShake = 0.15f;
    public float shakeDuration = 0.12f;

    Vector3 camOrigin;
    Coroutine shakeRoutine;

    void Awake()
    {
        Instance = this;
        if (!cam && Camera.main) cam = Camera.main.transform;
        if (cam) camOrigin = cam.localPosition;
    }

    public void Play(HitJudgement j, Vector3 position)
    {
        ParticleSystem prefab = j switch
        {
            HitJudgement.Perfect => perfectFx,
            HitJudgement.Great => greatFx,
            HitJudgement.Good => goodFx,
            _ => missFx
        };

        if (prefab)
        {
            // Instantiate + tự huỷ. Nếu game dày nốt, hãy thay bằng object pool.
            var fx = Instantiate(prefab, position, Quaternion.identity);
            fx.Play();
            Destroy(fx.gameObject, fx.main.duration + fx.main.startLifetime.constantMax);
        }

        if (j == HitJudgement.Perfect) Shake(perfectShake);
        else if (j == HitJudgement.Miss) Shake(missShake);
    }

    public void Shake(float magnitude)
    {
        if (!cam) return;
        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(ShakeCo(magnitude));
    }

    IEnumerator ShakeCo(float magnitude)
    {
        float t = 0f;
        while (t < shakeDuration)
        {
            t += Time.unscaledDeltaTime;
            float falloff = 1f - t / shakeDuration;
            Vector2 offset = Random.insideUnitCircle * magnitude * falloff;
            cam.localPosition = camOrigin + (Vector3)offset;
            yield return null;
        }
        cam.localPosition = camOrigin;
    }
}

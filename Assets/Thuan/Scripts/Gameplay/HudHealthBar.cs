using UnityEngine;
using UnityEngine.UI;

// Thanh mau neon: fill chay muot, lop "ghost" trang tut cham phia sau, nhap nhay khi mau thap.
public class HudHealthBar : MonoBehaviour
{
    [SerializeField] private Image fill;
    [SerializeField] private Image ghost;
    [SerializeField] private Color fillColor = Color.cyan;
    [SerializeField] private float fillSpeed = 8f;
    [SerializeField] private float ghostSpeed = 1.5f;
    [SerializeField, Range(0f, 1f)] private float lowThreshold = 0.25f;

    private float target = 1f, shown = 1f, ghostShown = 1f;

    public float Value { get { return target; } }

    public void SetValue(float normalized, bool instant = false)
    {
        target = Mathf.Clamp01(normalized);
        if (instant) { shown = ghostShown = target; Apply(); }
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        shown = Mathf.MoveTowards(shown, target, fillSpeed * dt * Mathf.Max(0.15f, Mathf.Abs(target - shown)) + dt * 0.05f);
        if (ghostShown < shown) ghostShown = shown;
        else ghostShown = Mathf.MoveTowards(ghostShown, shown, ghostSpeed * dt * 0.5f);
        Apply();
    }

    private void Apply()
    {
        if (fill != null)
        {
            fill.fillAmount = shown;
            Color c = fillColor;
            if (shown < lowThreshold)
            {
                float k = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 10f);
                c = Color.Lerp(fillColor, Color.white, k * 0.45f);
            }
            fill.color = c;
        }
        if (ghost != null) ghost.fillAmount = ghostShown;
    }
}

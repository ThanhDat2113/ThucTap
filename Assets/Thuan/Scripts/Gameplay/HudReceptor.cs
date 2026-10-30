using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// O bam (mui ten) o cuoi lan. Nhan duoc ca cham/click de choi tren dien thoai.
public class HudReceptor : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private Image arrow;
    [SerializeField] private Image glow;
    [SerializeField] private Image burst;
    [SerializeField] private Color laneColor = Color.white;

    public int Lane { get; set; }
    public Color LaneColor { get { return laneColor; } }
    public event Action<int, bool> PointerPress;

    private bool pressed;
    private float burstT = 1f;
    private float pressScale = 1f;

    public void SetPressed(bool on) { pressed = on; }

    public void PlayHit()
    {
        burstT = 0f;
        if (burst != null) burst.transform.localEulerAngles = new Vector3(0, 0, UnityEngine.Random.Range(0f, 360f));
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;
        pressScale = Mathf.Lerp(pressScale, pressed ? 0.88f : 1f, 1f - Mathf.Exp(-25f * dt));
        if (arrow != null)
        {
            arrow.rectTransform.localScale = new Vector3(pressScale, pressScale, 1f);
            arrow.color = pressed ? new Color(1.15f, 1.15f, 1.15f, 1f) : Color.white;
        }
        if (glow != null)
        {
            var c = laneColor; c.a = Mathf.Lerp(glow.color.a, pressed ? 0.85f : 0.25f, 1f - Mathf.Exp(-18f * dt));
            glow.color = c;
        }
        if (burst != null)
        {
            burstT = Mathf.Min(1f, burstT + dt / 0.35f);
            float s = Mathf.Lerp(0.6f, 1.6f, 1f - (1f - burstT) * (1f - burstT));
            burst.rectTransform.localScale = new Vector3(s, s, 1f);
            var c = laneColor; c.a = (1f - burstT) * 0.95f; burst.color = c;
        }
    }

    public void OnPointerDown(PointerEventData e) { if (PointerPress != null) PointerPress(Lane, true); }
    public void OnPointerUp(PointerEventData e) { if (PointerPress != null) PointerPress(Lane, false); }
    public void OnPointerExit(PointerEventData e) { if (pressed && PointerPress != null) PointerPress(Lane, false); }
}

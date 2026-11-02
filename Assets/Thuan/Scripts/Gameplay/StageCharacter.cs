using UnityEngine;

// Nhan vat dung tren san khau (SpriteRenderer + Animator cua nhom Phu).
// Doi trang thai idle / dance / miss theo ket qua bam not.
public class StageCharacter : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private string idleState = "idle";
    [SerializeField] private string danceState = "dance";
    [SerializeField] private string missState = "miss";
    [SerializeField] private float missHold = 0.8f;
    [SerializeField] private float danceTimeout = 1.8f;

    private enum Mode { Idle, Dance, Miss }
    private Mode mode = Mode.Idle;
    private float timer, punch, shake;
    private Vector3 baseScale, basePos;
    private bool ready;

    public void Init(Animator anim, string idle, string dance, string miss)
    {
        animator = anim;
        idleState = idle; danceState = dance; missState = miss;
        baseScale = transform.localScale;
        basePos = transform.localPosition;
        ready = true;
        Play(idleState);
    }

    // Lam tot: nhay (giu nhip nhay neu dang nhay)
    public void Cheer()
    {
        if (mode != Mode.Dance) Play(danceState);
        mode = Mode.Dance;
        timer = danceTimeout;
        punch = 1f;
    }

    // That bai: dien hoat anh miss mot luc roi ve dung yen
    public void Fail()
    {
        Play(missState);
        mode = Mode.Miss;
        timer = missHold;
    }

    // Bi trung don: rung nhe, khong doi hoat anh
    public void Hurt() { shake = 1f; }

    private void Play(string state)
    {
        if (animator != null && !string.IsNullOrEmpty(state)) animator.Play(state, 0, 0f);
    }

    private void Update()
    {
        if (!ready) return;
        float dt = Time.deltaTime;
        if (mode != Mode.Idle)
        {
            timer -= dt;
            if (timer <= 0f) { mode = Mode.Idle; Play(idleState); }
        }
        punch = Mathf.MoveTowards(punch, 0f, dt * 5f);
        shake = Mathf.MoveTowards(shake, 0f, dt * 4f);
        float s = 1f + 0.035f * punch;
        transform.localScale = new Vector3(baseScale.x * s, baseScale.y * s, baseScale.z);
        transform.localPosition = basePos + new Vector3(Mathf.Sin(Time.time * 70f) * 0.12f * shake, 0f, 0f);
    }
}

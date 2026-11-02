using UnityEngine;
using RhythmGame;

public class VfxBinder : MonoBehaviour
{
    [Header("Refs")]
    public InputHandler input;
    public JudgementSystem judgementSystem;
    public JudgementPopup judgementPopup;

    [Header("Theo thứ tự Lane0 -> Lane3")]
    public LaneBeam[] beams;

    void OnEnable()
    {
        if (input == null || judgementSystem == null)
        {
            Debug.LogError("VfxBinder: chưa gán đủ Input / Judgement System.", this);
            return;
        }

        input.OnLanePressed += HandleLanePressed;
        input.OnLaneReleased += HandleLaneReleased;
        judgementSystem.OnJudged += HandleJudged;
    }

    void OnDisable()
    {
        if (input != null)
        {
            input.OnLanePressed -= HandleLanePressed;
            input.OnLaneReleased -= HandleLaneReleased;
        }
        if (judgementSystem != null) judgementSystem.OnJudged -= HandleJudged;
    }

    void HandleLanePressed(int lane)
    {
        if (beams != null && lane >= 0 && lane < beams.Length && beams[lane] != null)
            beams[lane].Press();
    }

    void HandleLaneReleased(int lane)
    {
        if (beams != null && lane >= 0 && lane < beams.Length && beams[lane] != null)
            beams[lane].Release();
    }

    void HandleJudged(Judgement j, int points, bool hit, NoteOwner owner)
    {
        if (judgementPopup) judgementPopup.Show(j);
    }
}
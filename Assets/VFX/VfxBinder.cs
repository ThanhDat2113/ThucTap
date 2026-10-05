using UnityEngine;
using RhythmGame;

/// <summary>
/// Nối InputHandler + JudgementSystem có sẵn của bạn với các VFX (cột sáng, chữ judgement).
/// Gắn script này vào object "Systems" (nơi đã có InputHandler, JudgementSystem, GameManager...),
/// rồi kéo đủ các ô dưới đây trong Inspector.
///
/// Cột sáng bật khi NGƯỜI CHƠI BẤM PHÍM (InputHandler.OnLanePressed), tắt khi THẢ PHÍM
/// (OnLaneReleased) — độc lập với việc đánh trúng hay trượt nốt.
/// Chữ PERFECT/GOOD/BAD/MISS hiện khi NỐT ĐƯỢC CHẤM ĐIỂM (JudgementSystem.OnJudged).
/// </summary>
public class VfxBinder : MonoBehaviour
{
    [Header("Refs (đều có thể kéo chính object Systems vào)")]
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

    void HandleJudged(Judgement j, int points, bool hit)
    {
        if (judgementPopup) judgementPopup.Show(j);
    }
}
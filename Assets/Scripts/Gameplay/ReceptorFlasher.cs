using UnityEngine;

namespace RhythmGame
{
    /// <summary>Sáng receptor lên khi người chơi đang giữ phím lane tương ứng.</summary>
    public class ReceptorFlasher : MonoBehaviour
    {
        public SpriteRenderer[] receptors;
        public InputHandler input;
        public Color[] laneColors;

        void Update()
        {
            for (int l = 0; l < receptors.Length; l++)
            {
                Color dim = laneColors[l] * 0.35f;
                dim.a = 1f;
                receptors[l].color = input.IsHeld(l) ? laneColors[l] : dim;
            }
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

namespace RhythmGame.Core
{
    [CreateAssetMenu(menuName = "RhythmGame/Game Config", fileName = "GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Lanes")]
        public int laneCount = 4;
        public float laneWidth = 2.4f;
        public float laneStartX = -4.8f;

        [Header("Note Travel (world units)")]
        public float noteSpeed = 9f;
        public float hitPositionY = -3.2f;
        public float spawnLeadTime = 1.4f;

        [Header("Scoring (seconds)")]
        [Tooltip("Maximum |offset| in seconds for a Perfect hit.")]
        public float perfectWindow = 0.1f;
        [Tooltip("Maximum |offset| in seconds for a Good hit.")]
        public float goodWindow = 0.2f;

        [Header("Keyboard Bindings (index = LaneID)")]
        public Key[] laneKeys = { Key.A, Key.S, Key.W, Key.D };

        public float spawnPositionY => hitPositionY + noteSpeed * spawnLeadTime;

        public float GetLaneCenterX(LaneID lane)
        {
            return laneStartX + ((int)lane + 0.5f) * laneWidth;
        }
    }
}

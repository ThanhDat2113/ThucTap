using System;
using RhythmGame.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

namespace RhythmGame.Input
{
    [DisallowMultipleComponent]
    public class PlayerInputHandler : MonoBehaviour
    {
        public event Action<LaneID> OnLanePressed;
        public event Action<LaneID> OnLaneReleased;

        [Header("Play Area (touch columns divided from this; fallback = full screen)")]
        public RectTransform playArea;

        [Header("Camera (used for world-aligned touch mapping)")]
        public Camera worldCamera;

        [Header("Edge margin (fraction of lane width ignored at screen edges)")]
        [Range(0f, 0.5f)]
        public float edgeMargin = 0.05f;

        private GameConfig config;
        private int[] pressCount;
        private bool[] prevKeyboardDown;

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }

        private void Start()
        {
            config = GameManager.Instance?.Config;
            if (worldCamera == null)
                worldCamera = Camera.main;

            int lanes = config != null ? config.laneCount : 4;
            pressCount = new int[lanes];
            prevKeyboardDown = new bool[lanes];
        }

        private void Update()
        {
            HandleKeyboard();
            HandleTouch();
        }

        private void HandleKeyboard()
        {
            if (config == null || config.laneKeys == null)
                return;

            int lanes = Mathf.Min(config.laneKeys.Length, config.laneCount);
            for (int i = 0; i < lanes; i++)
            {
                bool down = Keyboard.current?[config.laneKeys[i]]?.isPressed ?? false;
                if (down && !prevKeyboardDown[i])
                    NotifyPress((LaneID)i);
                else if (!down && prevKeyboardDown[i])
                    NotifyRelease((LaneID)i);
                prevKeyboardDown[i] = down;
            }
        }

        private void HandleTouch()
        {
            if (config == null)
                return;

            var touches = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches;
            for (int i = 0; i < touches.Count; i++)
            {
                var touch = touches[i];
                if (touch.phase != UnityEngine.InputSystem.TouchPhase.Began &&
                    touch.phase != UnityEngine.InputSystem.TouchPhase.Ended &&
                    touch.phase != UnityEngine.InputSystem.TouchPhase.Canceled)
                    continue;

                var lane = ScreenPointToLane(touch.screenPosition);

                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
                    NotifyPress(lane);
                else
                    NotifyRelease(lane);
            }
        }

        private void NotifyPress(LaneID lane)
        {
            int i = (int)lane;
            if (i < 0 || i >= pressCount.Length)
                return;
            pressCount[i]++;
            if (pressCount[i] == 1)
                OnLanePressed?.Invoke(lane);
        }

        private void NotifyRelease(LaneID lane)
        {
            int i = (int)lane;
            if (i < 0 || i >= pressCount.Length)
                return;
            pressCount[i]--;
            if (pressCount[i] <= 0)
            {
                pressCount[i] = 0;
                OnLaneReleased?.Invoke(lane);
            }
        }

        public bool IsLaneHeld(LaneID lane)
        {
            int i = (int)lane;
            return i >= 0 && i < pressCount.Length && pressCount[i] > 0;
        }

        public LaneID ScreenPointToLane(Vector2 screenPos)
        {
            int lanes = config != null ? config.laneCount : 4;
            float margin = edgeMargin * (config != null ? config.laneWidth : 1f);

            Vector3 world;
            if (worldCamera != null)
            {
                world = worldCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
            }
            else if (playArea != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(playArea, screenPos, null, out var pt))
            {
                world = new Vector3(pt.x + playArea.rect.width * 0.5f, 0f, 0f);
            }
            else
            {
                world = new Vector3(screenPos.x, 0f, 0f);
            }

            float worldLeft = (config != null ? config.laneStartX : 0f) + margin;
            float worldWidth = (config != null ? config.laneCount * config.laneWidth : Screen.width) - margin * 2f;
            float normalized = Mathf.InverseLerp(worldLeft, worldLeft + worldWidth, world.x);
            normalized = Mathf.Clamp01(normalized);
            int index = Mathf.Clamp(Mathf.FloorToInt(normalized * lanes), 0, lanes - 1);
            return (LaneID)index;
        }
    }
}

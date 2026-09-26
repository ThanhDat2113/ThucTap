using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace RhythmGame
{
    /// <summary>
    /// Chỉ lo đọc phím và bắn event. Không biết gì về note, lane rendering
    /// hay judgement — nhờ vậy sau này đổi sang touch/mobile chỉ cần sửa
    /// mỗi file này.
    /// Phím: D F J K hoặc mũi tên Trái/Xuống/Lên/Phải. R để restart.
    /// </summary>
    public class InputHandler : MonoBehaviour
    {
        public int laneCount = 4;

        public System.Action<int> OnLanePressed;
        public System.Action<int> OnLaneReleased;
        public System.Action OnRestartPressed;

        bool[] held;

        void Awake() => held = new bool[laneCount];

        void Update()
        {
            for (int lane = 0; lane < laneCount; lane++)
            {
                bool down = IsDown(lane);
                bool up = IsUp(lane);

                if (down && !held[lane]) { held[lane] = true; OnLanePressed?.Invoke(lane); }
                if (up && held[lane]) { held[lane] = false; OnLaneReleased?.Invoke(lane); }
            }
            if (RestartDown()) OnRestartPressed?.Invoke();
        }

        public bool IsHeld(int lane) => held != null && lane < held.Length && held[lane];

#if ENABLE_INPUT_SYSTEM
        bool IsDown(int lane)
        {
            var kb = Keyboard.current;
            if (kb == null) return false;
            return PrimaryKey(kb, lane).wasPressedThisFrame || SecondaryKey(kb, lane).wasPressedThisFrame;
        }

        bool IsUp(int lane)
        {
            var kb = Keyboard.current;
            if (kb == null) return false;
            return !(PrimaryKey(kb, lane).isPressed || SecondaryKey(kb, lane).isPressed);
        }

        bool RestartDown() => Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;

        KeyControl PrimaryKey(Keyboard kb, int lane) => lane switch
        {
            0 => kb.dKey, 1 => kb.fKey, 2 => kb.jKey, _ => kb.kKey
        };

        KeyControl SecondaryKey(Keyboard kb, int lane) => lane switch
        {
            0 => kb.leftArrowKey, 1 => kb.downArrowKey, 2 => kb.upArrowKey, _ => kb.rightArrowKey
        };
#else
        bool IsDown(int lane) => Input.GetKeyDown(PrimaryKey(lane)) || Input.GetKeyDown(SecondaryKey(lane));
        bool IsUp(int lane) => !(Input.GetKey(PrimaryKey(lane)) || Input.GetKey(SecondaryKey(lane)));
        bool RestartDown() => Input.GetKeyDown(KeyCode.R);

        KeyCode PrimaryKey(int lane) => lane switch
        {
            0 => KeyCode.D, 1 => KeyCode.F, 2 => KeyCode.J, _ => KeyCode.K
        };

        KeyCode SecondaryKey(int lane) => lane switch
        {
            0 => KeyCode.LeftArrow, 1 => KeyCode.DownArrow, 2 => KeyCode.UpArrow, _ => KeyCode.RightArrow
        };
#endif
    }
}

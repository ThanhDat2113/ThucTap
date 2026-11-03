using UnityEngine;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace RhythmGame
{
    public enum KeyboardPreset
    {
        [Tooltip("D - F - J - K (Chuẩn Rhythm game 4 phím, ngón giữa và ngón trỏ 2 tay)")]
        DFJK,
        [Tooltip("F - G - J - K (Phím cho người chơi quen đặt ngón FG JK)")]
        FGJK,
        [Tooltip("Tự cài đặt phím tùy biến trong Inspector")]
        Custom
    }

    /// <summary>
    /// Chỉ lo đọc phím và bắn event. Không biết gì về note, lane rendering
    /// hay judgement — nhờ vậy sau này đổi sang touch/mobile chỉ cần sửa
    /// mỗi file này.
    /// Hỗ trợ cả New Input System (hoạt động hoàn hảo trong Device Simulator) lẫn Legacy Input.
    /// Phím: D F J K (hoặc F G J K) hoặc mũi tên Trái/Xuống/Lên/Phải. R để restart.
    /// </summary>
    public class InputHandler : MonoBehaviour
    {
        public int laneCount = 4;

        [Header("Keyboard Controls (Hỗ trợ Device Simulator & Game View)")]
        [Tooltip("Chọn kiểu phím bấm mặc định")]
        public KeyboardPreset keyboardPreset = KeyboardPreset.DFJK;

        [Tooltip("Phím tùy biến cho từng Lane nếu chọn Custom (mặc định D, F, J, K)")]
        public KeyCode[] customKeys = new KeyCode[] { KeyCode.D, KeyCode.F, KeyCode.J, KeyCode.K };

        /// <summary>
        /// Các Receptor (vùng đánh note) trên màn hình. Nếu gán đủ, chạm màn hình chỉ được tính
        /// khi chạm vào Receptor (tìm receptor gần vị trí chạm nhất theo trục X).
        /// Nếu để trống, fallback về chia đều màn hình thành laneCount vùng.
        /// </summary>
        [Header("Touch / Receptor")]
        public Transform[] receptorTransforms;
        [Tooltip("Radius vòng tròn xung quanh tia chạm để va chạm (pixel trên màn hình), dùng khi camera Perspective distort.")]
        public float touchRadiusPixels = 60f;

        public System.Action<int> OnLanePressed;
        public System.Action<int> OnLaneReleased;
        public System.Action OnRestartPressed;

        bool[] held;

        // Touch / mouse support (mobile + Editor simulation)
        // fingerId -> lane currently held by that touch
        private readonly Dictionary<int, int> touchFingerToLane = new Dictionary<int, int>();
        private int mouseHeldLane = -1;

        void Awake()
        {
            held = new bool[laneCount];
        }

        void Update()
        {
            HandleTouchAndMouse();

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

        bool IsDown(int lane)
        {
#if ENABLE_INPUT_SYSTEM
            if (IsInputSystemDown(lane)) return true;
#endif
            return Input.GetKeyDown(PrimaryKey(lane)) || Input.GetKeyDown(SecondaryKey(lane));
        }

        bool IsUp(int lane)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                return !IsInputSystemHeld(lane) && !IsLegacyHeld(lane);
            }
#endif
            return !IsLegacyHeld(lane);
        }

        bool IsLegacyHeld(int lane)
        {
            return Input.GetKey(PrimaryKey(lane)) || Input.GetKey(SecondaryKey(lane));
        }

        bool RestartDown()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) return true;
#endif
            return Input.GetKeyDown(KeyCode.R);
        }

#if ENABLE_INPUT_SYSTEM
        bool IsInputSystemDown(int lane)
        {
            var kb = Keyboard.current;
            if (kb == null) return false;

            var primary = PrimaryKeyControl(kb, lane);
            if (primary != null && primary.wasPressedThisFrame) return true;

            var sec = SecondaryKeyControl(kb, lane);
            if (sec != null && sec.wasPressedThisFrame) return true;

            return false;
        }

        bool IsInputSystemHeld(int lane)
        {
            var kb = Keyboard.current;
            if (kb == null) return false;

            var primary = PrimaryKeyControl(kb, lane);
            if (primary != null && primary.isPressed) return true;

            var sec = SecondaryKeyControl(kb, lane);
            if (sec != null && sec.isPressed) return true;

            return false;
        }

        KeyControl PrimaryKeyControl(Keyboard kb, int lane)
        {
            if (keyboardPreset == KeyboardPreset.FGJK)
            {
                return lane switch
                {
                    0 => kb.fKey,
                    1 => kb.gKey,
                    2 => kb.jKey,
                    3 => kb.kKey,
                    _ => null
                };
            }
            else if (keyboardPreset == KeyboardPreset.Custom && customKeys != null && lane < customKeys.Length)
            {
                return KeyCodeToKeyControl(kb, customKeys[lane]);
            }
            else // DFJK mặc định
            {
                return lane switch
                {
                    0 => kb.dKey,
                    1 => kb.fKey,
                    2 => kb.jKey,
                    3 => kb.kKey,
                    _ => null
                };
            }
        }

        KeyControl SecondaryKeyControl(Keyboard kb, int lane) => lane switch
        {
            0 => kb.leftArrowKey,
            1 => kb.downArrowKey,
            2 => kb.upArrowKey,
            3 => kb.rightArrowKey,
            _ => null
        };

        KeyControl KeyCodeToKeyControl(Keyboard kb, KeyCode kc)
        {
            return kc switch
            {
                KeyCode.A => kb.aKey,
                KeyCode.B => kb.bKey,
                KeyCode.C => kb.cKey,
                KeyCode.D => kb.dKey,
                KeyCode.E => kb.eKey,
                KeyCode.F => kb.fKey,
                KeyCode.G => kb.gKey,
                KeyCode.H => kb.hKey,
                KeyCode.I => kb.iKey,
                KeyCode.J => kb.jKey,
                KeyCode.K => kb.kKey,
                KeyCode.L => kb.lKey,
                KeyCode.S => kb.sKey,
                KeyCode.Space => kb.spaceKey,
                KeyCode.Return => kb.enterKey,
                _ => null
            };
        }
#endif

        KeyCode PrimaryKey(int lane)
        {
            if (keyboardPreset == KeyboardPreset.FGJK)
            {
                return lane switch
                {
                    0 => KeyCode.F,
                    1 => KeyCode.G,
                    2 => KeyCode.J,
                    3 => KeyCode.K,
                    _ => KeyCode.None
                };
            }
            else if (keyboardPreset == KeyboardPreset.Custom && customKeys != null && lane < customKeys.Length)
            {
                return customKeys[lane];
            }
            else // DFJK mặc định
            {
                return lane switch
                {
                    0 => KeyCode.D,
                    1 => KeyCode.F,
                    2 => KeyCode.J,
                    3 => KeyCode.K,
                    _ => KeyCode.None
                };
            }
        }

        KeyCode SecondaryKey(int lane) => lane switch
        {
            0 => KeyCode.LeftArrow,
            1 => KeyCode.DownArrow,
            2 => KeyCode.UpArrow,
            3 => KeyCode.RightArrow,
            _ => KeyCode.None
        };

        /// <summary>
        /// Xử lý chạm màn hình (mobile) và chuột (mô phỏng trong Editor).
        /// Nếu có receptorTransforms, chỉ tính lane khi chạm vào Receptor (gần nhất theo X).
        /// </summary>
        void HandleTouchAndMouse()
        {
            if (Input.touchCount > 0)
            {
                foreach (var touch in Input.touches)
                {
                    int lane = GetLaneFromScreenPos(touch.position);
                    if (lane < 0) continue;

                    if (touch.phase == UnityEngine.TouchPhase.Began)
                    {
                        if (!touchFingerToLane.ContainsKey(touch.fingerId))
                        {
                            touchFingerToLane[touch.fingerId] = lane;
                            OnLanePressed?.Invoke(lane);
                        }
                    }
                    else if (touch.phase == UnityEngine.TouchPhase.Ended || touch.phase == UnityEngine.TouchPhase.Canceled)
                    {
                        if (touchFingerToLane.TryGetValue(touch.fingerId, out int heldLane))
                        {
                            touchFingerToLane.Remove(touch.fingerId);
                            OnLaneReleased?.Invoke(heldLane);
                        }
                    }
                }
            }
            else
            {
                // Chuột để mô phỏng chạm trong Editor
                if (Input.GetMouseButtonDown(0))
                {
                    mouseHeldLane = GetLaneFromScreenPos(Input.mousePosition);
                    if (mouseHeldLane >= 0)
                        OnLanePressed?.Invoke(mouseHeldLane);
                }
                else if (Input.GetMouseButtonUp(0))
                {
                    if (mouseHeldLane >= 0)
                    {
                        OnLaneReleased?.Invoke(mouseHeldLane);
                        mouseHeldLane = -1;
                    }
                }
            }
        }

        /// <summary>
        /// Đổi vị trí trên màn hình sang lane index.
        /// Dùng Raycast từ Camera qua vị trí chạm, kiểm tra va chạm trực tiếp với Collider2D của mỗi Receptor.
        /// Cách này chính xác với camera Perspective (rotation X != 0).
        /// Trả về -1 nếu chạm ngoài mọi Receptor.
        /// Ngược lại (không gán receptor) chia đều màn hình thành laneCount vùng.
        /// </summary>
        int GetLaneFromScreenPos(Vector2 screenPos)
        {
            if (receptorTransforms != null && receptorTransforms.Length > 0)
            {
                Camera cam = Camera.main;
                if (cam == null) return -1;

                Ray ray = cam.ScreenPointToRay(screenPos);

                int bestLane = -1;
                float bestDist = float.MaxValue;

                // Chuyển touchRadiusPixels thành world distance gần đúng
                float worldRadius = touchRadiusPixels / 100f;

                for (int i = 0; i < receptorTransforms.Length; i++)
                {
                    if (receptorTransforms[i] == null) continue;

                    var collider = receptorTransforms[i].GetComponent<Collider2D>();
                    if (collider == null) continue;

                    // Raycast từ camera qua vị trí chạm, chỉ kiểm tra va chạm với collider của receptor này
                    RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity);
                    if (hit.collider != null && hit.collider == collider)
                    {
                        // Tính khoảng cách từ điểm chạm đến tâm receptor trên màn hình để chọn receptor gần nhất
                        Vector3 receptorScreenPos = cam.WorldToScreenPoint(receptorTransforms[i].position);
                        float dist = Vector2.Distance(screenPos, receptorScreenPos);
                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            bestLane = i;
                        }
                    }
                }

                return bestLane;
            }

            // Fallback: chia đều màn hình
            if (laneCount <= 0) return 0;
            float zoneWidth = Screen.width / (float)laneCount;
            int lane = Mathf.FloorToInt(screenPos.x / zoneWidth);
            return Mathf.Clamp(lane, 0, laneCount - 1);
        }
    }
}

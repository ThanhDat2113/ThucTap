using UnityEngine;
using UnityEditor;

namespace Luan.MainMenu.Editor
{
    /// <summary>
    /// Custom Inspector cho MainMenuManager, bổ sung các nút bấm tiện ích giúp người dùng:
    /// - Kéo Logo trên Scene View rồi bấm 1 nút để lưu tọa độ bên phải hoặc bên trái.
    /// - Bấm nút để xem trước vị trí bên phải / bên trái ngay trong Scene View mà không cần chạy game.
    /// </summary>
    [CustomEditor(typeof(MainMenuManager))]
    public class MainMenuManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // Vẽ giao diện mặc định của MainMenuManager
            DrawDefaultInspector();

            MainMenuManager manager = (MainMenuManager)target;

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("🎮 TIỆN ÍCH ĐIỀU CHỈNH VỊ TRÍ LOGO", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.HelpBox(
                "💡 Hướng Dẫn Sử Dụng:\n" +
                "1. Bạn kéo Logo trên Scene View sang góc bên phải theo ý muốn (hoặc nhập số vào Logo Right Position bên trên).\n" +
                "2. Bấm '📌 Lưu Hiện Tại Làm [Bên Phải]' để lưu lại tọa độ đó.\n" +
                "3. Bấm '👉 Xem Trước Bên Phải' hoặc '👈 Xem Trước Bên Trái' để kiểm tra nhanh trong Scene.\n" +
                "4. Khi vào game: Logo sẽ ở bên phải; sau khi đăng nhập thành công sẽ trượt về bên trái; khi đăng xuất sẽ trượt về bên phải.",
                MessageType.Info);

            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
            if (GUILayout.Button("📌 Lưu Hiện Tại Làm [Bên Phải]", GUILayout.Height(32)))
            {
                manager.SaveCurrentAsRightPosition();
            }

            GUI.backgroundColor = new Color(0.85f, 1f, 0.5f);
            if (GUILayout.Button("📌 Lưu Hiện Tại Làm [Bên Trái]", GUILayout.Height(32)))
            {
                manager.SaveCurrentAsLeftPosition();
            }
            EditorGUILayout.EndHorizontal();

            GUI.backgroundColor = Color.white;
            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("👉 Xem Trước [Bên Phải] (Right)", GUILayout.Height(28)))
            {
                manager.MoveLogoToRightInEditor();
            }

            if (GUILayout.Button("👈 Xem Trước [Bên Trái] (Left)", GUILayout.Height(28)))
            {
                manager.MoveLogoToLeftInEditor();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }
    }
}


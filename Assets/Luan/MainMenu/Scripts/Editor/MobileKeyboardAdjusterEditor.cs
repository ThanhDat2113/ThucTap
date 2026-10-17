using UnityEngine;
using UnityEditor;

namespace Luan.MainMenu.Editor
{
    [CustomEditor(typeof(MobileKeyboardAdjuster))]
    public class MobileKeyboardAdjusterEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            MobileKeyboardAdjuster adjuster = (MobileKeyboardAdjuster)target;

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("📱 KIỂM TRA ĐẨY BÀN PHÍM", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.HelpBox(
                "Khi chạy game trên điện thoại (màn hình ngang):\n" +
                "• Chạm vào ô nhập: Panel sẽ tự động trượt lên để bàn phím không che mất chữ.\n" +
                "• Chạm ra vùng trống hoặc bấm Hoàn tất: Panel tự động hạ về vị trí ban đầu.\n" +
                "• Bạn có thể bấm 2 nút dưới đây để xem trước độ cao nâng lên trong Scene.",
                MessageType.Info);

            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.4f, 0.85f, 1f);
            if (GUILayout.Button("🔼 Nâng Panel Lên (Test)", GUILayout.Height(30)))
            {
                adjuster.ContextLift();
            }

            GUI.backgroundColor = new Color(1f, 0.7f, 0.4f);
            if (GUILayout.Button("🔽 Hạ Panel Xuống (Reset)", GUILayout.Height(30)))
            {
                adjuster.ContextLower();
            }
            EditorGUILayout.EndHorizontal();

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndVertical();
        }
    }
}


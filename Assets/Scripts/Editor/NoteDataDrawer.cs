#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace RhythmGame.EditorTools
{
    [CustomPropertyDrawer(typeof(NoteData))]
    public class NoteDataDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => EditorGUIUtility.singleLineHeight;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var beatProp = property.FindPropertyRelative("beat");
            var laneProp = property.FindPropertyRelative("lane");
            var typeProp = property.FindPropertyRelative("type");
            var holdProp = property.FindPropertyRelative("holdBeats");

            var bpmProp = property.serializedObject.FindProperty("bpm");
            var offsetProp = property.serializedObject.FindProperty("firstNoteOffset");
            float bpm = bpmProp != null ? bpmProp.floatValue : 120f;
            float offset = offsetProp != null ? offsetProp.floatValue : 0f;
            float seconds = offset + beatProp.floatValue * 60f / Mathf.Max(bpm, 0.0001f);

            float w = position.width;
            float beatW = w * 0.22f, laneW = w * 0.13f, typeW = w * 0.20f, holdW = w * 0.17f;
            float timeW = w - beatW - laneW - typeW - holdW - 12f;
            float x = position.x;
            float y = position.y;
            float h = position.height;

            EditorGUIUtility.labelWidth = 32f;
            EditorGUI.PropertyField(new Rect(x, y, beatW, h), beatProp, new GUIContent("Beat"));
            x += beatW + 4;

            EditorGUIUtility.labelWidth = 28f;
            EditorGUI.PropertyField(new Rect(x, y, laneW, h), laneProp, new GUIContent("Lane"));
            x += laneW + 4;

            EditorGUIUtility.labelWidth = 0f;
            EditorGUI.PropertyField(new Rect(x, y, typeW, h), typeProp, GUIContent.none);
            x += typeW + 4;

            EditorGUIUtility.labelWidth = 26f;
            EditorGUI.PropertyField(new Rect(x, y, holdW, h), holdProp, new GUIContent("Hold"));
            x += holdW + 4;

            bool wasEnabled = GUI.enabled;
            GUI.enabled = false;
            EditorGUI.LabelField(new Rect(x, y, timeW, h), $"= {seconds:0.000}s");
            GUI.enabled = wasEnabled;

            EditorGUIUtility.labelWidth = 0f;
            EditorGUI.EndProperty();
        }
    }
}
#endif
using UnityEngine;
using UnityEditor;

namespace SeweralIdeas.UnityUtils.Editor
{
    [CustomEditor(typeof(Comments))]
    public class CommentsInspector : UnityEditor.Editor
    {

        private Comments Script => target as Comments;
        private readonly GUIStyle _style = new GUIStyle();

        private static readonly Color ColorPro  = new Color(0.5f, 0.7f, 0.3f, 1f);
        private static readonly Color ColorFree = new Color(0.2f, 0.3f, 0.1f, 1f);

        public override void OnInspectorGUI()
        {
            _style.wordWrap = true;
            _style.normal.textColor = EditorGUIUtility.isProSkin ? ColorPro : ColorFree;

            serializedObject.Update();
            var property = serializedObject.FindProperty(Comments.PropertyName);

            // BeginProperty needs the control's rect, which the layout TextArea doesn't expose up front:
            // wrap it in a vertical group and use that group's rect.
            Rect rect = EditorGUILayout.BeginVertical();
            EditorGUI.BeginProperty(rect, GUIContent.none, property);
            EditorGUI.BeginChangeCheck();
            string text = EditorGUILayout.TextArea(property.stringValue, _style);
            if (EditorGUI.EndChangeCheck())
            {
                property.stringValue = text;
                serializedObject.ApplyModifiedProperties();
            }
            EditorGUI.EndProperty();
            EditorGUILayout.EndVertical();

        }
    }
}
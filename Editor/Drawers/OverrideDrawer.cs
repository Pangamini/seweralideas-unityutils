using SeweralIdeas.Utils;
using UnityEditor;
using UnityEngine;

namespace SeweralIdeas.UnityUtils.Drawers.Editor
{
    // Draws Override<T> as a toggle in front of the value's own control, which is greyed out while the override is off.
    [CustomPropertyDrawer(typeof(Override<>), true)]
    public class OverrideDrawer : PropertyDrawer
    {
        private const float ToggleWidth = 16f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty valueProp = property.FindPropertyRelative("_value");
            return EditorGUI.GetPropertyHeight(valueProp, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty enabledProp = property.FindPropertyRelative("_enabled");
            SerializedProperty valueProp = property.FindPropertyRelative("_value");

            EditorGUI.BeginProperty(position, label, property);

            // The toggle goes next to the label, in the label's own column, so it lines up between fields.
            var toggleRect = new Rect(position.x + EditorGUIUtility.labelWidth - ToggleWidth, position.y, ToggleWidth, EditorGUIUtility.singleLineHeight);
            var valueRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, position.width - EditorGUIUtility.labelWidth, position.height);

            // The label is shortened by the toggle's width so that it isn't overlapped.
            var labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth - ToggleWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(labelRect, label);

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            enabledProp.boolValue = EditorGUI.Toggle(toggleRect, enabledProp.boolValue);
            EditorGUI.indentLevel = indent;

            using (new EditorGUI.DisabledScope(!enabledProp.boolValue))
                EditorGUI.PropertyField(valueRect, valueProp, GUIContent.none, true);

            EditorGUI.EndProperty();
        }
    }
}

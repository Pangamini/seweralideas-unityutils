using UnityEditor;
using UnityEngine;

namespace SeweralIdeas.Drawers.Editor
{
    [CustomPropertyDrawer(typeof(ColorAttribute))]
    public class ColorDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property,
            GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position,
            SerializedProperty property,
            GUIContent label)
        {
            var attrib = attribute as ColorAttribute;

            var old = GUI.backgroundColor;
            GUI.backgroundColor = attrib.backgroundColor;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.backgroundColor = old;
        }
    }
}
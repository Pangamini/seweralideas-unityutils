using UnityEngine;
using UnityEditor;

namespace SeweralIdeas.UnityUtils.Drawers.Editor
{
    [CustomPropertyDrawer(typeof(UnitsAttribute))]
    public class UnitsDrawer : PropertyDrawer
    {
        private const float LabelWidth = 64;
        
        public override float GetPropertyHeight(SerializedProperty property,
            GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position,
            SerializedProperty property,
            GUIContent label)
        {
            var unitsAttribute = (UnitsAttribute)this.attribute;
            Rect propertyRect = new Rect(position.x, position.y, position.width - LabelWidth, position.height);
            Rect labelRect = new Rect(position.xMax - LabelWidth, position.y, LabelWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(propertyRect, property, label, true);
            GUI.Label(labelRect, unitsAttribute.units);
        }
    }
}

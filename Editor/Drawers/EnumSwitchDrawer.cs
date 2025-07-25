using UnityEditor;
using UnityEngine;


namespace SeweralIdeas.UnityUtils.Drawers.Editor
{
    [CustomPropertyDrawer(typeof(EnumSwitchAttribute))]
    public class EnumToggleButtonsDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Draw label first
            position = EditorGUI.PrefixLabel(position, label);

            if (property.propertyType != SerializedPropertyType.Enum)
            {
                EditorGUI.LabelField(position, "EnumToggleButtons: Use on enum only");
                return;
            }

            var enumNames = property.enumDisplayNames;
            int currentIndex = property.enumValueIndex;

            // Layout toggles horizontally
            float spacing = 2f;
            float buttonWidth = (position.width - (enumNames.Length - 1) * spacing) / enumNames.Length;
            Rect buttonRect = new Rect(position.x, position.y, buttonWidth, position.height);

            for (int i = 0; i < enumNames.Length; i++)
            {
                bool wasActive = (i == currentIndex);
                
                GUIStyle style;
                if(enumNames.Length == 1) style = EditorStyles.miniButton;
                else if (i == 0) style = EditorStyles.miniButtonLeft;
                else if (i == enumNames.Length - 1) style = EditorStyles.miniButtonRight;
                else style = EditorStyles.miniButtonMid;

                bool isActive = GUI.Toggle(buttonRect, wasActive, enumNames[i], style);
                if(isActive && !wasActive)
                    property.enumValueIndex = i;
                buttonRect.x += buttonWidth + spacing;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
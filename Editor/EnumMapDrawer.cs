using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace SeweralIdeas.Drawers.Editor
{
    [CustomPropertyDrawer(typeof(EnumMapAttribute))]
    public class EnumMapDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            if (property.isExpanded)
            {
                var arrayProp = property.FindPropertyRelative("array");
                var attribute = (EnumMapAttribute)this.attribute;
                var length = System.Enum.GetNames(attribute.enumType).Length;
                var count = arrayProp.arraySize;
                for (int i = 0; i < length; ++i)
                {
                    if (i >= count)
                        arrayProp.InsertArrayElementAtIndex(i);
                    var elemProp = arrayProp.GetArrayElementAtIndex(i);
                    height += EditorGUI.GetPropertyHeight(elemProp);
                }
            }
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var arrayProp = property.FindPropertyRelative("array");
            var attribute = (EnumMapAttribute)this.attribute;
            var rect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.BeginChangeCheck();
            property.isExpanded = EditorGUI.Foldout(rect, property.isExpanded, label);
            
            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                var names = System.Enum.GetNames(attribute.enumType);
                var elemCount = arrayProp.arraySize;
                rect.y += rect.height;

                for (int i = 0; i < names.Length; ++i)
                {
                    if (i >= elemCount)
                        arrayProp.InsertArrayElementAtIndex(i);
                    var elemProp = arrayProp.GetArrayElementAtIndex(i);
                    rect.height = EditorGUI.GetPropertyHeight(elemProp);
                    EditorGUI.PropertyField(rect, elemProp, new GUIContent(names[i]), true);
                    rect.y += rect.height;
                }
                EditorGUI.indentLevel--;
            }
            if (EditorGUI.EndChangeCheck())
            {
                property.serializedObject.ApplyModifiedProperties();
                property.serializedObject.Update();
            }
        }
    }
}
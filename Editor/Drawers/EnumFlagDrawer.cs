using System;
using System.Collections;
using UnityEditor;
using UnityEngine;

namespace SeweralIdeas.UnityUtils.Drawers.Editor
{
    [CustomPropertyDrawer(typeof(EnumFlagAttribute))]
    public class EnumFlagDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Type enumType = GetEnumType();
            if (property.propertyType != SerializedPropertyType.Enum || enumType == null)
            {
                EditorGUI.LabelField(position, label, new GUIContent("[EnumFlag] needs an enum field"));
                return;
            }

            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();

            var current = (Enum)Enum.ToObject(enumType, property.enumValueFlag);
            Enum edited = EditorGUI.EnumFlagsField(position, label, current);

            if (EditorGUI.EndChangeCheck())
                property.enumValueFlag = unchecked((int)Convert.ToInt64(edited));

            EditorGUI.showMixedValue = false;
            EditorGUI.EndProperty();
        }

        // The enum the field is of: its own type, or its elements' for an array or list.
        private Type GetEnumType()
        {
            Type type = fieldInfo?.FieldType;
            if (type == null)
                return null;
            if (type.IsArray)
                type = type.GetElementType();
            else if (type.IsGenericType && typeof(IList).IsAssignableFrom(type))
                type = type.GetGenericArguments()[0];
            return type.IsEnum ? type : null;
        }
    }
}

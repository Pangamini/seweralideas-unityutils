using System;
using System.Collections.Generic;
using System.Reflection;
using SeweralIdeas.UnityUtils.Editor;
using UnityEditor;
using UnityEngine;

namespace SeweralIdeas.UnityUtils.Drawers.Editor
{
    [CustomPropertyDrawer(typeof(EnumFlagAttribute))]
    public class EnumFlagDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EnumFlagAttribute flagSettings = (EnumFlagAttribute)attribute;
            List<Enum> result = new List<Enum>();
            EditorReflectionUtility.GetVariable(property.propertyPath, property.serializedObject.targetObjects, result);

            var first = result[0];
            var mixed = false;
            for (int i = 1; i < result.Count; ++i)
                if (result[i] != first)
                {
                    mixed = true;
                    break;
                }

            string propName = flagSettings.enumName;
            if (string.IsNullOrEmpty(propName))
                propName = property.name;

            EditorGUI.showMixedValue = mixed;
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();
            Enum enumNew = EditorGUI.EnumFlagsField(position, label, first);
            if(EditorGUI.EndChangeCheck())
                property.intValue = (int)Convert.ToInt32(enumNew);
            EditorGUI.EndProperty();
        }

        static T GetBaseProperty<T>(SerializedProperty prop)
        {
            // Separate the steps it takes to get to this property
            string[] separatedPaths = prop.propertyPath.Split('.');

            // Go down to the root of this serialized property
            System.Object reflectionTarget = prop.serializedObject.targetObject as object;
            // Walk down the path to get the target object
            foreach (var path in separatedPaths)
            {
                FieldInfo fieldInfo = reflectionTarget.GetType().GetField(path);
                reflectionTarget = fieldInfo.GetValue(reflectionTarget);
            }
            return (T)reflectionTarget;
        }
    }
}
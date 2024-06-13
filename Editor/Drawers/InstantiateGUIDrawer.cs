using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using SeweralIdeas.UnityUtils.Editor;
using SeweralIdeas.Utils;
using UnityEditor;
using UnityEngine;

namespace SeweralIdeas.UnityUtils.Drawers.Editor
{
    [CustomPropertyDrawer(typeof(InstantiateGUI))]
    public class InstantiateGUIDrawer : PropertyDrawer
    {
        private static GUIStyle s_stylePlus;
        private static GUIStyle s_styleMinus;
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.managedReferenceValue != null)
                return EditorGUI.GetPropertyHeight(property);

            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            s_stylePlus ??= "OL Plus";
            s_styleMinus ??= "OL Minus";
            float buttonWidth = 24;

            Rect buttonRect = new Rect(position.xMax - buttonWidth, position.y, buttonWidth, EditorGUIUtility.singleLineHeight);

            if(property.managedReferenceValue != null)
            {
                if(GUI.Button(buttonRect, GUIContent.none, s_styleMinus))
                {
                    property.managedReferenceValue = null;
                    property.serializedObject.ApplyModifiedProperties();
                    return;
                }

                var myLabel = new GUIContent($"{label.text} ({property.managedReferenceValue.GetType().Name})");

                EditorGUI.PropertyField(position, property, myLabel, true);
            }

            else
            {
                var attrib = (InstantiateGUI)attribute;
                var myLabel = new GUIContent($"{label.text} ({attrib.BaseType.Name})");
                EditorGUI.LabelField(position, myLabel, new GUIContent("null"));

                Action<Type> onTypeSelected = type =>
                {
                    property.managedReferenceValue = Activator.CreateInstance(type);
                    property.serializedObject.ApplyModifiedProperties();
                };

                if(GUI.Button(buttonRect, GUIContent.none, s_stylePlus))
                {
                    TypeUtility.TypeQuery typeQuery = new TypeUtility.TypeQuery(attrib.BaseType, false, true);
                    TypeDropdown.ShowTypeDropdown(buttonRect, typeQuery, onTypeSelected);
                }
            }
        }

    }
}
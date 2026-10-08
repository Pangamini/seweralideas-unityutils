using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using SeweralIdeas.UnityUtils.Editor;
using SeweralIdeas.Utils;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

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

                if(GUI.Button(buttonRect, GUIContent.none, s_stylePlus))
                {
                    // The SerializedProperty is only good for this OnGUI, the dropdown answers later: find the property again then.
                    Object[] targets = property.serializedObject.targetObjects;
                    string path = property.propertyPath;

                    TypeUtility.TypeQuery typeQuery = new TypeUtility.TypeQuery(attrib.BaseType, false, true);
                    TypeDropdown.ShowTypeDropdown(buttonRect, typeQuery, type => OnTypeSelected(type, targets, path));
                }
            }
        }

        // One instance per target: with a shared one, editing one object's value would change the others'.
        private static void OnTypeSelected(Type type, Object[] targets, string path)
        {
            foreach (Object target in targets)
            {
                using var serializedObject = new SerializedObject(target);
                serializedObject.FindProperty(path).managedReferenceValue = Activator.CreateInstance(type);
                serializedObject.ApplyModifiedProperties();
            }
        }

    }
}
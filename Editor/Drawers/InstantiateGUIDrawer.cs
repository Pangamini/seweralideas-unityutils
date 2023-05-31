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
                
                EditorGUI.PropertyField(position, property, new GUIContent(property.managedReferenceValue.GetType().Name), true);
                return;
            }


            EditorGUI.LabelField(position, label, new GUIContent("null"));

            if(GUI.Button(buttonRect, GUIContent.none, s_stylePlus))
            {
                var attrib = (InstantiateGUI)attribute;
                TypeUtility.TypeList typeList = TypeUtility.GetDerivedTypes(new TypeUtility.TypeQuery(attrib.BaseType, false, true));
                GenericMenu menu = new GenericMenu();

                foreach (Type type in typeList.types)
                {
                    menu.AddItem(new GUIContent(type.Name), false, () =>
                    {
                        property.managedReferenceValue = Activator.CreateInstance(type);
                        property.serializedObject.ApplyModifiedProperties();
                    });
                }

                menu.ShowAsContext();
            }

        }
    }
}
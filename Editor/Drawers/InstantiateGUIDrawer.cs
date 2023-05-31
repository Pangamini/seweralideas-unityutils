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

                if(GUI.Button(buttonRect, GUIContent.none, s_stylePlus))
                {
                    var controlId = GUIUtility.GetControlID(FocusType.Passive, buttonRect);

                    TypeUtility.TypeList typeList = TypeUtility.GetDerivedTypes(new TypeUtility.TypeQuery(attrib.BaseType, false, true));

                    List<GUIContent> options = new();
                    foreach (Type type in typeList.types)
                    {
                        options.Add(new GUIContent(type.Name));
                    }

                    Action<int> onKeySelected = (selectedIndex) =>
                    {
                        property.managedReferenceValue = Activator.CreateInstance(typeList.types[selectedIndex]);
                        property.serializedObject.ApplyModifiedProperties();
                    };

                    var scrRect = new Rect(GUIUtility.GUIToScreenPoint(buttonRect.position), new Vector2(256, buttonRect.height));
                    AdvancedPopupWindow.ShowWindow(controlId, scrRect, 0, options, true, null, onKeySelected);
                }
            }

        }
    }
}
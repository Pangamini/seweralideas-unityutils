using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SeweralIdeas.UnityUtils.Editor
{
    [CustomPropertyDrawer(typeof(TypeReferenceAttribute))]
    public class TypeReferenceDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUIUtility.singleLineHeight;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attrib = (TypeReferenceAttribute)attribute;
            var buttonRect = EditorGUI.PrefixLabel(position, label);

            SerializedProperty stringProperty;
            if (property.propertyType == SerializedPropertyType.String)
                stringProperty = property;
            else
                stringProperty = property.FindPropertyRelative("_typeName");

            var refType = Type.GetType(stringProperty.stringValue);
            
            var displayName = stringProperty.hasMultipleDifferentValues? new GUIContent("-") : new GUIContent(refType?.Name ?? string.Empty, refType != null?HierarchyIcons.GetTexture(refType):null);
            
            if(GUI.Button(buttonRect, displayName, EditorStyles.popup))
            {
                var targets = property.serializedObject.targetObjects;
                var path = stringProperty.propertyPath;
                TypeDropdown.ShowTypeDropdown(buttonRect, attrib.Query, type => OnTypeSelected(type, targets, path));
            }
        }

        private void OnTypeSelected(Type type, Object[] targets, string path)
        {
            using var so = new SerializedObject(targets);
            var prop = so.FindProperty(path);
            prop.stringValue = type.AssemblyQualifiedName;
            so.ApplyModifiedProperties();
        }

    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using AdvancedDropdownWindow = SeweralIdeas.UnityUtils.Editor.AdvancedDropdownWindow; // (a plain "Editor." here would find this very namespace)
using Object = UnityEngine.Object;

namespace SeweralIdeas.UnityUtils.Drawers.Editor
{
    [CustomPropertyDrawer(typeof(ComponentPickerAttribute))]
    public class ComponentPickerDrawer : PropertyDrawer
    {
        private const float PickerWidth = 18f;
        private const float Spacing     = 2f;

        private static readonly GUIContent PickerContent = new(string.Empty, "Switch to another component on the same GameObject");

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUIUtility.singleLineHeight;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if(property.propertyType != SerializedPropertyType.ObjectReference)
            {
                EditorGUI.LabelField(position, label, new GUIContent("[ComponentPicker] needs an object reference"));
                return;
            }

            // Next to the object field, a small button to pick among the components of the referenced one's GameObject.
            List<Component> candidates = GetCandidates(property);
            bool hasPicker = candidates.Count > 1;

            Rect fieldRect = position;
            if(hasPicker)
                fieldRect.width -= PickerWidth + Spacing;

            // The plain object field. (Not EditorGUI.PropertyField: that would come right back to this drawer.)
            EditorGUI.BeginProperty(fieldRect, label, property);
            EditorGUI.BeginChangeCheck();
            Object picked = EditorGUI.ObjectField(fieldRect, label, property.objectReferenceValue, GetFieldType(), true);
            if(EditorGUI.EndChangeCheck())
                property.objectReferenceValue = picked;
            EditorGUI.EndProperty();

            if(!hasPicker)
                return;

            Rect pickerRect = new(position.xMax - PickerWidth, position.y, PickerWidth, position.height);
            if(EditorGUI.DropdownButton(pickerRect, PickerContent, FocusType.Keyboard, EditorStyles.miniPullDown))
            {
                // The window opens under the whole value area - the object field without its label, and the button -
                // not just under the little button.
                float valueX = position.x + EditorGUIUtility.labelWidth + Spacing;
                Rect valueRect = new(valueX, position.y, position.xMax - valueX, position.height);
                ShowPicker(valueRect, property, candidates);
            }
        }

        // The dropdown window is opened by hand, to have it list the components and call back with the choice.
        private static void ShowPicker(Rect anchorRect, SerializedProperty property, List<Component> candidates)
        {
            var options = new List<GUIContent>(candidates.Count);
            foreach (Component candidate in candidates)
            {
                Type type = candidate.GetType();
                options.Add(new GUIContent(type.Name, EditorGUIUtility.ObjectContent(candidate, type).image));
            }

            // The SerializedProperty is only good for this OnGUI, the window answers later: find the property again then.
            Object[] targets = property.serializedObject.targetObjects;
            string path = property.propertyPath;

            void OnSelected(int index)
            {
                var serializedObject = new SerializedObject(targets);
                serializedObject.FindProperty(path).objectReferenceValue = candidates[index];
                serializedObject.ApplyModifiedProperties();
            }

            int controlId = GUIUtility.GetControlID(FocusType.Passive, anchorRect);
            Rect screenRect = new(GUIUtility.GUIToScreenPoint(anchorRect.position), anchorRect.size);
            AdvancedDropdownWindow.ShowWindow(controlId, screenRect, options, searchBar: false, onElemClick: OnSelected);
        }

        // The components that can be switched to: those of the referenced one's GameObject that the field accepts.
        // None, if nothing is referenced yet or several objects with different references are being edited.
        private List<Component> GetCandidates(SerializedProperty property)
        {
            var result = new List<Component>();
            if(property.hasMultipleDifferentValues)
                return result;

            if(property.objectReferenceValue is not Component current)
                return result;

            Type fieldType = GetFieldType();
            foreach (Component component in current.gameObject.GetComponents<Component>())
            {
                if(component != null && fieldType.IsInstanceOfType(component)) // null: a missing script
                    result.Add(component);
            }

            return result;
        }

        // The type the field accepts: the field's own, or its elements' for an array or list.
        private Type GetFieldType()
        {
            Type type = fieldInfo.FieldType;
            if(type.IsArray)
                return type.GetElementType();
            if(type.IsGenericType && typeof(IList).IsAssignableFrom(type))
                return type.GetGenericArguments()[0];
            return typeof(Component).IsAssignableFrom(type) ? type : typeof(Component);
        }
    }
}

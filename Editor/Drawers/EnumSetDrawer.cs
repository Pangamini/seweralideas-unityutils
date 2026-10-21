using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SeweralIdeas.UnityUtils.Drawers.Editor
{
    // Draws EnumSet<T> as one dropdown button listing the chosen values; clicking opens a
    // menu with a check per value. Obsolete members are left out of the menu (and a value
    // that several names share is listed once), but a stored obsolete value is still shown
    // and can be unchecked.
    [CustomPropertyDrawer(typeof(EnumSet<>), true)]
    public class EnumSetDrawer : PropertyDrawer
    {
        private const string ValuesField = "_values";

        private readonly struct Option
        {
            public readonly string Name;
            public readonly int    Value;

            public Option(string name, int value)
            {
                Name  = name;
                Value = value;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUIUtility.singleLineHeight;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Type enumType = GetEnumType();
            SerializedProperty values = property.FindPropertyRelative(ValuesField);
            if(enumType == null || values == null)
            {
                EditorGUI.LabelField(position, label, new GUIContent("EnumSet needs an enum type"));
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            Rect buttonRect = EditorGUI.PrefixLabel(position, label);
            EditorGUI.showMixedValue = values.hasMultipleDifferentValues;
            string summary = values.hasMultipleDifferentValues ? "—" : Summarize(values, enumType);
            bool clicked = EditorGUI.DropdownButton(buttonRect, new GUIContent(summary, summary), FocusType.Keyboard);
            EditorGUI.showMixedValue = false;

            if(clicked)
                ShowMenu(buttonRect, property.serializedObject, values.propertyPath, enumType);

            EditorGUI.EndProperty();
        }

        // The enum the field is of: found through EnumSet<> itself, also when the field is an array or list of them.
        private Type GetEnumType()
        {
            Type type = fieldInfo?.FieldType;
            if(type == null)
                return null;

            if(type.IsArray)
                type = type.GetElementType();
            else if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                type = type.GetGenericArguments()[0];

            for(; type != null; type = type.BaseType)
            {
                if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(EnumSet<>))
                    return type.GetGenericArguments()[0];
            }
            return null;
        }

        private static List<Option> GetOptions(Type enumType)
        {
            var options = new List<Option>();
            var seen    = new HashSet<int>();
            foreach(FieldInfo field in enumType.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if(field.IsDefined(typeof(ObsoleteAttribute), false))
                    continue;

                int value = unchecked((int)Convert.ToInt64(field.GetRawConstantValue()));
                if(seen.Add(value))
                    options.Add(new Option(ObjectNames.NicifyVariableName(field.Name), value));
            }
            return options;
        }

        private static string NameOf(Type enumType, int value)
        {
            string name = Enum.GetName(enumType, value);
            return name != null ? ObjectNames.NicifyVariableName(name) : value.ToString();
        }

        private static string Summarize(SerializedProperty values, Type enumType)
        {
            if(values.arraySize == 0)
                return "Nothing";

            var names = new string[values.arraySize];
            for(int i = 0; i < names.Length; i++)
                names[i] = NameOf(enumType, values.GetArrayElementAtIndex(i).enumValueFlag);
            return string.Join(", ", names);
        }

        private static void ShowMenu(Rect buttonRect, SerializedObject serializedObject, string valuesPath, Type enumType)
        {
            // The menu outlives this OnGUI call, so it works on the property found again by its path.
            SerializedProperty values = serializedObject.FindProperty(valuesPath);
            var selected = new HashSet<int>();
            for(int i = 0; i < values.arraySize; i++)
                selected.Add(values.GetArrayElementAtIndex(i).enumValueFlag);

            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Nothing"), selected.Count == 0, () => Edit(serializedObject, valuesPath, null, false));
            menu.AddSeparator("");

            var listed = new HashSet<int>();
            foreach(Option option in GetOptions(enumType))
            {
                listed.Add(option.Value);
                int value = option.Value;
                menu.AddItem(new GUIContent(option.Name), selected.Contains(value), () => Edit(serializedObject, valuesPath, value, !selected.Contains(value)));
            }

            // stored values the menu doesn't list (obsolete ones): shown checked, so they can be removed
            foreach(int value in selected)
            {
                if(listed.Contains(value))
                    continue;
                int stored = value;
                menu.AddItem(new GUIContent(NameOf(enumType, stored) + " (obsolete)"), true, () => Edit(serializedObject, valuesPath, stored, false));
            }

            menu.DropDown(buttonRect);
        }

        // value == null clears the set; otherwise the value is added or removed.
        private static void Edit(SerializedObject serializedObject, string valuesPath, int? value, bool add)
        {
            serializedObject.Update();
            SerializedProperty values = serializedObject.FindProperty(valuesPath);

            if(value == null)
            {
                values.arraySize = 0;
            }
            else if(add)
            {
                values.arraySize++;
                values.GetArrayElementAtIndex(values.arraySize - 1).enumValueFlag = value.Value;
            }
            else
            {
                for(int i = values.arraySize - 1; i >= 0; i--)
                {
                    if(values.GetArrayElementAtIndex(i).enumValueFlag == value.Value)
                        values.DeleteArrayElementAtIndex(i);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SeweralIdeas.UnityUtils.Drawers.Editor
{
    // Draws EnumMap<TEnum, TValue> as a foldout with one row per option of the enum. Missing entries are added when the
    // map is drawn (a new option of the enum gets its row, with the default value). Obsolete options are left out (and a
    // value that several names share is listed once), but a stored entry of one is still shown. Entries of values that
    // are not in the enum any more are listed at the end, with a button to remove them.
    [CustomPropertyDrawer(typeof(EnumMap<,>), true)]
    public class EnumMapDrawer : PropertyDrawer
    {
        private const string EntriesField = "_entries";
        private const string KeyField     = "key";
        private const string ValueField   = "value";

        private const float RemoveButtonWidth = 22f;

        private readonly struct Option
        {
            public readonly string Name;
            public readonly int    Key;

            public Option(string name, int key)
            {
                Name = name;
                Key  = key;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            if(!property.isExpanded || !TryGetTypes(out Type enumType, out Type entryType))
                return height;

            SerializedProperty entries = property.FindPropertyRelative(EntriesField);
            for(int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty value = entries.GetArrayElementAtIndex(i).FindPropertyRelative(ValueField);
                if(value == null)
                    return height + EditorGUIUtility.singleLineHeight; // the "not serialized" message of OnGUI
                height += EditorGUIUtility.standardVerticalSpacing + EditorGUI.GetPropertyHeight(value, GUIContent.none, true);
            }
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Rect line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, label, true);

            if(!property.isExpanded)
                return;

            if(!TryGetTypes(out Type enumType, out Type entryType))
            {
                EditorGUI.LabelField(new Rect(position.x, line.yMax, position.width, line.height), "Not an EnumMap field");
                return;
            }

            SerializedProperty entries = Sync(property, enumType, entryType, out List<Option> options);
            var names = new Dictionary<int, string>();
            foreach(Option option in options)
                names[option.Key] = option.Name;

            if(entries.arraySize > 0 && entries.GetArrayElementAtIndex(0).FindPropertyRelative(ValueField) == null)
            {
                EditorGUI.LabelField(new Rect(position.x, line.yMax, position.width, line.height), "Entries have no serialized key/value");
                return;
            }

            int removeIndex = -1;
            float y = line.yMax + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.indentLevel++;
            for(int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                int key = entry.FindPropertyRelative(KeyField).intValue;
                SerializedProperty value = entry.FindPropertyRelative(ValueField);

                bool known = names.TryGetValue(key, out string name);
                if(!known)
                    name = (Enum.GetName(enumType, key) is { } removed ? ObjectNames.NicifyVariableName(removed) : key.ToString()) + " (not in the enum)";

                float height = EditorGUI.GetPropertyHeight(value, GUIContent.none, true);
                var rect = new Rect(position.x, y, position.width, height);
                if(!known)
                    rect.width -= RemoveButtonWidth + 2f;

                EditorGUI.PropertyField(rect, value, new GUIContent(name), true);

                if(!known && GUI.Button(new Rect(rect.xMax + 2f, y, RemoveButtonWidth, EditorGUIUtility.singleLineHeight), "x"))
                    removeIndex = i;

                y += height + EditorGUIUtility.standardVerticalSpacing;
            }
            EditorGUI.indentLevel--;

            if(removeIndex >= 0)
                entries.DeleteArrayElementAtIndex(removeIndex);
        }

        // Adds the entry of every option that has none (in the enum's order, after the existing ones) and returns the entries.
        // Only called from OnGUI: changing the array while the heights are being measured breaks the property handlers.
        private SerializedProperty Sync(SerializedProperty property, Type enumType, Type entryType, out List<Option> options)
        {
            SerializedProperty entries = property.FindPropertyRelative(EntriesField);
            options = GetOptions(enumType);

            var present = new HashSet<int>();
            for(int i = 0; i < entries.arraySize; i++)
                present.Add(entries.GetArrayElementAtIndex(i).FindPropertyRelative(KeyField).intValue);

            bool added = false;
            foreach(Option option in options)
            {
                if(present.Contains(option.Key))
                    continue;

                // Growing an array copies its last element, so the new entry is set to a default one
                int index = entries.arraySize;
                entries.arraySize = index + 1;
                SerializedProperty entry = entries.GetArrayElementAtIndex(index);
                entry.boxedValue = Activator.CreateInstance(entryType);
                entry.FindPropertyRelative(KeyField).intValue = option.Key;
                added = true;
            }

            if(added)
            {
                // The heights were measured for the old entry count: apply, and draw again from the start
                property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                property.serializedObject.Update();
                GUIUtility.ExitGUI();
            }

            return entries;
        }

        // The enum and the entry struct of the field: found through EnumMap<,> itself, also when the field is an array or list.
        private bool TryGetTypes(out Type enumType, out Type entryType)
        {
            enumType  = null;
            entryType = null;

            Type type = fieldInfo?.FieldType;
            if(type == null)
                return false;

            if(type.IsArray)
                type = type.GetElementType();
            else if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                type = type.GetGenericArguments()[0];

            for(; type != null; type = type.BaseType)
            {
                if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(EnumMap<,>))
                {
                    enumType = type.GetGenericArguments()[0];
                    FieldInfo entriesField = type.GetField(EntriesField, BindingFlags.NonPublic | BindingFlags.Instance);
                    entryType = entriesField?.FieldType.GetGenericArguments()[0];
                    return entryType != null;
                }
            }
            return false;
        }

        private static List<Option> GetOptions(Type enumType)
        {
            var options = new List<Option>();
            var seen    = new HashSet<int>();
            foreach(FieldInfo field in enumType.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if(field.IsDefined(typeof(ObsoleteAttribute), false))
                    continue;

                int key = unchecked((int)Convert.ToInt64(field.GetRawConstantValue()));
                if(seen.Add(key))
                    options.Add(new Option(ObjectNames.NicifyVariableName(field.Name), key));
            }
            return options;
        }
    }
}

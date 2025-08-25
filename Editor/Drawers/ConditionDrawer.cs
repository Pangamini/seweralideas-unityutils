using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace SeweralIdeas.UnityUtils.Drawers.Editor
{
    [CustomPropertyDrawer(typeof(ConditionAttribute))]
    public class ConditionDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (ShouldShow(property))
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return ShouldShow(property) ? EditorGUI.GetPropertyHeight(property, label, true) : 0f;
        }

        private bool ShouldShow(SerializedProperty property)
        {
            ConditionAttribute conditionAttr = (ConditionAttribute)attribute;

            object target = GetTargetObject(property);
            if (target == null)
                return true;

            string conditionName = conditionAttr.Condition;

            var type = target.GetType();
            var field = type.GetField(conditionName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null && field.FieldType == typeof(bool))
            {
                bool value = (bool)field.GetValue(target);
                return conditionAttr.Invert ? !value : value;
            }

            var prop = type.GetProperty(conditionName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null && prop.PropertyType == typeof(bool))
            {
                bool value = (bool)prop.GetValue(target);
                return conditionAttr.Invert ? !value : value;
            }

            Debug.LogWarning($"[Condition] No bool field or property named '{conditionName}' found on {type.Name}");
            return true;
        }

        private object GetTargetObject(SerializedProperty prop)
        {
            object obj = prop.serializedObject.targetObject;
            string[] elements = prop.propertyPath.Replace(".Array.data[", "[").Split('.');
            foreach (string element in elements[..^1])
            {
                if (element.Contains("["))
                {
                    var indexStart = element.IndexOf("[");
                    var name = element[..indexStart];
                    var index = int.Parse(element[(indexStart + 1)..^1]);

                    obj = GetValue(obj, name, index);
                }
                else
                {
                    obj = GetValue(obj, element);
                }

                if (obj == null)
                    return null;
            }
            return obj;
        }

        private object GetValue(object source, string name)
        {
            if (source == null)
                return null;

            var type = source.GetType();
            var f = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null)
                return f.GetValue(source);

            var p = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null)
                return p.GetValue(source);

            return null;
        }

        private object GetValue(object source, string name, int index)
        {
            var enumerable = GetValue(source, name) as System.Collections.IEnumerable;
            if (enumerable == null)
                return null;

            var enumerator = enumerable.GetEnumerator();
            for (int i = 0; i <= index; i++)
            {
                if (!enumerator.MoveNext())
                    return null;
            }
            return enumerator.Current;
        }
    }
}
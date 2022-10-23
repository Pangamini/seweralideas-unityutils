using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;

namespace SeweralIdeas.Drawers.Editor
{
    [CustomPropertyDrawer(typeof(ConditionAttribute))]
    public class ConditionDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (ShowVariable(property.propertyPath, property.serializedObject.targetObjects))
            {
                return EditorGUI.GetPropertyHeight(property, label);
            }
            else
                return 0;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (ShowVariable(property.propertyPath, property.serializedObject.targetObjects))
                EditorGUI.PropertyField(position, property, label, true);
        }

        bool ShowVariable(string path, Object[] objs)
        {
            var names = path.Split('.');
            //names = ArrayUtility.SubArray(names, 0, names.Length - 1);
            
            ConditionAttribute attr = (ConditionAttribute)attribute;

            var f = EditorReflectionUtility.FindFieldInfo(names, attr.condition, objs);
            if (f == null)
            {
                EditorGUILayout.LabelField("No condition field "+path+" found");
                return true;
            }

            List<object> results = new List<object>();
            EditorReflectionUtility.GetVariable(path, objs, results);

            foreach (object o in results)
            {
                bool boolVal;
                var memberValue = f.Value.GetValue(o);

                if (ReferenceEquals(memberValue, null) || memberValue.Equals(null))
                {
                    boolVal = false;
                }
                else
                {
                    try
                    {
                        boolVal = System.Convert.ToBoolean(memberValue);
                    }
                    catch (System.InvalidCastException)
                    {
                        boolVal = true; //non-null smething
                    }
                }

                if (boolVal)
                {
                    return !attr.invert;
                }
            }

            return attr.invert;
        }
    }
}
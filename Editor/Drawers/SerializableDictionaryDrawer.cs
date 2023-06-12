using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SeweralIdeas.UnityUtils.Drawers.Editor
{
    [CustomPropertyDrawer(typeof(SerializableDictionary<,>), true)]
    public class SerializableDictionaryDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var list = property.FindPropertyRelative("m_list");
            return EditorGUI.GetPropertyHeight(list);
        }

        private static void GetKeyValueTypes(System.Type dictType, out System.Type keyType, out System.Type valueType)
        {
            var args = dictType.GenericTypeArguments;
            Debug.Assert(args.Length == 2);
            keyType = args[0];
            valueType = args[1];
        }

        private static bool AreAssignable(System.Type targetType, Object[] objects)
        {
            foreach (Object o in objects)
            {
                if(!targetType.IsInstanceOfType(o))
                    return false;
            }

            return true;
        }
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var list = property.FindPropertyRelative("m_list");
            
            Rect dragRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            
            if (dragRect.Contains(Event.current.mousePosition))
            {
                GetKeyValueTypes(fieldInfo.FieldType, out var keyType, out var valType);

                bool toVal = AreAssignable(valType, DragAndDrop.objectReferences);
                //bool toKey = !toVal && AreAssignable(keyType, DragAndDrop.objectReferences);
                
                if(toVal)
                {
                    if(Event.current.type == EventType.DragUpdated)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        Event.current.Use();
                    }

                    else if(Event.current.type == EventType.DragPerform)
                    {
                        Event.current.Use();

                        foreach(var obj in DragAndDrop.objectReferences)
                        {
                            int i = list.arraySize;
                            list.InsertArrayElementAtIndex(i);
                            SerializedProperty elem = list.GetArrayElementAtIndex(i);
                            var elemValue = elem.FindPropertyRelative("m_value");
                            var elemKey = elem.FindPropertyRelative("m_key");

                            if(elemKey.propertyType == SerializedPropertyType.String)
                            {
                                elemKey.stringValue = obj.name;
                            }
                                
                            elemValue.objectReferenceValue = obj;
                        }
                        property.serializedObject.ApplyModifiedProperties();
                        return;
                    }

                }
            }
            
            EditorGUI.PropertyField(position, list, label);
            
        }
    }

    [CustomPropertyDrawer(typeof(SerializableDictionary<,>.Element), true)]
    public class SerializableDictionaryElementDrawer : PropertyDrawer
    {
        private static readonly GUIContent s_content_key = new GUIContent("key");
        private static readonly GUIContent s_content_val = new GUIContent("val");
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var name = property.FindPropertyRelative("m_key");
            var value = property.FindPropertyRelative("m_value");
            return Mathf.Max(EditorGUI.GetPropertyHeight(name), EditorGUI.GetPropertyHeight(value));
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var key = property.FindPropertyRelative("m_key");
            var value = property.FindPropertyRelative("m_value");

            int gap = 8;
            
            var keyRect = new Rect(position.x, position.y, position.width / 2 - gap, position.height);
            var valueRect = new Rect(keyRect.xMax + gap, position.y, position.width - keyRect.width, position.height);

            var oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 22;
            EditorGUI.PropertyField(keyRect, key, s_content_key);
            EditorGUI.PropertyField(valueRect, value, s_content_val);
            EditorGUIUtility.labelWidth = oldWidth;
        }
    }
}
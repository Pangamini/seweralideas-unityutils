using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SeweralIdeas.UnityUtils.Drawers.Editor
{
    /// <summary>
    /// Draws <see cref="InterfaceRef{T}"/> as one object field. Unity's object field cannot be limited to an interface, so what is
    /// dropped or picked is checked here: an object that implements it is taken, a GameObject gives its first component that does,
    /// anything else is refused. A reference that does not implement the interface (it did once, or the interface changed) is
    /// drawn red.
    /// </summary>
    [CustomPropertyDrawer(typeof(InterfaceRef<>), true)]
    public class InterfaceRefDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUIUtility.singleLineHeight;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty objectProp = property.FindPropertyRelative("_object");
            Type interfaceType = GetInterfaceType();

            label = new GUIContent(label.text, $"{label.tooltip}\nAccepts: {interfaceType.Name}".Trim());

            EditorGUI.BeginProperty(position, label, property);

            Color oldColor = GUI.color;
            Object current = objectProp.objectReferenceValue;
            if(!objectProp.hasMultipleDifferentValues && current != null && !interfaceType.IsInstanceOfType(current))
                GUI.color = new Color(1f, 0.5f, 0.5f);

            EditorGUI.BeginChangeCheck();
            Object picked = EditorGUI.ObjectField(position, label, current, typeof(Object), true);
            if(EditorGUI.EndChangeCheck())
            {
                Object accepted = Resolve(picked, interfaceType);
                if(picked == null || accepted != null)
                    objectProp.objectReferenceValue = accepted; // null clears it
                else
                    Debug.LogWarning($"'{picked.name}' does not implement {interfaceType.Name}.", picked);
            }

            GUI.color = oldColor;
            EditorGUI.EndProperty();
        }

        // The object itself if it implements the interface, else the first component of a GameObject that does.
        private static Object Resolve(Object picked, Type interfaceType)
        {
            if(picked == null)
                return null;
            if(interfaceType.IsInstanceOfType(picked))
                return picked;

            GameObject gameObject = picked switch
            {
                GameObject go => go,
                Component component => component.gameObject,
                _ => null,
            };
            if(gameObject == null)
                return null;

            foreach(Component component in gameObject.GetComponents<Component>())
            {
                if(component != null && interfaceType.IsInstanceOfType(component)) // null: a missing script
                    return component;
            }
            return null;
        }

        // The T of the InterfaceRef<T> field, or of its elements for an array or list.
        private Type GetInterfaceType()
        {
            Type type = fieldInfo.FieldType;
            if(type.IsArray)
                type = type.GetElementType();
            else if(type.IsGenericType && typeof(IList).IsAssignableFrom(type))
                type = type.GetGenericArguments()[0];
            return type.GetGenericArguments()[0];
        }
    }
}

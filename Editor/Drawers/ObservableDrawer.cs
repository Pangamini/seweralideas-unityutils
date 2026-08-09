using SeweralIdeas.UnityUtils.Editor;
using SeweralIdeas.Utils;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SeweralIdeas.UnityUtils.Drawers.Editor
{
    // Draws Observable<T> as a single line - the field's own value control,
    // no foldout - and routes edits through Observable<T>.Value's setter
    // rather than the serialized backing field directly, so Changed still
    // fires. A plain SerializedProperty edit would bypass the setter
    // entirely (Unity writes straight to the backing field), which is why
    // this can't just be the default drawer.
    [CustomPropertyDrawer(typeof(Observable<>), true)]
    public class ObservableDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty valueProp = property.FindPropertyRelative("_value");
            return EditorGUI.GetPropertyHeight(valueProp, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty valueProp = property.FindPropertyRelative("_value");

            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();

            // Delayed variants only exist for these three types - Unity has
            // no generic "delayed PropertyField". Everything else (bool,
            // enum, object reference, structs...) already commits atomically
            // per interaction, so there's no per-keystroke churn to delay.
            switch (valueProp.propertyType)
            {
                case SerializedPropertyType.Integer:
                    EditorGUI.DelayedIntField(position, valueProp, label);
                    break;
                case SerializedPropertyType.Float:
                    EditorGUI.DelayedFloatField(position, valueProp, label);
                    break;
                case SerializedPropertyType.String:
                    EditorGUI.DelayedTextField(position, valueProp, label);
                    break;
                default:
                    EditorGUI.PropertyField(position, valueProp, label, true);
                    break;
            }

            if (EditorGUI.EndChangeCheck())
            {
                // valueProp holds the pending edit here - it hasn't been
                // written to the backing field yet, since we never call
                // ApplyModifiedProperties on it below.
                object newValue = valueProp.boxedValue;
                Object[] targets = property.serializedObject.targetObjects;

                Undo.RecordObjects(targets, "Change " + label.text);
                EditorReflectionUtility.SetVariable(property.propertyPath + ".Value", targets, newValue);

                foreach (Object target in targets)
                    EditorUtility.SetDirty(target);

                // Re-sync the SerializedObject's cache from the live object
                // instead of applying the still-pending property edit.
                property.serializedObject.Update();
            }
            EditorGUI.EndProperty();
        }
    }
}

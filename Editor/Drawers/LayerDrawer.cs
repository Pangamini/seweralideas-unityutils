using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace SeweralIdeas.UnityUtils.Drawers.Editor
{
    [CustomPropertyDrawer(typeof(LayerAttribute))]
    class LayerDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {

            if (property.propertyType == SerializedPropertyType.Integer)
            {
                var layerNames = new List<GUIContent>();
                var layerValues = new List<int>();
                for (int i = 0; i < 32; ++i)
                {
                    var name = LayerMask.LayerToName(i);
                    if (string.IsNullOrEmpty(name)) continue;
                    layerNames.Add(new GUIContent(name));
                    layerValues.Add(i);
                }

                EditorGUI.IntPopup(position, property, layerNames.ToArray(), layerValues.ToArray(), label);
            }
            else
            {
                EditorGUI.LabelField(position, label, "Use only with int");
            }
        }
    }
}
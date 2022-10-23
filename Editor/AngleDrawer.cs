using UnityEditor;
using UnityEngine;
using System.Reflection;

namespace SeweralIdeas.Drawers.Editor
{
    [CustomPropertyDrawer(typeof(AngleAttribute))]
    public class AngleDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var height = EditorGUIUtility.singleLineHeight*2;
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (AngleAttribute)attribute;

            EditorGUI.LabelField(position, label);
            var lwidth = EditorGUIUtility.labelWidth;

            // draw thumb
            var thumbStyle = (GUIStyle)"ColorPickerHueRingThumb";
            var thumbSize = thumbStyle.CalcSize(GUIContent.none);
            var thumbRect = new Rect(position.x + lwidth, position.y, thumbSize.x, position.height);
            GUI.Label(thumbRect, GUIContent.none, thumbStyle);

            // get degrees
            var angle = property.floatValue;
            var degrees = attr.radians ? angle * Mathf.Rad2Deg : angle;

            // draw pin
            var center = thumbRect.min + thumbSize * 0.5f;
            var pinWidth = 2;
            var pinSize = new Vector2(thumbSize.x*0.5f + pinWidth*0.5f, pinWidth);
            var pinRect = new Rect(center.x - pinWidth * 0.5f, center.y - pinSize.y*0.5f, pinSize.x, pinSize.y);

            GUI.color = Color.black;
            GUIUtility.RotateAroundPivot(-degrees, center);
            GUI.DrawTexture(pinRect, EditorGUIUtility.whiteTexture);
            GUI.color = Color.white;
            GUI.matrix = Matrix4x4.identity;

            var propertyRect = new Rect(thumbRect.xMax, thumbRect.y, position.xMax - thumbRect.xMax, EditorGUIUtility.singleLineHeight);
            var oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 32;
            EditorGUI.PropertyField(propertyRect, property, new GUIContent(attr.radians ? "rad" : "deg"), false);
            EditorGUIUtility.labelWidth = oldWidth;
        }
    }
}
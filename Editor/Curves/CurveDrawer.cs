using System;
using System.Collections.Generic;
using System.Linq;
using SeweralIdeas.Pooling;
using SeweralIdeas.UnityUtils.Editor;
using SeweralIdeas.Utils;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;

namespace SeweralIdeas.UnityUtils.Curves.Editor
{
    [CustomPropertyDrawer(typeof(CurveAttribute))]
    public class ResponseCurveDrawer : PropertyDrawer
    {
        const float LeftEdgeOffset = 32f;

        private static GUIStyle s_funcValueStyle;

        private static GUIStyle FuncValueStyle()
        {
            if (s_funcValueStyle == null)
            {
                s_funcValueStyle = new GUIStyle(EditorStyles.miniLabel);
                s_funcValueStyle.alignment = TextAnchor.MiddleRight;
            }

            return s_funcValueStyle;
        }
        
        private static readonly Color[] s_curveColors =
        {
            Color.green,
            new(0.33f, 0.44f, 1f),
            new Color(1f, 0.25f, 0.24f),
            Color.yellow,
            Color.cyan,
            Color.white,
            new(1, 0.5f, 0, 1)
        };
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var curveAttribute = (CurveAttribute)attribute;
            float height = EditorGUIUtility.singleLineHeight;
            if (property.isExpanded)
            {
                height += curveAttribute.displayHeight;
                VisitChildren(property, (child) => height += EditorGUI.GetPropertyHeight(child, true));
            }
            return height;
        }

        public static string GetParentFieldPath(string fullPath, string field)
        {
            var split = fullPath.Split('.');
            split[^1] = field;
            using (StringBuilderPool.Get(out var sb))
            {
                for(int i = 0; i<split.Length -1; ++i)
                {
                    sb.Append(split[i]);
                    sb.Append(".");
                }
                sb.Append(field);
                return sb.ToString();
            }
        }

        private void VisitChildren(SerializedProperty property, Action<SerializedProperty> visitor)
        {
            var iterator = property.Copy();
            var stopAt = property.Copy();
            stopAt.NextVisible(false);
            bool first = true;
            while(iterator.NextVisible(first))
            {
                if(iterator.propertyPath == stopAt.propertyPath)
                    break;
                first = false;
                visitor(iterator);
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var curveAttribute = (CurveAttribute)attribute;
            var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            
            EditorGUI.BeginProperty(position, label, property);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label);
            EditorGUI.EndProperty();

            if (property.isExpanded)
            {
                var indented = EditorGUI.IndentedRect(position);
                var curveRect = new Rect(indented.x, indented.yMax - curveAttribute.displayHeight, indented.width, curveAttribute.displayHeight);
                var propertiesRect = new Rect(indented.x, foldoutRect.yMax, indented.width, indented.height - foldoutRect.height - curveRect.height);
                
                float childYpos = propertiesRect.y;
                EditorGUI.indentLevel++;

                VisitChildren(property, (iterator) =>
                {
                    float height = EditorGUI.GetPropertyHeight(iterator, true);
                    var propertyRect = new Rect(propertiesRect.x, childYpos, propertiesRect.width, height);
                    EditorGUI.PropertyField(propertyRect, iterator, null, true);
                    childYpos += height;
                });
                
                DrawCurve(property, curveAttribute, curveRect);
                
                EditorGUI.indentLevel--;
                
                HandleUtility.Repaint();
            }
        }
        private static void DrawCurve(SerializedProperty property, CurveAttribute curveAttribute, Rect curveRect)
        {
            if(Event.current.type != EventType.Repaint)
                return;
            
            using (ListPool<IRealCurve>.Get(out var curves))
            using (ListPool<float>.Get(out var currentValues))
            {
                EditorReflectionUtility.GetVariable(property.propertyPath, property.serializedObject.targetObjects, curves);

                if(!string.IsNullOrEmpty(curveAttribute.displayValue))
                {
                    string displayValuePath = GetParentFieldPath(property.propertyPath, curveAttribute.displayValue);
                    EditorReflectionUtility.GetVariable(displayValuePath, property.serializedObject.targetObjects, currentValues);
                }

                var xRange = new Vector2(curveAttribute.min, curveAttribute.max);

                string xFormat = curveAttribute.xFormat;
                string yFormat = curveAttribute.yFormat;

                CurveGUI(curveRect, curves, xRange, xFormat, yFormat, currentValues);
            }
        }

        public static Rect CurveGUI<T>(Rect position, IList<T> curves, Vector2 xRange, string xFormat = "N2", string yFormat = "N2", IList<float> values = null) where T : IRealCurve
        {
            return CurveGUI(position, curves, xRange, GetYRange(curves, xRange), xFormat, yFormat, values);
        }

        public static Rect CurveGUI<T>(Rect position, IList<T> curves, IList<float> xValues, Vector2 xRange, string xFormat = "N2", string yFormat = "N2", IList<float> values = null) where T : IRealCurve
        {
            var yRange = GetYRange(curves, xRange);
            var curveRect = CurveGUI(position, curves, xRange, yRange, xFormat, yFormat, values);
            if (!curveRect.Contains(Event.current.mousePosition))
            {
                DrawValues(curveRect, curves, xValues, xRange, yRange, yFormat);
            }

            return curveRect;
        }

        private static Vector2 GetYRange<T>(IList<T> curves, Vector2 xRange) where T : IRealCurve
        {
            var yRange = new Vector2(float.PositiveInfinity, float.NegativeInfinity);
            
            for (int i = 0; i < curves.Count; ++i)
            {
                var curveRange = curves[i].GetValueMinMax(xRange);
                yRange.x = Mathf.Min(yRange.x, curveRange.x);
                yRange.y = Mathf.Max(yRange.y, curveRange.y);
            }

            return yRange;
        }
        
        public static Rect CurveGUI<T>(Rect position, IList<T> curves, Vector2 xRange, Vector2 yRange, string xFormat ="N2", string yFormat = "N2", IList<float> values = null) where T:IRealCurve
        {
            Rect curvePosition = new Rect(position.x + LeftEdgeOffset, position.y, position.width - LeftEdgeOffset, position.height);
            
            // background
            GUI.Box(curvePosition, "");
            // transparent "button" to ensure a mouseOver event (is there another way?)
            GUI.color = Color.clear;
            GUI.Box(curvePosition, "", "Button");
            GUI.color = Color.white;

            DrawGrid(curvePosition, xRange, yRange);
            for (int i = curves.Count - 1; i >= 0; --i)
            {
                Color curveColor = s_curveColors[i % s_curveColors.Length];
                DrawCurve(curvePosition, curves[i], xRange, yRange, curveColor);
                if(values != null)
                {
                    float value = values[i];
                    float valueRelative = Mathf.InverseLerp(xRange.x, xRange.y, value);
                    DrawVerticalLine(curvePosition, curveColor, valueRelative);
                }
            }

            if (curvePosition.Contains(Event.current.mousePosition))
            {
                // HandleUtility.Repaint();
                DrawVerticalMouseLine(curvePosition, xRange, xFormat);
                DrawMouseValues(curvePosition, curves, xRange, yRange, yFormat);
            }

            DrawLabels(curvePosition, xRange, yRange, xFormat, yFormat);
            Handles.color = Color.white;
            return curvePosition;
        }

        private static void DrawVerticalLine(Rect curveRect, Color color,float xRelative)
        {
            Handles.color = color;
            float x = Mathf.Lerp(curveRect.xMin, curveRect.xMax, xRelative);
            Handles.DrawLine(new Vector2(x, curveRect.yMin), new Vector2(x, curveRect.yMax));
        }
        
        private static void DrawVerticalMouseLine(Rect curveRect, Vector2 xRange, string xLabelFormat)
        {
            float xPos = Mathf.Clamp(Event.current.mousePosition.x, curveRect.xMin, curveRect.xMax); // - curveRect.xMin;
            float xRelative = Mathf.InverseLerp(curveRect.xMin, curveRect.xMax, xPos);
            // float relativeMouseX = Mathf.InverseLerp(curveRect.xMin, curveRect.xMax, Event.current.mousePosition.x);
            float funcX = Mathf.Lerp(xRange.x, xRange.y, xRelative);
            
            DrawVerticalLine(curveRect, new Color(0, 0, 0, 0.5f), xRelative);
            
            var content = new GUIContent(funcX.ToString(xLabelFormat));
            float lineHeight = EditorGUIUtility.singleLineHeight;
            //GUI.color = Handles.color;
            GUI.Label(new Rect(curveRect.x, curveRect.yMax - lineHeight, curveRect.width, lineHeight), content, EditorStyles.centeredGreyMiniLabel);
        }

        private static void DrawMouseValues<T>(Rect curveRect, IList<T> curves, Vector2 xRange, Vector2 yRange, string yLabelFormat) where T:IRealCurve
        {
            var labelStyle = FuncValueStyle();
            float labelHeight = labelStyle.CalcSize(new GUIContent(" ")).y;
            float valueLabelStart = curveRect.center.y - ((curves.Count+0.5f) * labelHeight * 0.5f);
            
            for (int i = 0; i < curves.Count; ++i)
            {
                IRealCurve curve = curves[i];
                Color color = s_curveColors[i % s_curveColors.Length];
                
                float relativeMouseX = Mathf.InverseLerp(curveRect.xMin, curveRect.xMax, Event.current.mousePosition.x);
                float funcX = Mathf.Lerp(xRange.x, xRange.y, relativeMouseX);
                float funcVal = curve.Evaluate(funcX);
                float xPos = Mathf.Clamp(Event.current.mousePosition.x, curveRect.xMin, curveRect.xMax); // - curveRect.xMin;
                float yPos = Mathf.Lerp(curveRect.yMax, curveRect.yMin, Mathf.InverseLerp(yRange.x, yRange.y, funcVal));

                var curveColor = color;
                curveColor.a *= 0.5f;
                Handles.color = curveColor;
                Handles.DrawLine(new Vector2(curveRect.xMin, yPos), new Vector2(xPos, yPos));
                
                // value label  
                GUI.color = color;
                var content = new GUIContent(funcVal.ToString(yLabelFormat));
                
                var labelRect = new Rect(0, valueLabelStart + i*labelHeight, curveRect.x, labelHeight);
                GUI.Label(labelRect, content, FuncValueStyle());
            }
        }

        private static void DrawValues<T>(Rect curveRect, IList<T> curves, IList<float> values, Vector2 xRange, Vector2 yRange, string yLabelFormat) where T:IRealCurve
        {
            var labelStyle = FuncValueStyle();
            float labelHeight = labelStyle.CalcSize(new GUIContent(" ")).y;
            float valueLabelStart = curveRect.center.y - ((curves.Count+0.5f) * labelHeight * 0.5f);
            
            for (int i = 0; i < curves.Count; ++i)
            {
                IRealCurve curve = curves[i];
                Color color = s_curveColors[i % s_curveColors.Length];

                float funcX = values[i];
                float relX = Mathf.InverseLerp(xRange.x, xRange.y, funcX);
                float funcVal = curve.Evaluate(funcX);
                float xPos = Mathf.Lerp(curveRect.xMin, curveRect.xMax, relX); // - curveRect.xMin;
                float yPos = Mathf.Lerp(curveRect.yMax, curveRect.yMin, Mathf.InverseLerp(yRange.x, yRange.y, funcVal));

                var curveColor = color;
                curveColor.a *= 0.5f;
                Handles.color = curveColor;
                Handles.DrawLine(new Vector2(curveRect.xMin, yPos), new Vector2(xPos, yPos));
                Handles.DrawLine(new Vector2(xPos, curveRect.yMin), new Vector2(xPos, curveRect.yMax));
                
                // value label  
                GUI.color = color;
                var content = new GUIContent(funcVal.ToString(yLabelFormat));
                
                var labelRect = new Rect(0, valueLabelStart + i*labelHeight, curveRect.x, labelHeight);
                GUI.Label(labelRect, content, FuncValueStyle());
            }
        }

        private static float InverseLerpUnclamped(float a, float b, float value)
        {
            if (a != b)
                return ((value - a) / (b - a));
            else
                return 0.0f;
        }

        private static void DrawGrid(Rect curveRect, Vector2 rangeX, Vector2 rangeY)
        {
            // horizontal zero
            {
                float zeroRel = InverseLerpUnclamped(rangeY.x, rangeY.y, 0);
                if (zeroRel >= 0 && zeroRel <= 1)
                {
                    float zeroPos = Mathf.Lerp/*Unclamped*/(curveRect.yMax, curveRect.yMin, zeroRel);
                    Handles.color = Color.black;
                    Handles.DrawLine(new Vector2(curveRect.xMin, zeroPos), new Vector2(curveRect.xMax, zeroPos));
                }
            }

            // vertical zero
            {
                float zeroRel = InverseLerpUnclamped(rangeX.x, rangeX.y, 0);
                if (zeroRel >= 0 && zeroRel <= 1)
                {
                    float zeroPos = Mathf.Lerp/*Unclamped*/(curveRect.xMin, curveRect.xMax, zeroRel);
                    Handles.color = Color.black;
                    Handles.DrawLine(new Vector2(zeroPos, curveRect.yMin), new Vector2(zeroPos, curveRect.yMax));
                }
            }
        }

        private static void DrawLabels(Rect curveRect, Vector2 rangeX, Vector2 rangeY, string xLabelFormat, string yLabelFormat)
        {

            GUI.color = new Color(1, 0.5f, 0.5f, 0.5f);

            // xMin label
            {
                var content = new GUIContent(rangeX.x.ToString(xLabelFormat));
                var contentSize = EditorStyles.whiteMiniLabel.CalcSize(content);
                GUI.Label(new Rect(curveRect.x, curveRect.yMax - contentSize.y, contentSize.x, contentSize.y), content, EditorStyles.whiteMiniLabel);
            }

            // xMax label
            {
                var content = new GUIContent(rangeX.y.ToString(xLabelFormat));
                var contentSize = EditorStyles.whiteMiniLabel.CalcSize(content);
                GUI.Label(new Rect(curveRect.xMax - contentSize.x, curveRect.yMax - contentSize.y, contentSize.x, contentSize.y), content, EditorStyles.whiteMiniLabel);
            }

            GUI.color = new Color(.5f, 1, 0.5f, 0.5f);
            // yMin label
            {
                var content = new GUIContent(rangeY.x.ToString(yLabelFormat));
                var contentSize = EditorStyles.whiteMiniLabel.CalcSize(content);
                GUI.Label(new Rect(curveRect.x - contentSize.x - 4, curveRect.yMax - contentSize.y, contentSize.x, contentSize.y), content, EditorStyles.whiteMiniLabel);
            }

            // yMax label
            {
                var content = new GUIContent(rangeY.y.ToString(yLabelFormat));
                var contentSize = EditorStyles.whiteMiniLabel.CalcSize(content);
                GUI.Label(new Rect(curveRect.x - contentSize.x - 4, curveRect.y, contentSize.x, contentSize.y), content, EditorStyles.whiteMiniLabel);
            }


            GUI.color = Color.white;
        }

        private static void DrawCurve(Rect curveRect, IRealCurve curve, Vector2 rangeX, Vector2 rangeY, Color color)
        {
            float curveWidth = curveRect.width;
            int x = 0;

            Vector2 ToPoint(float xPos, float value)
            {
                float relValue = Mathf.InverseLerp(rangeY.x, rangeY.y, value);
                return new Vector2(xPos + curveRect.x, Mathf.Lerp/*Unclamped*/(curveRect.yMax, curveRect.yMin, relValue));
            }

            // draw curve
            {
                Handles.color = color;

                var prevPoint = ToPoint(0, curve.Evaluate(rangeX.x));

                for (x = 4; x < curveWidth; x += 4)
                {
                    float curveX = Mathf.Lerp(rangeX.x, rangeX.y, x / curveWidth);
                    var nextPoint = ToPoint(x, curve.Evaluate(curveX));

                    Handles.DrawLine(prevPoint, nextPoint);
                    prevPoint = nextPoint;
                }
            }

        }

    }
}
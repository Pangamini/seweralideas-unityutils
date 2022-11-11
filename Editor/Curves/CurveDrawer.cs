using System.Collections.Generic;
using SeweralIdeas.Pooling;
using SeweralIdeas.UnityUtils.Editor;
using UnityEditor;
using UnityEngine;

namespace SeweralIdeas.UnityUtils.Curves.Editor
{
    [CustomPropertyDrawer(typeof(CurveAttribute))]
    public class ResponseCurveDrawer : PropertyDrawer
    {
        
        const string LabelFormat = "N3";
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
                foreach (SerializedProperty child in property)
                {
                    height += EditorGUI.GetPropertyHeight(child, true);
                }
                height += curveAttribute.displayHeight;
            }
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            
            var curveAttribute = (CurveAttribute)attribute;
            var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label);

            if (property.isExpanded)
            {
                var indented = EditorGUI.IndentedRect(position);
                var curveRect = new Rect(indented.x+LeftEdgeOffset, indented.yMax - curveAttribute.displayHeight, indented.width-LeftEdgeOffset, curveAttribute.displayHeight);
                var propertiesRect = new Rect(indented.x, foldoutRect.yMax, indented.width, indented.height - foldoutRect.height - curveRect.height);

                if (Event.current.type == EventType.Repaint)
                {
                    using (ListPool<IRealCurve>.Get(out var curves))
                    {
                        EditorReflectionUtility.GetVariable(property.propertyPath, property.serializedObject.targetObjects, curves);
                        var xRange = new Vector2(curveAttribute.min, curveAttribute.max);
                        var yRange = new Vector2(float.PositiveInfinity, float.NegativeInfinity);
                        
                        foreach (var curve in curves)
                        {
                            var curveRange = curve.GetValueMinMax(xRange);
                            yRange.x = Mathf.Min(yRange.x, curveRange.x);
                            yRange.y = Mathf.Max(yRange.y, curveRange.y);
                        }

                        // background
                        GUI.Box(curveRect, "");
                        // transparent "button" to ensure a mouseOver event (is there another way?)
                        GUI.color = Color.clear;
                        GUI.Box(curveRect, "", "Button");
                        GUI.color = Color.white;
                        
                        DrawGrid(curveRect, xRange, yRange);
                        for(int i = curves.Count-1; i>=0;--i)
                        {
                            DrawCurve(curveRect, curves[i], xRange, yRange, s_curveColors[i % s_curveColors.Length]);
                        }

                        if (curveRect.Contains(Event.current.mousePosition))
                        {
                            HandleUtility.Repaint();

                            // draw vertical mouse line
                            {
                                Handles.color = new Color(0, 0, 0, 0.5f);
                                float xPos = Mathf.Clamp(Event.current.mousePosition.x, curveRect.xMin, curveRect.xMax); // - curveRect.xMin;
                                float relativeMouseX = Mathf.InverseLerp(curveRect.xMin, curveRect.xMax, Event.current.mousePosition.x);
                                float funcX = Mathf.Lerp(xRange.x, xRange.y, relativeMouseX);
                                Handles.DrawLine(new Vector2(xPos, curveRect.yMin), new Vector2(xPos, curveRect.yMax), 2f);
                                var content = new GUIContent(funcX.ToString(LabelFormat));
                                float lineHeight = EditorGUIUtility.singleLineHeight;
                                //GUI.color = Handles.color;
                                GUI.Label(new Rect(curveRect.x, curveRect.yMax - lineHeight, curveRect.width, lineHeight), content, EditorStyles.centeredGreyMiniLabel);
                            }

                            DrawMouseValues(curveRect, curves, xRange, yRange, s_curveColors);
                            
                        }

                        DrawLabels(curveRect, xRange, yRange);
                    }
                }

                float childYpos = propertiesRect.y;
                foreach (SerializedProperty child in property)
                {
                    float height = EditorGUI.GetPropertyHeight(child, true);
                    var propertyRect = new Rect(propertiesRect.x, childYpos, propertiesRect.width, height);
                    EditorGUI.PropertyField(propertyRect, child, null, true);
                    childYpos += height;
                }

            }
        }

        private void DrawMouseValues(Rect curveRect, IList<IRealCurve> curves, Vector2 xRange, Vector2 yRange, Color[] curveColors)
        {
            var labelStyle = FuncValueStyle();
            float labelHeight = labelStyle.CalcSize(new GUIContent(" ")).y;
            float valueLabelStart = curveRect.center.y - ((curves.Count+0.5f) * labelHeight * 0.5f);
            
            for (int i = 0; i < curves.Count; ++i)
            {
                IRealCurve curve = curves[i];
                Color color = curveColors[i % s_curveColors.Length];
                
                float relativeMouseX = Mathf.InverseLerp(curveRect.xMin, curveRect.xMax, Event.current.mousePosition.x);
                float funcX = Mathf.Lerp(xRange.x, xRange.y, relativeMouseX);
                float funcVal = curve.Evaluate(funcX);
                float xPos = Mathf.Clamp(Event.current.mousePosition.x, curveRect.xMin, curveRect.xMax); // - curveRect.xMin;
                float yPos = Mathf.Lerp(curveRect.yMax, curveRect.yMin, Mathf.InverseLerp(yRange.x, yRange.y, funcVal));

                var curveColor = color;
                curveColor.a *= 0.5f;
                Handles.color = curveColor;
                Handles.DrawLine(new Vector2(curveRect.xMin, yPos), new Vector2(xPos, yPos), 2f);
                
                // value label  
                GUI.color = color;
                var content = new GUIContent(funcVal.ToString(LabelFormat));
                
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

        private void DrawGrid(Rect curveRect, Vector2 rangeX, Vector2 rangeY)
        {
            // horizontal zero
            {
                float zeroRel = InverseLerpUnclamped(rangeY.x, rangeY.y, 0);
                if (zeroRel >= 0 && zeroRel <= 1)
                {
                    float zeroPos = Mathf.Lerp/*Unclamped*/(curveRect.yMax, curveRect.yMin, zeroRel);
                    Handles.color = Color.black;
                    Handles.DrawLine(new Vector2(curveRect.xMin, zeroPos), new Vector2(curveRect.xMax, zeroPos), 2f);
                }
            }

            // vertical zero
            {
                float zeroRel = InverseLerpUnclamped(rangeX.x, rangeX.y, 0);
                if (zeroRel >= 0 && zeroRel <= 1)
                {
                    float zeroPos = Mathf.Lerp/*Unclamped*/(curveRect.xMin, curveRect.xMax, zeroRel);
                    Handles.color = Color.black;
                    Handles.DrawLine(new Vector2(zeroPos, curveRect.yMin), new Vector2(zeroPos, curveRect.yMax), 2f);
                }
            }
        }

        private void DrawLabels(Rect curveRect, Vector2 rangeX, Vector2 rangeY)
        {

            GUI.color = new Color(1, 0.5f, 0.5f, 0.5f);

            // xMin label
            {
                var content = new GUIContent(rangeX.x.ToString(LabelFormat));
                var contentSize = EditorStyles.whiteMiniLabel.CalcSize(content);
                GUI.Label(new Rect(curveRect.x, curveRect.yMax - contentSize.y, contentSize.x, contentSize.y), content, EditorStyles.whiteMiniLabel);
            }

            // xMax label
            {
                var content = new GUIContent(rangeX.y.ToString(LabelFormat));
                var contentSize = EditorStyles.whiteMiniLabel.CalcSize(content);
                GUI.Label(new Rect(curveRect.xMax - contentSize.x, curveRect.yMax - contentSize.y, contentSize.x, contentSize.y), content, EditorStyles.whiteMiniLabel);
            }

            GUI.color = new Color(.5f, 1, 0.5f, 0.5f);
            // yMin label
            {
                var content = new GUIContent(rangeY.x.ToString(LabelFormat));
                var contentSize = EditorStyles.whiteMiniLabel.CalcSize(content);
                GUI.Label(new Rect(curveRect.x - contentSize.x - 4, curveRect.yMax - contentSize.y, contentSize.x, contentSize.y), content, EditorStyles.whiteMiniLabel);
            }

            // yMax label
            {
                var content = new GUIContent(rangeY.y.ToString(LabelFormat));
                var contentSize = EditorStyles.whiteMiniLabel.CalcSize(content);
                GUI.Label(new Rect(curveRect.x - contentSize.x - 4, curveRect.y, contentSize.x, contentSize.y), content, EditorStyles.whiteMiniLabel);
            }


            GUI.color = Color.white;
        }

        private void DrawCurve(Rect curveRect, IRealCurve curve, Vector2 rangeX, Vector2 rangeY, Color color)
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

                    Handles.DrawLine(prevPoint, nextPoint, 3f);
                    prevPoint = nextPoint;
                }
            }

        }

    }
}
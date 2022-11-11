using SeweralIdeas.Pooling;
using SeweralIdeas.UnityUtils.Editor;
using UnityEditor;
using UnityEngine;

namespace SeweralIdeas.UnityUtils.Curves.Editor
{
    [CustomPropertyDrawer(typeof(CurveAttribute))]
    public class ResponseCurveDrawer : PropertyDrawer
    {
        private readonly static Color[] m_curveColors = new[] { Color.green, Color.blue, Color.red, Color.yellow, Color.cyan, Color.white, new Color(1, 0.5f, 0, 1) };
        readonly static float s_curveHeight = EditorGUIUtility.singleLineHeight * 4;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            if (property.isExpanded)
            {
                foreach (SerializedProperty child in property)
                {
                    height += EditorGUI.GetPropertyHeight(child, true);
                }
                height += s_curveHeight;
            }
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attribute = (CurveAttribute)this.attribute;
            var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label);

            if (property.isExpanded)
            {
                var indented = EditorGUI.IndentedRect(position);
                var curveRect = new Rect(indented.x+16, indented.yMax - s_curveHeight, indented.width-16, s_curveHeight);
                var propertiesRect = new Rect(indented.x, foldoutRect.yMax, indented.width, indented.height - foldoutRect.height - curveRect.height);

                if (Event.current.type == EventType.Repaint)
                {
                    using (ListPool<IRangedCurve>.Get(out var curves))
                    {
                        EditorReflectionUtility.GetVariable(property.propertyPath, property.serializedObject.targetObjects, curves);
                        var xRange = new Vector2(attribute.min, attribute.max);
                        var yRange = new Vector2(float.PositiveInfinity, float.NegativeInfinity);

                        foreach (var curve in curves)
                        {
                            var curveRange = curve.GetValueMinMax(xRange);
                            yRange.x = Mathf.Min(yRange.x, curveRange.x);
                            yRange.y = Mathf.Max(yRange.y, curveRange.y);
                        }

                        DrawGrid(curveRect, xRange, yRange);

                        for(int i = curves.Count-1; i>=0;--i)
                        {
                            DrawCurve(curveRect, curves[i], xRange, yRange, m_curveColors[i % m_curveColors.Length]);
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


                GUI.Box(curveRect, "");
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

            const string labelFormat = "N3";

            // xMin label
            {
                var content = new GUIContent(rangeX.x.ToString(labelFormat));
                var contentSize = EditorStyles.whiteLabel.CalcSize(content);
                GUI.Label(new Rect(curveRect.x, curveRect.yMax - contentSize.y, contentSize.x, contentSize.y), content, EditorStyles.whiteLabel);
            }

            // xMax label
            {
                var content = new GUIContent(rangeX.y.ToString(labelFormat));
                var contentSize = EditorStyles.whiteLabel.CalcSize(content);
                GUI.Label(new Rect(curveRect.xMax - contentSize.x, curveRect.yMax - contentSize.y, contentSize.x, contentSize.y), content, EditorStyles.whiteLabel);
            }

            GUI.color = new Color(.5f, 1, 0.5f, 0.5f);
            // yMin label
            {
                var content = new GUIContent(rangeY.x.ToString(labelFormat));
                var contentSize = EditorStyles.whiteLabel.CalcSize(content);
                GUI.Label(new Rect(curveRect.x - contentSize.x - 4, curveRect.yMax - contentSize.y, contentSize.x, contentSize.y), content, EditorStyles.whiteLabel);
            }

            // yMax label
            {
                var content = new GUIContent(rangeY.y.ToString(labelFormat));
                var contentSize = EditorStyles.whiteLabel.CalcSize(content);
                GUI.Label(new Rect(curveRect.x - contentSize.x - 4, curveRect.y, contentSize.x, contentSize.y), content, EditorStyles.whiteLabel);
            }


            GUI.color = Color.white;
        }

        private void DrawCurve(Rect curveRect, IRangedCurve curve, Vector2 rangeX, Vector2 rangeY, Color color)
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
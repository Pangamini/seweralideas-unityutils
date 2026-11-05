using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SeweralIdeas.Tweening.Editor
{
    /// <summary>
    /// Adds a preview of the tween's value and speed over time to the inspector. The preview switches on at t=0 and,
    /// optionally, back off partway through the move, so the overshoot of the momentum-keeping modes is visible.
    /// It runs <see cref="Tween.Step"/> at a fixed rate, so it matches the real thing at steady frame rates.
    /// </summary>
    [CustomEditor(typeof(Tween))]
    [CanEditMultipleObjects]
    public class TweenEditor : UnityEditor.Editor
    {
        private const float SimStep = 1f / 120f;
        private const int MaxSteps = 2000;

        private static bool s_showPreview = true;
        private static float s_reverseAt = 0.4f; // in durations; 0 = never reverse

        private readonly List<float> m_values = new();
        private readonly List<float> m_speeds = new();

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();
            s_showPreview = EditorGUILayout.Foldout(s_showPreview, "Preview", true);
            if (!s_showPreview)
                return;

            s_reverseAt = EditorGUILayout.Slider(new GUIContent("Reverse at", "Switch back off after this many durations (0 = don't reverse)"), s_reverseAt, 0f, 2f);

            var mode = (Tween.Mode)serializedObject.FindProperty("m_mode").intValue;
            float duration = Mathf.Max(0.0001f, serializedObject.FindProperty("m_duration").floatValue);
            float maxSpeed = serializedObject.FindProperty("m_maxSpeed").floatValue;
            AnimationCurve curve = serializedObject.FindProperty("m_curve").animationCurveValue ?? AnimationCurve.Linear(0, 0, 1, 1);

            float reverseTime = s_reverseAt * duration;
            float totalTime = (s_reverseAt > 0f ? reverseTime : 0f) + duration * 2f;
            Simulate(mode, curve, duration, maxSpeed, reverseTime, totalTime);

            DrawPlot(m_values, totalTime, reverseTime, "Value", includeRange: (0f, 1f), guides: true);
            DrawPlot(m_speeds, totalTime, reverseTime, "Speed (units/s)", includeRange: (0f, 0f), guides: false);
        }

        private void Simulate(Tween.Mode mode, AnimationCurve curve, float duration, float maxSpeed, float reverseTime, float totalTime)
        {
            m_values.Clear();
            m_speeds.Clear();

            int steps = Mathf.Min(MaxSteps, Mathf.CeilToInt(totalTime / SimStep));
            float dt = totalTime / steps;

            float progress = 0f;
            float velocity = 0f;
            float value = Tween.MapProgress(mode, curve, progress);
            m_values.Add(value);
            m_speeds.Add(0f);

            for (int i = 1; i <= steps; i++)
            {
                float t = i * dt;
                float target = (s_reverseAt > 0f && t > reverseTime) ? 0f : 1f;
                progress = Tween.Step(mode, progress, target, ref velocity, duration, maxSpeed, dt);

                float newValue = Tween.MapProgress(mode, curve, progress);
                m_values.Add(newValue);
                m_speeds.Add((newValue - value) / dt);
                value = newValue;
            }
        }

        private static void DrawPlot(List<float> samples, float totalTime, float reverseTime, string label, (float min, float max) includeRange, bool guides)
        {
            Rect rect = GUILayoutUtility.GetRect(0f, guides ? 110f : 80f, GUILayout.ExpandWidth(true));
            if (Event.current.type != EventType.Repaint)
                return;

            float min = includeRange.min, max = includeRange.max;
            foreach (float s in samples)
            {
                min = Mathf.Min(min, s);
                max = Mathf.Max(max, s);
            }
            if (max - min < 0.0001f)
                max = min + 1f;
            float pad = (max - min) * 0.08f;
            min -= pad;
            max += pad;

            EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f, 1f));

            Vector3 ToPoint(float t, float v) => new(
                Mathf.Lerp(rect.xMin, rect.xMax, t / totalTime),
                Mathf.Lerp(rect.yMax, rect.yMin, Mathf.InverseLerp(min, max, v)),
                0f);

            var gridColor = new Color(1f, 1f, 1f, 0.15f);
            Handles.color = gridColor;
            void HLine(float v) => Handles.DrawLine(ToPoint(0f, v), ToPoint(totalTime, v));

            HLine(0f);
            if (guides)
                HLine(1f);
            if (s_reverseAt > 0f)
                Handles.DrawLine(ToPoint(reverseTime, min), ToPoint(reverseTime, max));

            var points = new Vector3[samples.Count];
            for (int i = 0; i < points.Length; i++)
                points[i] = ToPoint(totalTime * i / (points.Length - 1), samples[i]);

            Handles.color = guides ? new Color(0.4f, 0.8f, 1f) : new Color(1f, 0.7f, 0.3f);
            Handles.DrawAAPolyLine(2f, points);

            var style = EditorStyles.miniLabel;
            GUI.Label(new Rect(rect.x + 4, rect.y, rect.width - 8, 16), $"{label}   [{min + pad:0.##} .. {max - pad:0.##}]   {totalTime:0.##}s", style);
        }
    }
}

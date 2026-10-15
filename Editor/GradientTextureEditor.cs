using UnityEditor;
using UnityEngine;

namespace SeweralIdeas.UnityUtils.Editor
{
    [CustomEditor(typeof(GradientTexture))]
    public class GradientTextureEditor : UnityEditor.Editor
    {
        private enum PreviewMode
        {
            Color,
            Alpha,
            /// <summary> Draws the texture twice as large an area as itself, so the wrap modes show. </summary>
            Tiled,
        }

        private PreviewMode m_previewMode;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var is2D = serializedObject.FindProperty(nameof(GradientTexture.is2D));
            var vertical = serializedObject.FindProperty(nameof(GradientTexture.vertical));
            EditorGUILayout.PropertyField(is2D, new GUIContent("2D"));
            EditorGUILayout.PropertyField(vertical, new GUIContent(is2D.boolValue ? "Switch Axes" : "Vertical", vertical.tooltip));

            if (is2D.boolValue)
            {
                // Axis names follow the first/second gradient, which swap screen axes with "Switch Axes".
                string first = vertical.boolValue ? "First axis (Y)" : "First axis (X)";
                string second = vertical.boolValue ? "Second axis (X)" : "Second axis (Y)";
                DrawAxis(first, nameof(GradientTexture.gradient), nameof(GradientTexture.size), nameof(GradientTexture.wrapMode));
                DrawAxis(second, nameof(GradientTexture.gradient2), nameof(GradientTexture.size2), nameof(GradientTexture.wrapMode2));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GradientTexture.combineMode)));
            }
            else
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GradientTexture.gradient)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GradientTexture.size)));
                EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GradientTexture.wrapMode)));
            }

            EditorGUILayout.Space();
            DrawFormat(serializedObject.FindProperty(nameof(GradientTexture.format)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GradientTexture.linear)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GradientTexture.mipmaps)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(GradientTexture.filterMode)));

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary> Popup limited to the uncompressed formats the texture can be written in. </summary>
        private static void DrawFormat(SerializedProperty property)
        {
            var formats = GradientTexture.SupportedFormats;
            var names = new string[formats.Length];
            int selected = 0;
            for (int i = 0; i < formats.Length; ++i)
            {
                names[i] = formats[i].ToString();
                if ((int)formats[i] == property.intValue)
                    selected = i;
            }

            EditorGUI.BeginChangeCheck();
            selected = EditorGUILayout.Popup(property.displayName, selected, names);
            if (EditorGUI.EndChangeCheck())
                property.intValue = (int)formats[selected];
        }

        private void DrawAxis(string label, string gradientName, string sizeName, string wrapName)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty(gradientName));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(sizeName));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(wrapName));
            EditorGUI.indentLevel--;
        }

        public override bool HasPreviewGUI() => ((GradientTexture)target).Texture != null;

        public override void OnPreviewSettings()
        {
            m_previewMode = (PreviewMode)EditorGUILayout.EnumPopup(m_previewMode, EditorStyles.toolbarDropDown, GUILayout.Width(70f));
        }

        public override string GetInfoString()
        {
            var texture = ((GradientTexture)target).Texture;
            return texture == null ? "" : $"{texture.width}x{texture.height} {texture.format}";
        }

        public override void OnPreviewGUI(Rect rect, GUIStyle background)
        {
            var texture = ((GradientTexture)target).Texture;
            if (texture == null)
                return;

            switch (m_previewMode)
            {
                case PreviewMode.Alpha:
                    EditorGUI.DrawTextureAlpha(rect, texture, ScaleMode.StretchToFill);
                    break;
                case PreviewMode.Tiled:
                    EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));
                    GUI.DrawTextureWithTexCoords(rect, texture, new Rect(-0.5f, -0.5f, 2f, 2f));
                    break;
                default:
                    EditorGUI.DrawTextureTransparent(rect, texture, ScaleMode.StretchToFill);
                    break;
            }
        }
    }
}

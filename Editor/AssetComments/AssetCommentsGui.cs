#nullable enable
using UnityEditor;
using UnityEngine;

namespace SeweralIdeas.UnityUtils.Editor
{
    /// <summary>
    /// Shows <see cref="AssetComments"/>: an editable text under the header of the Inspector of any persistent
    /// object, and a marker with the comment as its tooltip in the Project window.
    /// </summary>
    [InitializeOnLoad]
    internal static class AssetCommentsGui
    {
        private static readonly Color ColorPro = new Color(0.5f, 0.7f, 0.3f, 1f);
        private static readonly Color ColorFree = new Color(0.2f, 0.3f, 0.1f, 1f);

        private const string ControlName = "SeweralIdeas.AssetComment";

        private static GUIContent? s_icon;
        private static GUIStyle? s_textStyle;
        private static GUIStyle? s_hintStyle;
        private static string? s_lastKey;
        private static string? s_editingKey; // the object whose empty comment is being written
        private static bool s_focusSeen;     // the field got the keyboard focus since it was opened

        static AssetCommentsGui()
        {
            UnityEditor.Editor.finishedDefaultHeaderGUI += OnHeaderGui;
            EditorApplication.projectWindowItemOnGUI += OnProjectItemGui;
        }

        private static void OnHeaderGui(UnityEditor.Editor editor)
        {
            if (!UnityUtilsSettings.IsAssetCommentsEnabled)
                return;
            if (editor.targets.Length != 1)
                return;
            if (!AssetComments.TryGetKey(editor.target, out string key))
                return;

            // same look as the Comments component's inspector: green text, no field background
            s_textStyle ??= new GUIStyle { wordWrap = true };
            s_textStyle.normal.textColor = EditorGUIUtility.isProSkin ? ColorPro : ColorFree;
            s_hintStyle ??= new GUIStyle(s_textStyle) { fontStyle = FontStyle.Italic, fontSize = 10 };

            // a text field being edited keeps its own copy of the text: drop that when another object is shown
            if (key != s_lastKey)
            {
                s_lastKey = key;
                s_editingKey = null;
                GUIUtility.keyboardControl = 0;
            }

            string text = AssetComments.Get(key);

            // No comment and not being written: just a small faint line to click, taking next to no space.
            if (text.Length == 0 && s_editingKey != key)
            {
                Rect hintRect = GUILayoutUtility.GetRect(GUIContent.none, s_hintStyle, GUILayout.Height(12f));
                Color oldColor = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, 0.4f);
                GUI.Label(hintRect, "+ Comment", s_hintStyle);
                GUI.color = oldColor;

                Event e = Event.current;
                if (e.type == EventType.MouseDown && e.button == 0 && hintRect.Contains(e.mousePosition))
                {
                    s_editingKey = key;
                    s_focusSeen = false;
                    e.Use();
                    editor.Repaint();
                }
                return;
            }

            EditorGUI.BeginChangeCheck();
            GUI.SetNextControlName(ControlName);
            string edited = EditorGUILayout.TextArea(text, s_textStyle, GUILayout.MinHeight(EditorGUIUtility.singleLineHeight));
            if (EditorGUI.EndChangeCheck())
                AssetComments.Set(key, edited);

            if (s_editingKey == key)
            {
                if (GUI.GetNameOfFocusedControl() == ControlName)
                    s_focusSeen = true;
                else if (!s_focusSeen)
                    EditorGUI.FocusTextInControl(ControlName); // the click that opened the field
                else if (edited.Length == 0)
                    s_editingKey = null; // left it empty: collapse again
            }

            // keep the text clear of the divider drawn under the header
            GUILayout.Space(4f);
        }

        private static void OnProjectItemGui(string guid, Rect rect)
        {
            if (!UnityUtilsSettings.IsAssetCommentsEnabled)
                return;

            string? text = AssetComments.GetTextForGuid(guid);
            if (text == null)
                return;

            s_icon ??= new GUIContent(EditorGUIUtility.IconContent("console.infoicon.sml").image);
            s_icon.tooltip = text;

            const float size = 14f;
            bool listMode = rect.height <= 20f;
            Rect iconRect = listMode
                ? new Rect(rect.xMax - size - 2f, rect.y + (rect.height - size) * 0.5f, size, size)
                : new Rect(rect.xMax - size - 2f, rect.y + 2f, size, size);

            GUI.Label(iconRect, s_icon);
        }
    }
}

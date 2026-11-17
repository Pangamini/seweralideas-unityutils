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
        private static string? s_editingKey; // the object whose empty comment is being written
        private static bool s_focusSeen;     // the field got the keyboard focus since it was opened

        static AssetCommentsGui()
        {
            UnityEditor.Editor.finishedDefaultHeaderGUI += OnHeaderGui;
            EditorApplication.projectWindowItemOnGUI += OnProjectItemGui;
            Selection.selectionChanged += OnSelectionChanged;
        }

        // A text field being edited keeps its own copy of the text, tied to its control id, which another object's
        // field can get: let go of it when the selection changes. Only if it is one of ours that has the focus.
        private static void OnSelectionChanged()
        {
            s_editingKey = null;
            if (GUI.GetNameOfFocusedControl().StartsWith(ControlName, System.StringComparison.Ordinal))
                GUIUtility.keyboardControl = 0;
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

            // One name per object: an Inspector draws this for every component header, and they must not share one.
            string controlName = ControlName + key;

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
            GUI.SetNextControlName(controlName);

            float wrapWidth = EditorGUIUtility.currentViewWidth - 30f;
            float height = Mathf.Max(EditorGUIUtility.singleLineHeight, s_textStyle.CalcHeight(new GUIContent(text.EndsWith('\n') ? text + " " : text), wrapWidth)); // a trailing line break is a line too
            Rect textRect = GUILayoutUtility.GetRect(0f, height, s_textStyle, GUILayout.ExpandWidth(true), GUILayout.Height(height));
            string edited = EditorGUI.TextArea(textRect, text, s_textStyle);
            if (EditorGUI.EndChangeCheck())
                AssetComments.Set(key, edited);

            if (s_editingKey == key && Event.current.type == EventType.Repaint)
            {
                if (GUI.GetNameOfFocusedControl() == controlName)
                    s_focusSeen = true;
                else if (!s_focusSeen)
                {
                    EditorGUI.FocusTextInControl(controlName); // the click that opened the field
                    editor.Repaint();
                }
                else if (edited.Length == 0)
                {
                    s_editingKey = null; // left it empty: collapse again
                    editor.Repaint();
                }
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

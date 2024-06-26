﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace SeweralIdeas.UnityUtils.Editor
{
    public static class AdvancedDropdown
    {
        private static readonly int s_buttonHint = "SeweralIdeas.UnityUtils.Editor.AdvancedDropdown".GetHashCode();

        public static int LayoutDropdown(GUIContent label, int index, IList<GUIContent> options, bool searchBar = true, bool usePrefixLabel = true,
            GUIStyle style = null, params GUILayoutOption[] layoutOptions)
        {

            if (style == null)
                style = "MiniPopup";

            var rect = GUILayoutUtility.GetRect(label, style, layoutOptions);
            return Dropdown(rect, label, index, options, searchBar, usePrefixLabel, style);
        }

        public static int LayoutDropdown(GUIContent label, int index, IList<string> options, bool searchBar = true, bool usePrefixLabel = true,
            GUIStyle style = null, params GUILayoutOption[] layoutOptions)
        {

            if (style == null)
                style = "MiniPopup";

            var rect = GUILayoutUtility.GetRect(label, style, layoutOptions);
            return Dropdown(rect, label, index, options, searchBar, usePrefixLabel, style);
        }

        public static int Dropdown(Rect rect, GUIContent label, int index, IList<string> options, bool searchBar = true, bool usePrefixLabel = true,
            GUIStyle style = null)
        {
            var optionsGui = new GUIContent[options.Count];

            //TODO some more GC-friendly version?
            for (int i = 0; i < options.Count; ++i)
                optionsGui[i] = new GUIContent(options[i]);

            return Dropdown(rect, label, index, optionsGui, searchBar, usePrefixLabel, style);
        }

        public static int Dropdown(Rect rect, GUIContent label, int index, IList<GUIContent> options, bool searchBar = true, bool usePrefixLabel = true,
            GUIStyle style = null)
        {
            var id = GUIUtility.GetControlID(s_buttonHint, FocusType.Passive, rect);
            GUIContent innerText = GUIContent.none;

            if (usePrefixLabel)
            {
                rect = EditorGUI.PrefixLabel(rect, label);
                if (index >= 0 && index < options.Count)
                    innerText = options[index];
            }
            else
                innerText = label;

            if (style == null)
                style = "MiniPopup";
            if (EditorGUI.DropdownButton(rect, innerText, FocusType.Keyboard, style))
            {
                var scrRect = new Rect(GUIUtility.GUIToScreenPoint(rect.position), rect.size);
                AdvancedDropdownWindow.ShowWindow(id, scrRect, options, searchBar);
            }

            int response;
            if (AdvancedDropdownWindow.GetResponse(id, out response))
            {
                if (response != index)
                {
                    GUI.changed = true;
                    return response;
                }
            }

            return index;
        }
    }
}
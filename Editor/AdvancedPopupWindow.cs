using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SeweralIdeas.UnityUtils.Editor
{
    public class AdvancedPopupWindow : EditorWindow
    {
        private IList<GUIContent> m_options;
        private List<KeyValuePair<int, GUIContent>> m_filteredOptions = new List<KeyValuePair<int, GUIContent>>();
        private bool m_searchBar;
        private string m_search = "";
        private string m_filteredSearch = null;
        private int m_controlID;
        private Vector2 m_scrollPos;
        private GUIStyle m_elemStyle;
        private float m_elemHeight;
        private Action<int> m_onElemClick;
        private static int s_responseControlID;
        private static int s_responseResult;

        public static void ShowWindow(int controlID, Rect buttonRect, int index, IList<GUIContent> options, bool searchBar, GUIStyle elementStye = null,
            Action<int> onElemClick = null)
        {
            if (elementStye == null)
                elementStye = EditorStyles.label;


            float scrollWidth = 0;

            for (int i = 0; i < options.Count; ++i)
            {
                float min;
                float max;
                elementStye.CalcMinMaxWidth(new GUIContent(options[i].text), out min, out max);
                scrollWidth = Mathf.Max(scrollWidth, max);
            }

            scrollWidth += 32;

            var size = new Vector2(Mathf.Max(scrollWidth, buttonRect.width), 256);
            var window = CreateInstance<AdvancedPopupWindow>();
            window.ShowAsDropDown(buttonRect, size);

            window.m_options = options;
            window.m_controlID = controlID;
            window.m_searchBar = searchBar;
            window.m_elemStyle = elementStye;
            window.m_elemHeight = EditorGUIUtility.singleLineHeight;
            window.m_onElemClick = onElemClick;

        }

        private void OnEnable()
        {
            wantsMouseMove = true;
            wantsMouseEnterLeaveWindow = true;

        }


        private void OnGUI()
        {
            GUILayout.BeginVertical("box");

            if (m_searchBar)
            {
                //toolbar
                GUILayout.BeginHorizontal("Toolbar");

                GUI.SetNextControlName("SearchBar");
                m_search = EditorGUILayout.TextField(GUIContent.none, m_search, "ToolbarSeachTextField", GUILayout.ExpandWidth(true));
                if (GUILayout.Button("", "ToolbarSeachCancelButton"))
                    m_search = "";

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            if (m_filteredSearch != m_search)
            {
                m_filteredSearch = m_search;
                m_filteredOptions.Clear();
                var lowerSearch = m_search.ToLower();
                for (int i = 0; i < m_options.Count; ++i)
                {
                    if (m_options[i].text.ToLower().Contains(lowerSearch))
                        m_filteredOptions.Add(new KeyValuePair<int, GUIContent>(i, m_options[i]));
                }
            }


            m_scrollPos = GUILayout.BeginScrollView(m_scrollPos);

            var scrollRect = GUILayoutUtility.GetRect(0f, m_elemHeight * m_filteredOptions.Count);

            for (int i = 0; i < m_filteredOptions.Count; ++i)
            {

                var buttonRect = new Rect(scrollRect.x, scrollRect.y + i * m_elemHeight, scrollRect.width, m_elemHeight);
                var contains = buttonRect.Contains(Event.current.mousePosition);
                if (contains)
                    GUI.Box(buttonRect, "", "SelectionRect");
                if (GUI.Button(buttonRect, m_filteredOptions[i].Value, m_elemStyle))
                {
                    OnElemClicked(m_filteredOptions[i].Key);
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            EditorGUI.FocusTextInControl("SearchBar");

            if (Event.current.type == EventType.MouseMove || Event.current.type == EventType.MouseLeaveWindow || Event.current.type == EventType.MouseDrag)
                Repaint();
        }

        private void OnElemClicked(int index)
        {
            if (m_onElemClick != null)
            {
                try
                {
                    m_onElemClick(index);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            s_responseControlID = m_controlID;
            s_responseResult = index;
            Close();
        }

        public static bool GetResponse(int controlID, out int result)
        {
            if (s_responseControlID == controlID)
            {
                result = s_responseResult;
                s_responseControlID = 0;
                return true;
            }

            result = -1;
            return false;
        }
    }
}
using System;
using System.Collections.Generic;
using SeweralIdeas.Utils;
using UnityEngine;

namespace SeweralIdeas.UnityUtils.Editor
{
    public static class TypeDropdown
    {
        public static void ShowTypeDropdown(Rect buttonRect, TypeUtility.TypeQuery typeQuery, Action<Type> onTypeSelected)
        {
            var controlId = GUIUtility.GetControlID(FocusType.Passive, buttonRect);

            TypeUtility.TypeList typeList = TypeUtility.GetDerivedTypes(typeQuery);

            List<GUIContent> options = new();
            foreach (Type type in typeList.types)
            {
                options.Add(new GUIContent(type.Name, HierarchyIcons.GetTexture(type)));
            }

            void OnKeySelected(int selectedIndex)
            {
                onTypeSelected(typeList.types[selectedIndex]);
            }

            var scrRect = new Rect(GUIUtility.GUIToScreenPoint(buttonRect.position), new Vector2(256, buttonRect.height));
            AdvancedDropdownWindow.ShowWindow(controlId, scrRect, options, true, null, OnKeySelected);
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace SeweralIdeas.UnityUtils
{
    [InitializeOnLoad]
    public static class HierarchyIcons
    {
        static readonly Dictionary<Type, Texture> Icons = new Dictionary<Type, Texture>();

        public static bool GetTexture(Type type, out Texture texture)
        {
            return Icons.TryGetValue(type, out texture);
        }

        public static Texture GetTexture(Type type)
        {
            Icons.TryGetValue(type, out Texture texture);
            return texture;
        }

        static HierarchyIcons()
        {
            EditorApplication.hierarchyWindowItemOnGUI += DrawHierarchyGUI;

            Dictionary<Type, Texture> iconsFromScript = new Dictionary<Type, Texture>();
            var guids = AssetDatabase.FindAssets("t:MonoScript");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                var type = script.GetClass();
                if (typeof(Object).IsAssignableFrom(type))
                {
                    var icon = EditorGUIUtility.ObjectContent(script, type).image;
                    if(icon && icon.name != "cs Script Icon")
                    {
                        if(!Icons.TryAdd(type, icon))
                        {
                            Debug.Log($"Already present: {type.Name}");
                        }

                    }
                }
            }
            //
            // var typeList = TypeUtility.GetDerivedTypes(new TypeUtility.TypeQuery(typeof(IHierarchyIcon), true, true));
            // foreach (var type in typeList.types)
            // {
            //     var searchType = type;
            //     while (searchType != null)
            //     {
            //         Texture icon;
            //         if (iconsFromScript.TryGetValue(searchType, out icon))
            //         {
            //             s_icons.Add(type, icon);
            //             break;
            //         }
            //         searchType = searchType.BaseType;
            //     }
            // }

        }

        static void DrawHierarchyGUI(int instanceID, Rect selectionRect)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (obj == null) return;

            Type type = null;
            
            using (ListPool<Component>.Get(out var components))
            {
                obj.GetComponents(components);
                foreach (var comp in components)
                {
                    if(!comp)
                        continue;
                    if(comp.GetType().GetCustomAttribute<HierarchyIconAttribute>() != null)
                    {
                        type = comp.GetType();
                        break;
                    }
                }
            }

            if(type == null)
                return;
            
            Texture texture;
            if (Icons.TryGetValue(type, out texture))
            {
                // place the icoon to the right of the list:
                Rect r = new Rect(selectionRect);
                r.x = selectionRect.x + selectionRect.width - 20;
                r.width = 18;

                GUI.DrawTexture(r, texture, ScaleMode.ScaleToFit, true);
                GUI.Label(r, new GUIContent("", type.Name));
            }

        }
    }
}
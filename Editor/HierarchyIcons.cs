using System;
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
        static bool s_iconsBuilt;
        public static Texture NullTypeIcon => null;

        public static bool GetTexture(Type type, out Texture texture)
        {
            return Icons.TryGetValue(type, out texture);
        }

        public static Texture GetTexture(Type type)
        {
            if (type == null)
                return NullTypeIcon;
            Icons.TryGetValue(type, out Texture texture);
            return texture;
        }

        static HierarchyIcons()
        {
            // Subscribing here is cheap regardless of the setting; the expensive part (scanning
            // every MonoScript in the project) is deferred to EnsureIconsBuilt, which only runs
            // lazily, on first actual use, and only when the feature is enabled. That way toggling
            // the setting in Project Settings takes effect immediately without a domain reload,
            // and projects that leave it off (the default) never pay the scan cost at all.
#if UNITY_6000_6_OR_NEWER
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI += DrawHierarchyGUI;
#else
            EditorApplication.hierarchyWindowItemOnGUI += DrawHierarchyGUI;
#endif
        }

        static void EnsureIconsBuilt()
        {
            if (s_iconsBuilt)
                return;
            s_iconsBuilt = true;

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
        }

#if UNITY_6000_6_OR_NEWER
        static void DrawHierarchyGUI(EntityId entityId, Rect selectionRect)
        {
            var obj = EditorUtility.EntityIdToObject(entityId) as GameObject;
            if (obj == null) return;
            DrawHierarchyGUI(obj, selectionRect);
        }
#else
        static void DrawHierarchyGUI(int instanceID, Rect selectionRect)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (obj == null) return;
            DrawHierarchyGUI(obj, selectionRect);
        }
#endif

        static void DrawHierarchyGUI(GameObject obj, Rect selectionRect)
        {
            if (!SeweralIdeas.UnityUtils.Editor.UnityUtilsSettings.IsHierarchyIconsEnabled)
                return;

            EnsureIconsBuilt();

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

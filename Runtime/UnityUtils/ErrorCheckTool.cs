using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;

namespace SeweralIdeas.UnityUtils
{
    public interface IErrorCheck
    {
        void CheckForErrors(ref ErrorCheckTool.GameObjectError error);
    }

#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public static class ErrorCheckTool
    {

        private const string ChildHasErrors = "Child has errors";
        private const string ChildHasWarnings = "Child has warnings";
        
#if UNITY_EDITOR
        static ErrorCheckTool()
        {
            EditorApplication.hierarchyWindowItemOnGUI += DrawHierarchyGUI;
            // EditorApplication.hierarchyChanged += EditorApplication.RepaintHierarchyWindow;
        }
#endif
        
        public struct GameObjectError
        {
            public GameObject gameObject;
            public string error;
            public string warning;
            public bool hasError;
            public bool childHasError;
            public bool hasWarning;
            public bool childHasWarning;

            public void AddError(string newError)
            {
                error += newError;
                hasError = true;
            }

            public void AddWarning(string newWarning)
            {
                warning += newWarning+"\n";
                hasWarning = true;
            }
        }
        
        public static GameObjectError ValidateGameObject(GameObject go)
        {
            GameObjectError error;

            error = new GameObjectError();
            error.gameObject = go;

            using (ListPool<IErrorCheck>.Get(out var results))
            {
                go.GetComponents(results);
                foreach (var comp in results)
                {
                    if (comp == null)
                        continue;
                    comp.CheckForErrors(ref error);
                }
            }

            //if (!error.hasError)
            {
                var childCount = go.transform.childCount;
                for (int i = 0; i < childCount; ++i)
                {
                    var child = go.transform.GetChild(i);
                    var childError = ValidateGameObject(child.gameObject);
                    if (childError.hasError || childError.childHasError)
                    {
                        error.childHasError = true;
                    }
                    if (childError.hasWarning || childError.childHasWarning)
                    {
                        error.childHasWarning = true;
                    }
                }
            }

            return error;
        }

#if UNITY_EDITOR
        static void DrawHierarchyGUI(int instanceID, Rect selectionRect)
        {
            var go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (go == null) return;

            if (!Application.isPlaying)
            {
                var error = ValidateGameObject(go);

                if (error.hasError || error.childHasError)
                {
                    var errorRect = selectionRect;
                    errorRect.width += errorRect.x;
                    errorRect.x = 0;
                    GUIContent content;
                    if (error.hasError)
                        content = new GUIContent("", error.error);
                    else
                    {
                        content = new GUIContent("", ChildHasErrors);
                        GUI.color = new Color(1, 1, 1, 0.5f);
                    }
                    GUI.Label(errorRect, content, "CN EntryErrorIconSmall");
                    GUI.color = Color.white;
                }
                if (error.hasWarning || error.childHasWarning)
                {
                    var errorRect = selectionRect;
                    errorRect.width += errorRect.x;
                    errorRect.x = 0;
                    GUIContent content;
                    if (error.hasWarning)
                        content = new GUIContent("", error.warning);
                    else
                    {
                        content = new GUIContent("", ChildHasWarnings);
                        GUI.color = new Color(1, 1, 1, 0.5f);
                    }
                    GUI.Label(errorRect, content, "CN EntryWarnIconSmall");
                    GUI.color = Color.white;
                }
            }
            
        }
#endif
    }
}
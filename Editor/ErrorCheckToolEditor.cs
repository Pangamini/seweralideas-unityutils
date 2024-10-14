using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;

namespace SeweralIdeas.UnityUtils.Editor
{
    [InitializeOnLoad]
    public static class ErrorCheckToolEditor
    {
        private const string ChildHasErrors   = "Child has errors";
        private const string ChildHasWarnings = "Child has warnings";

        private const string EnabledKey = "ErrorCheckTool_InspectionEnabled";

        public static bool Enabled
        {
            get => EditorPrefs.GetBool(EnabledKey, false);
            set
            {
                EditorPrefs.SetBool(EnabledKey, value);
                Menu.SetChecked(MenuItemPath, value);
            }
        }

        static ErrorCheckToolEditor()
        {
            EditorApplication.hierarchyWindowItemOnGUI += DrawHierarchyGUI;
            Menu.SetChecked(MenuItemPath, Enabled);
        }

        static void DrawHierarchyGUI(int instanceID, Rect selectionRect)
        {
            var go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (go == null) return;

            if(Application.isPlaying || !Enabled)
                return;
            
            var error = ValidateGameObject(go);

            if (error.hasError || error.childHasError)
            {
                ShowError(selectionRect, error);
            }
            if (error.hasWarning || error.childHasWarning)
            {
                ShowWarning(selectionRect, error);
            }

        }
        private static void ShowWarning(Rect selectionRect, ErrorCheckTool.GameObjectError error)
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
        
        private static void ShowError(Rect selectionRect, ErrorCheckTool.GameObjectError error)
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

        public static ErrorCheckTool.GameObjectError ValidateGameObject(GameObject go)
        {
            ErrorCheckTool.GameObjectError error;

            error = new ErrorCheckTool.GameObjectError();
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

        private const string MenuItemPath = "Window/Error Check Tool/Enable Hierarchy";
        [MenuItem(MenuItemPath)]
        static void MenuItemEnable()
        {
            Enabled = !Enabled;
        }
    }
}

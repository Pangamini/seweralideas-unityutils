#nullable enable

namespace SeweralIdeas.UnityUtils.Editor
{
    using UnityEditor;

    public static class SetSelectionDirty
    {
        private const string MenuItemPath = "Assets/Set Dirty";

        [MenuItem(MenuItemPath, priority = 2001)]
        private static void SetSelectedObjectsDirty()
        {
            foreach(var obj in Selection.objects)
                EditorUtility.SetDirty(obj);
        }
        
        [MenuItem(MenuItemPath, isValidateFunction:true)]
        private static bool SetSelectedObjectsDirtyValidation()
        {
            return Selection.count > 0;
        }
    }
}
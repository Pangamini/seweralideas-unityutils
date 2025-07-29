using System;
using System.Collections.Generic;
using System.IO;
using SeweralIdeas.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace SeweralIdeas.UnityUtils.Editor
{
    [CustomEditor(typeof(AutoAssetByNameLookup<>),editorForChildClasses:true)]
    public class AutoAssetByNameLookupEditor : UnityEditor.Editor
    {
        private static readonly string[] Exclude = {"_table"};
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, Exclude);
            
            var tableProp = serializedObject.FindProperty("_table");
            GUI.enabled = false;
            EditorGUILayout.PropertyField(tableProp);
            GUI.enabled = true;
            
            serializedObject.ApplyModifiedProperties();
        }
    }
    
    
    public class AutoAssetByNameLookupPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            var allTypes = TypeUtility.GetDerivedTypes(new TypeUtility.TypeQuery(typeof(AutoAssetByNameLookup<>), false, true));

            using (HashSetPool<ScriptableObject>.Get(out var autoLists))
            {
                // Collect all autoLists in a HashSet (for uniqueness)
                foreach (Type autoListType in allTypes.types)
                {
                    var listGuids = AssetDatabase.FindAssets($"t:{autoListType.Name}");
                    foreach (var listGuid in listGuids)
                    {
                        string listPath = AssetDatabase.GUIDToAssetPath(listGuid);
                        var autoListAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(listPath);
                        autoLists.Add(autoListAsset);
                    }
                }
                
                // rebuild every AutoList' content
                foreach (ScriptableObject autoList in autoLists)
                {
                    var assetPath = AssetDatabase.GetAssetPath(autoList);
                    var autoAssetByNameLookup = ((IAutoAssetByNameLookup)autoList);

                    using (ListPool<Object>.Get(out var elements))
                    {
                        FindAssets(assetPath, autoAssetByNameLookup.GetElementType(), autoAssetByNameLookup.Mode, elements);
                        
                        using var so = new SerializedObject(autoList);

                        var tableProp = so.FindProperty("_table");
                        var listProp = tableProp.FindPropertyRelative("_list");
                        
                        listProp.arraySize = elements.Count;

                        for (var index = 0; index < elements.Count; index++)
                        {
                            var element = elements[index];
                            var elementProp = listProp.GetArrayElementAtIndex(index);
                            elementProp.objectReferenceValue = element;
                        }

                        so.ApplyModifiedProperties();
                    }
                }
            }
        }


        private static void FindAssets(string assetPath, Type elementType, IAutoAssetByNameLookup.SearchMode searchMode, List<Object> results)
        {
            if (searchMode == IAutoAssetByNameLookup.SearchMode.None || elementType == null)
                return;

            string filter = $"t:{elementType.Name}";
            List<string> searchFolders = new();

            string folderPath = null;

            switch (searchMode)
            {
                case IAutoAssetByNameLookup.SearchMode.Folder:
                case IAutoAssetByNameLookup.SearchMode.FolderAndSubfolders:
                {
                    folderPath = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
                    if (!string.IsNullOrEmpty(folderPath) && AssetDatabase.IsValidFolder(folderPath))
                        searchFolders.Add(folderPath);
                }
                    break;

                case IAutoAssetByNameLookup.SearchMode.Project:
                    break; // null searchFolders searches whole project
            }

            var guids = AssetDatabase.FindAssets(filter, searchMode == IAutoAssetByNameLookup.SearchMode.Project ? null : searchFolders.ToArray());

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if(string.IsNullOrEmpty(path))
                    continue;
                
                // Exclude subfolders if in "Folder" mode
                if (searchMode == IAutoAssetByNameLookup.SearchMode.Folder && folderPath != null)
                {
                    string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
                    if (!string.Equals(parent, folderPath, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                var obj = AssetDatabase.LoadAssetAtPath(path, elementType);
                if (obj != null)
                    results.Add(obj);
            }
        }
    }
}

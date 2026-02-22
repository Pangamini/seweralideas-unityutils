#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Pool;

namespace SeweralIdeas.UnityUtils.Editor
{
    using UnityEditor;
    using UnityEngine;

    public static class SetSelectionParentAsset
    {
        private const string MenuItemPath = "Assets/Set Parent Asset (Make Subassets)";

        [MenuItem(MenuItemPath, priority = 2000)]
        private async static void SetParentAsset()
        {
            try
            {
                using (ListPool<Object>.Get(out var selectedAssets))
                {
                    GetEligibleSelectedAssets(selectedAssets);

                    if(selectedAssets == null || selectedAssets.Count == 0)
                        return;

                    // TODO: Replace this with a modal UI prompt that lets the user pick a parent asset
                    var (assetAccepted, parentAsset) = await PromptForParentAsset();

                    if(!assetAccepted)
                        return;

                    string? parentPath = null;
                    if(parentAsset != null)
                    {
                        parentPath = AssetDatabase.GetAssetPath(parentAsset);
                        if(string.IsNullOrEmpty(parentPath))
                        {
                            Debug.LogError("Invalid parent asset.");
                            return;
                        }
                    }

                    foreach (Object asset in selectedAssets)
                    {
                        if(asset == null || asset == parentAsset)
                            continue;

                        string assetPath = AssetDatabase.GetAssetPath(asset);

                        // Skip non-assets (folders, scene objects, etc.)
                        if(string.IsNullOrEmpty(assetPath))
                            continue;

                        // Remove asset from its original location if needed
                        AssetDatabase.RemoveObjectFromAsset(asset);

                        // Add as subasset
                        if(parentAsset != null)
                        {
                            AssetDatabase.AddObjectToAsset(asset, parentAsset);
                        }
                        else
                        {
                            string dir = System.IO.Path.GetDirectoryName(assetPath)!;
                            string newPath = AssetDatabase.GenerateUniqueAssetPath($"{dir}/{asset.name}.asset");
                            AssetDatabase.CreateAsset(asset, newPath);
                        }
                    }

                    if(parentPath != null)
                        AssetDatabase.ImportAsset(parentPath);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    if(parentAsset)
                        Debug.Log($"Added {selectedAssets.Count} assets as subassets of '{parentAsset.name}'.");
                    else
                        Debug.Log($"Removed {selectedAssets.Count} assets as subassets.");
                }
            }
            catch( Exception e )
            {
                Debug.LogException(e);
            }
        }

        private static Task<(bool accepted, Object asset)> PromptForParentAsset()
        {
            return PromptWindow.ShowAsync<Object>("Select parent asset", "", null, PromptFieldGui, IsEligibleParentAsset);
        }
        
        public static bool IsEligibleParentAsset(Object parent)
        {
            // null parent is valid (unparent the asset)
            if (parent == null)
                return true;

            // Must be an asset, not a scene object
            if (!EditorUtility.IsPersistent(parent))
                return false;

            string path = AssetDatabase.GetAssetPath(parent);

            // Must have a valid asset path
            if (string.IsNullOrEmpty(path))
                return false;

            // Cannot be a folder
            if (AssetDatabase.IsValidFolder(path))
                return false;

            // Must be editable
            if ((parent.hideFlags & HideFlags.NotEditable) != 0)
                return false;

            // Must not be a subasset of another asset
            // (prevents deep or confusing nesting chains)
            if (AssetDatabase.IsSubAsset(parent))
                return false;

            return true;
        }

        private static Object PromptFieldGui(Object arg) => EditorGUILayout.ObjectField(arg, typeof( Object ), false);

        [MenuItem(MenuItemPath, validate = true)]
        private static bool ValidateSetParentAsset()
        {
            using (ListPool<Object>.Get(out var result))
            {
                GetEligibleSelectedAssets(result);
                return result.Count > 0;
            }
        }

        /// <summary>
        /// Fills the provided list with selected objects that are eligible to be added as subassets.
        /// </summary>
        private static void GetEligibleSelectedAssets(ICollection<Object> results)
        {
            results.Clear();

            foreach (Object obj in Selection.objects)
            {
                if(obj == null)
                    continue;

                // Must be a persistent asset (not a scene object)
                if(!EditorUtility.IsPersistent(obj))
                    continue;

                string assetPath = AssetDatabase.GetAssetPath(obj);

                // Skip invalid paths and folders
                if(string.IsNullOrEmpty(assetPath) || AssetDatabase.IsValidFolder(assetPath))
                    continue;

                // // Skip main assets that Unity manages tightly (eg. imported model roots)
                // if(AssetDatabase.IsMainAsset(obj))
                // {
                //     // ScriptableObjects and similar are fine
                //     if(!(obj is ScriptableObject))
                //         continue;
                // }

                // Must be allowed to be edited
                if((obj.hideFlags & HideFlags.NotEditable) != 0)
                    continue;

                results.Add(obj);
            }
        }
    }
}
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

        private static bool _busy;

        [MenuItem(MenuItemPath, priority = 2000)]
        private async static void SetParentAsset()
        {
            if(_busy)
                return;

            _busy = true;
            try
            {
                using (ListPool<Object>.Get(out var selectedAssets))
                {
                    GetEligibleSelectedAssets(selectedAssets);

                    if(selectedAssets.Count == 0)
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

                    // Files that held a main asset which got moved; they may be left with nothing in them.
                    var vacatedPaths = new HashSet<string>();
                    // Files that lost a subasset and need to be rewritten.
                    var touchedPaths = new HashSet<string>();
                    int movedCount = 0;

                    foreach (Object asset in selectedAssets)
                    {
                        if(asset == null || asset == parentAsset)
                            continue;

                        string assetPath = AssetDatabase.GetAssetPath(asset);

                        // Skip non-assets (folders, scene objects, etc.)
                        if(string.IsNullOrEmpty(assetPath))
                            continue;

                        bool isMain = AssetDatabase.IsMainAsset(asset);

                        // A main asset can't be unparented, it already is standalone.
                        if(parentAsset == null && isMain)
                            continue;

                        // Moving a main asset would orphan the subassets that belong to it.
                        if(isMain && AssetDatabase.LoadAllAssetsAtPath(assetPath).Length > 1)
                        {
                            Debug.LogWarning($"Skipped '{assetPath}': it has subassets of its own.", asset);
                            continue;
                        }

                        if(isMain)
                            vacatedPaths.Add(assetPath);
                        else
                            touchedPaths.Add(assetPath);

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
                            string assetName = string.IsNullOrEmpty(asset.name) ? asset.GetType().Name : asset.name;
                            string newPath = AssetDatabase.GenerateUniqueAssetPath($"{dir}/{assetName}.asset");
                            AssetDatabase.CreateAsset(asset, newPath);
                        }

                        movedCount++;
                    }

                    if(parentAsset != null && movedCount > 0)
                        EditorUtility.SetDirty(parentAsset);

                    foreach (string touchedPath in touchedPaths)
                    {
                        Object? mainAsset = AssetDatabase.LoadMainAssetAtPath(touchedPath);
                        if(mainAsset != null)
                            EditorUtility.SetDirty(mainAsset);
                    }

                    if(parentPath != null)
                        AssetDatabase.ImportAsset(parentPath);
                    AssetDatabase.SaveAssets();

                    // Moving a main asset leaves its original file behind with no objects in it.
                    foreach (string vacatedPath in vacatedPaths)
                    {
                        AssetDatabase.ImportAsset(vacatedPath);
                        if(AssetDatabase.LoadAllAssetsAtPath(vacatedPath).Length == 0)
                            AssetDatabase.DeleteAsset(vacatedPath);
                    }

                    AssetDatabase.Refresh();

                    if(parentAsset)
                        Debug.Log($"Added {movedCount} assets as subassets of '{parentAsset.name}'.");
                    else
                        Debug.Log($"Removed {movedCount} assets as subassets.");
                }
            }
            catch( Exception e )
            {
                Debug.LogException(e);
            }
            finally
            {
                _busy = false;
            }
        }

        private static Task<(bool accepted, Object asset)> PromptForParentAsset()
        {
            return PromptWindow.ShowAsync<Object>("Select parent asset", "", null, PromptFieldGui, GetParentAssetError, new Vector2(350, 130));
        }

        public static bool IsEligibleParentAsset(Object parent) => GetParentAssetError(parent) == null;

        /// <summary>
        /// Returns why <paramref name="parent"/> can't be used as a parent asset, or null if it can.
        /// </summary>
        public static string? GetParentAssetError(Object parent)
        {
            // null parent is valid (unparent the asset)
            if (parent == null)
                return null;

            // Must be an asset, not a scene object
            if (!EditorUtility.IsPersistent(parent))
                return "Must be an asset, not a scene object.";

            string path = AssetDatabase.GetAssetPath(parent);

            // Must have a valid asset path
            if (string.IsNullOrEmpty(path))
                return "The object has no asset path.";

            // Cannot be a folder
            if (AssetDatabase.IsValidFolder(path))
                return "A folder can't be a parent asset.";

            // Only natively serialized assets (.asset, .controller, .mat, .anim, ...) can hold
            // subassets; those from other importers (fbx, png, ...) lose them on reimport.
            // Scenes are excluded as they aren't valid parents.
            // Native-format assets come back with the plain base AssetImporter (no managed subclass),
            // whereas other importers (ModelImporter, TextureImporter, ...) are subclasses.
            if (parent is SceneAsset)
                return "A scene can't be a parent asset.";
            if (parent is GameObject)
                return "A prefab or model can't be a parent asset.";
            AssetImporter? importer = AssetImporter.GetAtPath(path);
            if (importer == null || importer.GetType() != typeof(AssetImporter))
                return $"'{path}' is managed by {importer?.GetType().Name ?? "an importer"}; subassets would be lost on reimport.";

            // Must be editable
            if ((parent.hideFlags & HideFlags.NotEditable) != 0)
                return "The asset is not editable.";

            // Must not be a subasset of another asset
            // (prevents deep or confusing nesting chains)
            if (AssetDatabase.IsSubAsset(parent))
                return "The asset is already a subasset; pick the main asset of its file.";

            return null;
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
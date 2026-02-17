using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace SeweralIdeas.AddressableExtensions
{
    [Serializable]
    public class AssetReferenceComponent<T> : AssetReferenceT<GameObject>
        where T : Component
    {
        public AssetReferenceComponent(string guid) : base(guid) { }

        /* -----------------------------------------------------------
         * Load (no instantiation)
         * ----------------------------------------------------------- */

        public AsyncOperationHandle<T> LoadComponentAsync()
        {
            var prefabHandle = Addressables.LoadAssetAsync<GameObject>(RuntimeKey);

            return Addressables.ResourceManager.CreateChainOperation(
                prefabHandle,
                handle =>
                {
                    var go = handle.Result;
                    if(go == null)
                        throw new Exception($"Loaded prefab is null for {RuntimeKey}");

                    var component = go.GetComponent<T>();
                    if(component == null)
                        throw new Exception(
                            $"Prefab '{go.name}' does not contain component of type {typeof( T )}"
                        );

                    return Addressables.ResourceManager.CreateCompletedOperation(
                        component,
                        string.Empty
                    );
                }
            );
        }

        /* -----------------------------------------------------------
         * Instantiate
         * ----------------------------------------------------------- */

        public AsyncOperationHandle<T> InstantiateComponentAsync(
            Vector3 position,
            Quaternion rotation,
            Transform parent = null
        )
        {
            var instanceHandle = Addressables.InstantiateAsync(
                RuntimeKey,
                position,
                rotation,
                parent
            );

            return ChainToComponent(instanceHandle);
        }

        public AsyncOperationHandle<T> InstantiateComponentAsync(Transform parent = null)
        {
            var instanceHandle = Addressables.InstantiateAsync(RuntimeKey, parent);
            return ChainToComponent(instanceHandle);
        }

        /* -----------------------------------------------------------
         * Release helpers
         * ----------------------------------------------------------- */

        public void ReleaseInstance(T component)
        {
            if(component != null)
                Addressables.ReleaseInstance(component.gameObject);
        }

        /* -----------------------------------------------------------
         * Internal helpers
         * ----------------------------------------------------------- */

        private AsyncOperationHandle<T> ChainToComponent(
            AsyncOperationHandle<GameObject> instanceHandle
        )
        {
            return Addressables.ResourceManager.CreateChainOperation(
                instanceHandle,
                handle =>
                {
                    var go = handle.Result;
                    if(go == null)
                        throw new Exception($"Instantiated GameObject is null for {RuntimeKey}");

                    var component = go.GetComponent<T>();
                    if(component == null)
                        throw new Exception(
                            $"Instantiated prefab '{go.name}' does not contain component of type {typeof( T )}"
                        );

                    return Addressables.ResourceManager.CreateCompletedOperation(
                        component,
                        string.Empty
                    );
                }
            );
        }

        /* -----------------------------------------------------------
         * Editor validation
         * ----------------------------------------------------------- */

#if UNITY_EDITOR
        public override bool ValidateAsset(UnityEngine.Object obj)
        {
            if(!base.ValidateAsset(obj))
                return false;

            if(obj is GameObject go)
                return go.GetComponent<T>() != null;

            return false;
        }

        public override bool ValidateAsset(string path)
        {
            var go = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return go != null && go.GetComponent<T>() != null;
        }
#endif
    }
}
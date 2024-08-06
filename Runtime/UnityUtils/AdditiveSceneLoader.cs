using System;
using System.Collections.Generic;
using SeweralIdeas.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SeweralIdeas.UnityUtils
{
    public class AdditiveSceneLoader
    {
        private AsyncOperation _asyncOperation;
        private ToLoad?        _queued = null;

        private readonly Observable<Scene> _loadedScene = new();
        private                 int?         _globalHandle               = null;
        
        public Observable<Scene>.Readonly LoadedScene => _loadedScene.ReadOnly;
        private static readonly HashSet<int> OtherLoadersReservedHandles = new();
        
        [Serializable]
        public struct ToLoad
        {
            [field: SerializeField] public string ScenePath { get; set; }
            [field: SerializeField] public LocalPhysicsMode PhysicsMode { get; set; }
        }
        
        
        public void SetSceneToLoad(ToLoad toLoad)
        {
            // If already loading something, queue and return
            if (_asyncOperation != null)
            {
                _queued = toLoad;
                return;
            }

            // If some scene already loaded, unload
            var currentlyLoaded = _loadedScene.Value;
            if(currentlyLoaded.IsValid())
            {
                SceneManager.UnloadSceneAsync(currentlyLoaded);
            }
            
            // If the scene index is invalid, load "nothing"
            int myIndex = SceneUtility.GetBuildIndexByScenePath(toLoad.ScenePath);
            if (myIndex < 0)
            {
                OnMySceneLoaded(default);
                return;
            }
            
            // Gather all other instances of this scene that are already loaded, so we can ignore them later
            int loadedCount = SceneManager.loadedSceneCount;
            HashSet<int> alreadyLoadedHandles = new();
            for (int i = 0; i < loadedCount; ++i)
            {
                Scene someLoadedScene = SceneManager.GetSceneAt(i);
                if (someLoadedScene.buildIndex != myIndex)
                {
                    continue;
                }

                alreadyLoadedHandles.Add(someLoadedScene.handle);
            }
            
            // Begin loading
            _asyncOperation = SceneManager.LoadSceneAsync(toLoad.ScenePath, new LoadSceneParameters(LoadSceneMode.Additive, toLoad.PhysicsMode));
            if (_asyncOperation == null)
            {
                return;
            }
            
            _asyncOperation.completed += asyncOp => OnAsyncOperationCompleted(asyncOp, toLoad, alreadyLoadedHandles);
        }

        private void OnAsyncOperationCompleted(AsyncOperation obj, ToLoad toLoad, HashSet<int> alreadyLoadedHandles)
        {
            int myIndex = SceneUtility.GetBuildIndexByScenePath(toLoad.ScenePath);
            int loadedCount = SceneManager.loadedSceneCount;
            
            // Iterate over all loaded scenes.
            for (int i = 0; i < loadedCount; ++i)
            {
                // Skip scenes that are of different build Index
                Scene someLoadedScene = SceneManager.GetSceneAt(i);
                if (someLoadedScene.buildIndex != myIndex)
                {
                    continue;
                }

                // Skip scenes that were loaded before.
                if (alreadyLoadedHandles.Contains(someLoadedScene.handle))
                {
                    continue;
                }
                
                //Skip globally reserved scenes.
                if (OtherLoadersReservedHandles.Contains(someLoadedScene.handle))
                {
                    continue;
                }

                // someLoadedScene now hopefully contains our scene.
                // Did we queue up something else in the meantime? Then unload and repeat.
                var queued = _queued;
                var oldAsyncOp = _asyncOperation;
                _queued = null;
                _asyncOperation = null;

                if (oldAsyncOp != obj || queued.HasValue)
                {
                    if(queued.HasValue)
                    {
                        SetSceneToLoad(queued.Value);
                    }

                    return;
                }

                // Final callback, we're done!
                OnMySceneLoaded(someLoadedScene);
                return;
            }
        }

        private void OnMySceneLoaded(Scene loadedScene)
        {
            if (_globalHandle != null)
            {
                OtherLoadersReservedHandles.Remove(_globalHandle.Value);
            }
            
            _globalHandle = loadedScene.IsValid() ? loadedScene.handle : null;
            
            if (_globalHandle != null)
            {
                OtherLoadersReservedHandles.Add(_globalHandle.Value);
            }
            
            _loadedScene.Value = loadedScene;
        }

        public void Dispose()
        {
            SetSceneToLoad(default);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SeweralIdeas.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace SeweralIdeas.UnityUtils
{
    public class AdditiveSceneLoader
    {
        private          ToLoad?                           _queued               = null;
        private readonly Observable<ToLoad?>               _currentLoadProcess   = new();
        private readonly Observable<Scene>                 _loadedScene          = new();
        private readonly HashSet<int>                      _alreadyLoadedHandles = new();
        private readonly UnityAction<Scene, LoadSceneMode> _onSomeSceneLoadedAction;
        private          bool                              _subscribed = false;
        private          bool                              _disposed   = false;
        
        #if DEBUG
        private StackTrace _debug_constructionStackTrace;
        #endif
        
        public Observable<Scene>.Readonly LoadedScene => _loadedScene.ReadOnly;
        public Observable<ToLoad?>.Readonly CurrentLoadProcess => _currentLoadProcess.ReadOnly;
        
        private static readonly Dictionary<int, AdditiveSceneLoader> OtherLoadersReservedHandles = new();
        private static readonly HashSet<AdditiveSceneLoader>         SubscribedLoaders           = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            OtherLoadersReservedHandles.Clear();
            
            while (SubscribedLoaders.Count > 0)
            {
                var arr = SubscribedLoaders.ToArray();
                SubscribedLoaders.Clear();
                foreach (var loader in arr)
                {
                    loader.SetSubscribed(false);
                }
            }
        }

        public AdditiveSceneLoader()
        {
            _onSomeSceneLoadedAction = OnSomeSceneLoaded;
#if DEBUG
            _debug_constructionStackTrace = new StackTrace(1);
#endif
        }

        private void SetSubscribed(bool subscribe)
        {
            if (subscribe == _subscribed)
                return;
            if (subscribe)
            {
                SceneManager.sceneLoaded += _onSomeSceneLoadedAction;
                SubscribedLoaders.Add(this);
            }
            else
            {
                SubscribedLoaders.Remove(this);
                SceneManager.sceneLoaded -= _onSomeSceneLoadedAction; 
            }
            _subscribed = subscribe;
        }
        
        public void SetSceneToLoad(ToLoad toLoad)
        {
            CheckDisposed();
            
            // If already loading something, queue and return
            if (_currentLoadProcess.Value != null)
            {
                _queued = toLoad;
                return;
            }

            // If some scene already loaded, unload
            var currentlyLoaded = _loadedScene.Value;
            if(currentlyLoaded.IsValid())
                SceneManager.UnloadSceneAsync(currentlyLoaded);

            // If the scene index is invalid, load "nothing"
            int myIndex = SceneUtility.GetBuildIndexByScenePath(toLoad.ScenePath);
            if (myIndex < 0)
            {
                OnMySceneLoaded(default);
                return;
            }
            
            // Gather all other instances of this scene that are already loaded, so we can ignore them later
            int loadedCount = SceneManager.loadedSceneCount;
            _alreadyLoadedHandles.Clear();
            for (int i = 0; i < loadedCount; ++i)
            {
                Scene someLoadedScene = SceneManager.GetSceneAt(i);
                if (someLoadedScene.buildIndex != myIndex)
                    continue;

                _alreadyLoadedHandles.Add(someLoadedScene.handle);
            }
            
            // Begin loading
            _currentLoadProcess.Value = toLoad;
            SetSubscribed(true);
            SceneManager.LoadSceneAsync(toLoad.ScenePath, new LoadSceneParameters(LoadSceneMode.Additive, toLoad.PhysicsMode));
        }
        
        private void OnSomeSceneLoaded(Scene someLoadedScene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Additive)
                return;
            if (_currentLoadProcess.Value == null)
                return;

            ToLoad toLoad = _currentLoadProcess.Value.Value;
            
            int toLoadBuildIndex = SceneUtility.GetBuildIndexByScenePath(toLoad.ScenePath);
            
            // Skip scenes that are of different build Index
            if (someLoadedScene.buildIndex != toLoadBuildIndex)
                return;

            // Skip scenes that were loaded before.
            if (_alreadyLoadedHandles.Contains(someLoadedScene.handle))
                return;

            //Skip globally reserved scenes.
            if (OtherLoadersReservedHandles.ContainsKey(someLoadedScene.handle))
                return;

            // someLoadedScene now hopefully contains our scene.
            // Did we queue up something else in the meantime? Then unload and repeat.
            var queued = _queued;
            _queued = null;

            if(queued.HasValue)
                SetSceneToLoad(queued.Value);

            // Final callback, we're done!

            _currentLoadProcess.Value = null;
            OnMySceneLoaded(someLoadedScene);
        
        }

        public static bool IsSceneAssignedToAnyLoader(Scene scene) => OtherLoadersReservedHandles.ContainsKey(scene.handle);

        private void OnMySceneLoaded(Scene loadedScene)
        {
            if(loadedScene.handle != 0)
                OtherLoadersReservedHandles.Add(loadedScene.handle, this);
            _currentLoadProcess.Value = null;
            _loadedScene.Value = loadedScene;
            SetSubscribed(false);
        }

        public void AssignLoadedScene(Scene scene)
        {
            CheckDisposed();
            if (_currentLoadProcess.Value != null)
                throw new InvalidOperationException("Assigning a loaded scene during loading is not supported.");
            OnMySceneLoaded(scene);
        }
        
        public void Dispose()
        {
            CheckDisposed();
            SetSubscribed(false);
            SetSceneToLoad(default);
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        ~AdditiveSceneLoader()
        {
#if DEBUG
            UnityEngine.Debug.LogError($"{nameof(AdditiveSceneLoader)} not disposed of correctly. Creation at:\n{_debug_constructionStackTrace}");
#else
            UnityEngine.Debug.LogError($"{nameof(AdditiveSceneLoader)} not disposed of correctly.");
#endif
        }
        
        private void CheckDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException("AdditiveSceneLoader");
        }


        [Serializable]
        public struct ToLoad : IEquatable<ToLoad>
        {
            [field: SerializeField] public string ScenePath { get; set; }
            [field: SerializeField] public LocalPhysicsMode PhysicsMode { get; set; }
            
            #region Equality
            public bool Equals(ToLoad other) => ScenePath == other.ScenePath && PhysicsMode == other.PhysicsMode;
            public override bool Equals(object obj) => obj is ToLoad other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(ScenePath, (int)PhysicsMode);
            public static bool operator ==(ToLoad left, ToLoad right) => left.Equals(right);
            public static bool operator !=(ToLoad left, ToLoad right) => !left.Equals(right);
            #endregion
        }

    }
}

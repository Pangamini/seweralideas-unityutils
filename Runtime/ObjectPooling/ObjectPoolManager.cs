using System.Collections.Generic;
using SeweralIdeas.UnityUtils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SeweralIdeas.ObjectPooling
{
    /// <summary>Finds or creates the pool of a prefab, one per scene.</summary>
    public class ObjectPoolManager : SceneSingleton<ObjectPoolManager>
    {
        private readonly Dictionary<Component, ObjectPool> _prefabToPool = new();
        private readonly List<IDelayedRelease>             _delayedReleases = new();
        private          Transform                         _stage;

        /// <summary>
        /// An active parent in this scene, for the pools to wake a new instance up under for a moment (see
        /// ObjectPool.EnsureAwake): their own objects are inactive. One for all the pools, made when first needed.
        /// </summary>
        public Transform Stage
        {
            get
            {
                if(_stage == null)
                {
                    var go = new GameObject("Stage");
                    go.transform.SetParent(transform);
                    _stage = go.transform;
                }
                return _stage;
            }
        }

        /// <summary>
        /// Releases the item at the end of the frame (in LateUpdate), as destroying an object does: whatever else is
        /// still working with it this frame finds it intact, instead of having it deactivated under its hands.
        /// </summary>
        public void ReleaseAtEndOfFrame(IDelayedRelease item) => _delayedReleases.Add(item);

        protected void LateUpdate()
        {
            if(_delayedReleases.Count == 0)
                return;

            // An index loop: releasing something can queue more, which then go in this same round.
            for( int i = 0; i < _delayedReleases.Count; ++i )
                _delayedReleases[i].ReleaseNow();
            _delayedReleases.Clear();
        }

        protected override void OnAwake()
        {
            base.OnAwake();

            // Pools placed in the scene under the manager
            ObjectPool[] children = GetComponentsInChildren<ObjectPool>(true);
            foreach (var child in children)
            {
                if(child.Prefab != null)
                    _prefabToPool.TryAdd(child.Prefab, child);
            }
        }

        /// <summary>The scene's manager. Creates one if the scene doesn't have one yet.</summary>
        public static ObjectPoolManager GetOrCreate(Scene scene)
        {
            ObjectPoolManager instance = GetInstance(scene);
            if(instance)
                return instance;

            // A new GameObject starts out in the active scene, which may not be the one asked for.
            var go = new GameObject(nameof(ObjectPoolManager));
            SceneManager.MoveGameObjectToScene(go, scene);
            return go.AddComponent<ObjectPoolManager>(); // registers itself in Awake
        }

        public ObjectPool GetPool(Component prefab)
        {
            if(_prefabToPool.TryGetValue(prefab, out var pool))
            {
                if(pool == null)    // if pool was destroyed for some reason
                {
                    _prefabToPool.Remove(prefab);
                }
                else
                {
                    return pool;
                }
            }

            var go = new GameObject($"Pool({prefab})");
            go.transform.SetParent(transform);
            pool = go.AddComponent<ObjectPool>();   // its Awake runs now, before it has a prefab, and does nothing
            pool.Prefab = prefab;
            pool.Initialize();

            _prefabToPool.Add(prefab, pool);

            return pool;
        }
    }
}

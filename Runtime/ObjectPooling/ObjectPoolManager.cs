using System.Collections.Generic;
using SeweralIdeas.UnityUtils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SeweralIdeas.ObjectPooling
{
    /// <summary>Finds or creates the pool of a prefab, one per scene.</summary>
    public class ObjectPoolManager : SimpleSceneSingleton<ObjectPoolManager>
    {
        private readonly Dictionary<Component, ComponentPool> _prefabToPool = new();
        private readonly List<IDelayedRelease>             _delayedReleases = new();
        private          Transform                         _stage;

        /// <summary>
        /// An active parent in this scene, for the pools to wake a new instance up under for a moment (see
        /// ObjectPool{T}.EnsureAwake): their own objects are inactive. One for all the pools, made when first needed.
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
            ComponentPool[] children = GetComponentsInChildren<ComponentPool>(true);
            foreach (var child in children)
            {
                if(child.Prefab != null)
                    _prefabToPool.TryAdd(child.Prefab, child);
            }
        }

        /// <summary>The pool of a prefab, handing out its instances as the type the prefab is known by here.</summary>
        public Pool<T> GetPool<T>(T prefab) where T : Component => new(GetComponentPool(prefab));

        private ComponentPool GetComponentPool(Component prefab)
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
            pool = go.AddComponent<ComponentPool>();   // its Awake runs now, before it has a prefab, and does nothing
            pool.Prefab = prefab;
            pool.Initialize();

            _prefabToPool.Add(prefab, pool);

            return pool;
        }
    }
}

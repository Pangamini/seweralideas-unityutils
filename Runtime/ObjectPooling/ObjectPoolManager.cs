using System.Collections.Generic;
using SeweralIdeas.UnityUtils;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

namespace SeweralIdeas.ObjectPooling
{
    /// <summary>Finds or creates the pool of a prefab, one per scene.</summary>
    [DefaultExecutionOrder(-1000)] // Its release at the start of a step comes before everything else of the step
    public class ObjectPoolManager : SimpleSceneSingleton<ObjectPoolManager>
    {
        private readonly Dictionary<Component, ComponentPool> _prefabToPool = new();
        private readonly List<Spawnable>                   _frameReleases = new(); // ReleaseTiming.EndOfFrame
        private readonly List<Spawnable>                   _stepReleases  = new(); // ReleaseTiming.NextStep
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
        /// Releases the despawned object later, as its <see cref="Spawnable.DespawnTiming"/> says: at the end of the frame (in LateUpdate), as
        /// destroying an object does, or at the start of the next physics step, before anything else of it runs (the manager
        /// comes first in the execution order). Either way, whatever else is still working with the item meanwhile finds it
        /// intact, instead of having it deactivated under its hands. The step is for what takes part in the simulation: a frame
        /// may hold no step or several, and a release at the end of it would leave the item in the world for one more step or
        /// not, depending on when the frames fell.
        /// </summary>
        internal void ReleaseDelayed(Spawnable item)
        {
            if(item.DespawnTiming == ReleaseTiming.NextStep)
                _stepReleases.Add(item);
            else
                _frameReleases.Add(item);
        }

        protected void FixedUpdate()
        {
            ReleaseAll(_stepReleases);
        }

        protected void LateUpdate()
        {
            DestroyPoolsOfDestroyedPrefabs();

            ReleaseAll(_frameReleases);

            // No step is coming while time is stopped, and what waits for one would wait for the time to go on
            if(Time.timeScale <= 0f)
                ReleaseAll(_stepReleases);
        }

        private static void ReleaseAll(List<Spawnable> items)
        {
            if(items.Count == 0)
                return;

            // An index loop: releasing something can queue more, which then go in this same round.
            // One that throws is logged and doesn't keep the rest from being released, nor the list from being cleared:
            // it would be released again every frame.
            for( int i = 0; i < items.Count; ++i )
            {
                try { items[i].DespawnNow(); }
                catch (System.Exception e) { Debug.LogException(e); }
            }
            items.Clear();
        }

        // A pool whose prefab was destroyed (an object of the scene) has nothing to make instances from. The pool watches
        // for it itself (see ObjectPool<T>.Initialize), but that needs the prefab to tell, which an object that was never
        // active (OnDestroy isn't called on it) may not; looking at the prefabs is sure to notice.
        private void DestroyPoolsOfDestroyedPrefabs()
        {
            foreach (var entry in _prefabToPool)
            {
                if(entry.Key == null && entry.Value != null)
                    Destroy(entry.Value.gameObject);
            }
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
            // A destroyed prefab can't make instances, and its pool is gone (or going) with it. Making a new pool for it
            // would only fail later, as Pool(null).
            if(prefab == null)
                throw new MissingReferenceException("Cannot get a pool of a prefab that is null or has been destroyed.");

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

            // The pools of destroyed prefabs are gone with them (see ObjectPool<T>.Initialize); their entries are not
            using (ListPool<Component>.Get(out var gone))
            {
                foreach (var entry in _prefabToPool)
                {
                    if(entry.Value == null)
                        gone.Add(entry.Key);
                }

                foreach (Component key in gone)
                    _prefabToPool.Remove(key);
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

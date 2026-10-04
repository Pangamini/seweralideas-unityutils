using System.Collections.Generic;
using SeweralIdeas.UnityUtils;
using SeweralIdeas.UnityUtils.Drawers;
using UnityEngine;
using UnityEngine.Pool;

namespace SeweralIdeas.ObjectPooling
{
    /// <summary>
    /// A pool of instances of one prefab. The pool is itself an inactive GameObject and its instances live under it,
    /// so a new instance stays dormant (no Awake, no OnEnable) until it is activated.
    ///
    /// The lifecycle is Unity's own, nothing is added to it: an instance gets its Awake once, the first time it is
    /// activated; OnEnable every time it is activated; Start once, before its first Update. Returning it to the pool
    /// deactivates it (OnDisable). For a <see cref="Spawnable"/>, OnSpawn is "enabled and started": on OnEnable for a
    /// reused instance, on Start for a new one.
    /// </summary>
    public class ObjectPool : MonoBehaviour
    {
        [SerializeField, ComponentPicker] private Component _prefab;

        // Instances that would go back into a full pool are destroyed instead. 0 means no limit.
        [SerializeField, Min(0)] private int _maxSize = 256;

        private readonly Stack<Component> _stack = new();
        private bool _initialized;

        public Component Prefab
        {
            get => _prefab;
            internal set => _prefab = value;
        }

        public string InstanceName { get; private set; }

        protected void Awake()
        {
            // A pool made from code gets its prefab after Awake (see ObjectPoolManager), and initializes itself then.
            if(_prefab != null)
                Initialize();
        }

        protected void OnEnable()
        {
            gameObject.SetActive(false);
        }

        internal void Initialize()
        {
            if(_initialized)
                return;
            _initialized = true;

            InstanceName = _prefab.name + "(Pooled)";
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Makes sure at least <paramref name="count"/> instances are waiting in the pool, and that every one of them
        /// has already had its Awake - so that taking one costs no more than reusing one.
        /// </summary>
        public void Prewarm(int count)
        {
            Initialize();

            if(_maxSize > 0 && count > _maxSize)
            {
                Debug.LogWarning($"{name}: prewarming {count} instances, but the pool holds at most {_maxSize}.", this);
                count = _maxSize;
            }

            PruneDestroyed();
            while(_stack.Count < count)
            {
                Component instance = CreateInstance();
                EnsureAwake(instance);
                instance.transform.SetParent(transform);

                PoolReference reference = SetUpPoolReference(instance);
                reference.InPool = true;
                _stack.Push(instance);
            }
        }

        public void Return(Component obj)
        {
            PoolReference reference = obj.gameObject.GetOrAddComponent<PoolReference>();
            if(reference.InPool)
            {
                Debug.LogError($"{obj.name} is already in the pool.", obj);
                return;
            }

            obj.gameObject.SetActive(false); // despawns it: the Spawnables on it get their OnDespawn

            if(_maxSize > 0 && _stack.Count >= _maxSize)
                PruneDestroyed();

            if(_maxSize > 0 && _stack.Count >= _maxSize)
            {
                Destroy(obj.gameObject);
                return;
            }

            reference.InPool = true;
            obj.transform.SetParent(transform);
            _stack.Push(obj);
        }

        public Component Take(bool active = true, Transform parent = null)
        {
            Component instance;

            // get or instantiate
            while(true)
            {
                if(_stack.TryPop(out instance))
                {
                    if(instance == null)
                        continue;
                }
                else
                {
                    instance = CreateInstance();  // instantiate still parented to the Pool to make it Inactive
                }
                break;
            }

            // at this point, instance is parented to the pool. Make it inactive by itself
            instance.gameObject.SetActive(false);

            // Whoever takes it inactive wants to set it up before it is activated, so it has to have had its Awake
            if(!active)
                EnsureAwake(instance);

            // when inactive self, we can set parent
            instance.transform.SetParent(parent);

            // set up the pool reference
            SetUpPoolReference(instance);

            if(active)
                instance.gameObject.SetActive(true);

            return instance;
        }

        /// <summary>
        /// <see cref="Take(bool, Transform)"/>, typed. Pass active: false to get the instance deactivated, to put it
        /// in place before activating it yourself. It has had its Awake by then, but not its Start.
        /// </summary>
        public T Take<T>(bool active = true, Transform parent = null) where T : Component => (T)Take(active, parent);

        // Drops the instances that were destroyed while waiting in the pool, keeping the order of the rest.
        private void PruneDestroyed()
        {
            using (ListPool<Component> .Get(out var live))
            {
                foreach (Component instance in _stack) // from the top down
                {
                    if(instance != null)
                        live.Add(instance);
                }

                _stack.Clear();
                for( int i = live.Count - 1; i >= 0; --i )
                    _stack.Push(live[i]);
            }
        }

        private PoolReference SetUpPoolReference(Component instance)
        {
            PoolReference reference = instance.gameObject.GetOrAddComponent<PoolReference>();
            reference.OnTakenFromPool(this, instance);
            return reference;
        }

        // Created inactive by itself, under the (inactive) pool, so nothing runs on it yet.
        private Component CreateInstance()
        {
            var instance =  Instantiate(Prefab, transform);
            instance.gameObject.SetActive(false);
            instance.name = InstanceName;
            return instance;
        }

        // An instance under the pool can't wake up, the pool being inactive. Activated for a moment under an active
        // parent in the same scene it does, and gets its Awake and OnEnable, and OnDisable when deactivated again - as it
        // would have, instantiated as a root object. It is left inactive and under the stage, for the caller to move.
        private void EnsureAwake(Component instance)
        {
            if(instance is MonoBehaviour behaviour && behaviour.didAwake)
                return;

            instance.transform.SetParent(ObjectPoolManager.GetOrCreate(gameObject.scene).Stage);
            instance.gameObject.SetActive(true);
            instance.gameObject.SetActive(false);
        }
    }
}

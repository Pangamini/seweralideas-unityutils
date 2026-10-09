using System.Collections.Generic;
using System.Threading;
using SeweralIdeas.UnityUtils;
using SeweralIdeas.UnityUtils.Drawers;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace SeweralIdeas.ObjectPooling
{
    /// <summary>
    /// A pool of instances of one prefab. The pool is itself an inactive GameObject and its instances live under it,
    /// so a new instance stays dormant (no Awake, no OnEnable) until it is activated.
    ///
    /// Every instance is a <see cref="Spawnable"/> (one is added to a prefab that doesn't have it): the pool hands it out
    /// spawned, or, with <c>Take(active: false)</c>, to be set up by the caller, who then calls <see cref="Spawnable.Spawn"/>.
    /// An instance goes back to the pool when it is despawned (<see cref="Spawnable.Despawn"/>), which deactivates it.
    /// A new instance has had its Awake before it is handed out, and has not had its Start.
    ///
    /// Unity can't add a generic component, so a pool is a component through a concrete subclass:
    /// <see cref="ComponentPool"/>, or <c>class BulletPool : ObjectPool&lt;Bullet&gt; { }</c> for a pool that hands out its
    /// own type. Instances come from <see cref="CreateInstance"/>.
    /// </summary>
    public abstract class ObjectPool<T> : ObjectPool where T : Component
    {
        [SerializeField, ComponentPicker] private T _prefab;

        // Instances that would go back into a full pool are destroyed instead. 0 means no limit.
        [SerializeField, Min(0)] private int _maxSize = 256;

        private readonly Stack<T> _stack = new();
        private bool _initialized;
        private CancellationTokenRegistration _prefabDestroyed; // if the prefab is an object of a scene and can be destroyed
        private GameObject _listenedObject;                      // ... and has no script to ask for a token

        public T Prefab
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

            // A prefab that is an object of a scene can be destroyed (an asset can't, short of being unloaded). The pool has
            // nothing to make instances from then, so it goes too, with the instances waiting in it. The ones handed out
            // carry on, and are destroyed instead of returned when they despawn.
            // Only a script has a destroyCancellationToken: the prefab itself, or another one on its object. An object
            // with none (a pooled Rigidbody, say) gets a listener component instead.
            if(_prefab.gameObject.scene.IsValid())
            {
                var watched = _prefab as MonoBehaviour;
                if(watched == null)
                    _prefab.TryGetComponent(out watched);

                if(watched != null)
                {
                    _prefabDestroyed = watched.destroyCancellationToken.Register(s_onPrefabDestroyed, this);
                }
                else
                {
                    _listenedObject = _prefab.gameObject;
                    _listenedObject.SubscribeToDestroy(s_onPrefabDestroyed, this);
                }
            }
        }

        protected void OnDestroy()
        {
            _prefabDestroyed.Dispose();
            if(_listenedObject != null)
                _listenedObject.UnsubscribeFromDestroy(s_onPrefabDestroyed, this);
        }

        private static readonly System.Action<object> s_onPrefabDestroyed = pool =>
        {
            var objectPool = (ObjectPool<T>)pool;
            if(objectPool != null)
                Destroy(objectPool.gameObject);
        };

        /// <summary>
        /// Makes a new instance for the pool to hand out. It has to come back inactive and under the pool, so that
        /// nothing runs on it yet. The default instantiates <see cref="Prefab"/>.
        /// </summary>
        protected virtual T CreateInstance()
        {
            var instance = Instantiate(_prefab, transform);
            instance.gameObject.SetActive(false);
            instance.name = InstanceName;
            return instance;
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
                T instance = CreateInstance();
                Spawnable root = SetUpSpawnable(instance);
                EnsureAwake(root);
                instance.transform.SetParent(transform);

                root.InPool = true;
                _stack.Push(instance);
            }
        }

        // Takes in a spawnable whose life is over, deactivated: back into the stack, or destroyed if the pool is full.
        internal override void ReturnInstance(Spawnable root)
        {
            if(root.InPool)
            {
                Debug.LogError($"{root.name} is already in the pool.", root);
                return;
            }

            root.gameObject.SetActive(false);

            if(_maxSize > 0 && _stack.Count >= _maxSize)
                PruneDestroyed();

            if(_maxSize > 0 && _stack.Count >= _maxSize)
            {
                Destroy(root.gameObject);
                return;
            }

            root.InPool = true;
            root.transform.SetParent(transform);
            _stack.Push((T)root.PoolHandle);
        }

        /// <summary>
        /// Takes an instance out of the pool, or makes a new one, and spawns it. Pass active: false to get it
        /// un-spawned and deactivated, to put it in place before calling <see cref="Spawnable.Spawn"/> on it yourself.
        /// It has had its Awake by then, but not its Start.
        /// </summary>
        public T Take(bool active = true, Transform parent = null) => TakeInstance(active, parent, null);

        /// <summary>
        /// <see cref="Take(bool, Transform)"/>, placed at a world <paramref name="position"/> and <paramref name="rotation"/>
        /// before it is spawned, so that its OnEnable and OnSpawn already see it there.
        /// </summary>
        public T Take(Vector3 position, Quaternion rotation, bool active = true, Transform parent = null) =>
            TakeInstance(active, parent, new Pose(position, rotation));

        private T TakeInstance(bool active, Transform parent, Pose? pose)
        {
            T instance;

            // get or instantiate
            while(true)
            {
                if(_stack.TryPop(out instance))
                {
                    if((Object)instance == null)
                        continue;
                }
                else
                {
                    instance = CreateInstance();  // instantiated still parented to the Pool to make it Inactive
                }
                break;
            }

            // at this point, instance is parented to the pool. Make it inactive by itself
            instance.gameObject.SetActive(false);

            Spawnable root = SetUpSpawnable(instance);

            // Whoever takes it un-spawned wants to set it up first, so it has to have had its Awake
            if(!active)
                EnsureAwake(root);

            // when inactive self, we can set parent
            instance.transform.SetParent(parent);

            // world space, so after the parent is set
            if(pose is { } placement)
                instance.transform.SetPositionAndRotation(placement.position, placement.rotation);

            if(active)
                root.Spawn();

            return instance;
        }

        // Drops the instances that were destroyed while waiting in the pool, keeping the order of the rest.
        private void PruneDestroyed()
        {
            using (ListPool<T>.Get(out var live))
            {
                foreach (T instance in _stack) // from the top down
                {
                    if((Object)instance != null)
                        live.Add(instance);
                }

                _stack.Clear();
                for( int i = live.Count - 1; i >= 0; --i )
                    _stack.Push(live[i]);
            }
        }

        // The instance's root, which is the pool's way back to it. A prefab that has none gets a plain one, before any
        // Awake can run on it, so that its parts find it.
        private Spawnable SetUpSpawnable(T instance)
        {
            Spawnable root = instance.gameObject.GetOrAddComponent<Spawnable>();
            root.OnTakenFromPool(this, instance);
            return root;
        }

        // An instance under the pool can't wake up, the pool being inactive. Activated for a moment under an active
        // parent in the same scene it does, and gets its Awake and OnEnable, and OnDisable when deactivated again - as it
        // would have, instantiated as a root object. It is left inactive and under the stage, for the caller to move.
        private void EnsureAwake(Spawnable root)
        {
            if(root.IsAwake)
                return;

            root.transform.SetParent(ObjectPoolManager.GetInstance(gameObject.scene).Stage);
            root.gameObject.SetActive(true);
            root.gameObject.SetActive(false);
        }
    }
}

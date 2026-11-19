#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SeweralIdeas.UnityUtils;
using UnityEngine;

namespace SeweralIdeas.ObjectPooling
{
    /// <summary>
    /// The root of an object with a life: it is spawned, it lives, it is despawned - and that is the same whether it is
    /// pooled or not. There is one <see cref="Spawn"/> and one <see cref="Despawn"/>; pooling only decides where the object
    /// goes when its life is over: back to its pool, or, if it has none, destroyed.
    ///
    ///  - <see cref="Spawn"/> begins the life: it activates the object (Unity gives it Awake and OnEnable) and runs
    ///    <see cref="OnSpawn"/>, on the root first and then on its <see cref="SpawnablePart"/>s. A pooled instance is handed
    ///    out already spawned, or, with <c>Take(active: false)</c>, to be set up and spawned by the caller. An object that
    ///    nobody spawned and that isn't pooled spawns itself on its first Start (placed in a scene, or just instantiated).
    ///  - <see cref="Despawn"/> ends it, at the end of the frame (or at the start of the next physics step, see <see cref="DespawnTiming"/>), as <see cref="UnityEngine.Object.Destroy(UnityEngine.Object)"/>
    ///    does at the end of the frame, so whoever is still working with the object meanwhile finds it intact. <see cref="OnDespawn"/> runs on the
    ///    parts (last first) and then on the root. Destroying a spawned object despawns it too.
    ///
    /// Spawning does not depend on the object being active: a spawned object can be deactivated, hidden under an inactive
    /// parent, or have its components disabled, and its life goes on. Unity's own OnEnable and OnDisable are free for
    /// whatever should follow the object being active.
    ///
    /// What belongs to one life is set up in <see cref="OnSpawn"/> and cleared in <see cref="OnDespawn"/>. For a new
    /// instance, OnSpawn comes after Awake and OnEnable, but before Start.
    /// </summary>
    [DisallowMultipleComponent]
    [SelectionBase]
    public class Spawnable : MonoBehaviour
    {
        private readonly List<SpawnablePart> _parts = new();

        private bool _spawnRequested; // Spawn() was called: the life has begun
        private bool _spawned;         // ... and OnSpawn has been run, which waits for the Awake
        private bool _despawnQueued;
        private bool _activatingToSpawn;

        // the sink, set by the pool that hands the object out
        private ObjectPool? _pool;
        internal Component? PoolHandle { get; private set; }
        private bool _inPool;
        private bool _lifeOver; // RunDespawn has begun: the life is over until the object is taken out of its pool again

        internal bool InPool
        {
            get => _inPool;
            set
            {
                _inPool = value;
                if(!value)
                    _lifeOver = false; // taken out: the coming life
            }
        }

        private CancellationTokenSource? _spawnCts;
        private int                      _spawnGeneration;

        public int SpawnGeneration => _spawnGeneration;

        /// <summary>Whether the object is in its life: spawned, and not despawned yet (it may be waiting for the end of the frame, or for the next physics step, to be).</summary>
        public bool Spawned => _spawned;

        /// <summary>Whether <see cref="Despawn"/> has been called and the moment when it takes effect (see <see cref="DespawnTiming"/>) is yet to come.</summary>
        public bool IsDespawning => _despawnQueued;

        /// <summary>Whether the object goes back to a pool when it is despawned (and not to the bin).</summary>
        public bool IsPooled => _pool != null;

        /// <summary>Whether the object is waiting in its pool right now.</summary>
        public bool IsInPool => InPool;

        /// <summary>
        /// Raised when the life ends, after the despawn of the parts and the root. Each subscription is for one life:
        /// the handlers are dropped after they are called.
        /// </summary>
        public event Action<Spawnable>? Despawned;

        /// <summary>
        /// A token cancelled when the life ends (and when the object is destroyed), for async work that must not outlive
        /// it. An object that is not pooled ends its life by being destroyed, so for it this is its destroyCancellationToken.
        /// For a pooled one it is a new token for every life, made when first asked for, and disposed when the life ends.
        /// Asked for after Awake but before the object is spawned, it is for the coming life, as <see cref="AwaitDespawnAsync"/>
        /// is. Once the life is over - the despawn has begun, the object is in its pool, or destroyed - it is a token that
        /// is already cancelled, so it works the same whether the object is pooled or not.
        /// A pooled object that has not woken up yet throws: Unity doesn't call OnDestroy on such an object, so nothing
        /// could cancel the token if it were destroyed.
        /// </summary>
        public CancellationToken SpawnCancellationToken
        {
            get
            {
                if(!this)
                    return new CancellationToken(true);
                if(_pool == null)
                    return destroyCancellationToken;
                if(InPool || _lifeOver)
                    return new CancellationToken(true);
                if(!didAwake)
                    throw new InvalidOperationException($"{name}: SpawnCancellationToken requested before Awake.");
                _spawnCts ??= new CancellationTokenSource();
                return _spawnCts.Token;
            }
        }

        #region Unity messages
        protected void Awake()
        {
            OnAwake();

            // Spawn() may have come before the object could wake up (an inactive parent). If it is Spawn() that is
            // waking it, it runs OnSpawn itself, once the other components have woken up too.
            if(!_activatingToSpawn)
                TryRunSpawn();
        }

        protected void Start()
        {
            OnStart();

            // Nobody spawned it. A pooled instance is never spawned like this: it is the pool's caller who spawns it.
            if(!_spawnRequested && _pool == null)
                Spawn();
        }

        protected void OnDestroy()
        {
            if(_spawned)
                RunDespawn();
            else
            {
                IncrementGeneration();
                CancelSpawnToken(); // a token asked for after Awake but before the spawn, for a life that never came
            }
            OnDestroyed();
        }
        #endregion

        #region Hooks
        protected virtual void OnAwake() { }
        protected virtual void OnStart() { }

        /// <summary>The life begins. Runs on the root before the parts.</summary>
        protected virtual void OnSpawn() { }

        /// <summary>The life is over. Runs on the root after the parts.</summary>
        protected virtual void OnDespawn() { }
        protected virtual void OnDestroyed() { }
        #endregion

        /// <summary>
        /// Begins the life of the object: activates it, and spawns it and its parts. Does nothing if it has been spawned.
        /// </summary>
        public void Spawn()
        {
            if(_spawnRequested)
                return;

            if(InPool)
            {
                Debug.LogError($"{name} is in its pool: it is taken out of it by the pool.", this);
                return;
            }

            IncrementGeneration();
            _spawnRequested = true;

            // Awake and OnEnable of every component happen inside SetActive; OnSpawn waits until they are all done.
            _activatingToSpawn = true;
            try
            {
                gameObject.SetActive(true);
            }
            finally
            {
                _activatingToSpawn = false;
            }

            TryRunSpawn();
        }

        /// <summary>
        /// Ends the life of the object at the end of the frame, or at the start of the next physics step (see <see cref="DespawnTiming"/>): it is despawned, and then returned to its pool, or
        /// destroyed if it has none. Calling it again before then does nothing.
        /// </summary>
        public void Despawn()
        {
            if(_despawnQueued || InPool)
                return;

            _despawnQueued = true;
            ObjectPoolManager.GetInstance(gameObject.scene).ReleaseDelayed(this);
        }

        /// <summary>
        /// Completes when the current (or, if the object hasn't been spawned yet, the coming) life ends, or when the object is
        /// destroyed. Unlike awaiting the destruction of the object, it also works for a pooled one, which is not destroyed
        /// but returned. Completes at once if the object is in its pool; throws OperationCanceledException if <paramref name="ct"/> is cancelled first.
        /// </summary>
        public async ValueTask AwaitDespawnAsync(CancellationToken ct = default)
        {
            if(!this || InPool)
                return;

            AwaitableCompletionSource ended = new();
            Action<Spawnable> onDespawned = _ => ended.TrySetResult();
            Despawned += onDespawned;
            try
            {
                await using (ct.Register(() => ended.TrySetCanceled()))
                await using (destroyCancellationToken.Register(() => ended.TrySetResult()))
                {
                    await ended.Awaitable;
                }
            }
            finally
            {
                Despawned -= onDespawned;
            }
        }

        /// <summary>
        /// The end of the target's life as a token: the <see cref="SpawnCancellationToken"/> of its Spawnable, or, if it has
        /// none, its destroyCancellationToken. Already cancelled for a destroyed target. For a Spawnable that has not been
        /// spawned yet it is the end of the coming life.
        /// </summary>
        public static CancellationToken GetLifeToken(MonoBehaviour target)
        {
            if(!target) // TryGetComponent would throw
                return new CancellationToken(true);

            return target.TryGetComponent(out Spawnable spawnable) ? spawnable.SpawnCancellationToken : target.destroyCancellationToken;
        }

        /// <summary>
        /// Completes when the life of the target ends: <see cref="AwaitDespawnAsync"/> if it has a Spawnable (which also
        /// waits for a coming life), otherwise when it is destroyed. Completes at once for a destroyed target; throws
        /// OperationCanceledException if <paramref name="ct"/> is cancelled first.
        /// </summary>
        public async static ValueTask AwaitLifeEndAsync(MonoBehaviour target, CancellationToken ct = default)
        {
            if(!target)
                return;

            if(target.TryGetComponent(out Spawnable spawnable))
                await spawnable.AwaitDespawnAsync(ct);
            else
                await target.AwaitDestructionAsync(ct);
        }

        /// <summary>
        /// When <see cref="Despawn"/> takes effect: at the end of the frame, as destroying an object does (the default, for what is
        /// only shown), or at the start of the next physics step, for what takes part in the simulation. A frame may hold no step or
        /// several, so an object that was despawned in a step and released at the end of the frame stays in the world for one more
        /// step or not, depending on when the frames fell. A class that is part of the simulation overrides this with
        /// <see cref="ReleaseTiming.NextStep"/>; it is read when <see cref="Despawn"/> is called.
        /// </summary>
        protected internal virtual ReleaseTiming DespawnTiming => ReleaseTiming.EndOfFrame;


        // Ends the life on the spot. For when Despawn takes effect (see DespawnTiming); the manager calls it.
        internal void DespawnNow()
        {
            _despawnQueued = false; // whatever happens next, this request is over

            if(!this) // destroyed in the meantime: OnDestroy has despawned it, and there is nothing to return or destroy
                return;

            if(InPool) // not if it was despawned some other way in the meantime
                return;

            if(_spawned)
                RunDespawn();
            _spawnRequested = false;

            if(_pool != null)
                _pool.ReturnInstance(this);
            else
                Destroy(gameObject);
        }

        #region Parts
        internal void RegisterPart(SpawnablePart part)
        {
            _parts.Add(part);
            if(_spawned) // a part that wakes up late joins a life that has begun
                part.RunSpawn();
        }

        internal void UnregisterPart(SpawnablePart part) => _parts.Remove(part);
        #endregion

        internal void OnTakenFromPool(ObjectPool pool, Component handle)
        {
            _pool = pool;
            PoolHandle = handle;
            InPool = false;
        }

        // Runs OnSpawn once the life has begun and the object has woken up, if it hasn't been run yet.
        private void TryRunSpawn()
        {
            if(!_spawnRequested || _spawned || !didAwake)
                return;

            _spawned = true;

            try { OnSpawn(); }
            catch (Exception e) { Debug.LogException(e, this); }

            for (int i = 0; i < _parts.Count; ++i)
            {
                if(_parts[i] != null)
                    _parts[i].RunSpawn();
            }
        }

        private void RunDespawn()
        {
            _spawned = false;
            _lifeOver = true; // a token asked for from here on (in OnDespawn, say) is for a life that is over
            IncrementGeneration();
            CancelSpawnToken();

            // Parts last to first, then the root. One that throws doesn't keep the others from being despawned.
            for (int i = _parts.Count - 1; i >= 0; --i)
            {
                if(i < _parts.Count && _parts[i] != null)
                    _parts[i].RunDespawn();
            }

            try { OnDespawn(); }
            catch (Exception e) { Debug.LogException(e, this); }

            InvokeDespawned();
        }
        private void IncrementGeneration()
        {

            unchecked
            {
                _spawnGeneration++;
            }
        }

        private void CancelSpawnToken()
        {
            CancellationTokenSource? cts = _spawnCts;
            _spawnCts = null;
            if(cts == null)
                return;

            // Callbacks on the token run inside Cancel, and if one throws, Cancel does too. That must not skip the
            // despawn (it resets what belongs to the life), so it is logged instead.
            try
            {
                cts.Cancel();
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
            }
            finally
            {
                // Tokens handed out before stay cancelled.
                cts.Dispose();
            }
        }

        private void InvokeDespawned()
        {
            while(Despawned != null)
            {
                Action<Spawnable> despawned = Despawned;
                Despawned = null;
                try { despawned.Invoke(this); }
                catch (Exception e) { Debug.LogException(e, this); }
            }
        }

        #region Editor
        #if UNITY_EDITOR
        private const string DespawnContextName = "Despawn";

        [ContextMenu(DespawnContextName)]
        private void Context_Despawn()
        {
            if(!Context_DespawnValidate())
                return;
            Despawn();
        }

        [ContextMenu(DespawnContextName, true)]
        private bool Context_DespawnValidate()
        {
            if(!Application.isPlaying)
                return false;
            if(!gameObject.scene.IsValid())
                return false;
            if(InPool || !_spawned) // not in its life
                return false;
            return true;
        }
        #endif
        #endregion
    }
}

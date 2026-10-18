#nullable enable
using System;
using UnityEngine;

namespace SeweralIdeas.ObjectPooling
{
    /// <summary>
    /// A part of a <see cref="Spawnable"/> object: it is spawned and despawned by its root, the nearest
    /// <see cref="Spawnable"/> at or above it, and not by being enabled or disabled. The root spawns first and despawns
    /// last. A part that wakes up after its root has been spawned is spawned right after its Awake.
    ///
    /// A part without a <see cref="Spawnable"/> above it is its own root: it is spawned on its first Start, as a
    /// Spawnable that nobody spawned is, and despawned when it is destroyed.
    /// </summary>
    public abstract class SpawnablePart : MonoBehaviour
    {
        private Spawnable? _root;
        private bool       _spawned;

        /// <summary>The nearest <see cref="Spawnable"/> at or above the part, or null if there is none and the part spawns itself.</summary>
        public Spawnable? Root => _root;

        /// <summary>Whether the part is spawned, i.e. its root is and it has been told so.</summary>
        public bool Spawned => _spawned;

        protected void Awake()
        {
            _root = GetComponentInParent<Spawnable>(true);

            OnAwake();
            _root?.RegisterPart(this);
        }

        protected void Start()
        {
            // No root to spawn it: the first Start begins its life.
            if(_root == null)
                RunSpawn();
        }

        protected void OnDestroy()
        {
            if(_root != null)
                _root.UnregisterPart(this);
            RunDespawn(); // destroyed in the middle of its life
            OnDestroyed();
        }

        protected virtual void OnAwake() { }
        protected virtual void OnSpawn() { }
        protected virtual void OnDespawn() { }
        protected virtual void OnDestroyed() { }

        internal void RunSpawn()
        {
            if(_spawned)
                return;
            _spawned = true;

            try { OnSpawn(); }
            catch (Exception e) { Debug.LogException(e, this); }
        }

        internal void RunDespawn()
        {
            if(!_spawned)
                return;
            _spawned = false;

            try { OnDespawn(); }
            catch (Exception e) { Debug.LogException(e, this); }
        }
    }
}

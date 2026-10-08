#nullable enable
using System;
using UnityEngine;

namespace SeweralIdeas.ObjectPooling
{
    /// <summary>
    /// A part of a <see cref="Spawnable"/> object: it is spawned and despawned by its root, the nearest
    /// <see cref="Spawnable"/> at or above it, and not by being enabled or disabled. The root spawns first and despawns
    /// last. A part that wakes up after its root has been spawned is spawned right after its Awake.
    /// </summary>
    public abstract class SpawnablePart : MonoBehaviour
    {
        private Spawnable? _root;
        private bool       _spawned;

        public Spawnable Root => _root!;

        /// <summary>Whether the part is spawned, i.e. its root is and it has been told so.</summary>
        public bool Spawned => _spawned;

        protected void Awake()
        {
            _root = GetComponentInParent<Spawnable>(true);
            if(_root == null)
            {
                Debug.LogError($"{name}: {GetType().Name} is a part of a Spawnable, and there is none on this object or above it.", this);
                return;
            }

            OnAwake();
            _root.RegisterPart(this);
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

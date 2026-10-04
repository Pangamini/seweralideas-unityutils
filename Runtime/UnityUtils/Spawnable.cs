using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SeweralIdeas.UnityUtils
{
    /// <summary>
    /// A MonoBehaviour with a lifecycle that survives pooling. Unity's own messages stay as they are: Awake once,
    /// OnEnable on every activation, Start once. On top of them:
    ///
    ///  - OnAwake and OnStart are Awake and Start, once per object, never again for a pooled one.
    ///  - OnSpawn means "enabled and started": it runs on OnEnable if Start has been done, and on Start otherwise. So for
    ///    a new object it comes after Start, and for a reused one it comes with the activation.
    ///  - OnDespawn runs when a spawned object is disabled.
    ///
    /// What belongs to one life is set up in OnSpawn and cleared in OnDespawn. Clearing in OnDespawn also covers an
    /// object that is despawned again before its first OnSpawn ever ran.
    /// </summary>
    public class Spawnable : MonoBehaviour
    {
        private bool m_spawned = false;
        public bool Spawned => m_spawned;

        protected void Awake() => OnAwake();

        protected void OnEnable() => TrySpawn();

        protected void OnDisable() => TryDespawn();

        protected void OnDestroy() => OnDestroyed();

        private void TrySpawn()
        {
            if(!didStart || m_spawned)
                return;
            m_spawned = true;
            OnSpawn();
        }

        private void TryDespawn()
        {
            if(!m_spawned)
                return;
            m_spawned = false;
            OnDespawn();
        }

        protected void Start()
        {
            OnStart();
            TrySpawn();
        }
        
        protected virtual void OnAwake() {}
        protected virtual void OnSpawn() {}
        protected virtual void OnDespawn() {}
        protected virtual void OnStart() {}
        protected virtual void OnDestroyed() {}
    }
}

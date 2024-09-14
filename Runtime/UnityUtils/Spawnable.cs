using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SeweralIdeas.UnityUtils
{
    public class Spawnable : MonoBehaviour
    {
        private bool m_started = false;
        private bool m_spawned = false;
        public bool Spawned => m_spawned;

        protected void Awake() => OnAwake();

        protected void OnEnable() => TrySpawn();

        protected void OnDisable() => TryDespawn();

        protected void OnDestroy() => OnDestroyed();

        private void TrySpawn()
        {
            if(!m_started || m_spawned)
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
            m_started = true;
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

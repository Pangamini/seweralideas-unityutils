using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SeweralIdeas.UnityUtils
{
    public class MyBehaviour : MonoBehaviour
    {
        private bool m_started = false;
        private bool m_wokeUp = false;

        protected void Awake() => OnAwake();

        protected void OnEnable() => TryWakeup();

        protected void OnDisable() => TrySleep();

        protected void OnDestroy() => OnDestroyed();

        private void TryWakeup()
        {
            if(!m_started || m_wokeUp)
                return;
            m_wokeUp = true;
            OnWakeup();
        }

        private void TrySleep()
        {
            if(!m_wokeUp)
                return;
            m_wokeUp = false;
            OnSleep();
        }

        protected void Start()
        {
            m_started = true;
            TryWakeup();
            OnStart();
        }
        
        protected virtual void OnAwake() {}
        protected virtual void OnWakeup() {}
        protected virtual void OnSleep() {}
        protected virtual void OnStart() {}
        protected virtual void OnDestroyed() {}
    }
}

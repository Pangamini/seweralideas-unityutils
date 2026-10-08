#if !UNITY_SERVER
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SeweralIdeas.ReplayableEffects
{
    public abstract class EffectComponent : MonoBehaviour
    {
        private ReplayableEffect m_effect;
        public abstract void Play();
        public abstract void FastForward( float deltaTime );
        
        protected virtual void Awake()
        {
            m_effect = GetComponentInParent<ReplayableEffect>();
            if(m_effect)
            {
                m_effect.AddEffectComponent(this);
            }
        }

        protected virtual void OnDestroy()
        {
            if (m_effect)
            {
                m_effect.RemoveEffectComponent(this);
            }
        }
    }
    
}
#endif
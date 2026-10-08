#if !UNITY_SERVER
using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace SeweralIdeas.ReplayableEffects
{
    [AddComponentMenu("SeweralIdeas/ReplayableEffects/LightFlash")]
    [RequireComponent(typeof(Light))]
    [DisallowMultipleComponent]
    public class ReplayableLightFlash : EffectComponent
    {
        [FormerlySerializedAs("LightCurve")]
        [SerializeField] 
        private AnimationCurve m_lightCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [SerializeField]
        [HideInInspector]
        private float m_duration;
        
        private Light m_light;
        
        private float m_time = Mathf.Infinity;

#if UNITY_EDITOR
        protected void OnValidate()
        {
            m_duration = 0;
            var keys = m_lightCurve.keys;
            foreach (var key in keys)
            {
                m_duration = Mathf.Max(m_duration, key.time);
            }
        }
        #endif

        protected override void Awake()
        {
            m_light = GetComponent<Light>();
            m_light.intensity = m_lightCurve.Evaluate(0);
            base.Awake();
        }

        public override void Play()
        {
            m_time = 0f;
            enabled = true;
        }
        
        public override void FastForward( float deltaTime )
        {
            m_time += deltaTime;
        }

        protected void Update()
        {
            var curveValue = m_lightCurve.Evaluate(m_time);
            m_light.intensity = curveValue;
            
            if ( m_time >= m_duration )
            {
                enabled = false;
            }
            
            m_time += Time.deltaTime;
        }

        protected void OnEnable()
        {
            m_light.enabled = true;
        }

        protected void OnDisable()
        {
            m_light.enabled = false;
        }
    }
}
#endif
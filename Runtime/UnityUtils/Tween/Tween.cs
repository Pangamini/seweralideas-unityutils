using System;
using UnityEngine;
using UnityEngine.Events;

namespace SeweralIdeas.UnityUtils
{
    public class Tween : MonoBehaviour
    {
        private float m_value;
        private float m_velocity;

        [SerializeField] private float             m_smoothTime = 0.1f;
        [SerializeField] private bool              m_isOn;
        [SerializeField] private UnityEvent<float> m_onValueChanged;

        public event UnityAction<float> ValueChanged
        {
            add => m_onValueChanged.AddListener(value);
            remove => m_onValueChanged.RemoveListener(value);
        }

        public float Value => m_value;

        public void SetValue(float newValue)
        {
            newValue = Mathf.Clamp01(newValue);
            enabled = true;
        }

        protected void OnValidate() => enabled = true;

        protected void OnDidApplyAnimationProperties() => enabled = true;

        public bool IsOn
        {
            get => m_isOn;
            set
            {
                if(m_isOn == value)
                    return;
                
                m_isOn = value;
                enabled = true;
            }
        }
        
        void Awake()
        {
            m_value = IsOn ? 1 : 0;
        }

        void Update()
        {
            float target = IsOn ? 1 : 0;
            m_value = Mathf.SmoothDamp(m_value, target, ref m_velocity, m_smoothTime);
            
            if(Math.Abs(m_value - target) < 0.001f)
            {
                m_value = target;
                enabled = false;
            }

            ApplyValue();
        }
        
        private void ApplyValue() => m_onValueChanged?.Invoke(m_value);
    }
}
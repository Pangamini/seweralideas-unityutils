using System;
using UnityEngine;
using UnityEngine.Events;

namespace SeweralIdeas.UnityUtils
{
    public class Tween : MonoBehaviour
    {
        private float m_progress;

        [SerializeField] private float m_smoothTime = 0.1f;
        [SerializeField] private bool m_isOn;
        [SerializeField] private UnityEvent<float> m_onValueChanged;
        [SerializeField] private AnimationCurve m_curve;
        [SerializeField] private Mode m_mode;
        
        private float m_velocity;

        public enum Mode
        {
            Simple,
            SmoothDamp,
            Curve
        }
        
        public event UnityAction<float> ValueChanged
        {
            add => m_onValueChanged.AddListener(value);
            remove => m_onValueChanged.RemoveListener(value);
        }

        public float Progress => m_progress;

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
        
        void Start()
        {
            m_progress = IsOn ? 1 : 0;
        }

        void Update()
        {
            float target = IsOn ? 1 : 0;

            switch (m_mode)
            {
                case Mode.Simple:
                case Mode.Curve:
                    m_progress = Mathf.MoveTowards(m_progress, target, (1f / m_smoothTime)*Time.deltaTime);
                    break;
                
                case Mode.SmoothDamp:
                    m_progress = Mathf.SmoothDamp(m_progress, target, ref m_velocity, m_smoothTime);
                    break;
            }
            
            if(Math.Abs(m_progress - target) < 0.0001f)
            {
                m_progress = target;
                enabled = false;
            }

            ApplyValue();
        }
        
        private void ApplyValue()
        {
            float value = m_mode switch
            {
                Mode.Curve => m_curve.Evaluate(m_progress),
                Mode.SmoothDamp => m_progress,
                Mode.Simple => m_progress
            };
            m_onValueChanged?.Invoke(value);
        }
    }
}

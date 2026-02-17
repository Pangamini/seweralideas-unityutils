using System;
using SeweralIdeas.UnityUtils.Drawers;
using UnityEngine;
using UnityEngine.Events;

namespace SeweralIdeas.UnityUtils
{
    [DefaultExecutionOrder(ExecOrder)]
    [DisallowMultipleComponent]
    public class Tween : MonoBehaviour
    {
        public const int   ExecOrder = 0;
        private      float m_progress;

        [SerializeField] 
        private bool m_isOn;
        
        [SerializeField] 
        private Mode m_mode;
        
        [SerializeField]
        private float m_smoothTime = 0.1f;
        
        [SerializeField]
        private bool m_useUnscaledTime;
        
        [SerializeField]
        [Condition(nameof(IsCurve))]
        private AnimationCurve m_curve = AnimationCurve.EaseInOut(0,0,1,1);
        
        [SerializeField]
        private UnityEvent<float> m_onValueChanged;
        
        private float m_velocity;

        private bool IsCurve => m_mode == Mode.Curve;
        
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

        public void SetProgress(float newProgress)
        {
            m_progress = Mathf.Clamp01(newProgress);
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
            m_progress = IsOn ? 1 : 0;
        }

        private void OnEnable()
        {
            ApplyValue();
        }

        void Update()
        {
            float target = IsOn ? 1 : 0;

            float deltaTime = m_useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            switch (m_mode)
            {
                case Mode.Simple:
                case Mode.Curve:
                    m_progress = Mathf.MoveTowards(m_progress, target, (1f / m_smoothTime)*deltaTime);
                    break;
                
                case Mode.SmoothDamp:
                    m_progress = Mathf.SmoothDamp(m_progress, target, ref m_velocity, m_smoothTime, float.PositiveInfinity, deltaTime);
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
                Mode.Simple => m_progress,
                _ => throw new ArgumentOutOfRangeException()
            };
            m_onValueChanged?.Invoke(value);
        }
    }
}
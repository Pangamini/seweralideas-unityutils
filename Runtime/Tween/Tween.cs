using System;
using SeweralIdeas.UnityUtils.Drawers;
using UnityEngine;
using UnityEngine.Events;

namespace SeweralIdeas.Tweening
{
    [DefaultExecutionOrder(TweenBase.ExecOrder)]
    public class Tween : TweenBase
    {
        private float m_progress;

        [SerializeField] 
        private bool m_isOn;
        
        [SerializeField] 
        private Mode m_mode;
        
        [SerializeField]
        private float m_smoothTime = 0.1f;
        
        [SerializeField]
        [Condition(nameof(IsSmooth))]
        private float m_maxSpeed = float.PositiveInfinity;
        
        [SerializeField]
        private bool m_useUnscaledTime;
        
        [SerializeField]
        [Condition(nameof(IsCurve))]
        private AnimationCurve m_curve = AnimationCurve.EaseInOut(0,0,1,1);
        
        [SerializeField]
        private UnityEvent<bool> m_onTargetValueChanged = new();
        
        private float m_velocity;
        private bool  m_awoken;

        private bool IsCurve => m_mode == Mode.Curve;
        private bool IsSmooth => m_mode is Mode.SmoothDamp or Mode.SmoothDampOvershoot;

        public void Toggle() => IsOn = !IsOn;
        
        public enum Mode
        {
            Simple,
            SmoothDamp,
            Curve,
            SmoothDampOvershoot
        }
        
        public override float Progress => m_progress;

        public void SetProgress(float newProgress)
        {
            m_progress = Mathf.Clamp01(newProgress);
            enabled = true;
        }

        public void SetImmediate(bool isOn)
        {
            m_progress = isOn ? 1 : 0;
            IsOn = isOn;
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
                m_onTargetValueChanged.Invoke(m_isOn);
            }
        }
        
        void Awake()
        {
            InitializeProgress();
        }

        public override void EnsureInitialized()
        {
            InitializeProgress();
        }

        private void InitializeProgress()
        {
            if (m_awoken)
                return;
            m_awoken = true;
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
                    m_progress = Mathf.SmoothDamp(m_progress, target, ref m_velocity, m_smoothTime, m_maxSpeed, deltaTime);
                    break;

                case Mode.SmoothDampOvershoot:
                    m_progress = SmoothDampUnguarded(m_progress, target, ref m_velocity, m_smoothTime, m_maxSpeed, deltaTime);
                    break;
            }

            if(Math.Abs(m_progress - target) < 0.0001f && Math.Abs(m_velocity) < 0.0001f)
            {
                m_progress = target;
                m_velocity = 0;
                enabled = false;
            }

            ApplyValue();
        }

        /// <summary><see cref="Mathf.SmoothDamp(float,float,ref float,float,float,float)"/> without the clause that prevents overshooting the target.</summary>
        public static float SmoothDampUnguarded(float current, float target, ref float velocity, float smoothTime, float maxSpeed, float deltaTime)
        {
            smoothTime = Mathf.Max(0.0001f, smoothTime);
            float omega = 2f / smoothTime;
            float x = omega * deltaTime;
            float exp = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);
            float maxChange = maxSpeed * smoothTime;
            float change = Mathf.Clamp(current - target, -maxChange, maxChange);
            target = current - change;
            float temp = (velocity + omega * change) * deltaTime;
            velocity = (velocity - omega * temp) * exp;
            return target + (change + temp) * exp;
        }
        
        private void ApplyValue()
        {
            float value = m_mode switch
            {
                Mode.Curve => m_curve.Evaluate(m_progress),
                Mode.SmoothDamp => m_progress,
                Mode.SmoothDampOvershoot => m_progress,
                Mode.Simple => m_progress,
                _ => throw new ArgumentOutOfRangeException()
            };
            NotifyValueChanged(value);
        }
    }
}
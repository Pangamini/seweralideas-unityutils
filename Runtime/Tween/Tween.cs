using System;
using SeweralIdeas.UnityUtils.Drawers;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

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
        [FormerlySerializedAs("m_smoothTime")]
        private float m_duration = 0.1f;
        
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

        private bool IsCurve => m_mode == Mode.Curve;        private bool IsSmooth => m_mode is Mode.SmoothDamp or Mode.SmoothDampOvershoot or Mode.LinearRamp;

        public void Toggle() => IsOn = !IsOn;
        
        public enum Mode
        {
            Simple,
            SmoothDamp,
            Curve,
            SmoothDampOvershoot,
            /// <summary>
            /// Constant speed with linear ease-in/out ramps (trapezoid speed profile). A full 0..1 move takes exactly the smooth time.
            /// Reversing mid-move decelerates through zero first, so it overshoots.
            /// </summary>
            LinearRamp
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
            m_progress = Step(m_mode, m_progress, target, ref m_velocity, m_duration, m_maxSpeed, deltaTime);

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
        
        /// <summary>
        /// Moves toward the target with a trapezoid speed profile. <paramref name="duration"/> is the time of a full 1-unit move from rest;
        /// <paramref name="maxSpeed"/> caps the cruising speed; the acceleration is derived so the move still takes <paramref name="duration"/>.
        /// With no cap the profile is a triangle (half the time speeding up, half slowing down).
        /// Speed always changes at the same constant rate, so moving away from the target first brakes to zero, overshooting the new target.
        /// </summary>
        public static float LinearRampStep(float current, float target, ref float velocity, float duration, float maxSpeed, float deltaTime)
        {
            duration = Mathf.Max(0.0001f, duration);
            // Below 1/duration the move can't finish in time; just above it the ramps become near-instant (constant speed).
            float minSpeed = 1.001f / duration;
            maxSpeed = Mathf.Clamp(maxSpeed, minSpeed, 2f / duration);   // 2/duration is the triangle's peak: a higher cap is never reached
            // duration = 1/maxSpeed (cruise-equivalent) + maxSpeed/accel (ramps)
            float accel = maxSpeed / (duration - 1f / maxSpeed);

            float distance = target - current;
            // fastest speed from which we can still brake to a stop on the target
            float desired = Mathf.Sign(distance) * Mathf.Min(maxSpeed, Mathf.Sqrt(2f * accel * Mathf.Abs(distance)));
            velocity = Mathf.MoveTowards(velocity, desired, accel * deltaTime);

            float next = current + velocity * deltaTime;

            // arriving: this step reaches or crosses the target while heading for it. Settle there, otherwise the last
            // discrete step lands slightly past it and the speed swings through zero. (Moving away from it never matches.)
            if(velocity * distance >= 0f && (target - next) * distance <= 0f)
            {
                velocity = 0f;
                return target;
            }
            return next;
        }

        /// <summary>One frame of progress toward <paramref name="target"/> in the given mode. Public so editor previews run the exact same code.</summary>
        public static float Step(Mode mode, float current, float target, ref float velocity, float duration, float maxSpeed, float deltaTime)
        {
            switch (mode)
            {
                case Mode.Simple:
                case Mode.Curve:
                    return Mathf.MoveTowards(current, target, (1f / duration) * deltaTime);

                case Mode.SmoothDamp:
                    return Mathf.SmoothDamp(current, target, ref velocity, duration, maxSpeed, deltaTime);

                case Mode.SmoothDampOvershoot:
                    return SmoothDampUnguarded(current, target, ref velocity, duration, maxSpeed, deltaTime);

                case Mode.LinearRamp:
                    return LinearRampStep(current, target, ref velocity, duration, maxSpeed, deltaTime);

                default:
                    throw new ArgumentOutOfRangeException(nameof(mode));
            }
        }

        /// <summary>The value that listeners receive for a given progress.</summary>
        public static float MapProgress(Mode mode, AnimationCurve curve, float progress)
        {
            return mode == Mode.Curve ? curve.Evaluate(progress) : progress;
        }

        private void ApplyValue() => NotifyValueChanged(MapProgress(m_mode, m_curve, m_progress));
    }
}
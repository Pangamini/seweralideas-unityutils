using SeweralIdeas.UnityUtils.Drawers;
using UnityEngine;

namespace SeweralIdeas.Tweening
{
    /// <summary>
    /// A <see cref="TweenBase"/> raised by <see cref="Trigger(float)"/>: the value jumps (smoothed) to the intensity,
    /// then falls linearly back to 0. A weaker trigger never cuts a stronger one that is still decaying short.
    /// </summary>
    [DefaultExecutionOrder(TweenBase.ExecOrder)]
    public class ImpulseTween : TweenBase
    {
        [Button(nameof(Trigger))]
        [SerializeField] private float _decayRate = 2f; // intensity lost per second
        [SerializeField] private float _smoothTime = 0.1f;
        [SerializeField] private bool  _useUnscaledTime;

        private float _intensity; // 0..1, before smoothing
        private float _smoothed;
        private float _smoothVelocity;

        public override float Progress => _smoothed;

        /// <summary>Raises the impulse to at least intensity (clamped to 0..1).</summary>
        public void Trigger(float intensity)
        {
            _intensity = Mathf.Max(_intensity, Mathf.Clamp01(intensity));
            enabled    = true;
        }

        public void Trigger() => Trigger(1f);

        protected void OnValidate() => enabled = true;

        protected void Update()
        {
            float deltaTime = _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            _intensity = Mathf.Max(0f, _intensity - _decayRate * deltaTime);
            _smoothed  = Mathf.SmoothDamp(_smoothed, _intensity, ref _smoothVelocity, _smoothTime, float.PositiveInfinity, deltaTime);

            if(_intensity == 0f && _smoothed < 0.0001f)
            {
                _smoothed       = 0f;
                _smoothVelocity = 0f;
                enabled         = false;
            }

            NotifyValueChanged(_smoothed);
        }
    }
}

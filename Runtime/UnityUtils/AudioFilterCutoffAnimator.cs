#nullable enable
using UnityEngine;

namespace SeweralIdeas.UnityUtils
{
    // AudioLowPassFilter/AudioHighPassFilter's own cutoffFrequency is a plain
    // C# property, not a serialized field, so it never shows up as an
    // animatable target in the Animation window's "Add Property" list. This
    // relays a serialized (and therefore animatable) CutoffFrequency onto
    // whichever filter(s) are assigned - same shape as
    // AnimationClipSampler.Time: an ordinary serialized field, a scripted
    // property for code, and OnDidApplyAnimationProperties for when the
    // Animation window or a Timeline writes to the field directly.
    public class AudioFilterCutoffAnimator : MonoBehaviour
    {
        [SerializeField] private AudioLowPassFilter?  _lowPassFilter;
        [SerializeField] private AudioHighPassFilter? _highPassFilter;
        [SerializeField] private float                _cutoffFrequency = 5000f;

        public float CutoffFrequency
        {
            get => _cutoffFrequency;
            set
            {
                _cutoffFrequency = value;
                Apply();
            }
        }

        protected void OnEnable() => Apply();

        // Called by Unity after the Animation window or a Timeline writes
        // animated values into this component's serialized fields (edit-time
        // preview included) - the reliable hook for "CutoffFrequency just
        // changed from outside the property setter above".
        protected void OnDidApplyAnimationProperties() => Apply();

#if UNITY_EDITOR
        protected void OnValidate() => Apply();
#endif

        private void Apply()
        {
            if(_lowPassFilter != null)
                _lowPassFilter.cutoffFrequency = _cutoffFrequency;
            if(_highPassFilter != null)
                _highPassFilter.cutoffFrequency = _cutoffFrequency;
        }
    }
}

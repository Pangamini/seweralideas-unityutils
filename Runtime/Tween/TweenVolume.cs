#if UNITY_RENDER_PIPELINES_CORE
using UnityEngine;
using UnityEngine.Rendering;

namespace SeweralIdeas.Tweening
{
    public class TweenVolume : TweenComponent
    {
        [SerializeField] private Volume _volume;

        [SerializeField]
        private float _offValue = 0;

        [SerializeField]
        private float _onValue = 1;

        protected override void OnValueChanged(float progress) => _volume.weight = Mathf.LerpUnclamped(_offValue, _onValue, progress);
    }
}
#endif

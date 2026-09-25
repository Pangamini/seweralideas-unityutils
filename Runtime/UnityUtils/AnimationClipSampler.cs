#nullable enable
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace SeweralIdeas.UnityUtils
{
    [RequireComponent(typeof(Animator))]
    public class AnimationClipSampler : MonoBehaviour, IAnimationClipSource
    {
        [SerializeField] private AnimationClip? _clip;
        [SerializeField] private float          _time;

        private Animator?             _animator;
        private PlayableGraph         _graph;
        private AnimationClipPlayable _clipPlayable;
        private bool                  _graphValid;

        public AnimationClip? Clip
        {
            get => _clip;
            set
            {
                if(_clip == value)
                    return;
                _clip = value;
                RebuildGraph();
            }
        }

        // Seconds into the clip. Setting this immediately re-samples.
        public float Time
        {
            get => _time;
            set
            {
                _time = value;
                Sample();
            }
        }

        // 0-1 across the clip's length. Just Time expressed as a fraction -
        // there's no separate serialized backing for it.
        public float NormalizedTime
        {
            get => _clip != null && _clip.length > 0f ? _time / _clip.length : 0f;
            set => Time = _clip != null ? value * _clip.length : 0f;
        }

        protected void OnEnable()
        {
            _animator = GetComponent<Animator>();
            RebuildGraph();
        }

        protected void OnDisable() => DestroyGraph();

        // Called by Unity after the Animation window or a Timeline writes
        // animated values into this component's serialized fields (edit-time
        // preview included) - the only reliable hook for "Time just changed
        // from outside the Time property above".
        protected void OnDidApplyAnimationProperties() => Sample();

#if UNITY_EDITOR
        protected void OnValidate()
        {
            if(!didAwake)
                return;
            if(_animator == null)
                _animator = GetComponent<Animator>();
            RebuildGraph();
        }
#endif

        private void RebuildGraph()
        {
            DestroyGraph();

            if(_clip == null || _animator == null)
                return;

            _graph = PlayableGraph.Create($"{nameof(AnimationClipSampler)}({name})");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual); // never auto-advance - only Sample() ever evaluates it

            _clipPlayable = AnimationClipPlayable.Create(_graph, _clip);

            var output = AnimationPlayableOutput.Create(_graph, "Output", _animator);
            output.SetSourcePlayable(_clipPlayable);

            _graphValid = true;
            Sample();
        }

        private void DestroyGraph()
        {
            if(!_graphValid)
                return;
            _graphValid = false;
            _graph.Destroy();
        }

        private void Sample()
        {
            if(!_graphValid)
                return;
            _clipPlayable.SetTime(_time);
            _graph.Evaluate(0f);
        }

        void IAnimationClipSource.GetAnimationClips(List<AnimationClip> results)
        {
            if(_clip)
                results.Add(_clip);
        }
    }
}

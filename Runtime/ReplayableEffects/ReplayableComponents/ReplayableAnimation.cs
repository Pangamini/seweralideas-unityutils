#if !UNITY_SERVER
using SeweralIdeas.UnityUtils;
using UnityEngine;

namespace SeweralIdeas.ReplayableEffects
{
    /// <summary>
    /// Plays the <see cref="AnimationClip"/> of an <see cref="AnimationClipSampler"/> along with the effect, from its start
    /// when the effect plays. The clip is sampled, not run by an Animator controller, so it fast-forwards exactly. Unless it
    /// loops, it holds its last pose when the clip is over.
    /// </summary>
    [AddComponentMenu("SeweralIdeas/ReplayableEffects/ReplayableAnimation")]
    [RequireComponent(typeof(AnimationClipSampler))]
    [DisallowMultipleComponent]
    public class ReplayableAnimation : EffectComponent
    {
        [Tooltip("Playback speed, 1 = the clip's own.")]
        [SerializeField] private float m_speed = 1f;
        [Tooltip("Restart the clip when it is over, for as long as the effect lasts. Otherwise it holds its last pose.")]
        [SerializeField] private bool m_loop;

        private AnimationClipSampler m_sampler;
        private float                m_time;

        protected override void Awake()
        {
            m_sampler = GetComponent<AnimationClipSampler>();
            enabled = false; // nothing to advance until the effect plays
            base.Awake();
        }

        public override void Play()
        {
            m_time = 0f;
            m_sampler.Time = 0f;
            enabled = true;
        }

        public override void FastForward(float deltaTime) => Advance(deltaTime);

        protected void Update() => Advance(Time.deltaTime);

        private void Advance(float deltaTime)
        {
            AnimationClip clip = m_sampler.Clip;
            if(clip == null)
            {
                enabled = false;
                return;
            }

            float length = clip.length;
            m_time += deltaTime * Mathf.Max(0f, m_speed);

            if(m_loop && length > 0f)
            {
                m_time %= length;
            }
            else if(m_time >= length)
            {
                m_time = length;
                enabled = false; // holds the last pose
            }

            m_sampler.Time = m_time;
        }
    }
}
#endif

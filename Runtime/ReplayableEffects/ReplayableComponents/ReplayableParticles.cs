#if !UNITY_SERVER
using UnityEngine;

namespace SeweralIdeas.ReplayableEffects
{
    [AddComponentMenu("SeweralIdeas/ReplayableEffects/ReplayableParticles")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ParticleSystem))]
    
    public class ReplayableParticles : EffectComponent
    {
        private ParticleSystem m_particles;
        
        protected override void Awake()
        {
            m_particles = GetComponent<ParticleSystem>();
            base.Awake();
        }
        
        private void ConfigureParticles()
        {
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(m_particles, "Configure ParticleSystem");
#endif

            var main = m_particles.main;
            var parName = m_particles.ToString();
            if ( main.playOnAwake )
            {
                main.playOnAwake = false;
                Debug.Log(parName + " main.playOnAwake set to false");
            }
            if ( main.loop )
            {
                main.loop = false;
                Debug.Log(parName + " main.loop set to false");
            }
        
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(m_particles);
#endif
        }
        
        public override void Play()
        {
            m_particles.Play(false);
        }

        public override void FastForward( float deltaTime )
        {
            m_particles.Simulate(deltaTime, false, false);
            m_particles.Play(false);
        }
    }
}
#endif
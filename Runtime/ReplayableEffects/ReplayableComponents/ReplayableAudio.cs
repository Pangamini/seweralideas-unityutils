#if !UNITY_SERVER
using System;
using UnityEngine;
using SeweralIdeas.UnityUtils;
using UnityEngine.Serialization;


#pragma warning disable CS0649  
namespace SeweralIdeas.ReplayableEffects
{
    [AddComponentMenu("SeweralIdeas/ReplayableEffects/ReplayableAudio")]
    [RequireComponent(typeof(AudioSource))]
    [DisallowMultipleComponent]
    public class ReplayableAudio : EffectComponent
    {
        [FormerlySerializedAs("clipCollection")] 
        [SerializeField] 
        private AudioClipCollection m_clipCollection;
        
        [FormerlySerializedAs("loop")]
        [SerializeField]
        private bool m_loop;
        
        [FormerlySerializedAs("volumeOverTime")]
        [SerializeField] 
        private AnimationCurve m_volumeOverTime;
        
        [FormerlySerializedAs("duration")]
        [SerializeField] 
        private float m_duration = 5f;

        [FormerlySerializedAs("playAsNew")]
        [SerializeField]
        public bool m_playAsNew = false;
        
        [SerializeField]
        [HideInInspector]
        private Vector2 m_volumeOverTimeRange;

        private AudioSource m_audioSource;
        private float m_progress;

        private void ConfigureSources()
        {
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(m_audioSource, "Configure Audio Sources");
#endif
            if ( m_audioSource.playOnAwake )
            {
                m_audioSource.playOnAwake = false;
                Debug.Log(m_audioSource.ToString()+" playOnAwake set to false");
            }
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(m_audioSource);
#endif
        }

        protected void OnValidate()
        {
            if (m_volumeOverTime == null || m_volumeOverTime.length <= 0)
                return;
            
            Keyframe[] keys = m_volumeOverTime.keys;
            m_volumeOverTimeRange.x = keys[0].time;
            
            if (keys.Length > 1)
            {
                m_volumeOverTimeRange.y = keys[^1].time;
            }
            else
            {
                m_volumeOverTimeRange.y = m_volumeOverTimeRange.x;
            }
        }

        protected override void Awake()
        {
            m_audioSource = GetComponent<AudioSource>();
            base.Awake();
        }
        
        public override void Play()
        {
            if ( !gameObject.activeInHierarchy )
                return;
            
            // With a collection, a clip and a pitch are picked for the source. Without one, the source is left as it is:
            // its resource may be an AudioRandomContainer, which has no clip (reading `clip` gives null, and writing it back would clear the resource).
            AudioClip clip = null;
            if (m_clipCollection != null)
            {
                m_audioSource.pitch = m_clipCollection.PickRandomPitch();
                clip = m_clipCollection.PickRandom();
                m_audioSource.clip = clip;
            }
            else
            {
                clip = m_audioSource.clip;
            }

            m_audioSource.loop = m_loop;

            if ( m_loop )
            {
                m_progress = 0;
                enabled = true;
            }

            // PlayOneShot needs a clip; a source with a container resource can only Play().
            if ( m_playAsNew && !m_loop && clip != null )
                m_audioSource.PlayOneShot(clip);
            else
                m_audioSource.Play();
        }


        protected void Update()
        {
            if ( !m_loop || m_progress > m_duration )
            {
                enabled = false;
                return;
            }

            m_progress += Time.deltaTime;
            var relativeProgress = m_progress / m_duration;          
            m_audioSource.volume = m_volumeOverTime.Evaluate(Mathf.Lerp(m_volumeOverTimeRange.x, m_volumeOverTimeRange.y, relativeProgress));

        }

        protected void OnDisable()
        {
            if(m_loop)
                m_audioSource.Stop();
        }

        public override void FastForward( float deltaTime )
        {
            m_progress += deltaTime;
            if ( m_audioSource.clip == null )  //this may happen when playAsNew is false
            {
                if (m_clipCollection != null && m_audioSource != null)
                {
                    m_audioSource.pitch = m_clipCollection.PickRandomPitch();
                    m_audioSource.clip = m_clipCollection.PickRandom();
                }
            }

            float newTime = m_audioSource.time + deltaTime;
            if ( newTime >= m_duration)
            {
                m_audioSource.Stop();
            }
            else if ( m_audioSource.clip != null ) // no clip when the source plays an AudioRandomContainer
            {
                m_audioSource.time = Mathf.Repeat( newTime, m_audioSource.clip.length);
            }
        }
    }
}
#endif
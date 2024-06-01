using System;
using SeweralIdeas.UnityUtils.Drawers;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SeweralIdeas.UnityUtils
{
    public class CollisionFX : MonoBehaviour
    {
        [Serializable]
        public struct CollisionVariant
        {
            [field: SerializeField] [field: Units("Ns")] public float MinImpulse { get; set;  }
            [field: SerializeField] public AudioClipCollection Collection { get; set;  }
            [field: SerializeField] [field: Units("Ns->vol")] public AnimationCurve ImpulseToVolume { get; set; }

        }
        
        [SerializeField]
        private AudioSource m_audioSource;

        [SerializeField]
        private CollisionVariant[] m_collisionVariants;

        [SerializeField]
        private float m_minSoundDelay = 0.1f;

        [SerializeField] private bool m_logCollisions;
        
        private float m_timeOfLastAudibleCollision = float.NegativeInfinity;
        private int m_previousClipId = -1;
        
        protected void OnValidate()
        {
            Array.Sort(m_collisionVariants, (lhs, rhs) => (int)Mathf.Sign(lhs.MinImpulse - rhs.MinImpulse));
        }
        
        protected void OnCollisionEnter(Collision collision)
        {
            // float normalImpulse = 0;
            // float tangentImpulse = 0;
            // for(int i = 0; i < collision.contactCount; ++i)
            // {
            //     var contact = collision.GetContact(i);
            //     float contactNormalImpulse = Vector3.Dot(contact.normal, contact.impulse);
            //     normalImpulse += contactNormalImpulse;
            // }

            float normalImpulse = collision.impulse.magnitude;  // unfortunately, the impulse is always normal...

            if(m_logCollisions)
            {
                Debug.Log($"\"{gameObject.name}\" collision with \"{collision.collider.gameObject.name}\" at {normalImpulse} Ns", gameObject);
            }
            
            if(Time.time - m_timeOfLastAudibleCollision < m_minSoundDelay)
                return;
            
            PlayCollisionEffect(normalImpulse);
        }
        
        public void PlayCollisionEffect(float normalImpulse)
        {
            for( int index = m_collisionVariants.Length - 1; index >= 0; index-- )
            {
                CollisionVariant sound = m_collisionVariants[index];
                
                if(!(normalImpulse >= sound.MinImpulse))
                    continue;
                
                float volume = sound.ImpulseToVolume.Evaluate(normalImpulse);
                AudioClip clip = PickRandomClip(sound.Collection);
                if(clip)
                    m_audioSource.PlayOneShot(clip, volume);
                m_timeOfLastAudibleCollision = Time.deltaTime;
                break;
            }
        }

        private AudioClip PickRandomClip(AudioClipCollection collection)
        {
            if (collection.Count == 0)
                return default;
            if(collection.Count == 1)
                return collection[0];   // otherwise the previousClip approach wouldn't work
            
            
            int random = Random.Range(0, collection.Count-1);
            
            if(random == m_previousClipId)
                random++;
            m_previousClipId = random;
            return collection[random];
        }
    }
    
}

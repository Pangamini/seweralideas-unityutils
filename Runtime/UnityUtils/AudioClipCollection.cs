using UnityEngine;

namespace SeweralIdeas.UnityUtils
{
    [CreateAssetMenu(menuName ="SeweralIdeas/AssetCollections/AudioClipCollection")]
    public class AudioClipCollection : AssetCollection<AudioClip>
    {
        [SerializeField] public Vector2 m_pitchRange = Vector2.one;

        public float PickRandomPitch()
        {
            return Random.Range(m_pitchRange.x, m_pitchRange.y);
        }

        public void PlayOneShot(AudioSource source)
        {
            source.pitch = PickRandomPitch();
            source.PlayOneShot(PickRandom());
        }
    }
}

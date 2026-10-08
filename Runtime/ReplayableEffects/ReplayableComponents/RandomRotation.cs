#if !UNITY_SERVER
using UnityEngine;
using UnityEngine.Serialization;

namespace SeweralIdeas.ReplayableEffects
{
    [AddComponentMenu("SeweralIdeas/ReplayableEffects/RandomRotation")]
    [DisallowMultipleComponent]
    public class RandomRotation : EffectComponent
    {
        [FormerlySerializedAs("RotateX")] [SerializeField] private bool m_rotateX;
        [FormerlySerializedAs("RotateY")] [SerializeField] private bool m_rotateY;
        [FormerlySerializedAs("RotateZ")] [SerializeField] private bool m_rotateZ = true;
        
        public override void Play()
        {
            var eulerAngles = Vector3.zero;
            if ( m_rotateX )
                eulerAngles.x = Random.Range(0, 360);
            if ( m_rotateY )
                eulerAngles.y = Random.Range(0, 360);
            if ( m_rotateZ )
                eulerAngles.z = Random.Range(0, 360);
            transform.Rotate(eulerAngles);
        }

        public override void FastForward( float deltaTime ) { }
    }
}
#endif
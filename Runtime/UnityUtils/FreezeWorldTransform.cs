using SeweralIdeas.UnityUtils.Drawers;
using SeweralIdeas.Utils;
using UnityEngine;

namespace Rockwell.Framework
{
    public class FreezeWorldTransform : MonoBehaviour
    {
        [SerializeField]
        private Override<Vector3> _position;        
        
        [SerializeField]
        [OverrideValue]
        private Override<Quaternion> _rotation;
        
        void LateUpdate()
        {
            if(_position.HasValue && _position.HasValue)
            {
                transform.SetPositionAndRotation((Vector3)_position, (Quaternion)_rotation);
            }
            else
            {
                if(_position.TryGetValue(out var pos))
                    transform.position = pos;
                else if(_rotation.TryGetValue(out var rot))
                    transform.rotation = rot;
            }
        }
    }
}

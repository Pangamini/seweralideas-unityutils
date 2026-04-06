using UnityEngine;

namespace SeweralIdeas.UnityUtils
{
    public class TweenVector3 : TweenValue<Vector3>
    {
        protected override Vector3 Interpolate(float t) => Vector3.Lerp(OffValue, OnValue, t);
    }
}

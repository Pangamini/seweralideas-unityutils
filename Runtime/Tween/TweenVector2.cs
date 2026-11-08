using UnityEngine;

namespace SeweralIdeas.Tweening
{
    public class TweenVector2 : TweenValue<Vector2>
    {
        protected override Vector2 Interpolate(float t) => Vector2.Lerp(OffValue, OnValue, t);
    }
}

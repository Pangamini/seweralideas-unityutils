using UnityEngine;

namespace SeweralIdeas.UnityUtils.Curves
{
    public interface IRangedCurve
    {
        float Evaluate(float input);
        Vector2 GetValueMinMax(Vector2 range);
    }
}

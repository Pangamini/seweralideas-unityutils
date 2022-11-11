using UnityEngine;

namespace SeweralIdeas.UnityUtils.Curves
{
    public interface IRealCurve
    {
        float Evaluate(float input);
        Vector2 GetValueMinMax(Vector2 range);
    }
}

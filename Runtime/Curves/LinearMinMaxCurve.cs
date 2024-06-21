using System;
using UnityEngine;

namespace SeweralIdeas.UnityUtils.Curves
{
    [Serializable]
    public struct LinearMinMaxCurve : IRealCurve
    {
        public Vector2 xRange;
        public Vector2 yRange;

        public float Evaluate(float input)
        {
            float inv = Mathf.InverseLerp(xRange.x, xRange.y, input);
            return Mathf.LerpUnclamped(yRange.x, yRange.y, inv);
        }

        public Vector2 GetValueMinMax(Vector2 range)
        {
            var startValue = Evaluate(range.x);
            var endValue = Evaluate(range.y);
            return new Vector2(Mathf.Min(startValue, endValue), Mathf.Max(startValue, endValue));
        }
    }
}
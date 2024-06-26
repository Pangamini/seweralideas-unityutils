using System;
using UnityEngine;

namespace SeweralIdeas.UnityUtils.Curves
{
    [Serializable]
    public struct SigmoidCurve : IRealCurve
    {
        public float mul;
        public float exp;
        public float xOffset;
        public float yOffset;
        public float lin;

        public float Evaluate(float input)
        {
            input += xOffset;
            return input * lin + yOffset + mul / (1 + Mathf.Exp(exp*input));
        }

        public Vector2 GetValueMinMax(Vector2 range)
        {
            var startValue = Evaluate(range.x);
            var endValue = Evaluate(range.y);
            return new Vector2(Mathf.Min(startValue, endValue), Mathf.Max(startValue, endValue));
        }
    }
}

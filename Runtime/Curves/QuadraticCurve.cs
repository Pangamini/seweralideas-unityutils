using System;
using UnityEngine;

namespace SeweralIdeas.UnityUtils.Curves
{
    [Serializable]
    public struct QuadraticCurve : IRealCurve
    {
        public float c;
        public float x1;
        public float x2;

        public float Evaluate(float input)
        {
            return c + input * x1 + input * input * x2;
        }

        public Vector2 GetValueMinMax(Vector2 range)
        {
            // Ensure range.x <= range.y
            float tStart = Mathf.Min(range.x, range.y);
            float tEnd = Mathf.Max(range.x, range.y);

            if (Mathf.Approximately(x2, 0f))
            {
                // Linear function case
                float yStart = c + x1 * tStart;
                float yEnd = c + x1 * tEnd;

                float minValue = Mathf.Min(yStart, yEnd);
                float maxValue = Mathf.Max(yStart, yEnd);

                return new Vector2(minValue, maxValue);
            }
            else
            {
                // Analytical minimum and maximum
                float tVertex = -x1 / (2 * x2); // Vertex of the parabola

                // Evaluate function at critical points and endpoints
                float yStart = Evaluate(tStart);
                float yEnd = Evaluate(tEnd);
                float yVertex = Evaluate(tVertex);

                // Find min and max values
                float minValue = Mathf.Min(yStart, Mathf.Min(yEnd, yVertex));
                float maxValue = Mathf.Max(yStart, Mathf.Max(yEnd, yVertex));

                return new Vector2(minValue, maxValue);
            }
        }
    }
}
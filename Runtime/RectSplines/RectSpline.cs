using System;
using UnityEngine;

namespace SeweralIdeas.UnityUtils.SplineMesh
{
    [Serializable]
    public class RectSpline
    {
        [SerializeField] private RectBezierKnot[] _knots = Array.Empty<RectBezierKnot>();

        public RectBezierKnot[] Knots => _knots;

        public Vector2 EvaluatePosition(float t)
        {
            if (_knots.Length == 0) return Vector2.zero;
            if (_knots.Length == 1) return _knots[0].position;

            int segmentCount = _knots.Length - 1;
            float scaled = t * segmentCount;
            int seg = Mathf.Clamp(Mathf.FloorToInt(scaled), 0, segmentCount - 1);
            float localT = scaled - seg;

            var k0 = _knots[seg];
            var k1 = _knots[seg + 1];

            Vector2 p0 = k0.position;
            Vector2 p1 = k0.position + k0.tangentOut;
            Vector2 p2 = k1.position + k1.tangentIn;
            Vector2 p3 = k1.position;

            return CubicBezier(p0, p1, p2, p3, localT);
        }

        public Vector2 EvaluateTangent(float t)
        {
            if (_knots.Length < 2) return Vector2.right;

            int segmentCount = _knots.Length - 1;
            float scaled = t * segmentCount;
            int seg = Mathf.Clamp(Mathf.FloorToInt(scaled), 0, segmentCount - 1);
            float localT = scaled - seg;

            var k0 = _knots[seg];
            var k1 = _knots[seg + 1];

            Vector2 p0 = k0.position;
            Vector2 p1 = k0.position + k0.tangentOut;
            Vector2 p2 = k1.position + k1.tangentIn;
            Vector2 p3 = k1.position;

            return CubicBezierTangent(p0, p1, p2, p3, localT);
        }

        public float GetLength(int samples = 64)
        {
            if (_knots.Length < 2) return 0f;

            float length = 0f;
            Vector2 prev = EvaluatePosition(0f);
            for (int i = 1; i <= samples; i++)
            {
                float t = (float)i / samples;
                Vector2 curr = EvaluatePosition(t);
                length += Vector2.Distance(prev, curr);
                prev = curr;
            }
            return length;
        }

        public static Vector2 CubicBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float u = 1f - t;
            float uu = u * u;
            float tt = t * t;
            return uu * u * p0 + 3f * uu * t * p1 + 3f * u * tt * p2 + tt * t * p3;
        }

        public static Vector2 CubicBezierTangent(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float u = 1f - t;
            return 3f * u * u * (p1 - p0) + 6f * u * t * (p2 - p1) + 3f * t * t * (p3 - p2);
        }
    }
}

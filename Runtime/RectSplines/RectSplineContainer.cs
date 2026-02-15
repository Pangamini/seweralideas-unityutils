using System;
using UnityEngine;

namespace SeweralIdeas.UnityUtils.SplineMesh
{
    [RequireComponent(typeof(RectTransform))]
    public class RectSplineContainer : MonoBehaviour
    {
        [SerializeField] private RectSpline[] _splines = Array.Empty<RectSpline>();

        private RectTransform _rectTransform;

        public int SplineCount => _splines.Length;

        public event Action Changed;

        public RectTransform RectTransform
        {
            get
            {
                if (_rectTransform == null)
                    _rectTransform = (RectTransform)transform;
                return _rectTransform;
            }
        }

        /// <summary>Returns position in the RectTransform's local space (rect-local units).</summary>
        public Vector2 EvaluatePosition(int index, float t)
        {
            Vector2 normalized = _splines[index].EvaluatePosition(t);
            return NormalizedToRectLocal(normalized);
        }

        /// <summary>Returns tangent in the RectTransform's local space (rect-local units).</summary>
        public Vector2 EvaluateTangent(int index, float t)
        {
            Vector2 normalizedTangent = _splines[index].EvaluateTangent(t);
            Rect rect = RectTransform.rect;
            return new Vector2(
                normalizedTangent.x * rect.width,
                normalizedTangent.y * rect.height);
        }

        /// <summary>Arc length in rect-local units.</summary>
        public float GetSplineLength(int index)
        {
            var spline = _splines[index];
            Rect rect = RectTransform.rect;

            const int samples = 64;
            float length = 0f;
            Vector2 prev = NormalizedToRectLocal(spline.EvaluatePosition(0f), rect);
            for (int i = 1; i <= samples; i++)
            {
                float st = (float)i / samples;
                Vector2 curr = NormalizedToRectLocal(spline.EvaluatePosition(st), rect);
                length += Vector2.Distance(prev, curr);
                prev = curr;
            }
            return length;
        }

        public float NormalizedScalarToRectLocal(float normalized)
        {
            Rect rect = RectTransform.rect;
            return normalized * Mathf.Min(rect.width, rect.height);
        }

        private Vector2 NormalizedToRectLocal(Vector2 normalized)
        {
            Rect rect = RectTransform.rect;
            return new Vector2(
                rect.x + normalized.x * rect.width,
                rect.y + normalized.y * rect.height);
        }

        private static Vector2 NormalizedToRectLocal(Vector2 normalized, Rect rect)
        {
            return new Vector2(
                rect.x + normalized.x * rect.width,
                rect.y + normalized.y * rect.height);
        }

        protected void OnDrawGizmosSelected()
        {
            if (_splines == null || _splines.Length == 0)
                return;

            RectTransform rt = RectTransform;

            // Draw rect bounds
            Gizmos.color = Color.yellow;
            Vector3[] rectCorners = new Vector3[4];
            rt.GetWorldCorners(rectCorners);
            for (int i = 0; i < 4; i++)
                Gizmos.DrawLine(rectCorners[i], rectCorners[(i + 1) % 4]);

            const int samples = 64;

            for (int s = 0; s < _splines.Length; s++)
            {
                var spline = _splines[s];
                if (spline == null)
                    continue;

                Gizmos.color = Color.red;

                Vector2 prevLocal = EvaluatePosition(s, 0f);
                Vector3 prev = rt.TransformPoint(prevLocal);

                for (int i = 1; i <= samples; i++)
                {
                    float t = (float)i / samples;
                    Vector2 currLocal = EvaluatePosition(s, t);
                    Vector3 curr = rt.TransformPoint(currLocal);

                    Gizmos.DrawLine(prev, curr);

                    Vector2 tangentLocal = EvaluateTangent(s, t);
                    Vector3 tangent = rt.TransformDirection(new Vector3(tangentLocal.x, tangentLocal.y, 0f)).normalized;
                    Gizmos.DrawLine(curr, curr + tangent * 10f);

                    prev = curr;
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            Changed?.Invoke();
        }
#endif
    }
}

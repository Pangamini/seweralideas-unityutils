using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.UI;

namespace SeweralIdeas.UnityUtils.SplineMesh
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class SplineGraphic : MaskableGraphic
    {
        protected override void OnEnable()
        {
            base.OnEnable();
            Spline.Changed += OnSplineChanged;
            SplineContainer.SplineAdded += OnSplineContainerChanged;
            SplineContainer.SplineRemoved += OnSplineContainerChanged;
        }

        protected override void OnDisable()
        {
            Spline.Changed -= OnSplineChanged;
            SplineContainer.SplineAdded -= OnSplineContainerChanged;
            SplineContainer.SplineRemoved -= OnSplineContainerChanged;
            base.OnDisable();
        }

        private void OnSplineChanged(Spline spline, int knotIndex, SplineModification modification)
        {
            if (_splineContainer == null) return;
            if (_splineIndex < 0 || _splineIndex >= _splineContainer.Splines.Count) return;
            if (_splineContainer.Splines[_splineIndex] == spline)
                SetVerticesDirty();
        }

        private void OnSplineContainerChanged(SplineContainer container, int index)
        {
            if (container == _splineContainer)
                SetVerticesDirty();
        }

        [Header("Spline Settings")]
        [SerializeField] private SplineContainer _splineContainer;
        [SerializeField] private int _splineIndex = 0;
        [SerializeField] private float _width = 50f;

        [Header("Fill Interval")]
        [SerializeField, Range(0f, 1f)] private float _fillStart = 0f;
        [SerializeField, Range(0f, 1f)] private float _fillEnd = 1f;

        [Header("Caps")]
        [SerializeField] private bool _enableCaps = true;

        [Header("Mesh Quality")]
        [SerializeField] private int _segmentsPerUnit = 10;
        [SerializeField] private int _minSegments = 10;
        [SerializeField] private int _maxSegments = 500;

        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Color32> _colors = new List<Color32>();
        private readonly List<int> _triangles = new List<int>();
        private readonly List<Vector4> _uvs = new List<Vector4>();

        public SplineContainer SplineContainer
        {
            get => _splineContainer;
            set
            {
                _splineContainer = value;
                SetVerticesDirty();
            }
        }

        public int SplineIndex
        {
            get => _splineIndex;
            set
            {
                _splineIndex = value;
                SetVerticesDirty();
            }
        }

        public float Width
        {
            get => _width;
            set
            {
                _width = value;
                SetVerticesDirty();
            }
        }

        public float FillStart
        {
            get => _fillStart;
            set
            {
                _fillStart = Mathf.Clamp01(value);
                SetVerticesDirty();
            }
        }

        public float FillEnd
        {
            get => _fillEnd;
            set
            {
                _fillEnd = Mathf.Clamp01(value);
                SetVerticesDirty();
            }
        }

        public bool EnableCaps
        {
            get => _enableCaps;
            set
            {
                _enableCaps = value;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            if (_splineContainer == null || _splineContainer.Splines.Count == 0)
                return;

            if (_splineIndex < 0 || _splineIndex >= _splineContainer.Splines.Count)
                return;

            if (_fillStart > _fillEnd)
                return;

            if (_fillStart >= _fillEnd && !_enableCaps)
                return;

            Spline spline = _splineContainer.Splines[_splineIndex];
            GenerateSplineMesh(spline, vh);
        }

        private void GenerateSplineMesh(Spline spline, VertexHelper vh)
        {
            _vertices.Clear();
            _colors.Clear();
            _triangles.Clear();
            _uvs.Clear();

            float splineLength = spline.GetLength();
            int segmentCount = Mathf.Clamp(
                Mathf.RoundToInt(splineLength * _segmentsPerUnit),
                _minSegments,
                _maxSegments
            );

            float startDist = _fillStart * splineLength;
            float endDist = _fillEnd * splineLength;

            int vertexOffset = 0;

            // Generate start cap if enabled
            if (_enableCaps)
            {
                float normalizedT = startDist / splineLength;
                Vector3 worldPos = _splineContainer.EvaluatePosition(_splineIndex, normalizedT);
                Vector3 worldTangent = EvaluateTangentSafe(_splineIndex, normalizedT);

                Vector3 localPos = transform.InverseTransformPoint(worldPos);
                Vector3 localTangent = transform.InverseTransformDirection(worldTangent).normalized;
                Vector3 perpendicular = new Vector3(-localTangent.y, localTangent.x, 0f).normalized;

                float radius = _width * 0.5f;

                Vector3 capStart = localPos - localTangent * radius;

                Vector3 capBackLeft = capStart - perpendicular * radius;
                Vector3 capBackRight = capStart + perpendicular * radius;
                Vector3 capFrontLeft = localPos - perpendicular * radius;
                Vector3 capFrontRight = localPos + perpendicular * radius;

                _vertices.Add(capBackLeft);
                _vertices.Add(capBackRight);
                _vertices.Add(capFrontLeft);
                _vertices.Add(capFrontRight);

                _uvs.Add(EncodeCapDistanceUV(capBackLeft - localPos, localTangent, perpendicular, radius, false));
                _uvs.Add(EncodeCapDistanceUV(capBackRight - localPos, localTangent, perpendicular, radius, false));
                _uvs.Add(EncodeCapDistanceUV(capFrontLeft - localPos, localTangent, perpendicular, radius, false));
                _uvs.Add(EncodeCapDistanceUV(capFrontRight - localPos, localTangent, perpendicular, radius, false));

                _colors.Add(color);
                _colors.Add(color);
                _colors.Add(color);
                _colors.Add(color);

                _triangles.Add(0);
                _triangles.Add(2);
                _triangles.Add(1);

                _triangles.Add(1);
                _triangles.Add(2);
                _triangles.Add(3);

                vertexOffset = 4;
            }

            // Generate vertices along the spline (skip when fill range is zero)
            bool hasBody = startDist < endDist;
            if (!hasBody) segmentCount = 0;
            for (int i = 0; i <= segmentCount; i++)
            {
                float t = (float)i / segmentCount;
                float currentDist = Mathf.Lerp(startDist, endDist, t);
                float normalizedT = currentDist / splineLength;

                Vector3 worldPos = _splineContainer.EvaluatePosition(_splineIndex, normalizedT);
                Vector3 worldTangent = EvaluateTangentSafe(_splineIndex, normalizedT);

                Vector3 localPos = transform.InverseTransformPoint(worldPos);
                Vector3 localTangent = transform.InverseTransformDirection(worldTangent).normalized;

                Vector3 perpendicular = new Vector3(-localTangent.y, localTangent.x, 0f).normalized;

                Vector3 leftVertex = localPos - perpendicular * (_width * 0.5f);
                Vector3 rightVertex = localPos + perpendicular * (_width * 0.5f);

                _vertices.Add(leftVertex);
                _vertices.Add(rightVertex);

                _uvs.Add(EncodeDistanceUV(t, -1f));
                _uvs.Add(EncodeDistanceUV(t, 1f));

                _colors.Add(color);
                _colors.Add(color);
            }

            // Generate triangles for spline body
            int splineVertexStart = vertexOffset;
            for (int i = 0; i < segmentCount; i++)
            {
                int baseIndex = splineVertexStart + i * 2;

                _triangles.Add(baseIndex);
                _triangles.Add(baseIndex + 2);
                _triangles.Add(baseIndex + 1);

                _triangles.Add(baseIndex + 1);
                _triangles.Add(baseIndex + 2);
                _triangles.Add(baseIndex + 3);
            }

            // Generate end cap if enabled
            if (_enableCaps)
            {
                float normalizedT = endDist / splineLength;
                Vector3 worldPos = _splineContainer.EvaluatePosition(_splineIndex, normalizedT);
                Vector3 worldTangent = EvaluateTangentSafe(_splineIndex, normalizedT);

                Vector3 localPos = transform.InverseTransformPoint(worldPos);
                Vector3 localTangent = transform.InverseTransformDirection(worldTangent).normalized;
                Vector3 perpendicular = new Vector3(-localTangent.y, localTangent.x, 0f).normalized;

                float radius = _width * 0.5f;

                Vector3 capEnd = localPos + localTangent * radius;

                Vector3 capFrontLeft = localPos - perpendicular * radius;
                Vector3 capFrontRight = localPos + perpendicular * radius;
                Vector3 capBackLeft = capEnd - perpendicular * radius;
                Vector3 capBackRight = capEnd + perpendicular * radius;

                int endCapStart = _vertices.Count;

                _vertices.Add(capFrontLeft);
                _vertices.Add(capFrontRight);
                _vertices.Add(capBackLeft);
                _vertices.Add(capBackRight);

                _uvs.Add(EncodeCapDistanceUV(capFrontLeft - localPos, localTangent, perpendicular, radius, true));
                _uvs.Add(EncodeCapDistanceUV(capFrontRight - localPos, localTangent, perpendicular, radius, true));
                _uvs.Add(EncodeCapDistanceUV(capBackLeft - localPos, localTangent, perpendicular, radius, true));
                _uvs.Add(EncodeCapDistanceUV(capBackRight - localPos, localTangent, perpendicular, radius, true));

                _colors.Add(color);
                _colors.Add(color);
                _colors.Add(color);
                _colors.Add(color);

                _triangles.Add(endCapStart);
                _triangles.Add(endCapStart + 2);
                _triangles.Add(endCapStart + 1);

                _triangles.Add(endCapStart + 1);
                _triangles.Add(endCapStart + 2);
                _triangles.Add(endCapStart + 3);
            }

            // Add to vertex helper
            for (int i = 0; i < _vertices.Count; i++)
            {
                vh.AddVert(_vertices[i], _colors[i], _uvs[i]);
            }

            for (int i = 0; i < _triangles.Count; i += 3)
            {
                vh.AddTriangle(_triangles[i], _triangles[i + 1], _triangles[i + 2]);
            }
        }

        private Vector3 EvaluateTangentSafe(int index, float t)
        {
            Vector3 tangent = _splineContainer.EvaluateTangent(index, t);
            if (tangent.sqrMagnitude < 0.0001f)
            {
                const float epsilon = 0.001f;
                if (t < 0.5f)
                    tangent = _splineContainer.EvaluateTangent(index, t + epsilon);
                else
                    tangent = _splineContainer.EvaluateTangent(index, t - epsilon);
            }
            if (tangent.sqrMagnitude < 0.0001f)
            {
                const float epsilon = 0.001f;
                float tA = Mathf.Clamp01(t - epsilon);
                float tB = Mathf.Clamp01(t + epsilon);
                tangent = _splineContainer.EvaluatePosition(index, tB) - _splineContainer.EvaluatePosition(index, tA);
            }
            return tangent;
        }

        private static Vector4 EncodeDistanceUV(float alongSpline, float perpendicular)
        {
            // x: distance along spline (0-1)
            // y: perpendicular distance (-1 to 1)
            // z: region indicator (0 = start cap, 0.5 = main body, 1 = end cap)
            return new Vector4(alongSpline, perpendicular, 0.5f, 0f);
        }

        private static Vector4 EncodeCapDistanceUV(Vector3 offsetFromEndpoint, Vector3 tangent, Vector3 perpendicular, float radius, bool isEndCap)
        {
            // x: offset along tangent / radius (-1 to 1)
            // y: offset along perpendicular / radius (-1 to 1)
            // z: region indicator (0 = start cap, 1 = end cap)
            float alongTangent = Vector3.Dot(offsetFromEndpoint, tangent) / radius;
            float alongPerpendicular = Vector3.Dot(offsetFromEndpoint, perpendicular) / radius;

            return new Vector4(alongTangent, alongPerpendicular, isEndCap ? 1f : 0f, 0f);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            if (_fillStart > _fillEnd)
            {
                _fillStart = _fillEnd;
            }

            SetVerticesDirty();
        }
#endif
    }
}

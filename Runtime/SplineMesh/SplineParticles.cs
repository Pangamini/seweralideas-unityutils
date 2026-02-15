using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.UI;

namespace SeweralIdeas.UnityUtils.SplineMesh
{
    public class SplineParticles : MonoBehaviour
    {
        [Header("Spline Settings")]
        [SerializeField] private SplineContainer _splineContainer;
        [SerializeField] private int _splineIndex = 0;

        [Header("Interval")]
        [SerializeField, Range(0f, 1f)] private float _fillStart = 0f;
        [SerializeField, Range(0f, 1f)] private float _fillEnd = 1f;

        [Header("Particles")]
        [SerializeField] private Graphic _prefab;
        [SerializeField] private float    _spacing = 50f;
        [SerializeField] private float    _speed   = 100f;
        [SerializeField] private Gradient _colorOverFill = new();

        private readonly List<Graphic> _instances = new List<Graphic>();
        private float _offset;
        private float _intervalLength;
        private bool _dirty;

        public SplineContainer SplineContainer
        {
            get => _splineContainer;
            set { _splineContainer = value; SetDirty(); }
        }

        public int SplineIndex
        {
            get => _splineIndex;
            set { _splineIndex = value; SetDirty(); }
        }

        public float FillStart
        {
            get => _fillStart;
            set { _fillStart = Mathf.Clamp01(value); SetDirty(); }
        }

        public float FillEnd
        {
            get => _fillEnd;
            set { _fillEnd = Mathf.Clamp01(value); SetDirty(); }
        }

        public Graphic Prefab
        {
            get => _prefab;
            set { _prefab = value; SetDirty(); }
        }

        public float Spacing
        {
            get => _spacing;
            set { _spacing = value; SetDirty(); }
        }

        public float Speed
        {
            get => _speed;
            set => _speed = value;
        }

        private void SetDirty() => _dirty = true;

        private void OnEnable()
        {
            Spline.Changed += OnSplineChanged;
            SplineContainer.SplineAdded += OnSplineContainerChanged;
            SplineContainer.SplineRemoved += OnSplineContainerChanged;
            SetDirty();
        }

        private void OnDisable()
        {
            Spline.Changed -= OnSplineChanged;
            SplineContainer.SplineAdded -= OnSplineContainerChanged;
            SplineContainer.SplineRemoved -= OnSplineContainerChanged;
            SetInstanceCount(0);
        }

        private void OnSplineChanged(Spline spline, int knotIndex, SplineModification modification)
        {
            if (_splineContainer == null) return;
            if (_splineIndex < 0 || _splineIndex >= _splineContainer.Splines.Count) return;
            if (_splineContainer.Splines[_splineIndex] == spline)
                SetDirty();
        }

        private void OnSplineContainerChanged(SplineContainer container, int index)
        {
            if (container == _splineContainer)
                SetDirty();
        }

        private void LateUpdate()
        {
            if (_dirty)
            {
                _dirty = false;
                Refresh();
            }

            if (_splineContainer == null || _instances.Count == 0 || _intervalLength <= 0f)
                return;

            _offset += _speed * Time.deltaTime;

            UpdateParticles();
        }

        private void Refresh()
        {
            if (_splineContainer == null || _splineContainer.Splines.Count == 0
                || _splineIndex < 0 || _splineIndex >= _splineContainer.Splines.Count
                || _fillStart >= _fillEnd || _prefab == null || _spacing <= 0f)
            {
                _intervalLength = 0f;
                SetInstanceCount(0);
                return;
            }

            Spline spline = _splineContainer.Splines[_splineIndex];
            float splineLength = spline.GetLength();
            _intervalLength = (_fillEnd - _fillStart) * splineLength;

            if (_intervalLength <= 0f)
            {
                SetInstanceCount(0);
                return;
            }

            int count = Mathf.Max(1, Mathf.FloorToInt(_intervalLength / _spacing));
            SetInstanceCount(count);
        }

        private void SetInstanceCount(int count)
        {
            // Remove excess
            while (_instances.Count > count)
            {
                int last = _instances.Count - 1;
                if (_instances[last] != null)
                {
                    if (Application.isPlaying)
                        Destroy(_instances[last].gameObject);
                    else
                        DestroyImmediate(_instances[last].gameObject);
                }
                _instances.RemoveAt(last);
            }

            // Add missing
            while (_instances.Count < count)
            {
                Graphic instance = Instantiate(_prefab, transform);
                instance.gameObject.SetActive(true);
                _instances.Add(instance);
            }
        }

        private void UpdateParticles()
        {
            int count = _instances.Count;
            if (count == 0) return;

            float splineLength = _splineContainer.Splines[_splineIndex].GetLength();
            float evenSpacing = _intervalLength / count;

            // Wrap offset to interval to avoid floating point drift
            float wrappedOffset = _offset % _intervalLength;
            if (wrappedOffset < 0f) wrappedOffset += _intervalLength;

            for (int i = 0; i < count; i++)
            {
                float dist = (i * evenSpacing + wrappedOffset) % _intervalLength;

                // Position within the interval (0-1)
                float fillT = dist / _intervalLength;

                // Convert to normalized spline position
                float normalizedT = _fillStart + dist / splineLength;

                Vector3 worldPos = _splineContainer.EvaluatePosition(_splineIndex, normalizedT);
                Vector3 worldTangent = EvaluateTangentSafe(_splineIndex, normalizedT);

                Transform instanceTransform = _instances[i].transform;
                instanceTransform.position = worldPos;

                if (worldTangent.sqrMagnitude > 0.0001f)
                {
                    float angle = Mathf.Atan2(worldTangent.y, worldTangent.x) * Mathf.Rad2Deg;
                    instanceTransform.rotation = Quaternion.Euler(0f, 0f, angle);
                }

                _instances[i].color = _colorOverFill.Evaluate(fillT);
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

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_fillStart > _fillEnd)
                _fillStart = _fillEnd;

            if (_spacing < 0.01f)
                _spacing = 0.01f;

            if (isActiveAndEnabled)
                SetDirty();
        }
#endif
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SeweralIdeas.UnityUtils.SplineMesh
{
    public class SplineParticles : MonoBehaviour
    {
        [Header("Spline Settings")]
        [SerializeField] private RectSplineContainer _splineContainer;
        [SerializeField] private int _splineIndex = 0;

        [Header("Interval")]
        [SerializeField, Range(0f, 1f)] private float _fillStart = 0f;
        [SerializeField, Range(0f, 1f)] private float _fillEnd = 1f;

        [Header("Particles")]
        [SerializeField] private Graphic _prefab;
        [SerializeField] private float    _spacing       = 0.1f;
        [SerializeField] private float    _particleSize  = 0.05f;
        [SerializeField] private float    _speed         = 100f;
        [SerializeField] private Gradient _colorOverFill = new();

        private readonly List<Graphic> _instances = new List<Graphic>();
        private          float         _offset;
        private          float         _intervalLength;
        private          bool          _dirty;

        public RectSplineContainer SplineContainer
        {
            get => _splineContainer;
            set
            {
                UnsubscribeChanged();
                _splineContainer = value;
                SubscribeChanged();
                SetDirty();
            }
        }

        public int SplineIndex
        {
            get => _splineIndex;
            set
            {
                _splineIndex = value;
                SetDirty();
            }
        }

        public float FillStart
        {
            get => _fillStart;
            set
            {
                _fillStart = Mathf.Clamp01(value);
                SetDirty();
            }
        }

        public float FillEnd
        {
            get => _fillEnd;
            set
            {
                _fillEnd = Mathf.Clamp01(value);
                SetDirty();
            }
        }

        public Graphic Prefab
        {
            get => _prefab;
            set
            {
                _prefab = value;
                SetDirty();
            }
        }

        public float Spacing
        {
            get => _spacing;
            set
            {
                _spacing = value;
                SetDirty();
            }
        }

        public float ParticleSize
        {
            get => _particleSize;
            set
            {
                _particleSize = value;
                SetDirty();
            }
        }

        public float Speed
        {
            get => _speed;
            set => _speed = value;
        }

        private void SetDirty() => _dirty = true;

        private void OnEnable()
        {
            SubscribeChanged();
            SetDirty();
        }

        private void OnDisable()
        {
            UnsubscribeChanged();
            SetInstanceCount(0);
        }

        private void SubscribeChanged()
        {
            if(_splineContainer != null)
                _splineContainer.Changed += OnContainerChanged;
        }

        private void UnsubscribeChanged()
        {
            if(_splineContainer != null)
                _splineContainer.Changed -= OnContainerChanged;
        }

        private void OnContainerChanged()
        {
            SetDirty();
        }

        private void LateUpdate()
        {
            if(_dirty)
            {
                _dirty = false;
                Refresh();
            }

            if(_splineContainer == null || _instances.Count == 0 || _intervalLength <= 0f)
                return;

            _offset += _speed * Time.deltaTime;

            UpdateParticles();
        }

        private void Refresh()
        {
            if(_splineContainer == null || _splineContainer.SplineCount == 0
                || _splineIndex < 0 || _splineIndex >= _splineContainer.SplineCount
                || _fillStart >= _fillEnd || _prefab == null || _spacing <= 0f || _particleSize < 0f)
            {
                _intervalLength = 0f;
                SetInstanceCount(0);
                return;
            }

            float splineLength = _splineContainer.GetSplineLength(_splineIndex);
            _intervalLength = (_fillEnd - _fillStart) * splineLength;

            if(_intervalLength <= 0f)
            {
                SetInstanceCount(0);
                return;
            }

            float spacingPixels = _splineContainer.NormalizedScalarToRectLocal(_spacing);
            int count = Mathf.Max(1, Mathf.FloorToInt(_intervalLength / spacingPixels));
            SetInstanceCount(count);
        }

        private void SetInstanceCount(int count)
        {
            // Remove excess
            while(_instances.Count > count)
            {
                int last = _instances.Count - 1;
                if(_instances[last] != null)
                {
                    if(Application.isPlaying)
                        Destroy(_instances[last].gameObject);
                    else
                        DestroyImmediate(_instances[last].gameObject);
                }
                _instances.RemoveAt(last);
            }

            // Add missing
            while(_instances.Count < count)
            {
                Graphic instance = Instantiate(_prefab, transform);
                instance.gameObject.SetActive(true);
                _instances.Add(instance);
            }
        }

        private void UpdateParticles()
        {
            int count = _instances.Count;
            if(count == 0) return;

            float splineLength = _splineContainer.GetSplineLength(_splineIndex);
            float evenSpacing = _intervalLength / count;
            float sizePixels = _splineContainer.NormalizedScalarToRectLocal(_particleSize);
            Transform containerTransform = _splineContainer.RectTransform;

            // Wrap offset to interval to avoid floating point drift
            float wrappedOffset = _offset % _intervalLength;
            if(wrappedOffset < 0f) wrappedOffset += _intervalLength;

            for( int i = 0; i < count; i++ )
            {
                float dist = (i * evenSpacing + wrappedOffset) % _intervalLength;

                // Position within the interval (0-1)
                float fillT = dist / _intervalLength;

                // Convert to normalized spline position
                float normalizedT = _fillStart + dist / splineLength;

                Vector2 rectLocal = _splineContainer.EvaluatePosition(_splineIndex, normalizedT);
                Vector2 tangentLocal = EvaluateTangentSafe(normalizedT);

                Transform instanceTransform = _instances[i].transform;
                instanceTransform.position = containerTransform.TransformPoint(rectLocal);

                if(tangentLocal.sqrMagnitude > 0.0001f)
                {
                    Vector3 worldTangent = containerTransform.TransformDirection(new Vector3(tangentLocal.x, tangentLocal.y, 0f));
                    float angle = Mathf.Atan2(worldTangent.y, worldTangent.x) * Mathf.Rad2Deg;
                    instanceTransform.rotation = Quaternion.Euler(0f, 0f, angle);
                }

                instanceTransform.localScale = new Vector3(sizePixels, sizePixels, 1f);
                _instances[i].color = _colorOverFill.Evaluate(fillT);
            }
        }

        private Vector2 EvaluateTangentSafe(float t)
        {
            Vector2 tangent = _splineContainer.EvaluateTangent(_splineIndex, t);
            if(tangent.sqrMagnitude < 0.0001f)
            {
                const float epsilon = 0.001f;
                if(t < 0.5f)
                    tangent = _splineContainer.EvaluateTangent(_splineIndex, t + epsilon);
                else
                    tangent = _splineContainer.EvaluateTangent(_splineIndex, t - epsilon);
            }
            if(tangent.sqrMagnitude < 0.0001f)
            {
                const float epsilon = 0.001f;
                float tA = Mathf.Clamp01(t - epsilon);
                float tB = Mathf.Clamp01(t + epsilon);
                tangent = _splineContainer.EvaluatePosition(_splineIndex, tB)
                    - _splineContainer.EvaluatePosition(_splineIndex, tA);
            }
            return tangent;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if(_fillStart > _fillEnd)
                _fillStart = _fillEnd;

            if(_spacing < 0.001f)
                _spacing = 0.001f;

            if(_particleSize < 0f)
                _particleSize = 0f;

            if(isActiveAndEnabled)
                SetDirty();
        }
#endif
    }
}
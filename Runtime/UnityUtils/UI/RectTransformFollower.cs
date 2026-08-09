using UnityEngine;
using UnityEngine.UI;

namespace SeweralIdeas.UnityUtils
{
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class RectTransformFollower : MonoBehaviour, ILayoutIgnorer
    {
        [SerializeField] private RectTransform _destination;
        [SerializeField] private float         _smoothTime = 0.2f;
        [SerializeField] private float         _maxSpeed = Mathf.Infinity;
        [SerializeField] private bool          _snapOnEnable = true;

        private RectTransform _rectTransform;
        private Vector3       _positionVelocity;
        private Vector2       _sizeVelocity;
        private Vector3       _rotationVelocity;

        private DrivenRectTransformTracker _tracker = new();

        public bool ignoreLayout => true;

        public RectTransform Destination
        {
            get => _destination;
            set => _destination = value;
        }

        protected void Awake()
        {
            _rectTransform = (RectTransform)transform;
        }
        
        protected void OnEnable()
        {
            _rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _rectTransform.anchorMax = new Vector2(0.5f, 0.5f);

            _tracker.Clear();
            _tracker.Add(
                this,
                _rectTransform,
                DrivenTransformProperties.Anchors | DrivenTransformProperties.AnchoredPosition3D | DrivenTransformProperties.SizeDelta | DrivenTransformProperties.Rotation);

            if(_snapOnEnable)
                SnapToDestination();
        }

        protected void OnDisable()
        {
            _tracker.Clear();
        }

        public void SnapToDestination()
        {
            if(!_destination)
                return;

            _rectTransform.sizeDelta   = CalculateTargetSizeDelta();
            _rectTransform.eulerAngles = _destination.eulerAngles;
            _rectTransform.position    = CalculateTargetPosition();

            _sizeVelocity     = Vector2.zero;
            _rotationVelocity = Vector3.zero;
            _positionVelocity = Vector3.zero;
        }

        protected void Update()
        {
            if(!_destination)
                return;

            float dt = Time.deltaTime;

            // Size and rotation are updated first: the pivot-compensated position
            // target below depends on this frame's (post-smoothing) size and rotation.
            _rectTransform.sizeDelta = Vector2.SmoothDamp(
                _rectTransform.sizeDelta,
                CalculateTargetSizeDelta(),
                ref _sizeVelocity,
                _smoothTime,
                _maxSpeed,
                dt);

            Vector3 currentEuler = _rectTransform.eulerAngles;
            Vector3 targetEuler  = _destination.eulerAngles;
            currentEuler.x = Mathf.SmoothDampAngle(currentEuler.x, targetEuler.x, ref _rotationVelocity.x, _smoothTime, _maxSpeed, dt);
            currentEuler.y = Mathf.SmoothDampAngle(currentEuler.y, targetEuler.y, ref _rotationVelocity.y, _smoothTime, _maxSpeed, dt);
            currentEuler.z = Mathf.SmoothDampAngle(currentEuler.z, targetEuler.z, ref _rotationVelocity.z, _smoothTime, _maxSpeed, dt);
            _rectTransform.eulerAngles = currentEuler;

            // RectTransform.position is the world position of the pivot, not the
            // rect's center, so with mismatched pivots two rects can share a
            // position yet not overlap. Compensate by aiming for the world-space
            // position our own pivot would need in order for both rects' centers
            // to coincide.
            _rectTransform.position = Vector3.SmoothDamp(
                _rectTransform.position,
                CalculateTargetPosition(),
                ref _positionVelocity,
                _smoothTime,
                _maxSpeed,
                dt);
        }

        private Vector2 CalculateTargetSizeDelta()
        {
            Vector2 targetWorldSize = Vector2.Scale(_destination.rect.size, _destination.lossyScale);
            Vector2 selfScale       = _rectTransform.lossyScale;
            return new Vector2(targetWorldSize.x / selfScale.x, targetWorldSize.y / selfScale.y);
        }

        private Vector3 CalculateTargetPosition()
        {
            Vector3 destinationCenterOffset = _destination.TransformVector(
                new Vector3((0.5f - _destination.pivot.x) * _destination.rect.width, (0.5f - _destination.pivot.y) * _destination.rect.height, 0f));
            Vector3 targetCenter = _destination.position + destinationCenterOffset;

            Vector3 selfCenterOffset = _rectTransform.TransformVector(
                new Vector3((0.5f - _rectTransform.pivot.x) * _rectTransform.rect.width, (0.5f - _rectTransform.pivot.y) * _rectTransform.rect.height, 0f));
            return targetCenter - selfCenterOffset;
        }
    }
}

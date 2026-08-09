using System;
using UnityEngine;
using UnityEngine.UI;

namespace SeweralIdeas.UnityUtils
{
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class RectTransformFollower : MonoBehaviour, ILayoutIgnorer
    {
        [SerializeField] private RectTransform _destination;
        [SerializeField] private float         _smoothTime   = 0.2f;
        [SerializeField] private float         _maxSpeed     = Mathf.Infinity;
        [SerializeField] private bool          _snapOnEnable = true;
        [SerializeField] private UpdateMode    _updateMode;

        public enum UpdateMode : byte
        {
            Normal,
            Unscaled
        }
        
        private RectTransform _rectTransform;
        private Vector3       _center;
        private Vector3       _centerVelocity;
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

            // Establish the smoothed center from wherever we currently sit, so that if
            // _snapOnEnable is false, Update's first SmoothDamp step starts from the
            // truth instead of (0,0,0).
            _center = _rectTransform.position + CalculateSelfCenterOffset();

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

            _center = CalculateDestinationCenter();
            _rectTransform.position = _center - CalculateSelfCenterOffset();

            _sizeVelocity     = Vector2.zero;
            _rotationVelocity = Vector3.zero;
            _centerVelocity   = Vector3.zero;
        }

        protected void Update()
        {
            if(!_destination)
                return;

            float dt = _updateMode switch
            {
                UpdateMode.Normal => Time.deltaTime,
                UpdateMode.Unscaled => Time.unscaledDeltaTime,
                _ => throw new ArgumentOutOfRangeException()
            };

            // Rotation first: the pivot<->center conversion below needs it.
            Vector3 currentEuler = _rectTransform.eulerAngles;
            Vector3 targetEuler  = _destination.eulerAngles;
            currentEuler.x = Mathf.SmoothDampAngle(currentEuler.x, targetEuler.x, ref _rotationVelocity.x, _smoothTime, _maxSpeed, dt);
            currentEuler.y = Mathf.SmoothDampAngle(currentEuler.y, targetEuler.y, ref _rotationVelocity.y, _smoothTime, _maxSpeed, dt);
            currentEuler.z = Mathf.SmoothDampAngle(currentEuler.z, targetEuler.z, ref _rotationVelocity.z, _smoothTime, _maxSpeed, dt);
            _rectTransform.eulerAngles = currentEuler;

            _rectTransform.sizeDelta = Vector2.SmoothDamp(
                _rectTransform.sizeDelta,
                CalculateTargetSizeDelta(),
                ref _sizeVelocity,
                _smoothTime,
                _maxSpeed,
                dt);

            // Smooth the rect's CENTER toward the destination's exact (never-smoothed)
            // center - a fixed target every frame. Deriving the position SmoothDamp's
            // target from our own still-interpolating size instead (as a previous
            // version did) makes the target itself lag/move each frame, which
            // compounds into a floaty double-smoothed result whenever pivot != 0.5;
            // that term is only zero, and the bug invisible, at pivot 0.5.
            _center = Vector3.SmoothDamp(
                _center,
                CalculateDestinationCenter(),
                ref _centerVelocity,
                _smoothTime,
                _maxSpeed,
                dt);

            // Converting the smoothed center back to a pivot position is a plain
            // algebraic step using this frame's already-smoothed size/rotation - not
            // itself part of the smoothing - so it can't reintroduce the coupling above.
            _rectTransform.position = _center - CalculateSelfCenterOffset();
        }

        private Vector2 CalculateTargetSizeDelta()
        {
            Vector2 targetWorldSize = Vector2.Scale(_destination.rect.size, _destination.lossyScale);
            Vector2 selfScale       = _rectTransform.lossyScale;
            return new Vector2(targetWorldSize.x / selfScale.x, targetWorldSize.y / selfScale.y);
        }

        private Vector3 CalculateDestinationCenter()
        {
            Vector3 offset = _destination.TransformVector(
                new Vector3((0.5f - _destination.pivot.x) * _destination.rect.width, (0.5f - _destination.pivot.y) * _destination.rect.height, 0f));
            return _destination.position + offset;
        }

        private Vector3 CalculateSelfCenterOffset()
        {
            return _rectTransform.TransformVector(
                new Vector3((0.5f - _rectTransform.pivot.x) * _rectTransform.rect.width, (0.5f - _rectTransform.pivot.y) * _rectTransform.rect.height, 0f));
        }
    }
}

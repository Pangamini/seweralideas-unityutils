using System;
using UnityEngine;
using UnityEngine.UI;

namespace SeweralIdeas.Tweening
{
    /// <summary>
    /// Moves a RectTransform between two poses, given as two other RectTransforms: <c>Off</c> at progress 0 and <c>On</c> at 1.
    /// Everything that makes the pose is interpolated: anchors, pivot, anchored position, size, rotation and scale. While it is enabled
    /// the component drives these fields of the target, which the Inspector then shows as locked (<see cref="DrivenRectTransformTracker"/>).
    /// The poses are the fields of the two RectTransforms as they are, so they should have the same parent as the target (or be laid
    /// out in the same space), and are best kept inactive or hidden.
    /// </summary>
    public class TweenRectTransform : TweenComponent
    {
        /// <summary>The properties of the pose that can be tweened.</summary>
        [Flags]
        public enum Properties
        {
            Anchors  = 1 << 0,
            Pivot    = 1 << 1,
            Position = 1 << 2,
            Size     = 1 << 3,
            Rotation = 1 << 4,
            Scale    = 1 << 5,
            All      = Anchors | Pivot | Position | Size | Rotation | Scale
        }

        [Tooltip("What is tweened. The others are left alone, and not locked in the target's Inspector. The position is relative to the anchors and the pivot, so if Position is on and Anchors or Pivot is off, the poses should have the target's anchors and pivot.")]
        [EnumButtons]
        [SerializeField] private Properties _properties = Properties.All;

        [SerializeField] private RectTransform _target;
        [Tooltip("The pose at progress 0.")]
        [SerializeField] private RectTransform _off;
        [Tooltip("The pose at progress 1.")]
        [SerializeField] private RectTransform _on;

        [Tooltip("Apply the pose every frame, not only when the tween's value changes: for when the Off and On poses move (a layout, an animation) or something else may have changed the target.")]
        [SerializeField] private bool _continuous;

        private DrivenRectTransformTracker _tracker;

        protected virtual void LateUpdate()
        {
            if(_continuous && Source != null)
                Apply(Source.Progress);
        }

        public RectTransform Target => _target;
        public RectTransform Off    => _off;
        public RectTransform On     => _on;

        protected virtual void OnEnable()
        {
            RegisterDriven();
            if(Source != null)
                Apply(Source.Progress);
        }

        protected virtual void OnDisable()
        {
            _tracker.Clear();
        }

        // Also in the editor when the references change, so the target is shown as driven as soon as it is set
        protected virtual void OnValidate()
        {
            if(isActiveAndEnabled)
                RegisterDriven();
        }

        private void RegisterDriven()
        {
            _tracker.Clear();
            if(_target == null)
                return;

            DrivenTransformProperties driven = 0;
            if((_properties & Properties.Anchors) != 0)  driven |= DrivenTransformProperties.Anchors;
            if((_properties & Properties.Pivot) != 0)    driven |= DrivenTransformProperties.Pivot;
            if((_properties & Properties.Position) != 0) driven |= DrivenTransformProperties.AnchoredPosition3D;
            if((_properties & Properties.Size) != 0)     driven |= DrivenTransformProperties.SizeDelta;
            if((_properties & Properties.Rotation) != 0) driven |= DrivenTransformProperties.Rotation;
            if((_properties & Properties.Scale) != 0)    driven |= DrivenTransformProperties.Scale;

            if(driven != 0)
                _tracker.Add(this, _target, driven);
        }

        protected override void OnValueChanged(float progress)
        {
            base.OnValueChanged(progress);
            Apply(progress);
        }

        private void Apply(float progress)
        {
            if(_target == null || _off == null || _on == null)
                return;

            // Unclamped, so that an easing that overshoots does too. Anchors and pivot first: the position is relative to them.
            if((_properties & Properties.Anchors) != 0)
            {
                _target.anchorMin = Vector2.LerpUnclamped(_off.anchorMin, _on.anchorMin, progress);
                _target.anchorMax = Vector2.LerpUnclamped(_off.anchorMax, _on.anchorMax, progress);
            }

            if((_properties & Properties.Pivot) != 0)
                _target.pivot = Vector2.LerpUnclamped(_off.pivot, _on.pivot, progress);

            // The 3D position, so that Z is driven too
            if((_properties & Properties.Position) != 0)
                _target.anchoredPosition3D = Vector3.LerpUnclamped(_off.anchoredPosition3D, _on.anchoredPosition3D, progress);

            if((_properties & Properties.Size) != 0)
            {
                Vector2 sizeDelta = Vector2.LerpUnclamped(_off.sizeDelta, _on.sizeDelta, progress);
                if(_target.sizeDelta != sizeDelta)
                {
                    _target.sizeDelta = sizeDelta;
                    LayoutRebuilder.MarkLayoutForRebuild(_target);
                }
            }

            if((_properties & Properties.Rotation) != 0)
                _target.localRotation = Quaternion.SlerpUnclamped(_off.localRotation, _on.localRotation, progress);

            if((_properties & Properties.Scale) != 0)
                _target.localScale = Vector3.LerpUnclamped(_off.localScale, _on.localScale, progress);
        }
    }
}

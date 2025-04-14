using System;
using SeweralIdeas.UnityUtils;
using SeweralIdeas.UnityUtils.Drawers;
using UnityEngine;
namespace SeweralIdeas.UnityUtils
{
    public class TweenRectPos : TweenComponent
    {
        public enum Direction
        {
            Left,
            Right,
            Up,
            Down
        }

        [SerializeField] private Direction     _hideDirection;
        [SerializeField] private RectTransform _rectTransform;
        [SerializeField] private bool          _overrideTravelDistance = false;
        [Condition(nameof(_travelDistance))]
        [SerializeField] private float         _travelDistance;
        private                  Vector2       _startAnchoredPos;

        protected override void Start()
        {
            _startAnchoredPos = _rectTransform.anchoredPosition;
            base.Start();
        }

        protected override void OnValueChanged(float progress)
        {
            base.OnValueChanged(progress);
            var anchorOffset = Vector2.zero;

            Vector2 hideOffset = (_hideDirection, _overrideTravelDistance) switch
            {
                (Direction.Left, false) => new(-_rectTransform.rect.width, 0),
                (Direction.Right, false) => new(_rectTransform.rect.width, 0),
                (Direction.Up, false) => new(0, -_rectTransform.rect.height),
                (Direction.Down, false) => new(0, _rectTransform.rect.height),
                (Direction.Left, true) => new(-_travelDistance, 0),
                (Direction.Right, true) => new(_travelDistance, 0),
                (Direction.Up, true) => new(0, -_travelDistance),
                (Direction.Down, true) => new(0, _travelDistance),
                _ => throw new ArgumentOutOfRangeException()
            };

            _rectTransform.anchoredPosition = _startAnchoredPos + anchorOffset + hideOffset * (1 - progress);
        }
    }
}

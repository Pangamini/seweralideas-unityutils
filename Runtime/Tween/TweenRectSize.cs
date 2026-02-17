using System;
using SeweralIdeas.UnityUtils;
using UnityEngine;
using UnityEngine.UI;
namespace SeweralIdeas.UnityUtils
{
    public class TweenRectSize : TweenComponent
    {
        [Flags]
        public enum Axis
        {
            Horizontal = 1 << 0,
            Vertical = 1 << 1,
        }

        [SerializeField] private Axis          _axis;
        [SerializeField] private RectTransform _rectTransform;
        private                  Vector2       _startSize;

        protected override void Start()
        {
            _startSize = _rectTransform.sizeDelta;
            base.Start();
        }

        protected override void OnValueChanged(float progress)
        {
            base.OnValueChanged(progress);
            var sizeDelta = _rectTransform.sizeDelta;
            var destination = _startSize * progress;

            if((_axis & Axis.Horizontal) != 0)
                sizeDelta.x = destination.x;
            if((_axis & Axis.Vertical) != 0)
                sizeDelta.y = destination.y;

            if(_rectTransform.sizeDelta != sizeDelta)
            {
                _rectTransform.sizeDelta = sizeDelta;
                LayoutRebuilder.MarkLayoutForRebuild(_rectTransform);
            }
        }
    }
}

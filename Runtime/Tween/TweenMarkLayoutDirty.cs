using UnityEngine;
using UnityEngine.UI;

namespace SeweralIdeas.Tweening
{
    public class TweenMarkLayoutDirty : TweenComponent
    {
        [SerializeField] private RectTransform _target;

        protected void Reset()
        {
            _target = GetComponent<RectTransform>();
        }

        protected override void OnValueChanged(float progress)
        {
            base.OnValueChanged(progress);
            LayoutRebuilder.MarkLayoutForRebuild(_target);
        }
    }
}

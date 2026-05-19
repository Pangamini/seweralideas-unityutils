using UnityEngine;
using UnityEngine.UI;

namespace SeweralIdeas.UnityUtils.UI
{
    public class VerticalLayoutGroupWithNavigation : VerticalLayoutGroup
    {
        [SerializeField] private Selectable _navigateLeft;
        [SerializeField] private Selectable _navigateRight;

        public override void SetLayoutVertical()
        {
            base.SetLayoutVertical();
            VerticalNavigationSetter.SetNavigation(rectChildren, _navigateLeft, _navigateRight);
        }
    }
}
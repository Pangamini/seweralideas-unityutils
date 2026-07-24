using UnityEngine;
using UnityEngine.UI;

namespace SeweralIdeas.UnityUtils.UI
{
    public class HorizontalLayoutGroupWithNavigation : HorizontalLayoutGroup
    {
        [SerializeField] private Selectable _navigateUp;
        [SerializeField] private Selectable _navigateDown;

        public override void SetLayoutHorizontal()
        {
            base.SetLayoutHorizontal();
            HorizontalNavigationSetter.SetNavigation(rectChildren, _navigateUp, _navigateDown);
        }
    }
}
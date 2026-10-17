using SeweralIdeas.UnityUtils.Drawers;
using UnityEngine;
using UnityEngine.UI;

namespace SeweralIdeas.UnityUtils.UI
{
    public class VerticalLayoutGroupWithNavigation : VerticalLayoutGroup
    {
        [Condition(nameof(Inherits), Invert = true)]
        [SerializeField] private Selectable _navigateLeft;
        [Condition(nameof(Inherits), Invert = true)]
        [SerializeField] private Selectable _navigateRight;

        [Tooltip("Left and Right lead from the elements of this group to where they lead from this selectable (what it finds: " +
                 "its explicit Left and Right, or the automatic ones). Replaces the two Navigate fields above, which are then not used.")]
        [SerializeField] private Selectable _inheritFrom;

        private bool Inherits => _inheritFrom; // for the Condition of the fields that it replaces

        private Selectable _appliedLeft;
        private Selectable _appliedRight;

        public override void SetLayoutVertical()
        {
            base.SetLayoutVertical();

            if(_inheritFrom)
            {
                _appliedLeft = InheritedNavigation.Left(_inheritFrom, transform);
                _appliedRight = InheritedNavigation.Right(_inheritFrom, transform);
            }
            else
            {
                _appliedLeft = _navigateLeft;
                _appliedRight = _navigateRight;
            }
            VerticalNavigationSetter.SetNavigation(rectChildren, _appliedLeft, _appliedRight);
        }

        // See HorizontalLayoutGroupWithNavigation
        protected virtual void LateUpdate()
        {
            if(!_inheritFrom)
                return;

            if(InheritedNavigation.Left(_inheritFrom, transform) != _appliedLeft
               || InheritedNavigation.Right(_inheritFrom, transform) != _appliedRight)
                SetDirty();
        }
    }
}

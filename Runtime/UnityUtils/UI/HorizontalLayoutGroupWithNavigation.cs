using SeweralIdeas.UnityUtils.Drawers;
using UnityEngine;
using UnityEngine.UI;

namespace SeweralIdeas.UnityUtils.UI
{
    public class HorizontalLayoutGroupWithNavigation : HorizontalLayoutGroup
    {
        [Condition(nameof(Inherits), Invert = true)]
        [SerializeField] private Selectable _navigateUp;
        [Condition(nameof(Inherits), Invert = true)]
        [SerializeField] private Selectable _navigateDown;

        [Tooltip("Up and Down lead from the elements of this group to where they lead from this selectable (what it finds: " +
                 "its explicit Up and Down, or the automatic ones). Replaces the two Navigate fields above, which are then not used.")]
        [SerializeField] private Selectable _inheritFrom;

        private bool Inherits => _inheritFrom; // for the Condition of the fields that it replaces

        private Selectable _appliedUp;
        private Selectable _appliedDown;

        public override void SetLayoutHorizontal()
        {
            base.SetLayoutHorizontal();

            if(_inheritFrom)
            {
                _appliedUp = InheritedNavigation.Up(_inheritFrom, transform);
                _appliedDown = InheritedNavigation.Down(_inheritFrom, transform);
            }
            else
            {
                _appliedUp = _navigateUp;
                _appliedDown = _navigateDown;
            }
            HorizontalNavigationSetter.SetNavigation(rectChildren, _appliedUp, _appliedDown);
        }

        // The selectable that is inherited from can be navigated by something else (another layout group) later than we are set,
        // or change afterwards: the navigation is set again when it did.
        protected virtual void LateUpdate()
        {
            if(!_inheritFrom)
                return;

            if(InheritedNavigation.Up(_inheritFrom, transform) != _appliedUp
               || InheritedNavigation.Down(_inheritFrom, transform) != _appliedDown)
                SetDirty();
        }
    }
}

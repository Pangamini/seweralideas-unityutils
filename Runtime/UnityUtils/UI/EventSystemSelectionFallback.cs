#nullable enable
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SeweralIdeas.UnityUtils
{
    // Keeps EventSystem.currentSelectedGameObject pointing at *something*
    // usable at all times, so keyboard/gamepad navigation always has somewhere
    // to go. Purely a fallback - anything that cares about a specific default
    // selection (opening a menu, etc.) should still call
    // EventSystem.SetSelectedGameObject itself; this only steps in when
    // nothing else has, or when the current selection quietly stopped being
    // usable (deactivated, or its Selectable became non-interactable) without
    // EventSystem ever clearing it.
    [RequireComponent(typeof(EventSystem))]
    [DisallowMultipleComponent]
    public class EventSystemSelectionFallback : MonoBehaviour
    {
        private EventSystem  _eventSystem = null!;
        private Selectable[] _selectableBuffer = new Selectable[16];

        private void Awake() => _eventSystem = GetComponent<EventSystem>();

        private void LateUpdate()
        {
            if(IsSelectionUsable())
                return;

            var fallback = FindFallback();
            if(fallback != null)
            {
                _eventSystem.SetSelectedGameObject(fallback.gameObject);
            }
            else if(_eventSystem.currentSelectedGameObject != null)
            {
                // Nothing valid to select at all - don't leave a stale/inactive
                // reference behind, so anything watching selection (e.g. a
                // selection cursor) knows there's genuinely nothing selected.
                _eventSystem.SetSelectedGameObject(null);
            }
        }

        private bool IsSelectionUsable()
        {
            var selected = _eventSystem.currentSelectedGameObject;
            if(selected == null || !selected.activeInHierarchy)
                return false;

            // A selected GameObject with no Selectable (rare, but legal) always
            // counts as usable - only a Selectable can be active yet
            // non-interactable.
            if(selected.TryGetComponent(out Selectable selectable) && !selectable.IsInteractable())
                return false;

            return true;
        }

        private Selectable? FindFallback()
        {
            int count = Selectable.allSelectableCount;
            if(_selectableBuffer.Length < count)
                _selectableBuffer = new Selectable[count];

            Selectable.AllSelectablesNoAlloc(_selectableBuffer);

            for(int i = 0; i < count; i++)
            {
                var selectable = _selectableBuffer[i];
                if(selectable != null && selectable.IsInteractable())
                    return selectable;
            }

            return null;
        }
    }
}

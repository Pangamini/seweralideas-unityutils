#nullable enable
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace SeweralIdeas.UnityUtils
{
    // Placed alongside a Selectable that stands in for a whole group of child
    // Selectables in the navigation graph (e.g. a panel, a tab's contents).
    // Remembers whichever child was last selected, and - the moment default
    // navigation lands ON the group itself - redirects selection straight to
    // that child, so the group is never really "the" selection for more than
    // an instant.
    //
    // The group's own Selectable should have its Transition set to None in
    // the Inspector - it's a routing stop, not something meant to visibly
    // look selected, and an Animator-based transition can flash before the
    // redirect below ever gets a chance to run.
    //
    // DefaultExecutionOrder is deliberately earlier than
    // EventSystemSelectionTrigger's (default, 0): Unity doesn't guarantee
    // LateUpdate order between unrelated components, so without this, the
    // trigger's own LateUpdate could run first in a given frame, observe the
    // group as the current selection, and report it to everything listening
    // (e.g. SelectionCursor) before the redirect below ever runs. Running
    // first guarantees the redirect has always already happened by the time
    // anything polls selection through the trigger.
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Selectable))]
    public class SelectionGroup : MonoBehaviour, ISelectHandler
    {
        [SerializeField] private Selectable? _defaultChild;

        private Selectable                   _selectable = null!;
        private Selectable?                  _lastSelectedChild;
        private EventSystemSelectionTrigger? _trigger;
        private bool                         _redirectPending;

        private void Awake()
        {
            _selectable = GetComponent<Selectable>();
            _lastSelectedChild = _defaultChild;
        }

        private void OnEnable()
        {
            _trigger = EventSystemSelectionTrigger.Get();
            if(_trigger == null)
                return;
            _trigger.OnSelectionChanged.AddListener(OnAnySelectionChanged);
        }

        private void OnDisable()
        {
            if(_trigger != null)
                _trigger.OnSelectionChanged.RemoveListener(OnAnySelectionChanged);
            _trigger = null;
            _redirectPending = false;
        }

        // Tracks whichever of our children was most recently selected, by
        // whatever means (click, nav, ...), not just while redirecting -
        // otherwise clicking a child directly would never update the memory.
        private void OnAnySelectionChanged(GameObject selection)
        {
            if(selection == null || selection == gameObject)
                return;
            if(!selection.transform.IsChildOf(transform))
                return;
            if(selection.TryGetComponent(out Selectable childSelectable))
                _lastSelectedChild = childSelectable;
        }

        // Deliberately NOT redirecting synchronously here - see the class
        // comment. Just flag it; LateUpdate (still this same frame) does the
        // actual redirect once EventSystem's own OnSelect dispatch has
        // finished.
        void ISelectHandler.OnSelect(BaseEventData eventData) => _redirectPending = true;

        private void LateUpdate()
        {
            if(!_redirectPending)
                return;
            _redirectPending = false;

            var eventSystem = EventSystem.current;
            if(eventSystem == null || eventSystem.currentSelectedGameObject != gameObject)
                return; // something else took selection before this ran

            var target = ResolveTarget();
            if(target != null)
                eventSystem.SetSelectedGameObject(target.gameObject);
        }

        // Remembered child, else the configured default, else - so even a
        // group that's never been entered before and has no default set
        // still has somewhere to redirect to on its very first selection -
        // the first active, interactable child Selectable found.
        private Selectable? ResolveTarget()
        {
            if(IsUsable(_lastSelectedChild))
                return _lastSelectedChild;
            if(IsUsable(_defaultChild))
                return _defaultChild;
            return FindFirstChild();
        }

        private static bool IsUsable(Selectable? selectable)
            => selectable != null && selectable.gameObject.activeInHierarchy && selectable.IsInteractable();

        private Selectable? FindFirstChild()
        {
            using (ListPool<Selectable>.Get(out var candidates))
            {
                GetComponentsInChildren(false, candidates);
                foreach (var candidate in candidates)
                {
                    if(candidate == _selectable)
                        continue;
                    if(candidate.IsInteractable())
                        return candidate;
                }
            }
            return null;
        }
    }
}

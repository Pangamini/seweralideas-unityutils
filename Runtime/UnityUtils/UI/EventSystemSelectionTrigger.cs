#nullable enable
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace SeweralIdeas.UnityUtils
{
    [RequireComponent(typeof(EventSystem))]
    public class EventSystemSelectionTrigger : MonoBehaviour
    {
        [SerializeField] private UnityEvent<GameObject> _onSelectionChanged = new();
        private                  EventSystem            _eventSystem = null!;
        private                  GameObject?            _reported;
        
        public UnityEvent<GameObject> OnSelectionChanged => _onSelectionChanged;
        public GameObject Selection => _eventSystem.currentSelectedGameObject;

        public static EventSystemSelectionTrigger Get(EventSystem system) => system.gameObject.GetOrAddComponent<EventSystemSelectionTrigger>();

        public static EventSystemSelectionTrigger Get() => Get(EventSystem.current);

        private void Awake()
        {
            _eventSystem = GetComponent<EventSystem>();
            _reported = Selection;
        }

        private void LateUpdate()
        {
            var current = Selection;
            if(_reported == current)
                return;
            _reported = current;
            
            OnSelectionChanged.Invoke(current);
        }
    }
}

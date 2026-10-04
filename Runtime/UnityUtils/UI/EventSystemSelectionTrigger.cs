#nullable enable
using SeweralIdeas.Utils;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SeweralIdeas.UnityUtils
{
    public class EventSystemSelectionTrigger : SimpleSingletonBehaviour<EventSystemSelectionTrigger>
    {
        private readonly Observable<GameObject?> _selection = new();
        
        public Observable<GameObject?>.Readonly Selection => _selection;
        
        private void Awake()
        {
            gameObject.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(gameObject);
            _selection.Value = null;
        }

        private void LateUpdate()
        {
            var system = EventSystem.current;
            _selection.Value = system? system.currentSelectedGameObject : null;
        }
    }
}

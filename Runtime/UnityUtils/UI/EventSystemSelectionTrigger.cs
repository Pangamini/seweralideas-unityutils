#nullable enable
using SeweralIdeas.Utils;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SeweralIdeas.UnityUtils
{
    public class EventSystemSelectionTrigger : MonoBehaviour
    {
        private static   EventSystemSelectionTrigger? _instance;
        private readonly Observable<GameObject?>      _selection = new();
        
        public Observable<GameObject?>.Readonly Selection => _selection;
        
        private void LateUpdate()
        {
            var system = EventSystem.current;
            _selection.Value = system
                ? system.currentSelectedGameObject 
                    ? system.currentSelectedGameObject 
                    : null
                : null;
        }
        
        public static EventSystemSelectionTrigger GetInstance()
        {
            if(_instance)
                return _instance;

            _instance = new GameObject(nameof(EventSystemSelectionTrigger)).AddComponent<EventSystemSelectionTrigger>();
            
            _instance.gameObject.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(_instance.gameObject);
            _instance._selection.Value = null;

            return _instance;
        }
    }
}

#nullable enable
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Scripting;

namespace SeweralIdeas.UnityUtils
{
    public class AnimationEventRelay : MonoBehaviour
    {
        [SerializeField] private SerializableDictionary<string, UnityEvent> _events = new();
        
        [Preserve] public void TriggerEvent(string eventName)
        {
            if(!_events.TryGetValue(eventName, out var unityEvent))
            {
                Debug.LogWarning($"{nameof(AnimationEventRelay)} has no event with name \"{eventName}\"");
                return;
            }
            
            unityEvent.Invoke();
        }
    }
}

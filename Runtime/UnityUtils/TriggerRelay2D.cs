using UnityEngine;
using UnityEngine.Events;

namespace SeweralIdeas.UnityUtils
{
    public class TriggerRelay2D : MonoBehaviour
    {
        [SerializeField] private UnityEvent<Collider2D> _triggerEnter = new();
        [SerializeField] private UnityEvent<Collider2D> _triggerExit = new();
        
        public UnityEvent<Collider2D> TriggerEnter => _triggerEnter;
        public UnityEvent<Collider2D> TriggerExit => _triggerExit;

        protected void OnTriggerEnter2D(Collider2D other) => _triggerEnter.Invoke(other);
        protected void OnTriggerExit2D(Collider2D other) => _triggerExit.Invoke(other);
    }
}

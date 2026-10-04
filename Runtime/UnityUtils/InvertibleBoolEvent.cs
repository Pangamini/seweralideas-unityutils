using System;
using UnityEngine;
using UnityEngine.Events;

namespace SeweralIdeas.UnityUtils
{
    /// <summary>
    /// A bool event where each callback chooses whether it receives the value or its negation.
    /// Callbacks are kept in two lists - "On Value" gets the value as is, "On Inverted" gets it negated - which is
    /// all a per-callback invert flag amounts to, and keeps the plain UnityEvent inspector.
    /// </summary>
    [Serializable]
    public class InvertibleBoolEvent
    {
        [SerializeField] private UnityEvent<bool> _onValue    = new();
        [SerializeField] private UnityEvent<bool> _onInverted = new();

        public void Invoke(bool value)
        {
            _onValue.Invoke(value);
            _onInverted.Invoke(!value);
        }

        public void AddListener(UnityAction<bool> call, bool invert = false) => (invert ? _onInverted : _onValue).AddListener(call);

        public void RemoveListener(UnityAction<bool> call, bool invert = false) => (invert ? _onInverted : _onValue).RemoveListener(call);

        /// <summary>Removes the code-added listeners; the ones set up in the inspector stay.</summary>
        public void RemoveAllListeners()
        {
            _onValue.RemoveAllListeners();
            _onInverted.RemoveAllListeners();
        }
    }
}

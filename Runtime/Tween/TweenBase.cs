using UnityEngine;
using UnityEngine.Events;

namespace SeweralIdeas.Tweening
{
    /// <summary>
    /// The source of a 0..1 value that <see cref="TweenComponent"/>s turn into an effect.
    /// <see cref="Tween"/> eases between on and off; <see cref="ImpulseTween"/> is raised by a trigger and decays.
    /// One per object, so the components next to it know which one they follow.
    /// </summary>
    [DefaultExecutionOrder(ExecOrder)]
    [DisallowMultipleComponent]
    public abstract class TweenBase : MonoBehaviour
    {
        public const int ExecOrder = 0;

        [SerializeField]
        private UnityEvent<float> m_onValueChanged = new();

        public event UnityAction<float> ValueChanged
        {
            add => m_onValueChanged.AddListener(value);
            remove => m_onValueChanged.RemoveListener(value);
        }

        public abstract float Progress { get; }

        /// <summary>Sets up the state that Awake would, for a caller that needs it before then.</summary>
        public virtual void EnsureInitialized() { }

        protected void NotifyValueChanged(float value) => m_onValueChanged?.Invoke(value);
    }
}

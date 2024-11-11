using UnityEngine;
using UnityEngine.Events;

namespace SeweralIdeas.UnityUtils
{
    public abstract class TweenValue<T> : TweenComponent
    {
        [ColorUsage(true, true)]
        [SerializeField]
        private T m_offValue;

        [ColorUsage(true, true)]
        [SerializeField]
        private T m_onValue;
        
        [SerializeField]
        private UnityEvent<T> m_onValueChanged;
        
        public event UnityAction<T> ValueChanged
        {
            add => m_onValueChanged.AddListener(value);
            remove => m_onValueChanged.RemoveListener(value);
        }
        
        public T OffValue => m_offValue;
        public T OnValue => m_onValue;

        protected sealed override void OnValueChanged(float progress)
        {
            T newValue = Interpolate(progress);
            m_onValueChanged.Invoke(newValue);
            OnValueInterpolated(progress, newValue);
        }

        protected abstract T Interpolate(float t);
        protected virtual void OnValueInterpolated(float t, T newValue) { }
    }
}

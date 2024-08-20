using System;
#if UNITY_5_3_OR_NEWER
using UnityEngine;
#endif

namespace SeweralIdeas.Utils
{
    public interface IReadonlyObservable<out T>
    {
        public event Action<T> Changed;
        public T Value { get; }
    }
    
    [Serializable]
    public class Observable<T> : IReadonlyObservable<T>
    {
        public Readonly ReadOnly =>  new Readonly(this);
#if UNITY_5_3_OR_NEWER
        [SerializeField]
#endif
        
        private T         m_value;
        private Action<T> m_onChanged;

        public Observable(T defaultValue = default)
        {
            m_value = defaultValue;
        }

        public T Value
        {
            get => m_value;
            set
            {
                
                if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(m_value, value))
                    return;
                m_value = value;
                m_onChanged?.Invoke(value);
            }
        }

        public event Action<T> Changed
        {
            add
            {
                m_onChanged += value;
                value(Value);
            }
            remove => m_onChanged -= value;
        }

        public struct Readonly : IReadonlyObservable<T>
        {
            public Readonly(Observable<T> observable)
            {
                m_observable = observable;
            }

            private Observable<T> m_observable;
            public T Value => m_observable.Value;

            public event Action<T> Changed
            {
                add => m_observable.Changed += value;
                remove => m_observable.Changed -= value;
            }

            public static implicit operator Readonly(Observable<T> observable) => observable.ReadOnly;
        }
    }

}
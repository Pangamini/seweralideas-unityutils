using System;

namespace SeweralIdeas.Utils
{
    [System.Serializable]
    public class Reactive<T>
    {
        public readonly Readonly readOnly;
        private T m_value;
        private Action<T> m_onChanged;

        public Reactive(T defaultValue = default)
        {
            m_value = defaultValue;
            readOnly = new Readonly(this);
        }

        public T Value
        {
            get { return m_value; }
            set
            {
                
                if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(m_value, value))
                    return;
                m_value = value;
                m_onChanged?.Invoke(value);
            }
        }

        public event Action<T> onChanged
        {
            add
            {
                m_onChanged += value;
                value(Value);
            }
            remove
            {
                m_onChanged -= value;
            }
        }

        public struct Readonly
        {
            public Readonly(Reactive<T> reactive)
            {
                m_reactive = reactive;
            }

            private Reactive<T> m_reactive;
            public T Value => m_reactive.Value;

            public event Action<T> onChanged
            {
                add
                {
                    m_reactive.onChanged += value;
                }
                remove
                {
                    m_reactive.onChanged -= value;
                }
            }
        }
    }

}
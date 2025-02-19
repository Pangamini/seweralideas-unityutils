#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;

namespace SeweralIdeas.Collections
{
    public readonly struct SetOrDictEnumerable<TKey, T> : IEnumerable<T>
    {
        private readonly Dictionary<TKey, T>? m_dict;
        private readonly HashSet<T>?          m_set;

        public SetOrDictEnumerable(Dictionary<TKey, T> dict)
        {
            m_dict = dict ?? throw new ArgumentNullException(nameof(dict));
            m_set = null;
        }

        public SetOrDictEnumerable(HashSet<T> set)
        {
            m_set = set ?? throw new ArgumentNullException(nameof(set));
            m_dict = null;
        }

        public Enumerator GetEnumerator() => m_set != null ? new Enumerator(m_set) : new Enumerator(m_dict!);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<T>
        {
            private          Dictionary<TKey, T>.Enumerator m_dictEnumerator;
            private          HashSet<T>.Enumerator          m_setEnumerator;
            private readonly bool                           m_dict;

            public Enumerator(HashSet<T> set)
            {
                m_setEnumerator = set.GetEnumerator();
                m_dictEnumerator = default;
                m_dict = false;
            }

            public Enumerator(Dictionary<TKey, T> dict)
            {
                m_setEnumerator = default;
                m_dictEnumerator = dict.GetEnumerator();
                m_dict = true;
            }

            public T Current => m_dict ? m_dictEnumerator.Current.Value : m_setEnumerator.Current;

            object? IEnumerator.Current => Current;

            public void Dispose()
            {
                if (m_dict)
                    m_dictEnumerator.Dispose();
                else
                    m_setEnumerator.Dispose();
            }

            public bool MoveNext()
            {
                if (m_dict)
                    return m_dictEnumerator.MoveNext();
                else
                    return m_setEnumerator.MoveNext();
            }

            void IEnumerator.Reset()
            {
                throw new NotImplementedException();
            }
        }
    }
}
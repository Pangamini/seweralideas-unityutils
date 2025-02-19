#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;

namespace SeweralIdeas.Collections
{
    public readonly struct ReadonlyArrayView<T> : IReadOnlyList<T>
    {
        private readonly T[] m_array;
        
        public ReadonlyArrayView(T[]? array) => m_array = array?? Array.Empty<T>();

        public T this[int index] => m_array[index];
        public int Count => m_array.Length;
        public static implicit operator ReadonlyArrayView<T> (T[] arr) => new ReadonlyArrayView<T>(arr);
        public Enumerator GetEnumerator() => new(m_array);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public override string ToString() => m_array.ToString();

        public struct Enumerator : IEnumerator<T>
        {
            private readonly T[] m_array;
            private          int  m_i;
            
            public Enumerator(T[] array)
            {
                m_i = -1;
                m_array = array;
            }

            public bool MoveNext()
            {
                m_i++;
                return m_i < m_array.Length;
            }

            public void Reset()
            {
                m_i = -1;
            }

            object? IEnumerator.Current => Current;
            public T Current => m_array[m_i];
            void IDisposable.Dispose() { }
        }
    }
}
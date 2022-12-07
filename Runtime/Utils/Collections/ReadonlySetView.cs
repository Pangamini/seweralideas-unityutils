using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SeweralIdeas.Collections
{
    public readonly struct ReadonlySetView<T> : IReadOnlyCollection<T>
    {
        private readonly HashSet<T> m_set;
        public ReadonlySetView(HashSet<T> set)
        {
            m_set = set;
        }

        public int Count => m_set.Count;

        public bool Contains(T item) => m_set.Contains(item);
        
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public HashSet<T>.Enumerator GetEnumerator()
        {
            return m_set.GetEnumerator();
        }

    }
}
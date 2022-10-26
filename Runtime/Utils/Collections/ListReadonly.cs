using System.Collections;
using System.Collections.Generic;

namespace SeweralIdeas.Collections
{
    public class ListReadonly<T> : IReadOnlyList<T>
    {
        private List<T> m_list;
        public ListReadonly(List<T> list)
        {
            m_list = list;
        }

        public T this[int index] => m_list[index];

        public int Count => m_list.Count;

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public List<T>.Enumerator GetEnumerator()
        {
            return m_list.GetEnumerator();
        }

    }
}
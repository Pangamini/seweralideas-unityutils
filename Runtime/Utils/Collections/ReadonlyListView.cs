#nullable enable
using System.Collections;
using System.Collections.Generic;

namespace SeweralIdeas.Collections
{
    public readonly struct ReadonlyListView<T> : IReadOnlyList<T>
    {
        private static readonly List<T> Empty = new();
        private readonly        List<T> m_list;
        
        public ReadonlyListView(List<T>? list) => m_list = list ?? Empty;

        public T this[int index] => m_list[index];
        public int Count => m_list.Count;
        public static implicit operator ReadonlyListView<T>(List<T> list) => new ReadonlyListView<T>(list);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public List<T>.Enumerator GetEnumerator() => m_list.GetEnumerator();

    }
}
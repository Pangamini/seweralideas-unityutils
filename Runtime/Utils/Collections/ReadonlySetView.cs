#nullable enable
using System.Collections;
using System.Collections.Generic;

namespace SeweralIdeas.Collections
{
    public readonly struct ReadonlySetView<T> : IReadOnlyCollection<T>
    {
        private static readonly HashSet<T> Empty = new();
        private readonly        HashSet<T> m_set;
        
        public ReadonlySetView(HashSet<T>? set) => m_set = set ?? Empty;

        public int Count => m_set.Count;
        public bool Contains(T item) => m_set.Contains(item);
        public static implicit operator ReadonlySetView<T>(HashSet<T> set) => new ReadonlySetView<T>(set);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public HashSet<T>.Enumerator GetEnumerator() => m_set.GetEnumerator();

    }
}
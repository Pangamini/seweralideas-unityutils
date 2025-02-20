#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;

namespace SeweralIdeas.Collections
{
    public readonly struct ReadonlyListView<T> : IReadOnlyList<T>, IEquatable<ReadonlyListView<T>>
    {
        private readonly        List<T> m_list;

        public ReadonlyListView(List<T>? list) => m_list = list!;

        public T this[int index] => m_list[index];
        public int Count => m_list.Count;
        public static implicit operator ReadonlyListView<T>(List<T> list) => new ReadonlyListView<T>(list);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public List<T>.Enumerator GetEnumerator() => m_list.GetEnumerator();
        public bool Equals(ReadonlyListView<T> other) => m_list == other.m_list;
        public override bool Equals(object? obj) => obj is ReadonlyListView<T> other && Equals(other);
        public override int GetHashCode() => m_list.GetHashCode();
        public static bool operator ==(ReadonlyListView<T> left, ReadonlyListView<T> right) => left.m_list == right.m_list;
        public static bool operator !=(ReadonlyListView<T> left, ReadonlyListView<T> right) => left.m_list != right.m_list;
    }
}
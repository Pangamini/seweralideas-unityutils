#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;

namespace SeweralIdeas.Collections
{
    public readonly struct ReadonlySetView<T> : IReadOnlyCollection<T>, IEquatable<ReadonlySetView<T>>
    {
        private readonly        HashSet<T> m_set;
        public ReadonlySetView(HashSet<T>? set) => m_set = set!;

        public int Count => m_set.Count;
        public bool Contains(T item) => m_set.Contains(item);
        public static implicit operator ReadonlySetView<T>(HashSet<T> set) => new ReadonlySetView<T>(set);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public HashSet<T>.Enumerator GetEnumerator() => m_set.GetEnumerator();
        public bool Equals(ReadonlySetView<T> other) => m_set == other.m_set;
        public override bool Equals(object? obj) => obj is ReadonlySetView<T> other && Equals(other);
        public override int GetHashCode() => m_set.GetHashCode();
        public static bool operator ==(ReadonlySetView<T> left, ReadonlySetView<T> right) => left.m_set == right.m_set;
        public static bool operator !=(ReadonlySetView<T> left, ReadonlySetView<T> right) => left.m_set != right.m_set;
    }
}
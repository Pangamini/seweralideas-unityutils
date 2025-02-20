#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using SeweralIdeas.UnityUtils;

namespace SeweralIdeas.Collections
{
    public readonly struct ReadonlyBictView<TKey, TVal> : IReadOnlyBictionary<TKey, TVal>, IEquatable<ReadonlyBictView<TKey, TVal>>
    {
        private readonly        Bictionary<TKey, TVal> m_bict;
        
        public ReadonlyBictView(Bictionary<TKey, TVal>? bict) => m_bict = bict!;
        
        public int Count => m_bict.Count;
        public bool ContainsKey(TKey key) => m_bict.ContainsKey(key);
        public IReadOnlyBictionary<TVal, TKey> Reversed => m_bict.Reversed;
        public bool ContainsValue(TVal value) => m_bict.ContainsValue(value);
        public bool TryGetKey(TVal value, out TKey key) => m_bict.TryGetKey(value, out key);
        public bool TryGetValue(TKey key, out TVal value) => m_bict.TryGetValue(key, out value);
        public TVal this[TKey key] => m_bict != null ? m_bict[key] : throw new KeyNotFoundException();
        public Dictionary<TKey, TVal>.Enumerator GetEnumerator() => m_bict.GetEnumerator();
        public static implicit operator ReadonlyBictView<TKey, TVal> (Bictionary<TKey, TVal> dict) => new ReadonlyBictView<TKey, TVal>(dict);
        IEnumerator<KeyValuePair<TKey, TVal>> IEnumerable<KeyValuePair<TKey, TVal>>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TVal>.Keys => m_bict.Keys;
        IEnumerable<TVal> IReadOnlyDictionary<TKey, TVal>.Values => m_bict.Values;
        public bool Equals(ReadonlyBictView<TKey, TVal> other) => m_bict == other.m_bict;
        public override bool Equals(object? obj) => obj is ReadonlyBictView<TKey, TVal> other && Equals(other);
        public override int GetHashCode() => m_bict.GetHashCode();
        public static bool operator ==(ReadonlyBictView<TKey, TVal> left, ReadonlyBictView<TKey, TVal> right) => left.m_bict == right.m_bict;
        public static bool operator !=(ReadonlyBictView<TKey, TVal> left, ReadonlyBictView<TKey, TVal> right) => left.m_bict != right.m_bict;
    }
}
#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;

namespace SeweralIdeas.Collections
{
    public readonly struct ReadonlyDictView<TKey, TVal> : IReadOnlyDictionary<TKey, TVal>, IEquatable<ReadonlyDictView<TKey, TVal>>
    {
        private readonly        Dictionary<TKey, TVal> m_dict;
        
        public ReadonlyDictView(Dictionary<TKey, TVal>? dict) => m_dict = dict!;
        
        public int Count => m_dict.Count;
        public bool ContainsKey(TKey key) => m_dict.ContainsKey(key);
        public bool TryGetValue(TKey key, out TVal value) => m_dict.TryGetValue(key, out value);
        public TVal this[TKey key] => m_dict[key];
        public Dictionary<TKey, TVal>.Enumerator GetEnumerator() => m_dict.GetEnumerator();
        public static implicit operator ReadonlyDictView<TKey, TVal> (Dictionary<TKey, TVal> dict) => new ReadonlyDictView<TKey, TVal>(dict);
        IEnumerator<KeyValuePair<TKey, TVal>> IEnumerable<KeyValuePair<TKey, TVal>>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TVal>.Keys => m_dict.Keys;
        IEnumerable<TVal> IReadOnlyDictionary<TKey, TVal>.Values => m_dict.Values;
        public bool Equals(ReadonlyDictView<TKey, TVal> other) => m_dict == other.m_dict;
        public override bool Equals(object? obj) => obj is ReadonlyDictView<TKey, TVal> other && Equals(other);
        public override int GetHashCode() => m_dict.GetHashCode();
        public static bool operator ==(ReadonlyDictView<TKey, TVal> left, ReadonlyDictView<TKey, TVal> right) => left.m_dict == right.m_dict;
        public static bool operator !=(ReadonlyDictView<TKey, TVal> left, ReadonlyDictView<TKey, TVal> right) => left.m_dict != right.m_dict;
    }
}
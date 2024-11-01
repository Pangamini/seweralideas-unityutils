using System.Collections;
using System.Collections.Generic;

namespace SeweralIdeas.Collections
{
    public readonly struct ReadonlyDictView<TKey, TVal> : IReadOnlyDictionary<TKey, TVal>
    {
        private static readonly Dictionary<TKey, TVal> Empty = new();
        private readonly        Dictionary<TKey, TVal> m_dict;
        
        public ReadonlyDictView(Dictionary<TKey, TVal> dict)
        {
            m_dict = dict;
        }

        public int Count => m_dict?.Count ?? 0;

        public bool ContainsKey(TKey key) => m_dict?.ContainsKey(key)??false;

        public bool TryGetValue(TKey key, out TVal value)
        {
            if(m_dict != null)
                return m_dict.TryGetValue(key, out value);
            
            value = default;
            return false;
        }

        public TVal this[TKey key] => m_dict != null ? m_dict[key] : throw new KeyNotFoundException();
        
        public Dictionary<TKey, TVal>.Enumerator GetEnumerator() => (m_dict ?? Empty).GetEnumerator();

        
        
        IEnumerator<KeyValuePair<TKey, TVal>> IEnumerable<KeyValuePair<TKey, TVal>>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TVal>.Keys => m_dict.Keys;
        IEnumerable<TVal> IReadOnlyDictionary<TKey, TVal>.Values => m_dict.Values;
    }
}
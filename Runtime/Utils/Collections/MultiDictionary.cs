#nullable enable
using System.Collections.Generic;

namespace SeweralIdeas.Collections
{
    public class MultiDictionary<TKey, TVal>
    {
        private static readonly HashSet<TVal> Empty = new();
        
        private readonly Dictionary<TKey, HashSet<TVal>> m_dict;

        public MultiDictionary() => m_dict = new Dictionary<TKey, HashSet<TVal>>();

        public MultiDictionary(IEqualityComparer<TKey> comparer) => m_dict = new Dictionary<TKey, HashSet<TVal>>(comparer);

        private HashSet<TVal>? TryGetList(TKey item) => m_dict.GetValueOrDefault(item);

        public bool Add(TKey key, TVal val)
        {
            HashSet<TVal>? valList = TryGetList(key);
            if (valList == null)
            {
                valList = new HashSet<TVal>();
                m_dict.Add(key, valList);
            }
            return valList.Add(val);
        }

        public bool Add(TKey key, TVal val, out bool keyAdded)
        {
            HashSet<TVal>? valList = TryGetList(key);
            if (valList == null)
            {
                valList = new HashSet<TVal>();
                m_dict.Add(key, valList);
                keyAdded = true;
            }
            else
            {
                keyAdded = false;
            }
            return valList.Add(val);
        }

        public bool Remove(TKey key, TVal val)
        {
            HashSet<TVal>? valList = TryGetList(key);
            if (valList != null)
            {

                bool ret = valList.Remove(val);
                if (valList.Count == 0)
                    m_dict.Remove(key);
                return ret;
            }
            return false;
        }

        public void Clear() => m_dict.Clear();

        public void RemoveKey(TKey key) => m_dict.Remove(key);

        public ReadonlySetView<TVal> GetItems(TKey key)
        {
            HashSet<TVal>? valList = TryGetList(key);

            if (valList != null)
                return new(valList);
            
            return new(Empty);
        }

        public int KeysCount => m_dict.Count;

        public int ValuesCount
        {
            get
            {
                int res = 0;
                foreach (TKey key in Keys)
                    res += GetValuesCount(key);
                return res;
            }
        }

        public int GetValuesCount(TKey key)
        {
            HashSet<TVal>? valList = TryGetList(key);
            return valList?.Count ?? 0;
        }
        
        public Dictionary<TKey, HashSet<TVal>>.KeyCollection Keys => m_dict.Keys;

        public bool ContainsKey(TKey key) => m_dict.ContainsKey(key);

        public Enumerator GetEnumerator() => new Enumerator(this);

        public struct Enumerator
        {
            public Enumerator(MultiDictionary<TKey, TVal> dict)
            {
                m_setReady = false;
                m_setEnumerator = default;
                m_dictEnumerator = dict.m_dict.GetEnumerator();
            }

            public bool MoveNext()
            {
                while (true)
                {
                    if (!m_setReady && !m_dictEnumerator.MoveNext())
                        return false;
                    if (!m_setReady)
                    {
                        m_setEnumerator = m_dictEnumerator.Current.Value.GetEnumerator();
                        m_setReady = true;
                    }
                    if (m_setEnumerator.MoveNext())
                        return true;
                    else
                        m_setReady = false;
                }
            }

            public KeyValuePair<TKey, TVal> Current => new KeyValuePair<TKey, TVal>(m_dictEnumerator.Current.Key, m_setEnumerator.Current);
            private Dictionary<TKey, HashSet<TVal>>.Enumerator m_dictEnumerator;
            private HashSet<TVal>.Enumerator m_setEnumerator;
            private bool m_setReady;
        }
    }
}
using System.Collections.Generic;

namespace SeweralIdeas.Collections
{
    public class MultiDictionary<TKey, TVal>
    {

        private Dictionary<TKey, HashSet<TVal>> m_dict;

        ///////////////////////////////////////////////////////////////////////////////////////

        public MultiDictionary()
        {
            m_dict = new Dictionary<TKey, HashSet<TVal>>();
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public MultiDictionary(IEqualityComparer<TKey> comparer)
        {
            m_dict = new Dictionary<TKey, HashSet<TVal>>(comparer);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private HashSet<TVal> TryGetList(TKey item)
        {

            HashSet<TVal> hset;
            if (m_dict.TryGetValue(item, out hset))
                return hset;
            return null;
        }

        ////////////////////////////////////////////////////////////////////////////

        public bool Add(TKey key, TVal val)
        {

            HashSet<TVal> valList = TryGetList(key);
            if (valList == null)
            {
                valList = new HashSet<TVal>();
                m_dict.Add(key, valList);
            }
            return valList.Add(val);
        }

        ////////////////////////////////////////////////////////////////////////////

        public bool Remove(TKey key, TVal val)
        {

            HashSet<TVal> valList = TryGetList(key);
            if (valList != null)
            {

                bool ret = valList.Remove(val);
                if (valList.Count == 0)
                    m_dict.Remove(key);
                return ret;
            }
            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public void Clear()
        {
            m_dict.Clear();
        }

        ////////////////////////////////////////////////////////////////////////////

        public void RemoveKey(TKey key)
        {
            m_dict.Remove(key);
        }

        ////////////////////////////////////////////////////////////////////////////

        public HashSet<TVal> GetItems(TKey key)
        {
            HashSet<TVal> valList = TryGetList(key);

            if (valList != null)
                return valList;
            return null;
        }


        ///////////////////////////////////////////////////////////////////////////////////////

        public int keysCount
        {
            get { return m_dict.Count; }
        }

        public int valuesCount
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
            HashSet<TVal> vals = GetItems(key);
            if (vals != null)
                return vals.Count;
            else
                return 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public Dictionary<TKey, HashSet<TVal>>.KeyCollection Keys
        {
            get { return m_dict.Keys; }
        }

        ////////////////////////////////////////////////////////////////////////////

        public bool ContainsKey(TKey key)
        {
            return m_dict.ContainsKey(key);
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

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

            public KeyValuePair<TKey, TVal> Current
            { get { return new KeyValuePair<TKey, TVal>(m_dictEnumerator.Current.Key, m_setEnumerator.Current); } }

            private Dictionary<TKey, HashSet<TVal>>.Enumerator m_dictEnumerator;
            private HashSet<TVal>.Enumerator m_setEnumerator;
            private bool m_setReady;
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using SeweralIdeas.Collections;
using UnityEngine;

namespace SeweralIdeas.UnityUtils
{
    [Serializable]
    public class SerializableDictionary<TKey, TVal> : ISerializationCallbackReceiver, IReadOnlyDictionary<TKey, TVal>
    {
        [Serializable]
        public struct Element
        {
            public TKey m_key;
            public TVal m_value;
            
            public Element(TKey key, TVal value)
            {
                m_key = key;
                m_value = value;
            }
        }


        [SerializeField] private List<Element> m_list = new();
        [NonSerialized] private bool m_dictDirty;
        [NonSerialized] private bool m_listDirty;

        private readonly Dictionary<TKey, TVal> m_dict = new();
        private void EnsureDictUpToDate()
        {
            if (!m_dictDirty)
                return;
            
            Debug.Assert(!m_listDirty);
            
            m_dict.Clear();
            m_dict.EnsureCapacity(m_list.Count);
            foreach (Element element in m_list)
            {
                if(element.m_key != null)
                {
                    TKey key = element.m_key;
                    if (m_dict.ContainsKey(key))
                    {
                        Debug.LogError($"{GetType().Name} contains duplicate key {key}");
                        continue;
                    }
                
                    m_dict.Add(key, element.m_value);
                }
            }
                
            m_dictDirty = false;
        }

        public ReadonlyDictView<TKey, TVal> GetReadonlyView()
        {
            EnsureDictUpToDate();
            return new(m_dict);
        }

        public int Count => m_dict.Count;

        public bool ContainsKey(TKey key)
        {
            EnsureDictUpToDate();
            return m_dict.ContainsKey(key);
        }

        public bool TryGetValue(TKey key, out TVal value)
        {
            EnsureDictUpToDate();
            return m_dict.TryGetValue(key, out value);
        }

        public TVal this[TKey key]
        {
            get
            {
                EnsureDictUpToDate();
                return m_dict[key];
            }
            set
            {
                EnsureDictUpToDate();
                m_dict[key] = value;
                m_listDirty = true;
            }
        }

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TVal>.Keys
        {
            get
            {
                EnsureDictUpToDate();
                return m_dict.Keys;
            }
        }
        
        IEnumerable<TVal> IReadOnlyDictionary<TKey, TVal>.Values
        {
            get
            {
                EnsureDictUpToDate();
                return m_dict.Values;
            }
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            if(!m_listDirty)
                return;
            
            m_list.Clear();
            foreach (var pair in m_dict)
            {
                m_list.Add(new Element(pair.Key, pair.Value));
            }
                
            m_listDirty = false;
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            m_listDirty = false;
            m_dictDirty = true;
        }

        IEnumerator<KeyValuePair<TKey, TVal>> IEnumerable<KeyValuePair<TKey, TVal>>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public Dictionary<TKey, TVal>.Enumerator GetEnumerator()
        {
            EnsureDictUpToDate();
            return m_dict.GetEnumerator();
        }
        
        public void Clear()
        {
            m_dict.Clear();
            m_list.Clear();
        }
        
        public void Add(TKey key, TVal value)
        {
            EnsureDictUpToDate();
            m_dict.Add(key, value);
            m_listDirty = true;
        }

        public bool Remove(TKey key)
        {
            EnsureDictUpToDate();
            if(!m_dict.Remove(key))
                return false;
            
            m_listDirty = true;
            return true;
        }
    }
}

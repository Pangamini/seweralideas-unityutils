#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;

namespace SeweralIdeas.UnityUtils
{
    public interface IReadOnlyBictionary<TKey, TVal> : IReadOnlyDictionary<TKey, TVal>
    {
        IReadOnlyBictionary<TVal, TKey> Reversed { get; }
        bool ContainsValue(TVal value);
        bool TryGetKey(TVal value, out TKey key);
    }
    
    public interface IBictionary<TKey, TVal> : IDictionary<TKey, TVal>, IReadOnlyBictionary<TKey, TVal>
    {
        new IBictionary<TVal, TKey> Reversed { get; }
    }
    
    public class Bictionary<TKey, TVal> : IBictionary<TKey, TVal>
    {
        private readonly Dictionary<TKey, TVal> m_forward;
        private readonly Dictionary<TVal, TKey> m_reverse;

        public readonly Bictionary<TVal, TKey> Reversed;

        IBictionary<TVal, TKey> IBictionary<TKey, TVal>.Reversed => Reversed;
        IReadOnlyBictionary<TVal, TKey> IReadOnlyBictionary<TKey, TVal>.Reversed => Reversed;
        
        public Bictionary()
        {
            m_forward = new Dictionary<TKey, TVal>();
            m_reverse = new Dictionary<TVal, TKey>();
            Reversed = new Bictionary<TVal, TKey>(this);
        }
        
        // Reverse constructor
        private Bictionary(Bictionary<TVal, TKey> reversed)
        {
            Reversed = reversed;
            m_forward = reversed.m_reverse;
            m_reverse = reversed.m_forward;
        }
        
        public void Add(TKey key, TVal value)
        {
            if(m_reverse.ContainsKey(value))
                throw new ArgumentException("Value already present in the Bictionary");
            
            m_forward.Add(key, value);
            m_reverse.Add(value, key);
        }

        public void Add(KeyValuePair<TKey, TVal> item) => Add(item.Key, item.Value);

        public bool Remove(TKey key)
        {
            if(!m_forward.Remove(key, out TVal value))
                return false;
            
            bool removed2 = m_reverse.Remove(value, out var key2);
            System.Diagnostics.Debug.Assert(removed2);
            System.Diagnostics.Debug.Assert(EqualityComparer<TKey>.Default.Equals(key, key2));
            
            return true;
        }
        public bool Remove(KeyValuePair<TKey, TVal> item)
        {
            if (!m_forward.TryGetValue(item.Key, out TVal value) || !EqualityComparer<TVal>.Default.Equals(value, item.Value))
                return false;

            return Remove(item.Key);
        }

        public void Clear()
        {
            m_forward.Clear();
            m_reverse.Clear();
        }

        public bool ContainsKey(TKey key) => m_forward.ContainsKey(key);
        public bool ContainsValue(TVal value) => m_reverse.ContainsKey(value);

        public bool TryGetValue(TKey key, out TVal value) => m_forward.TryGetValue(key, out value);
        public bool TryGetKey(TVal value, out TKey key) => m_reverse.TryGetValue(value, out key);
        
        public TVal this[TKey key]
        {
            get => m_forward[key];
            set
            {
                if (m_forward.TryGetValue(key, out var oldValue))
                {
                    // Remove old mapping if key already exists
                    m_reverse.Remove(oldValue);
                }
                
                if (m_reverse.ContainsKey(value))
                    throw new ArgumentException("Value is already associated with another key.");

                m_forward[key] = value;
                m_reverse[value] = key;
            }
        }
        
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TVal>.Keys => Keys;
        IEnumerable<TVal> IReadOnlyDictionary<TKey, TVal>.Values => Values;
        public ICollection<TKey> Keys => m_forward.Keys;
        public ICollection<TVal> Values => m_reverse.Keys;
        
        IEnumerator<KeyValuePair<TKey, TVal>> IEnumerable<KeyValuePair<TKey, TVal>>.GetEnumerator() => m_forward.GetEnumerator();

        public Dictionary<TKey, TVal>.Enumerator GetEnumerator() => m_forward.GetEnumerator();
        
        IEnumerator IEnumerable.GetEnumerator() => m_forward.GetEnumerator();
        
        public bool Contains(KeyValuePair<TKey, TVal> item) => ((ICollection<KeyValuePair<TKey, TVal>>)m_forward).Contains(item);

        public void CopyTo(KeyValuePair<TKey, TVal>[] array, int arrayIndex) => ((ICollection<KeyValuePair<TKey, TVal>>)m_forward).CopyTo(array, arrayIndex);
        
        public int Count => m_forward.Count;
        public bool IsReadOnly => false;
        
        public void EnsureCapacity(int capacity)
        {
            m_forward.EnsureCapacity(capacity);
            m_reverse.EnsureCapacity(capacity);
        }
    }
}

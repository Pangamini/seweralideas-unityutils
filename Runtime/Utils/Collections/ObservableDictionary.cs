#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace SeweralIdeas.Collections
{
    public interface IObservableDictionary : IReadonlyObservableDictionary
    {
    }

    public interface IReadonlyObservableDictionary : IEnumerable
    {
        Type GetKeyType();
        Type GetValueType();
        int Count { get; }
    }

    public interface IObservableDictionary<TKey, out TVal> : IObservableDictionary, IReadonlyObservableDictionary<TKey, TVal>
    {
        
    }

    public interface IReadonlyObservableDictionary<TKey, out TVal> : IReadonlyObservableDictionary
    {
        public event Action<TKey, TVal>? Added;
        public event Action<TKey, TVal>? Removed;
        public bool Contains(TKey element);
        public void VisitAll(Action<TKey, TVal> visitor);
        public TVal GetValue(TKey key, out bool hasValue);
    }

    public static class ReadonlyObservableExtensions
    {
        public static bool TryGetValue<TKey, TVal>(this IReadonlyObservableDictionary<TKey, TVal> dict, TKey key, out TVal value)
        {
            value = dict.GetValue(key, out var hasValue);
            return hasValue;
        }
    }

    public class ObservableDictionary<TKey, TVal> : IObservableDictionary<TKey, TVal>
    {
        private readonly Dictionary<TKey, TVal> m_dict = new ();
        public event Action<TKey, TVal>? Added;
        public event Action<TKey, TVal>? Removed;
        public int Count => m_dict.Count;

        public Type GetKeyType() => typeof(TKey);
        public Type GetValueType() => typeof(TVal);

        public void Clear()
        {
            if (m_dict.Count == 0) 
                return;

            if (Removed == null)
            {
                m_dict.Clear();
                return;
            }
            
            Action<TKey, TVal> removed = Removed;                           // make a copy, so we are not affected by callbacks subscribing more events
            List<KeyValuePair<TKey, TVal>> callList = new (m_dict.Count);   // make a copy, so we only call Removed on currently present objects (also so we don't invalidate enumerators)
            foreach(var elem in m_dict)
                callList.Add(elem);

            m_dict.Clear();
            
            foreach (var obj in callList)
                removed(obj.Key, obj.Value);
        }
        
        public void Add( TKey key, TVal val )
        {
            m_dict.Add(key, val);
            Added?.Invoke(key, val);
        }

        public bool Remove( TKey key )
        {
            var ret = m_dict.Remove(key, out var val);
            if ( ret )
                Removed?.Invoke(key, val);
            return ret;
        }

        public bool Contains( TKey key ) => m_dict.ContainsKey(key);

        public ReadonlyObservableDictionary<TKey, TVal> GetReadonly() => new(this);

        [MustDisposeResource(false)]
        public Dictionary<TKey ,TVal>.Enumerator GetEnumerator() => m_dict.GetEnumerator();
        
        IEnumerator IEnumerable.GetEnumerator() => m_dict.GetEnumerator();

        public void VisitAll(Action<TKey, TVal> visitor)
        {
            foreach (var pair in m_dict)
                visitor(pair.Key, pair.Value);
        }
        
        public TVal GetValue(TKey key, out bool hasValue)
        {
            hasValue = m_dict.TryGetValue(key, out var ret);
            return ret;
        }
    }

    public readonly struct ReadonlyObservableDictionary<TKey, TVal> : IEnumerable<KeyValuePair<TKey, TVal>>, IReadonlyObservableDictionary<TKey, TVal>, IEquatable<ReadonlyObservableDictionary<TKey, TVal>>
    {
        private readonly ObservableDictionary<TKey, TVal> m_observableDict;

        public ReadonlyObservableDictionary( ObservableDictionary<TKey, TVal> observableObservableSet ) => m_observableDict = observableObservableSet;

        public Type GetKeyType() => typeof(TKey);

        public Type GetValueType() => typeof(TVal);

        public int Count => m_observableDict.Count;

        public event Action<TKey, TVal>? Added
        {
            add => m_observableDict.Added += value;
            remove => m_observableDict.Added -= value;
        }

        public event Action<TKey, TVal>? Removed
        {
            add => m_observableDict.Removed += value;
            remove => m_observableDict.Removed -= value;
        }

        public bool Contains( TKey key ) => m_observableDict.Contains(key);
        IEnumerator<KeyValuePair<TKey, TVal>> IEnumerable<KeyValuePair<TKey, TVal>>.GetEnumerator() => m_observableDict.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => m_observableDict.GetEnumerator();
        public Dictionary<TKey, TVal>.Enumerator GetEnumerator() => m_observableDict.GetEnumerator();
        public void VisitAll(Action<TKey, TVal> visitor) => m_observableDict.VisitAll(visitor);
        public TVal GetValue(TKey key, out bool hasValue) => m_observableDict.GetValue(key, out hasValue);
        public bool TryGetValue(TKey key, out TVal value) => m_observableDict.TryGetValue(key, out value);
        public bool Equals(ReadonlyObservableDictionary<TKey, TVal> other) => m_observableDict.Equals(other.m_observableDict);
        public override bool Equals(object? obj) => obj is ReadonlyObservableDictionary<TKey, TVal> other && Equals(other);
        public override int GetHashCode() => m_observableDict.GetHashCode();
        public static bool operator ==(ReadonlyObservableDictionary<TKey, TVal> left, ReadonlyObservableDictionary<TKey, TVal> right) => left.Equals(right);
        public static bool operator !=(ReadonlyObservableDictionary<TKey, TVal> left, ReadonlyObservableDictionary<TKey, TVal> right) => !left.Equals(right);
    }
}
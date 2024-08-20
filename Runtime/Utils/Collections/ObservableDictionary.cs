using System;
using System.Collections;
using System.Collections.Generic;

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

    public interface IObservableDictionary<TKey, TVal> : IObservableDictionary, IReadonlyObservableDictionary<TKey, TVal> { }

    public interface IReadonlyObservableDictionary<TKey, TVal> : IReadonlyObservableDictionary, IEnumerable<KeyValuePair<TKey, TVal>>
    {
        public event Action<TKey, TVal> Added;
        public event Action<TKey, TVal> Removed;
        public bool Contains(TKey element);
        public void VisitAll(Action<TKey, TVal> visitor);
    }

    public class ObservableDictionary<TKey, TVal> : IObservableDictionary<TKey, TVal>
    {
        private Dictionary<TKey, TVal> m_dict = new ();
        public event Action<TKey, TVal> Added;
        public event Action<TKey, TVal> Removed;
        private readonly IReadonlyObservableDictionary<TKey, TVal> m_readonly;

        public ObservableDictionary()
        {
            m_readonly = new ReadonlyObservableDictionary<TKey, TVal>(this);
        }

        public int Count => m_dict.Count;

        public Type GetKeyType() => typeof(TKey);
        public Type GetValueType() => typeof(TVal);

        public void Clear()
        {
            if (m_dict.Count == 0) return;
            var set = m_dict;
            m_dict = null;   // to prevent anyone from modifying it from the callbacks
            if (Removed != null)
            {
                foreach (var obj in set)
                    Removed(obj.Key, obj.Value);
            }
            set.Clear();
            m_dict = set;
        }

        // bool ICollection<T>.IsReadOnly => false;

        // void ICollection<T>.Add(T item) => Add(item);

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

        public IReadonlyObservableDictionary<TKey, TVal> GetReadonly() => m_readonly;

        public Dictionary<TKey ,TVal>.Enumerator GetEnumerator() => m_dict.GetEnumerator();

        IEnumerator<KeyValuePair<TKey, TVal>> IEnumerable<KeyValuePair<TKey, TVal>>.GetEnumerator() => m_dict.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => m_dict.GetEnumerator();

        public void VisitAll(Action<TKey, TVal> visitor)
        {
            foreach (var pair in m_dict)
                visitor(pair.Key, pair.Value);
        }
    }

    public class ReadonlyObservableDictionary<TKey, TVal> : IEnumerable<KeyValuePair<TKey, TVal>>, IReadonlyObservableDictionary<TKey, TVal>
    {
        private readonly ObservableDictionary<TKey, TVal> m_observableDict;

        public ReadonlyObservableDictionary( ObservableDictionary<TKey, TVal> observableObservableSet )
        {
            m_observableDict = observableObservableSet ?? throw new NullReferenceException("set cannot be null");
        }

        public Type GetKeyType() => typeof(TKey);

        public Type GetValueType() => typeof(TVal);

        public int Count => m_observableDict.Count;

        public event Action<TKey, TVal> Added
        {
            add => m_observableDict.Added += value;
            remove => m_observableDict.Added -= value;
        }

        public event Action<TKey, TVal> Removed
        {
            add => m_observableDict.Removed += value;
            remove => m_observableDict.Removed -= value;
        }

        public bool Contains( TKey key ) => m_observableDict.Contains(key);

        IEnumerator<KeyValuePair<TKey, TVal>> IEnumerable<KeyValuePair<TKey, TVal>>.GetEnumerator() => ((IEnumerable<KeyValuePair<TKey, TVal>>)m_observableDict).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<KeyValuePair<TKey, TVal>>)m_observableDict).GetEnumerator();

        public Dictionary<TKey, TVal>.Enumerator GetEnumerator() => m_observableDict.GetEnumerator();
        
        public void VisitAll(Action<TKey, TVal> visitor) => m_observableDict.VisitAll(visitor);
    }
}
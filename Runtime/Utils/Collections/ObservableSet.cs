using System;
using System.Collections;
using System.Collections.Generic;

namespace SeweralIdeas.Collections
{
    public static class SetExtensions
    {
        public static bool GetAny<T>(this IEnumerable<T> enumerable, out T value)
        {
            var enumerator = enumerable.GetEnumerator();
            if (enumerator.MoveNext())
            {
                value = enumerator.Current;
                return true;
            }
            value = default;
            return false;
        }
    }

    public interface IObservableSet : IReadonlyObservableSet
    {
    }

    public interface IReadonlyObservableSet
    {
        Type GetContainedType();
        
        int Count { get; }
    }

    public interface IObservableSet<T> : IObservableSet, IReadonlyObservableSet<T> { }

    public interface IReadonlyObservableSet<T> : IReadonlyObservableSet
    {
        public event Action<T> Added;
        public event Action<T> Removed;
        public bool Contains(T element);
    }

    public class ObservableSet<T> : ICollection<T>, IObservableSet<T>
    {
        private HashSet<T> m_set = new HashSet<T>();
        public event Action<T> Added;
        public event Action<T> Removed;
        private readonly ReadonlyObservableSet<T> m_readonlyObservableSet;

        public ObservableSet()
        {
            m_readonlyObservableSet = new ReadonlyObservableSet<T>(this);
        }

        public int Count => m_set.Count;

        public Type GetContainedType() => typeof(T);

        public void Clear()
        {
            if (m_set.Count == 0) return;
            var set = m_set;
            m_set = null;   // to prevent anyone from modifying it from the callbacks
            if (Removed != null)
            {
                foreach (var obj in set)
                    Removed(obj);
            }
            set.Clear();
            m_set = set;
        }

        bool ICollection<T>.IsReadOnly => false;

        void ICollection<T>.Add(T item) => Add(item);

        public bool Add( T obj )
        {
            var ret = m_set.Add(obj);
            if ( ret )
                Added?.Invoke(obj);
            return ret;
        }

        public bool Remove( T obj )
        {
            var ret = m_set.Remove(obj);
            if ( ret )
                Removed?.Invoke(obj);
            return ret;
        }

        public bool Contains( T obj )
        {
            return m_set.Contains(obj);
        }

        public ReadonlyObservableSet<T> GetReadonly()
        {
            return m_readonlyObservableSet;
        }

        public HashSet<T>.Enumerator GetEnumerator()
        {
            return m_set.GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return m_set.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_set.GetEnumerator();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            m_set.CopyTo(array, arrayIndex);
        }

        public void VisitAll(Action<T> visitor)
        {
            foreach (var obj in m_set)
            {
                visitor(obj);
            }
            
        }
    }

    public class ReadonlyObservableSet<T> : IEnumerable<T>, IReadonlyObservableSet
    {
        private readonly ObservableSet<T> m_observableObservableSet;

        public ReadonlyObservableSet( ObservableSet<T> observableObservableSet )
        {
            m_observableObservableSet = observableObservableSet ?? throw new NullReferenceException("set cannot be null");
        }

        public int Count => m_observableObservableSet.Count;

        public event Action<T> Added
        {
            add => m_observableObservableSet.Added += value;
            remove => m_observableObservableSet.Added -= value;
        }

        public event Action<T> Removed
        {
            add => m_observableObservableSet.Removed += value;
            remove => m_observableObservableSet.Removed -= value;
        }

        public bool Contains( T obj ) => m_observableObservableSet.Contains(obj);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)m_observableObservableSet).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<T>)m_observableObservableSet).GetEnumerator();

        public HashSet<T>.Enumerator GetEnumerator() => m_observableObservableSet.GetEnumerator();

        Type IReadonlyObservableSet.GetContainedType() => typeof(T);

        public void VisitAll(Action<T> visitor) => m_observableObservableSet.VisitAll(visitor);
    }
}
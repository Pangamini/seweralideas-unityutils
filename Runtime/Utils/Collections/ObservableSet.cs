#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace SeweralIdeas.Collections
{
    public static class SetExtensions
    {
        public static bool GetAny<T>(this IEnumerable<T> enumerable, out T? value)
        {
            using var enumerator = enumerable.GetEnumerator();
            if (enumerator.MoveNext())
            {
                value = enumerator.Current;
                return true;
            }
            value = default;
            return false;
        }
    }

    public interface IObservableSet : IReadonlyObservableSet { }

    public interface IReadonlyObservableSet
    {
        Type GetContainedType();
        int Count { get; }
    }

    public interface IObservableSet<T> : IObservableSet, IReadonlyObservableSet<T> { }

    public interface IReadonlyObservableSet<T> : IReadonlyObservableSet
    {
        public event Action<T>? Added;
        public event Action<T>? Removed;
        public bool Contains(T element);
    }

    public class ObservableSet<T> : ICollection<T>, IObservableSet<T>
    {
        private readonly HashSet<T> m_set = new HashSet<T>();
        public event Action<T>? Added;
        public event Action<T>? Removed;
        
        public int Count => m_set.Count;

        public Type GetContainedType() => typeof(T);

        public void Clear()
        {
            if (m_set.Count == 0) 
                return;

            if (Removed == null)
            {
                m_set.Clear();
                return;
            }
            
            Action<T> removed = Removed;                    // make a copy, so we are not affected by callbacks subscribing more events
            List<T> callList = new List<T>(m_set.Count);    // make a copy, so we only call Removed on currently present objects (also so we don't invalidate enumerators)
            foreach(var elem in m_set)
                callList.Add(elem);

            m_set.Clear();
            
            foreach (var obj in callList)
                removed(obj);
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

        public bool Contains( T obj ) => m_set.Contains(obj);

        public ReadonlyObservableSet<T> GetReadonly() => new(this);

        [MustDisposeResource(false)]
        public HashSet<T>.Enumerator GetEnumerator() => m_set.GetEnumerator();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => m_set.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => m_set.GetEnumerator();

        public void CopyTo(T[] array, int arrayIndex) => m_set.CopyTo(array, arrayIndex);

        public void VisitAll(Action<T> visitor)
        {
            foreach (var obj in m_set)
                visitor(obj);
            
        }
    }

    public readonly struct ReadonlyObservableSet<T> : IEnumerable<T>, IReadonlyObservableSet<T>, IEquatable<ReadonlyObservableSet<T>>
    {
        private readonly ObservableSet<T> m_observableObservableSet;

        public ReadonlyObservableSet( ObservableSet<T> observableObservableSet ) => m_observableObservableSet = observableObservableSet;

        public int Count => m_observableObservableSet.Count;

        public event Action<T>? Added
        {
            add => m_observableObservableSet.Added += value;
            remove => m_observableObservableSet.Added -= value;
        }

        public event Action<T>? Removed
        {
            add => m_observableObservableSet.Removed += value;
            remove => m_observableObservableSet.Removed -= value;
        }

        public bool Contains( T obj ) => m_observableObservableSet.Contains(obj);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => m_observableObservableSet.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => m_observableObservableSet.GetEnumerator();
        public HashSet<T>.Enumerator GetEnumerator() => m_observableObservableSet.GetEnumerator();
        Type IReadonlyObservableSet.GetContainedType() => typeof(T);
        public void VisitAll(Action<T> visitor) => m_observableObservableSet.VisitAll(visitor);
        public bool Equals(ReadonlyObservableSet<T> other) => m_observableObservableSet.Equals(other.m_observableObservableSet);
        public override bool Equals(object? obj) => obj is ReadonlyObservableSet<T> other && Equals(other);
        public override int GetHashCode() => m_observableObservableSet.GetHashCode();
        public static bool operator ==(ReadonlyObservableSet<T> left, ReadonlyObservableSet<T> right) => left.Equals(right);
        public static bool operator !=(ReadonlyObservableSet<T> left, ReadonlyObservableSet<T> right) => !left.Equals(right);
    }
}
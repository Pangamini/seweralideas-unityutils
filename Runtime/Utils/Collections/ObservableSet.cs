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
        private readonly HashSet<T> _set = new HashSet<T>();
        public event Action<T>? Added;
        public event Action<T>? Removed;
        
        public int Count => _set.Count;

        public Type GetContainedType() => typeof(T);

        public void Clear()
        {
            if (_set.Count == 0) 
                return;

            if (Removed == null)
            {
                _set.Clear();
                return;
            }
            
            Action<T> removed = Removed;                    // make a copy, so we are not affected by callbacks subscribing more events
            List<T> callList = new List<T>(_set.Count);    // make a copy, so we only call Removed on currently present objects (also so we don't invalidate enumerators)
            foreach(var elem in _set)
                callList.Add(elem);

            _set.Clear();
            
            foreach (var obj in callList)
                removed(obj);
        }

        bool ICollection<T>.IsReadOnly => false;

        void ICollection<T>.Add(T item) => Add(item);

        public bool Add( T obj )
        {
            var ret = _set.Add(obj);
            if ( ret )
                Added?.Invoke(obj);
            return ret;
        }

        public bool Remove( T obj )
        {
            var ret = _set.Remove(obj);
            if ( ret )
                Removed?.Invoke(obj);
            return ret;
        }

        public bool Contains( T obj ) => _set.Contains(obj);

        public ReadonlyObservableSet<T> GetReadonly() => new(this);

        public static implicit operator ReadonlyObservableSet<T>(ObservableSet<T> set) => set.GetReadonly();
        
        [MustDisposeResource(false)]
        public HashSet<T>.Enumerator GetEnumerator() => _set.GetEnumerator();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => _set.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _set.GetEnumerator();

        public void CopyTo(T[] array, int arrayIndex) => _set.CopyTo(array, arrayIndex);

        public void VisitAll(Action<T> visitor)
        {
            foreach (var obj in _set)
                visitor(obj);
        }
        
        public void SubscribeAndEnumerate(Action<T> onAdded, Action<T> onRemoved)
        {
            Added += onAdded;
            Removed += onRemoved;
            VisitAll(onAdded);
        }
        
        public void UnsubscribeAndEnumerate(Action<T> onAdded, Action<T> onRemoved)
        {
            VisitAll(onRemoved);
            Added -= onAdded;
            Removed -= onRemoved;
        }
    }

    public readonly struct ReadonlyObservableSet<T> : IEnumerable<T>, IReadonlyObservableSet<T>, IEquatable<ReadonlyObservableSet<T>>
    {
        private readonly ObservableSet<T> _observableObservableSet;

        public ReadonlyObservableSet( ObservableSet<T> observableObservableSet ) => _observableObservableSet = observableObservableSet;

        public int Count => _observableObservableSet.Count;

        public event Action<T>? Added
        {
            add => _observableObservableSet.Added += value;
            remove => _observableObservableSet.Added -= value;
        }

        public event Action<T>? Removed
        {
            add => _observableObservableSet.Removed += value;
            remove => _observableObservableSet.Removed -= value;
        }

        public bool Contains( T obj ) => _observableObservableSet.Contains(obj);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => _observableObservableSet.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _observableObservableSet.GetEnumerator();
        public HashSet<T>.Enumerator GetEnumerator() => _observableObservableSet.GetEnumerator();
        Type IReadonlyObservableSet.GetContainedType() => typeof(T);
        public void VisitAll(Action<T> visitor) => _observableObservableSet.VisitAll(visitor);
        public bool Equals(ReadonlyObservableSet<T> other) => _observableObservableSet.Equals(other._observableObservableSet);
        public override bool Equals(object? obj) => obj is ReadonlyObservableSet<T> other && Equals(other);
        public override int GetHashCode() => _observableObservableSet.GetHashCode();
        public static bool operator ==(ReadonlyObservableSet<T> left, ReadonlyObservableSet<T> right) => left.Equals(right);
        public static bool operator !=(ReadonlyObservableSet<T> left, ReadonlyObservableSet<T> right) => !left.Equals(right);
        public void SubscribeAndEnumerate(Action<T> onAdded, Action<T> onRemoved) => _observableObservableSet.SubscribeAndEnumerate(onAdded, onRemoved);
        public void UnsubscribeAndEnumerate(Action<T> onAdded, Action<T> onRemoved) => _observableObservableSet.UnsubscribeAndEnumerate(onAdded, onRemoved);
    }
}
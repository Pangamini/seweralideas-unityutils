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

    public interface ISet
    {
        System.Type GetContainedType();
    }

    public interface IReadonlySet
    {
        System.Type GetContainedType();
    }

    public class Set<T> : ICollection<T>, ISet
    {
        private HashSet<T> m_set = new HashSet<T>();
        public event System.Action<T> Added;
        public event System.Action<T> Removed;
        private ReadonlySet<T> m_readonly;

        public Set()
        {
            m_readonly = new ReadonlySet<T>(this);
        }

        public int Count => m_set.Count;

        System.Type ISet.GetContainedType()
        {
            return typeof(T);
        }

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

        public ReadonlySet<T> GetReadonly()
        {
            return m_readonly;
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

    public class ReadonlySet<T> : IEnumerable<T>, IReadonlySet
    {
        private Set<T> m_set;

        public ReadonlySet( Set<T> set )
        {
            if ( set == null )
                throw new System.NullReferenceException("set cannot be null");
            m_set = set;
        }

        public int Count
        {
            get { return m_set.Count; }
        }

        public event System.Action<T> onAdded
        {
            add  { m_set.Added += value; }
            remove { m_set.Added -= value;}
        }

        public event System.Action<T> onRemoved
        {
            add { m_set.Removed += value; }
            remove { m_set.Removed -= value; }
        }

        public bool Contains( T obj )
        {
            return m_set.Contains(obj);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return ((IEnumerable<T>)m_set).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)m_set).GetEnumerator();
        }

        public HashSet<T>.Enumerator GetEnumerator()
        {
            return m_set.GetEnumerator();
        }

        System.Type IReadonlySet.GetContainedType()
        {
            return typeof(T);
        }

        public void VisitAll(Action<T> visitor)
        {
            m_set.VisitAll(visitor);
        }
    }
}
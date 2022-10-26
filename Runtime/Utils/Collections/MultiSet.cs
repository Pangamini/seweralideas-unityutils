using System;
using System.Collections;
using System.Collections.Generic;

namespace SeweralIdeas.Collections
{
    public interface IMultiSet
    {
        Type GetContainedType();
    }

    public interface IReadonlyMultiSet
    {
        Type GetContainedType();
    }
    

    [System.Serializable]
    public class MultiSet<T> : IEnumerable<T>, IMultiSet
    {
        private Dictionary<T,uint> m_multiSet = new Dictionary<T,uint>();
        public event Action<T> onAdded;
        public event Action<T> onRemoved;
        private ReadonlyMultiSet<T> m_readonly;

        public override string ToString()
        {
            var separator = ", ";
            var builder = new System.Text.StringBuilder();
            foreach (var obj in m_multiSet)
            {
                if (builder.Length != 0)
                    builder.Append(separator);
                builder.Append(obj);
            }
            return builder.ToString();
        }

        public Enumerable Enumerate()
        {
            return new Enumerable(m_multiSet.GetEnumerator());
        }

        public struct Enumerable
        {
            public Dictionary<T, uint>.Enumerator enumerator;

            public Enumerable(Dictionary<T, uint>.Enumerator enumerator)
            {
                this.enumerator = enumerator;
            }
            public Dictionary<T, uint>.Enumerator GetEnumerator()
            {
                return enumerator;
            }
        }

        public MultiSet()
        {
            m_readonly = new ReadonlyMultiSet<T>(this);
        }

        public int Count
        {
            get { return m_multiSet.Count; }
        }
        
        Type IMultiSet.GetContainedType()
        {
            return typeof(T);
        }

        [NonSerialized]
        private bool m_clearing = false;
        public void Clear()
        {
            m_clearing = true;
            try
            {
                if (onRemoved != null)
                    foreach (var obj in m_multiSet)
                        onRemoved(obj.Key);
            }
            finally
            {
                m_multiSet.Clear();
                m_clearing = false;
            }
        }


        public bool Add(T obj)
        {
            return Add(obj, 1);
        }

        public bool Add(T obj, uint count)
        {
            if (m_clearing)
                throw new System.InvalidOperationException("Cannot add objects to set while Clear() is running");
            if (!m_multiSet.ContainsKey(obj))
            {
                m_multiSet.Add(obj, count);
                if (onAdded != null)
                    onAdded(obj);
                return true;
            }

            m_multiSet[obj] += count;

            return false;
        }

        public bool Remove(T obj)
        {
            return Remove(obj, 1);
        }

        public bool Remove(T obj, uint count)
        {
            if (m_clearing)
                return false;
            uint value;
            if ( m_multiSet.TryGetValue(obj, out value) )
            {
                if ( count >= value )
                {
                    var ret = m_multiSet.Remove(obj);
                    if ( ret )
                        if ( onRemoved != null )
                            onRemoved(obj);

                    return true;
                }

                m_multiSet[obj] -= count;
            }

            
            return false;
        }
        
        public bool Contains(T obj)
        {
            return m_multiSet.ContainsKey(obj);
        }

        public ReadonlyMultiSet<T> GetReadonly()
        {
            return m_readonly;
        }

        public Dictionary<T,uint>.KeyCollection.Enumerator GetEnumerator()
        {
            return m_multiSet.Keys.GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return m_multiSet.Keys.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_multiSet.Keys.GetEnumerator();
        }

        public uint GetCount(T t) {
            return m_multiSet[t];
        }
    }

    public class ReadonlyMultiSet<T> : IEnumerable<T>
    {
        private MultiSet<T> m_set;

        public ReadonlyMultiSet(MultiSet<T> set)
        {
            if (set == null)
                throw new NullReferenceException("set cannot be null");
            m_set = set;
        }

        public int Count
        {
            get { return m_set.Count; }
        }      

        public uint GetValue(T t) {
            return m_set.GetCount(t);
        }

        public event Action<T> onAdded
        {
            add { m_set.onAdded += value; }
            remove { m_set.onAdded -= value; }
        }

        public event Action<T> onRemoved
        {
            add { m_set.onRemoved += value; }
            remove { m_set.onRemoved -= value; }
        }
        

        public bool Contains(T obj)
        {
            return m_set.Contains(obj);
        }

        public Dictionary<T, uint>.KeyCollection.Enumerator GetEnumerator()
        {
            return m_set.GetEnumerator();
        }
        

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        

    }
}
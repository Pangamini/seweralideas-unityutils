#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;

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
    
    [Serializable]
    public class MultiSet<T> : IEnumerable<T>, IMultiSet, IReadonlyObservableSet<T>
    {
        private Dictionary<T,uint> m_multiSet = new Dictionary<T,uint>();
        public event Action<T>? Added;
        public event Action<T>? Removed;
        
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

        public Enumerable Enumerate() => new Enumerable(m_multiSet.GetEnumerator());

        public struct Enumerable
        {
            private readonly Dictionary<T, uint>.Enumerator m_enumerator;
            public Enumerable(Dictionary<T, uint>.Enumerator enumerator) => m_enumerator = enumerator;
            public Dictionary<T, uint>.Enumerator GetEnumerator() => m_enumerator;
        }
        
        public int Count => m_multiSet.Count;

        public Type GetContainedType() => typeof(T);

        public void Clear()
        {
            if (m_multiSet.Count == 0) 
                return;
            
            var set = m_multiSet;
            m_multiSet = null!;   // to prevent anyone from modifying it from the callbacks
            List<Exception>? exceptions = null;

            try
            {
                if(Removed != null)
                {
                    Action<T> removed = Removed; // make a copy
                    foreach (var pair in set)
                    {
                        try
                        {
                            removed(pair.Key);
                        }
                        catch( Exception e )
                        {
                            exceptions ??= new List<Exception>();
                            exceptions.Add(e);
                        }
                    }
                }
            }
            finally
            {
                set.Clear();
                m_multiSet = set;
            }

            if(exceptions != null)
                throw new AggregateException(exceptions);
        }


        public bool Add(T obj)
        {
            return Add(obj, 1);
        }

        public bool Add(T obj, uint count)
        {
            if (m_multiSet.TryAdd(obj, count))
            {
                Added?.Invoke(obj);
                return true;
            }

            m_multiSet[obj] += count;

            return false;
        }

        public bool Remove(T obj) => Remove(obj, 1);

        public void RemoveAll(T obj)
        {
            if (m_multiSet.Remove(obj))
            {
                Removed?.Invoke(obj);
            }
        }
        
        public bool Remove(T obj, uint count)
        {
            uint value;
            if ( m_multiSet.TryGetValue(obj, out value) )
            {
                if ( count >= value )
                {
                    bool ret = m_multiSet.Remove(obj);
                    if ( ret )
                    {
                        Removed?.Invoke(obj);
                    }

                    return true;
                }

                m_multiSet[obj] -= count;
            }

            
            return false;
        }
        
        public bool Contains(T obj) => m_multiSet.ContainsKey(obj);

        public ReadonlyMultiSet<T> GetReadonly() => new ReadonlyMultiSet<T>(this);

        [MustDisposeResource(false)]
        public Dictionary<T,uint>.KeyCollection.Enumerator GetEnumerator() => m_multiSet.Keys.GetEnumerator();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => m_multiSet.Keys.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => m_multiSet.Keys.GetEnumerator();

        public uint GetCount(T t) => m_multiSet[t];

    }

    public readonly struct ReadonlyMultiSet<T> : IEnumerable<T>, IReadonlyObservableSet<T>
    {
        private readonly MultiSet<T> m_set;

        public ReadonlyMultiSet(MultiSet<T> set)
        {
            m_set = set;
        }

        public Type GetContainedType() => typeof( T );
        
        public int Count => m_set.Count;

        public uint GetValue(T t) {
            return m_set.GetCount(t);
        }

        public event Action<T>? Added
        {
            add => m_set.Added += value;
            remove => m_set.Added -= value;
        }

        public event Action<T>? Removed
        {
            add => m_set.Removed += value;
            remove => m_set.Removed -= value;
        }
        

        public bool Contains(T obj)
        {
            return m_set.Contains(obj);
        }

        public Dictionary<T, uint>.KeyCollection.Enumerator GetEnumerator() => m_set.GetEnumerator();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


    }
}
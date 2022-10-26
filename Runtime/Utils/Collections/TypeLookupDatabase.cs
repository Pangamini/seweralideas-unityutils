using System;
using System.Collections.Generic;

namespace SeweralIdeas.Collections
{
    /// <summary>
    /// A simple database of objects organized by their type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TypeLookupDatabase<T> where T : class
    {
        private Dictionary<Type, IMySet> m_objectsByType = new Dictionary<Type, IMySet>();

        public TypeLookup<T> Readonly { get; }

        private readonly ICollection<T> m_externalCollection;
        
        /// <param name="externalCollection">External collection, must always return all the objects maintained by this object.</param>
        public TypeLookupDatabase(ICollection<T> externalCollection)
        {
            m_externalCollection = externalCollection;
            Readonly = new TypeLookup<T>(this);
        }

        /// <summary>
        /// Must always be called when a new object is added to the external collection.
        /// </summary>
        public void AddObject(T obj)
        {
            foreach (var pair in m_objectsByType)
            {
                pair.Value.TryAdd(obj);
            }
        }

        /// <summary>
        /// Must always be called when a new object is removed from the external collection.
        /// </summary>
        public void RemoveObject(T obj)
        {
            foreach (var pair in m_objectsByType)
            {
                pair.Value.TryRemove(obj);
            }
        }

        private interface IMySet
        {
            void TryAdd(T elem);
            void TryRemove(T elem);
            void Clear();
        }
        
        private class MySet<TElem> : Set<TElem>, IMySet
        {
            public void TryAdd(T elem)
            {
                if (elem is TElem typed)
                    Add(typed);
            }

            public void TryRemove(T elem)
            {
                if (elem is TElem typed)
                    Remove(typed);
            }
        }

        public ReadonlySet<TType> GetObjectsByType<TType>() where TType : class
        {
            if (m_objectsByType.TryGetValue(typeof(TType), out IMySet set))
            {
                return ((Set<TType>)set).GetReadonly();
            }
            
            var mySet = new MySet<TType>();
            set = mySet;

            foreach(var obj in m_externalCollection)
            {
                if (obj is TType typed)
                    mySet.Add(typed);
            }
           
            m_objectsByType.Add(typeof(TType), set);
            return ((Set<TType>)set).GetReadonly();
        }

        public void Clear()
        {
            foreach (KeyValuePair<Type, IMySet> pair in m_objectsByType)
            {
                pair.Value.Clear();
            }
        }

    }
    
    public class TypeLookup<T> where T:class
    {
        private readonly TypeLookupDatabase<T> m_lookupDatabase;

        public TypeLookup(TypeLookupDatabase<T> lookupDatabase)
        {
            m_lookupDatabase = lookupDatabase;
        }

        public ReadonlySet<TType> GetObjectsByType<TType>() where TType : class => m_lookupDatabase.GetObjectsByType<TType>();
    }


}
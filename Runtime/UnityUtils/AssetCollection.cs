using SeweralIdeas.Collections;
using UnityEngine;

namespace SeweralIdeas.UnityUtils
{
    public class AssetCollection<T> : ScriptableObject// where T:Object
    {
        [SerializeField] private T[] m_collection;

        public T this[int index] => m_collection[index];
        public int Count => m_collection.Length;

        public T PickRandom()
        {
            if (m_collection.Length == 0)
                return default;
            return m_collection[Random.Range(0, m_collection.Length)];
        }

        public ReadonlyArrayView<T>.Enumerator GetEnumerator() => new(m_collection);
    }
}
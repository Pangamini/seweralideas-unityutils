using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SeweralIdeas.UnityUtils
{
    public class DistributedRandom<T>
    {
        private List<T> m_options = new List<T>();
        private List<T> m_currentOptions = new List<T>();

        public void Initialize(IEnumerable<T> options)
        {          
            m_options.Clear();
            m_currentOptions.Clear();

            m_options.AddRange(options);

            if (m_options.Count == 0)
                throw new System.ArgumentException($"{nameof(DistributedRandom<T>)} must be initialized with a non-empty collection");

        }

        private void TryRefill()
        {
            // if currentOptions are depleted, copy in the m_options
            if (m_currentOptions.Count == 0)
            {
                m_currentOptions.Capacity = m_options.Count;
                for (int i = 0; i < m_options.Count; ++i)
                    m_currentOptions.Add(m_options[i]);
            }

        }

        public bool UseSpecificElement(T element)
        {
            TryRefill();
            return m_currentOptions.Remove(element);
        }

        public T GetRandom()
        {
            TryRefill();
            var randomIndex = Random.Range(0, m_currentOptions.Count);
            var element = m_currentOptions[randomIndex];
            var lastIndex = m_currentOptions.Count - 1;

            // remove the element from the end, it's more efficient
            m_currentOptions[randomIndex] = m_currentOptions[lastIndex];
            m_currentOptions.RemoveAt(lastIndex);

            return element;
        }

    }
}
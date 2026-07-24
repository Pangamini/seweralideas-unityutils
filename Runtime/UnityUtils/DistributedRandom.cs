#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace SeweralIdeas.UnityUtils
{
    public class DistributedRandom<T>
    {
        private readonly List<T> _options        = new List<T>();
        private readonly List<T> _currentOptions = new List<T>();

        public void Initialize(IEnumerable<T> options)
        {          
            _options.Clear();
            _currentOptions.Clear();

            _options.AddRange(options);

            if (_options.Count == 0)
                throw new System.ArgumentException($"{nameof(DistributedRandom<T>)} must be initialized with a non-empty collection");

        }

        private void TryRefill()
        {
            // if currentOptions are depleted, copy in the m_options
            if (_currentOptions.Count == 0)
            {
                _currentOptions.Capacity = _options.Count;
                for (int i = 0; i < _options.Count; ++i)
                    _currentOptions.Add(_options[i]);
            }

        }

        public bool UseSpecificElement(T element)
        {
            TryRefill();
            return _currentOptions.Remove(element);
        }

        public T GetRandom()
        {
            TryRefill();
            var randomIndex = Random.Range(0, _currentOptions.Count);
            var element = _currentOptions[randomIndex];
            var lastIndex = _currentOptions.Count - 1;

            // remove the element from the end, it's more efficient
            _currentOptions[randomIndex] = _currentOptions[lastIndex];
            _currentOptions.RemoveAt(lastIndex);

            return element;
        }

    }
}
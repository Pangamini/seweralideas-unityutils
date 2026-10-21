using System;
using System.Collections.Generic;
using UnityEngine;

namespace SeweralIdeas.UnityUtils.Drawers
{
    /// <summary>
    /// A serialized set of enum values, drawn as one multi-select dropdown instead of an array.
    /// Unlike a flags enum it works with any enum, whatever its values are. (A <see cref="PropertyAttribute"/>
    /// on an array applies to each element, so a drawer can't turn a plain <c>T[]</c> into one control.)
    /// </summary>
    [Serializable]
    public class EnumSet<T> where T : struct, Enum
    {
        [SerializeField] private T[] _values = Array.Empty<T>();

        public IReadOnlyList<T> Values => _values;

        public bool Contains(T value)
        {
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            foreach(T element in _values)
            {
                if(comparer.Equals(element, value))
                    return true;
            }
            return false;
        }
    }
}

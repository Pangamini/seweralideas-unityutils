using System;
using UnityEngine;

namespace SeweralIdeas.Utils
{
    /// <summary>
    /// A serializable value that may or may not override something else, like the fields of a Volume. There is no default
    /// to fall back on here: the reader decides what applies when there is no override (see <see cref="ValueOr"/>). The override
    /// itself may be null for a reference type, which is not the same as having none. For a value type,
    /// <see cref="OverrideExtensions.AsNullable{T}"/> gives a <c>T?</c> that is null without one.
    /// </summary>
    [Serializable]
    public struct Override<T> 
    {
        [SerializeField] private bool _enabled;
        [SerializeField] private T    _value;

        public Override(T value)
        {
            _enabled = true;
            _value = value;
        }

        public bool HasValue => _enabled;

        /// <summary>The override. Throws if there is none.</summary>
        public T Value => _enabled ? _value : throw new InvalidOperationException("The override is not enabled.");

        public bool TryGetValue(out T value)
        {
            value = _value;
            return _enabled;
        }

        /// <summary>The override if there is one, otherwise the fallback.</summary>
        public T ValueOr(T fallback) => _enabled ? _value : fallback;

        public static implicit operator Override<T>(T value) => new(value);

        /// <summary>The override. Throws if there is none, like <see cref="Value"/>.</summary>
        public static explicit operator T(Override<T> o) => o.Value;
    }

    public static class OverrideExtensions
    {
        /// <summary>The override, or null if there is none.</summary>
        public static T? AsNullable<T>(this Override<T> o) where T : struct => o.HasValue ? o.Value : null;
    }
}

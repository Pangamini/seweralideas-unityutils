#nullable enable
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SeweralIdeas.UnityUtils
{
    /// <summary>
    /// A serialized reference to a <see cref="UnityEngine.Object"/> (a component, a ScriptableObject...) that implements the
    /// interface <typeparamref name="T"/>. Unity cannot serialize an interface, so the object is stored as a plain object reference
    /// and cast to <typeparamref name="T"/> when read. The inspector only accepts an object that implements it: a GameObject
    /// dropped on the field gives its first component that does.
    /// <para>
    /// The cast is done on every read instead of being cached: a cache in a struct is lost on copies and goes stale across hot
    /// reloads, and the cast is cheap.
    /// </para>
    /// </summary>
    [Serializable]
    public struct InterfaceRef<T> where T : class
    {
        [SerializeField] private Object? _object;

        /// <exception cref="ArgumentException">The value is not a <see cref="UnityEngine.Object"/>.</exception>
        public InterfaceRef(T? value)
        {
            _object = ToObject(value);
        }

        /// <summary>
        /// The referenced object as <typeparamref name="T"/>, or null if nothing is referenced, the object was destroyed, or it does
        /// not implement <typeparamref name="T"/> (which the inspector prevents, but a changed interface or a script can cause).
        /// </summary>
        public T? Value
        {
            get => _object != null ? _object as T : null; // != null is Unity's: a destroyed object is not a reference to use
            set => _object = ToObject(value);
        }

        /// <summary>Whether <see cref="Value"/> is not null.</summary>
        public bool HasValue => Value != null;

        /// <summary>The referenced object itself, without the cast. For things that need the Unity object, like pinging or destroying it.</summary>
        public Object? UnityObject => _object != null ? _object : null;

        public static implicit operator T?(InterfaceRef<T> reference) => reference.Value;
        public static implicit operator InterfaceRef<T>(T? value) => new(value);

        private static Object? ToObject(T? value)
        {
            if(value == null)
                return null;
            if(value is Object unityObject)
                return unityObject;
            throw new ArgumentException($"{value.GetType().Name} is not a UnityEngine.Object, so it cannot be serialized as an {nameof(InterfaceRef<T>)}<{typeof(T).Name}>.");
        }
    }
}

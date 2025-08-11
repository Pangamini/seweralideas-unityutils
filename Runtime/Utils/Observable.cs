using System;
#if UNITY_5_3_OR_NEWER
using UnityEngine;
#endif

namespace SeweralIdeas.Utils
{
    public interface IReadonlyObservable<out T>
    {
        public event Action<T, T> Changed;
        public T Value { get; }
    }

    [Serializable]
    public class Observable<T> : IReadonlyObservable<T>
    {
        public Readonly ReadOnly => new Readonly(this);
#if UNITY_5_3_OR_NEWER
        [SerializeField]
#endif

        private T _value;

        private Action<T, T> _onChanged;

        public Observable(T defaultValue = default)
        {
            _value = defaultValue;
        }

        public T Value
        {
            get => _value;
            set
            {
                if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(_value, value))
                    return;
                var old = _value;
                _value = value;
                _onChanged?.Invoke(value, old);
            }
        }

        public event Action<T, T> Changed
        {
            add
            {
                _onChanged += value;
                value(Value, default);
            }
            remove
            {
                value(default, Value);
                _onChanged -= value;
            }
        }

        public readonly struct Readonly : IReadonlyObservable<T>, IEquatable<Readonly>
        {
            public Readonly(Observable<T> observable)
            {
                _observable = observable;
            }

            private readonly Observable<T> _observable;
            public T Value => _observable.Value;

            public event Action<T, T> Changed
            {
                add => _observable.Changed += value;
                remove => _observable.Changed -= value;
            }

            public static implicit operator Readonly(Observable<T> observable) => observable.ReadOnly;

            public bool Equals(Readonly other) => _observable.Equals(other._observable);
            public override bool Equals(object obj) => obj is Readonly other && Equals(other);
            public override int GetHashCode() => _observable.GetHashCode();
            public static bool operator ==(Readonly left, Readonly right) => left.Equals(right);
            public static bool operator !=(Readonly left, Readonly right) => !left.Equals(right);
        }
    }
}
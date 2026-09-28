using System;
#if UNITY_5_3_OR_NEWER
using UnityEngine;
#endif

namespace SeweralIdeas.Utils
{
    [Obsolete("Observable<T>.Changed no longer reports the previous value (it was unused by nearly every subscriber and was unreliable around subscribe/unsubscribe notifications). Migrate handlers to a single T parameter (Action<T>); compute deltas yourself if you still need one.")]
    public delegate void ObservableAction<in T>(T newValue, T oldValue);

    public interface IReadonlyObservable<out T>
    {
        public event Action<T> Changed;
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

        private Action<T> _onChanged;

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
                _value = value;
                _onChanged?.Invoke(value);
            }
        }

        // Subscribing immediately invokes the handler with the current value.
        // Unsubscribing does not invoke the handler.
        public event Action<T> Changed
        {
            add
            {
                _onChanged += value;
                value(Value);
            }
            remove => _onChanged -= value;
        }

        [Obsolete("Unsubscribing no longer notifies listeners, so this is now identical to 'Changed -= listener'. Use that instead.")]
        public void UnsubscribeWithoutNotify(Action<T> listener) => _onChanged -= listener;

        public readonly struct Readonly : IReadonlyObservable<T>, IEquatable<Readonly>
        {
            public Readonly(Observable<T> observable)
            {
                _observable = observable;
            }

            private readonly Observable<T> _observable;
            public T Value => _observable.Value;

            public event Action<T> Changed
            {
                add => _observable.Changed += value;
                remove => _observable.Changed -= value;
            }

            [Obsolete("Unsubscribing no longer notifies listeners, so this is now identical to 'Changed -= listener'. Use that instead.")]
            public void UnsubscribeWithoutNotify(Action<T> listener) => _observable.Changed -= listener;

            public static implicit operator Readonly(Observable<T> observable) => observable.ReadOnly;

            public bool Equals(Readonly other) => _observable.Equals(other._observable);
            public override bool Equals(object obj) => obj is Readonly other && Equals(other);
            public override int GetHashCode() => _observable.GetHashCode();
            public static bool operator ==(Readonly left, Readonly right) => left.Equals(right);
            public static bool operator !=(Readonly left, Readonly right) => !left.Equals(right);
        }
    }
}
using System;
using System.Collections.Generic;

namespace SeweralIdeas.Utils
{
    /// <summary>
    /// A value that all of the enabled requests contribute to, where <see cref="MultiControl{T}"/> is decided by the one
    /// request with the highest priority. The value is the default value with every enabled request folded into it by the
    /// combine function given to the constructor: <c>value = combine(value, request.Value)</c>, for each request in the order
    /// they were enabled. With no requests it is the default value.
    ///
    /// So the default value is where the folding starts, and for the usual combine functions the neutral element:
    /// <code>
    /// new AggregateControl&lt;float&gt;(float.PositiveInfinity, Mathf.Min)   // the lowest requested value (no limit without requests)
    /// new AggregateControl&lt;float&gt;(1f, (a, b) => a * b)                 // all the requested values multiplied
    /// new AggregateControl&lt;float&gt;(0f, (a, b) => a + b)                 // their sum
    /// new AggregateControl&lt;bool&gt;(false, (a, b) => a || b)              // whether anything asks for it
    /// </code>
    /// A default value that is not neutral takes part like a request that is always there: a default of 10 with the
    /// lowest-value function gives at most 10.
    ///
    /// The value is worked out again whenever a request is enabled, disabled, or changes its value, and reaches
    /// <see cref="Observable"/> if it differs from before. Requests have to be disposed of when they are no longer needed.
    /// </summary>
    public class AggregateControl<T>
    {
        private readonly T                 _defaultValue;
        private readonly Func<T, T, T>     _combine;
        private readonly List<Request>     _requests = new();
        private readonly Observable<T>     _observable;

        public Observable<T>.Readonly Observable => _observable;

        /// <summary>The value now.</summary>
        public T Value => _observable.Value;

        public AggregateControl(T defaultValue, Func<T, T, T> combine)
        {
            _defaultValue = defaultValue;
            _combine = combine ?? throw new ArgumentNullException(nameof(combine));
            _observable = new Observable<T>(defaultValue);
        }

        public AggregateControl(T defaultValue, Func<T, T, T> combine, Action<T> callback) : this(defaultValue, combine)
        {
            _observable.Changed += callback;
        }

        public Request CreateRequest(string name, T value = default, bool enabled = true)
        {
            return new Request(name, this, value, enabled);
        }

        private void Evaluate()
        {
            T value = _defaultValue;
            for (int i = 0; i < _requests.Count; i++)
                value = _combine(value, _requests[i].Value);

            // after the whole value is known, so that a listener can change the requests without upsetting this
            _observable.Value = value;
        }

        public sealed class Request : IMultiControlRequest
        {
            private AggregateControl<T> _owner;
            private T                   _value;
            private bool                _enabled;

            internal Request(string name, AggregateControl<T> owner, T value, bool enabled)
            {
                Name = name;
                _owner = owner ?? throw new ArgumentNullException(nameof(owner));
                _value = value;
                Enabled = enabled;
            }

            public string Name { get; set; }

            /// <summary>What this request asks for. Counts only while it is enabled.</summary>
            public T Value
            {
                get => _value;
                set
                {
                    if (EqualityComparer<T>.Default.Equals(_value, value))
                        return;
                    _value = value;
                    if (_enabled)
                        _owner.Evaluate();
                }
            }

            public bool Enabled
            {
                get => _enabled;
                set
                {
                    if (_enabled == value)
                        return;

                    if (_owner == null)
                        throw new ObjectDisposedException($"AggregateControl.Request {Name}");

                    _enabled = value;
                    if (_enabled)
                        _owner._requests.Add(this);
                    else
                        _owner._requests.Remove(this);
                    _owner.Evaluate();
                }
            }

            public void Dispose()
            {
                if (_owner == null)
                    return;
                Enabled = false;
                _owner = null;
                GC.SuppressFinalize(this);
            }

            ~Request()
            {
                if (_owner == null)
                    return;

                // Only a disabled request can end up here: an enabled one is still held by its control.
                string message = $"AggregateControl.Request {Name} was not disposed of properly";
#if UNITY_5_3_OR_NEWER
                UnityEngine.Debug.LogError(message);
#else
                Console.WriteLine(message);
#endif
            }

            public override string ToString() => $"{Name}(value={Value}, {(Enabled ? "enabled" : "disabled")})";
        }
    }
}

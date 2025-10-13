using System;
using System.Collections.Generic;
using UnityEngine;

namespace SeweralIdeas.Utils
{
    public interface IMultiControlRequest : IDisposable
    {
        bool Enabled { get; set; }
        string Name { get; set; }
    }

    public class MultiControl<T> : IComparer<MultiControl<T>.Request>
    {
        private readonly T                  _defaultValue;
        private readonly SortedSet<Request> _requests;
        private readonly Observable<T>      _observable;

        public Observable<T>.Readonly Observable => _observable;

        public MultiControl(T defaultValue = default)
        {
            _requests = new SortedSet<Request>(this);
            _defaultValue = defaultValue;
            _observable = new(_defaultValue);
        }

        public MultiControl(T defaultValue, Action<T, T> callback) : this(defaultValue)
        {
            _observable.Changed += callback;
        }

        int IComparer<Request>.Compare(Request x, Request y) => CompareRequests(x, y);

        private static int CompareRequests(Request lhs, Request rhs)
        {
            if (ReferenceEquals(lhs, rhs))
                return 0;

            int result = -lhs.Priority.CompareTo(rhs.Priority);
            if (result != 0)
                return result;

            result = lhs.UniqueId.CompareTo(rhs.UniqueId);
            Debug.Assert(result != 0); // can't be, we compared ReferenceEquals
            return result;
        }

        public Request CreateRequest(string name, int priority, T value = default, bool enabled = true)
        {
            return new Request(name, priority, this, value, enabled);
        }

        private void AddRequest(Request request)
        {
            bool added = _requests.Add(request);
            Debug.Assert(added);
            OnRequestChanged(request);
        }

        private void RemoveRequest(Request request)
        {
            bool removed = _requests.Remove(request);
            Debug.Assert(removed);
            OnRequestChanged(request);
        }

        private void OnRequestChanged(Request changedRequest)
        {
            var value = _defaultValue;

            foreach (var request in _requests)
            {
                Debug.Assert(request.Enabled);
                value = request.Value;
                break;
            }

            _observable.Value = value;
        }

        public class Request : IMultiControlRequest
        {
            private static int _idCounter = 0;

            private MultiControl<T> _owner;
            private T               _value;
            private bool            _enabled = false;

            public readonly int UniqueId;
            public readonly int Priority;

            public Request(string name, int priority, MultiControl<T> multiControl, T value = default, bool enabled = true)
            {
                Name = name;
                _owner = multiControl ?? throw new ArgumentNullException();
                Priority = priority;
                Value = value;
                UniqueId = _idCounter++;
                Enabled = enabled;
            }

            public string Name { get; set; }

            public T Value
            {
                get => _value;
                set
                {
                    if (EqualityComparer<T>.Default.Equals(_value, value)) return;
                    _value = value;
                    if (Enabled)
                        _owner.OnRequestChanged(this);
                }
            }

            public bool Enabled
            {
                get => _enabled;
                set
                {
                    if (_enabled == value) return;
                    _enabled = value;
                    if (_enabled)
                        _owner.AddRequest(this);
                    else
                        _owner.RemoveRequest(this);
                }
            }

            public void Dispose()
            {
                if (ReferenceEquals(_owner, null))
                    return;
                Enabled = false;
                _owner = null;
                _value = default;
                GC.SuppressFinalize(this);
            }

            ~Request()
            {
                Dispose();
                string msg = $"MultiControl.Request {Name} was not disposed of properly";
#if UNITY
                UnityEngine.Debug.LogError(msg);
#else
                Console.WriteLine(msg);
#endif
            }

            public override string ToString() => $"{Name}[{Priority}](value={Value}, {(Enabled ? "enabled" : "disabled")})";
        }
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SeweralIdeas.Utils
{
    public class AccumulativeControl<T> : IComparer<AccumulativeControl<T>.Request>
    {
        public event Action BecameDirty
        {
            add
            {
                _becameDirty += value;
                if(_dirty)
                    value();
            }
            remove => _becameDirty -= value;
        }

        public readonly  T                  DefaultValue;
        private          Action             _becameDirty;
        private          bool               _dirty;
        private readonly SortedSet<Request> _requests;
        private readonly Observable<T>      _value = new();
        
        public Observable<T>.Readonly Value => _value;

        public AccumulativeControl(T defaultValue = default)
        {
            _requests = new SortedSet<Request>(this);
            DefaultValue = defaultValue;
            _value.Value = DefaultValue;
        }

        int IComparer<Request>.Compare(Request x, Request y) => CompareRequests(x, y);

        private static int CompareRequests(Request lhs, Request rhs)
        {
            if(ReferenceEquals(lhs, rhs))
                return 0;
            
            int result = lhs.Priority.CompareTo(rhs.Priority);
            if(result != 0)
                return result;
            
            result = lhs.UniqueId.CompareTo(rhs.UniqueId);
            Debug.Assert(result != 0);  // can't be, we compared ReferenceEquals
            return result;
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
            if(_dirty)
                return;
            _dirty = true;
            _becameDirty?.Invoke();
        }

        public void Update()
        {
            if(!_dirty)
                return;

            T value = DefaultValue;
            foreach (var request in _requests)
            {
                request.Evaluate(ref value);
            }
            _value.Value = value;
            
            _dirty = false;
        }
        
        public abstract class Request
        {
            // ReSharper disable once StaticMemberInGenericType
            private static int s_idCounter = 0;

            protected Request(AccumulativeControl<T> accumulativeControl, int priority, bool enabled = true)
            {
                _owner = accumulativeControl ?? throw new ArgumentNullException();
                Priority = priority;
                UniqueId = s_idCounter++;
                Enabled = enabled;
            }

            public abstract void Evaluate(ref T currentValue);
            public void RequestUpdate() => _owner.OnRequestChanged(this);

            internal readonly int UniqueId;
            public readonly int Priority;
            
            private AccumulativeControl<T> _owner;
            private bool                   _enabled = false;

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
                GC.SuppressFinalize(this);
            }

            ~Request()
            {
                Dispose();
                string msg = $"AccumulativeControl.Request was not disposed of properly";
#if UNITY
                UnityEngine.Debug.LogError(msg);
#else
                Console.WriteLine(msg);
#endif
            }
        }
    }
}
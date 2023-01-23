using System;
using System.Collections.Generic;

namespace SeweralIdeas.Utils
{
    public interface IMultiControlRequest : IDisposable
    {
        bool Enabled { get; set; }
        string Name { get; set; }
    }

    public class MultiControl<T> : IComparer<MultiControl<T>.Request>
    {
        private T m_defaultValue;
        private SortedSet<Request> m_requests;

        private T m_value;

        public T Value
        {
            get { return m_value; }
            private set
            {
                if (EqualityComparer<T>.Default.Equals(m_value, value))
                    return;
                m_value = value;
                ValueChanged?.Invoke(Value);
            }
        }

        public MultiControl(T defaultValue = default)
        {
            m_requests = new SortedSet<Request>(this);
            m_defaultValue = defaultValue;
            m_value = m_defaultValue;
        }

        public MultiControl(T defaultValue, Action<T> callback) : this(defaultValue)
        {
            ValueChanged += callback;
        }

        public event Action<T> ValueChanged;

        int IComparer<Request>.Compare(Request x, Request y)
        {
            return CompareRequests(x, y);
        }

        private static int CompareRequests(Request lhs, Request rhs)
        {
            int result = -lhs.Priority.CompareTo(rhs.Priority);
            return result != 0 ? result : lhs.UniqueId.CompareTo(rhs.UniqueId);
        }

        public Request CreateRequest(string name, int priority, T value = default, bool enabled = true)
        {
            return new Request(name, priority, this, value, enabled);
        }

        private void AddRequest(Request request)
        {
            m_requests.Add(request);
            OnRequestChanged(request);
        }

        private void RemoveRequest(Request request)
        {
            m_requests.Remove(request);
            OnRequestChanged(request);
        }

        private void OnRequestChanged(Request changedRequest)
        {
            var value = m_defaultValue;
            
            foreach(var request in m_requests)
            {
                value = request.Value;
                break;
            }

            Value = value;
        }

        public class Request : IMultiControlRequest
        {
            private static int idCounter = 0;

            public Request(string name, int priority, MultiControl<T> multiControl, T value = default, bool enabled = true)
            {
                Name = name;
                if (multiControl == null)
                    throw new ArgumentNullException();
                m_owner = multiControl;
                Priority = priority;
                Value = value;
                Enabled = enabled;
                UniqueId = idCounter++;
            }

            public readonly int UniqueId;
            public readonly int Priority;


            private MultiControl<T> m_owner;

            public string Name { get; set; }

            private T m_value;

            public T Value
            {
                get { return m_value; }
                set
                {
                    if (EqualityComparer<T>.Default.Equals(m_value, value)) return;
                    m_value = value;
                    if (Enabled)
                        m_owner.OnRequestChanged(this);
                }
            }

            private bool m_enabled = false;

            public bool Enabled
            {
                get { return m_enabled; }
                set
                {
                    if (m_enabled == value) return;
                    m_enabled = value;
                    if (m_enabled)
                        m_owner.AddRequest(this);
                    else
                        m_owner.RemoveRequest(this);
                }
            }

            public void Dispose()
            {
                if (ReferenceEquals(m_owner, null))
                    return;
                Enabled = false;
                m_owner = null;
                m_value = default;
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

            public override string ToString()
            {
                return $"{Name}[{Priority}](value={Value}, {(Enabled ? "enabled" : "disabled")})";
            }
        }
    }
}
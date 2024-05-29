using System;
using System.Collections.Generic;
using SeweralIdeas.Pooling;
using SeweralIdeas.Utils;
using UnityEngine;

namespace SeweralIdeas.UnityUtils
{
    public class OnDestroyListener : MonoBehaviour
    {
        private readonly HashSet<(Action<object>, object)> m_callbackSet = new ();

        public void AddListener(Action<object> callback, object argument) => m_callbackSet.Add((callback, argument));
        public void RemoveListener(Action<object> callback, object argument) => m_callbackSet.Remove((callback, argument));
        
        protected void OnDestroy()
        {
            using (ListPool<(Action<object>, object)>.Get(out var cbackList))
            {
                while(m_callbackSet.Count > 0)
                {
                    cbackList.AddSet(m_callbackSet);
                    m_callbackSet.Clear();
                    foreach (var cbackPair in cbackList)
                    {
                        cbackPair.Item1(cbackPair.Item2);
                    }
                }
            }
        }
    }

    public static class OnDestroyExtensions
    {
        public static void SubscribeToDestroy(this GameObject gameObject, Action<object> callback, object argument)
        {
            var listener = gameObject.GetComponent<OnDestroyListener>();
            if(listener == null)
            {
                listener = gameObject.AddComponent<OnDestroyListener>();
                listener.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
            }
            listener.AddListener(callback, argument);
        }
        
        public static void UnsubscribeFromDestroy(this GameObject gameObject, Action<object> callback, object argument)
        {
            var listener = gameObject.GetComponent<OnDestroyListener>();
            if(listener == null)
                return;
            listener.RemoveListener(callback, argument);
        }
    }
}

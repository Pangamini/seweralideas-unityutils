using System;
using UnityEngine;

namespace SeweralIdeas.UnityUtils
{
    /*
        SimpleSingletonBehaviour instance gets created when it is requested and missing; Note that only a simple GameObject with the <T> component is instantiated 
    */
    public abstract class SimpleSingletonBehaviour<T> : MonoBehaviour where T : SimpleSingletonBehaviour<T>
    {
        private static T s_instance;
        public static T GetInstance()
        {
            if (!s_instance)
            {
                s_instance = new GameObject(typeof(T).Name).AddComponent<T>();
            }
            return s_instance;
        }

        void Awake()
        {
            if (s_instance == null)
                s_instance = (T)this;
            else
            {
                if (s_instance != (T)this)
                    throw new Exception("Singleton with multiple instances detected: " + typeof(T).Name);
            }

            OnAwake();
        }


        protected virtual void OnAwake() { }
    }
}
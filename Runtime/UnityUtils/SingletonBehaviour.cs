using UnityEngine;

namespace SeweralIdeas.UnityUtils
{
    public abstract class SingletonBehaviour<T> : MonoBehaviour where T : SingletonBehaviour<T>
    {
        private static T s_instance;
        public static T GetInstance(bool printError = true)
        {
            if (s_instance == null)
            {
                s_instance = FindObjectOfType<T>();
                if (s_instance == null)
                {
                    if(printError)
                        Debug.LogError("Instance of singletonBehaviour "+typeof(T)+" not found");
                }
            }
            return s_instance;
        }

        protected void Awake()
        {         
            if ( s_instance == null )
                s_instance = (T)this;
            else
            {
                if ( s_instance != (T)this )
                    Debug.LogError("Singleton with multiple instances detected: " + typeof(T).Name, gameObject);
            }

            OnAwake();
        }

        protected void OnDestroy()
        {
            if (s_instance == this)
            {
                s_instance = null;
            }
            OnDestroyed();
        }

        protected virtual void OnAwake() { }
        protected virtual void OnDestroyed() { }
    }
}
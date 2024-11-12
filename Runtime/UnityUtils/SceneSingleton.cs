using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SeweralIdeas.UnityUtils
{
    public class SceneSingleton<T> : MonoBehaviour where T : SceneSingleton<T>
    {
        private static readonly Dictionary<Scene, T> s_instances = new Dictionary<Scene, T>();
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSingleton() => s_instances.Clear();

        public static T GetInstance(Scene scene)
        {
            if (s_instances.TryGetValue(scene, out T output))
                return output;

            output = UnityExtensions.FindObjectOfType<T>(scene);
            if (output)
                s_instances.Add(scene, output);
            return output;

        }

        public static bool GetInstance(Scene scene, out T instance)
        {
            instance = GetInstance(scene);
            return instance != null;
        }


        protected void Awake()
        {
            var key = gameObject.scene;
            if (s_instances.TryGetValue(key, out T instance))
            {
                if (instance == null)
                    s_instances[key] = (T)this;
                else if (instance != this)
                    throw new Exception($"Singleton of type {typeof(T).Name} already exists");
            }
            else
                s_instances.Add(key, (T)this);

            OnAwake();
        }

        protected virtual void OnAwake() { }
        protected virtual void OnDestroyed() { }


        protected void OnDestroy()
        {
            try
            {
                OnDestroyed();
            }
            finally
            {
                if(s_instances.TryGetValue(gameObject.scene, out T instance))
                    if(instance == this)
                        s_instances.Remove(gameObject.scene);
            }
        }

    }
}
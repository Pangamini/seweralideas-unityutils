using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SeweralIdeas.UnityUtils
{
    public class SceneSingleton<T> : MonoBehaviour where T : SceneSingleton<T>
    {
        // Scene doesn't implement IEquatable<Scene>, so EqualityComparer<Scene>.Default
        // falls back to ObjectEqualityComparer<T>, which boxes the key on every lookup.
        // Scene's == operator and GetHashCode() compare handles directly with no boxing.
        private sealed class SceneComparer : IEqualityComparer<Scene>
        {
            public static readonly SceneComparer Instance = new();
            public bool Equals(Scene x, Scene y) => x == y;
            public int GetHashCode(Scene scene) => scene.GetHashCode();
        }

        private static readonly Dictionary<Scene, T> s_instances = new Dictionary<Scene, T>(SceneComparer.Instance);

        public static T GetInstance(Scene scene)
        {
            if (s_instances.TryGetValue(scene, out T output) && output != null)
                return output;

            output = UnityExtensions.FindObjectOfType<T>(scene);
            if (output)
                s_instances[scene] = output;    // using assignment instead of Add() so we overwrite destroyed singletons. (checked above)
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
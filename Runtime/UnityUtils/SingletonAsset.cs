using UnityEngine;
namespace SeweralIdeas.UnityUtils
{
    public abstract class SingletonAsset<T> : ScriptableObject where T : SingletonAsset<T>
    {
        private static T s_instance;

        public static T GetInstance()
        {
            if (s_instance) return s_instance;
            s_instance = Resources.Load<T>(typeof(T).Name);
            if (s_instance) return s_instance;
            Debug.LogError($"Instance of {typeof( T ).Name} not found in Resources");
            s_instance = CreateInstance<T>();
            return s_instance;
        }
    }
}
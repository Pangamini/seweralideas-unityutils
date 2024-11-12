using UnityEngine;
using UnityEngine.SceneManagement;
namespace SeweralIdeas.UnityUtils
{
    public class SimpleSceneSingleton<T> : SceneSingleton<T> where T : SimpleSceneSingleton<T>
    {
        public new static T GetInstance(Scene scene)
        {
            var instance = SceneSingleton<T>.GetInstance(scene);
            if(!instance)
            {
                instance = new GameObject(typeof(T).Name).AddComponent<T>();
            }
            return instance;
        }
    }
}

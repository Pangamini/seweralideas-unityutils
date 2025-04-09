using UnityEngine;
using UnityEngine.SceneManagement;
namespace SeweralIdeas.UnityUtils
{
    public class SimpleSceneSingleton<T> : SceneSingleton<T> where T : SimpleSceneSingleton<T>
    {
        public static new T GetInstance(Scene scene)
        {
            var instance = SceneSingleton<T>.GetInstance(scene);
            if(!instance)
            {
                var go = new GameObject(typeof(T).Name);
                SceneManager.MoveGameObjectToScene(go, scene);
                instance = go.AddComponent<T>();
            }
            return instance;
        }
    }
}

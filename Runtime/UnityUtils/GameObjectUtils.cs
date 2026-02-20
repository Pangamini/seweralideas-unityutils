using UnityEngine;

namespace SeweralIdeas.UnityUtils
{
    public static class GameObjectUtils
    {
        private static GameObject s_inactiveHolder;

        private static Transform GetInactiveHolder()
        {
            if(s_inactiveHolder == null)
            {
                s_inactiveHolder = new GameObject("__InactiveHolder__");
                s_inactiveHolder.hideFlags = HideFlags.HideAndDontSave;
                s_inactiveHolder.SetActive(false);
                Object.DontDestroyOnLoad(s_inactiveHolder);
            }
            return s_inactiveHolder.transform;
        }

        // Instantiates prefab as child of the inactive holder so Awake/OnEnable are suppressed,
        // then explicitly deactivates the instance before reparenting, so it stays inactive
        // even after being moved to an active parent.
        private static T InstantiateUnderHolder<T>(T prefab) where T : Object
        {
            var instance = Object.Instantiate(prefab, GetInactiveHolder());
            GetGameObject(instance).SetActive(false);
            return instance;
        }

        private static GameObject GetGameObject<T>(T obj) where T : Object
        {
            if(obj is GameObject go) return go;
            return ((Component)(object)obj).gameObject;
        }

        public static T InstantiateInactive<T>(T prefab) where T : Object
        {
            var instance = InstantiateUnderHolder(prefab);
            GetGameObject(instance).transform.SetParent(null, true);
            return instance;
        }

        public static T InstantiateInactive<T>(T prefab, Transform parent) where T : Object
        {
            var instance = InstantiateUnderHolder(prefab);
            GetGameObject(instance).transform.SetParent(parent, true);
            return instance;
        }

        public static T InstantiateInactive<T>(T prefab, Transform parent, bool worldPositionStays) where T : Object
        {
            var instance = InstantiateUnderHolder(prefab);
            GetGameObject(instance).transform.SetParent(parent, worldPositionStays);
            return instance;
        }

        public static T InstantiateInactive<T>(T prefab, Vector3 position, Quaternion rotation) where T : Object
        {
            var instance = InstantiateUnderHolder(prefab);
            var t = GetGameObject(instance).transform;
            t.SetPositionAndRotation(position, rotation);
            t.SetParent(null, true);
            return instance;
        }

        public static T InstantiateInactive<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent) where T : Object
        {
            var instance = InstantiateUnderHolder(prefab);
            var t = GetGameObject(instance).transform;
            t.SetPositionAndRotation(position, rotation);
            t.SetParent(parent, true);
            return instance;
        }
    }
}

using System;
using UnityEditor;
using UnityEngine;

namespace SeweralIdeas.UnityUtils
{
    [Serializable]
    public struct SceneReference : ISerializationCallbackReceiver
    {
#if UNITY_EDITOR
        [SerializeField] private SceneAsset m_sceneAsset;
#endif
        [SerializeField] private string m_scenePath;

        public string Path => m_scenePath;

        public SceneReference(string scenePath)
        {
#if UNITY_EDITOR
            m_sceneAsset = null;
#endif
            m_scenePath = scenePath;
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
#if UNITY_EDITOR
            m_scenePath = AssetDatabase.GetAssetPath(m_sceneAsset);
#endif
        }
    }
}
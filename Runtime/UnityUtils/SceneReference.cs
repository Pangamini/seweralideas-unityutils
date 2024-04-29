#if UNITY_EDITOR
using UnityEditor;
#endif

using System;
using UnityEngine;

namespace SeweralIdeas.UnityUtils
{
        [Serializable]
        public struct SceneReference : ISerializationCallbackReceiver
        {
#if UNITY_EDITOR
                [SerializeField] private SceneAsset m_sceneAsset;
                private bool m_getPathFromAsset;
#endif
                [SerializeField] private string m_scenePath;

                public string Path
                {
                    get
                    {
                        #if UNITY_EDITOR
                        if(m_getPathFromAsset)
                        {
                            m_getPathFromAsset = false;
                            m_scenePath = AssetDatabase.GetAssetPath(m_sceneAsset);
                        }
                        #endif
                        return m_scenePath;
                    }
                }

                public SceneReference(string scenePath)
                {
#if UNITY_EDITOR
                        m_sceneAsset = null;
                        m_getPathFromAsset = false;
#endif
                        m_scenePath = scenePath;
                }

                void ISerializationCallbackReceiver.OnAfterDeserialize()
                {
#if UNITY_EDITOR
                    m_getPathFromAsset = true;
#endif
                }

                void ISerializationCallbackReceiver.OnBeforeSerialize()
                {
#if UNITY_EDITOR
                    m_scenePath = AssetDatabase.GetAssetPath(m_sceneAsset);
#endif
                }
        }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SeweralIdeas.UnityUtils
{
    [Serializable]
    public abstract class AssetByNameTable
    {
#if UNITY_EDITOR
        [ContextMenu("Find All Assets")]
        protected void Editor_FindAll()
        {
            Editor_AbstractFindAll();
        }

        protected abstract void Editor_AbstractFindAll();
#endif
    }
    
    [Serializable]
    public class AssetByNameTable<T> : AssetByNameTable, ISerializationCallbackReceiver, IReadOnlyDictionary<string, T> where T:UnityEngine.Object
    {
        [SerializeField] private List<T> m_list;
        [NonSerialized] private bool m_dictDirty;

        private readonly Dictionary<string, T> m_dict = new();
        
        private void EnsureDictUpToDate()
        {
            if (!m_dictDirty)
                return;
            
            m_dict.Clear();
            m_dict.EnsureCapacity(m_list.Count);
            foreach (T element in m_list)
            {
                if (element == null)
                    continue;
                
                var key = element.name;
                if (m_dict.ContainsKey(key))
                {
                    Debug.LogError($"{GetType().Name} contains duplicate key {key}");
                    continue;
                }
                
                m_dict.Add(key, element);
            }
                
            m_dictDirty = false;
        }

        public int Count => m_dict.Count;

        public bool ContainsKey(string key)
        {
            EnsureDictUpToDate();
            return m_dict.ContainsKey(key);
        }

        public bool TryGetValue(string key, out T value)
        {
            EnsureDictUpToDate();
            return m_dict.TryGetValue(key, out value);
        }

        public T this[string key]
        {
            get
            {
                EnsureDictUpToDate();
                return m_dict[key];
            }
        }

        IEnumerable<string> IReadOnlyDictionary<string, T>.Keys
        {
            get
            {
                EnsureDictUpToDate();
                return m_dict.Keys;
            }
        }
        
        IEnumerable<T> IReadOnlyDictionary<string, T>.Values
        {
            get
            {
                EnsureDictUpToDate();
                return m_dict.Values;
            }
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize() { }

        void ISerializationCallbackReceiver.OnAfterDeserialize() => m_dictDirty = true;

        IEnumerator<KeyValuePair<string, T>> IEnumerable<KeyValuePair<string, T>>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public Dictionary<string, T>.Enumerator GetEnumerator()
        {
            EnsureDictUpToDate();
            return m_dict.GetEnumerator();
        }
        
        
#if UNITY_EDITOR
        protected override void Editor_AbstractFindAll()
        {
            m_dictDirty = true;
            m_list.Clear();
            var guids = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                T asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
                m_list.Add(asset);
            }

            EnsureDictUpToDate();
        }
#endif

        class AssetByNameLookup<T> : ScriptableObject where T:UnityEngine.Object
        {
            [SerializeField] private AssetByNameTable<T> m_table;
        }
    }
}

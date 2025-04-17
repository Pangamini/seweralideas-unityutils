using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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

    public interface IAssetByNameTable<T> : IReadOnlyDictionary<string, T>, IReadOnlyList<T>
        where T : UnityEngine.Object
    {
        new List<T>.Enumerator GetEnumerator();
        new int Count { get; }
        bool ContainsValue(T value);
        bool TryGetKey(T value, out string key);
    }
    
    [Serializable]
    public class AssetByNameTable<T> : AssetByNameTable, ISerializationCallbackReceiver, IAssetByNameTable<T>
        where T:UnityEngine.Object
    {
        [FormerlySerializedAs("m_list")]
        [SerializeField] private List<T> _list = new();
        [NonSerialized] private bool _dictDirty;

        private readonly Bictionary<string, T> _dict = new();

        public void Clear()
        {
            _list.Clear();
            _dict.Clear();
            _dictDirty = false;
        }

        public void Add(T asset)
        {
            _list.Add(asset);
            _dictDirty = true;
        }
        
        public void Remove(T asset)
        {
            if (_list.Remove(asset))
            {
                _dictDirty = true;
            }
        }
        
        private void EnsureDictUpToDate()
        {
            if (!_dictDirty)
                return;
            
            _dict.Clear();
            _dict.EnsureCapacity(_list.Count);
            foreach (T element in _list)
            {
                if (element == null)
                    continue;
                
                var key = element.name;
                if (_dict.ContainsKey(key))
                {
                    Debug.LogError($"{GetType().Name} contains duplicate key {key}");
                    continue;
                }
                
                _dict.Add(key, element);
            }
                
            _dictDirty = false;
        }

        public int Count => _list.Count;

        public bool ContainsKey(string key)
        {
            EnsureDictUpToDate();
            return _dict.ContainsKey(key);
        }

        public bool TryGetValue(string key, out T value)
        {
            EnsureDictUpToDate();
            return _dict.TryGetValue(key, out value);
        }
        
        public bool ContainsValue(T value)
        {
            EnsureDictUpToDate();
            return _dict.ContainsValue(value);
        }
        
        public bool TryGetKey(T value, out string key)
        {
            EnsureDictUpToDate();
            return _dict.TryGetKey(value, out key);
        }
        
        public T this[string key]
        {
            get
            {
                EnsureDictUpToDate();
                return _dict[key];
            }
        }
        
        public T this[int index] => _list[index];

        IEnumerable<string> IReadOnlyDictionary<string, T>.Keys
        {
            get
            {
                EnsureDictUpToDate();
                return _dict.Keys;
            }
        }
        
        IEnumerable<T> IReadOnlyDictionary<string, T>.Values
        {
            get
            {
                EnsureDictUpToDate();
                return _dict.Values;
            }
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            if(_list != null)
                _list.Sort((lhs, rhs) => string.Compare(lhs?lhs.name:null, rhs?rhs.name:null, StringComparison.Ordinal));
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize() => _dictDirty = true;

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => _list.GetEnumerator();
        List<T>.Enumerator IAssetByNameTable<T>.GetEnumerator() => _list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
        IEnumerator<KeyValuePair<string, T>> IEnumerable<KeyValuePair<string, T>>.GetEnumerator() => GetEnumerator();
        public Dictionary<string, T>.Enumerator GetEnumerator()
        {
            EnsureDictUpToDate();
            return _dict.GetEnumerator();
        }

#if UNITY_EDITOR
        protected override void Editor_AbstractFindAll()
        {
            _dictDirty = true;
            _list.Clear();
            var guids = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                T asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
                _list.Add(asset);
            }

            EnsureDictUpToDate();
        }
#endif
    }
    
    public class AssetByNameLookup<T> : ScriptableObject, IReadOnlyList<T>, IAssetByNameTable<T>
        where T:UnityEngine.Object
    {
        [FormerlySerializedAs("m_table")]
        [SerializeField] private AssetByNameTable<T> _table;

        public T this[string key] => _table[key];
        public T this[int index] => _table[index];

        public int Count => _table.Count;
        public bool ContainsKey(string key) => _table.ContainsKey(key);
        public bool TryGetValue(string key, out T value) => _table.TryGetValue(key, out value);
        public bool ContainsValue(T value) => _table.ContainsValue(value);
        public IReadOnlyBictionary<T, string> Reversed { get; }
        public bool TryGetKey(T value, out string key) => _table.TryGetKey(value, out key);

        IEnumerable<string> IReadOnlyDictionary<string, T>.Keys => ((IReadOnlyDictionary<string, T>)_table).Keys;
        IEnumerable<T> IReadOnlyDictionary<string, T>.Values => ((IReadOnlyDictionary<string, T>)_table).Values;
        List<T>.Enumerator IAssetByNameTable<T>.GetEnumerator() => ((IAssetByNameTable<T>)_table).GetEnumerator();
        IEnumerator<KeyValuePair<string, T>> IEnumerable<KeyValuePair<string, T>>.GetEnumerator() => _table.GetEnumerator();
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)_table).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<T>)_table).GetEnumerator();
        public Dictionary<string, T>.Enumerator GetEnumerator() => _table.GetEnumerator();

    }
}

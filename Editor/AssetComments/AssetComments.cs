#nullable enable
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SeweralIdeas.UnityUtils.Editor
{
    /// <summary>
    /// Editor-only comments attached to any asset or sub-asset, and any object of a saved scene.
    /// The comments are not stored in the commented asset: they live in one settings asset
    /// (ProjectSettings/AssetComments.asset), keyed by the object's <see cref="GlobalObjectId"/>, so they survive
    /// moving and renaming and never touch the asset's own file or meta.
    /// </summary>
    [FilePath("ProjectSettings/AssetComments.asset", FilePathAttribute.Location.ProjectFolder)]
    public class AssetComments : ScriptableSingleton<AssetComments>, ISerializationCallbackReceiver
    {
        private const double SaveDelay = 1.0;

        [Serializable]
        private struct Entry
        {
            public string key;
            public string text;
        }

        // NOTE: no field initializers. ScriptableSingleton loads its file in the base constructor, before derived
        // initializers run, so an initializer would overwrite what was just loaded.
        [SerializeField] private List<Entry>? _entries;

        // The dictionary is the source of truth; _entries is only the serialized form of it.
        [NonSerialized] private Dictionary<string, string>? _map;
        [NonSerialized] private Dictionary<string, string>? _textByGuid;
        [NonSerialized] private bool _savePending;
        [NonSerialized] private double _saveDue;

        private Dictionary<string, string> Map
        {
            get
            {
                if (_map != null)
                    return _map;

                _map = new Dictionary<string, string>();
                if (_entries != null)
                    foreach (Entry entry in _entries)
                        _map[entry.key] = entry.text;
                return _map;
            }
        }

        /// <summary>Raised after any comment changes.</summary>
        public static event Action? Changed;

        /// <summary>
        /// The key a comment of this object is stored under. False when the object has none (null, runtime-only, or in
        /// a scene that was never saved).
        /// An importer (what the Inspector shows for textures, models, ...) stands for its asset.
        /// </summary>
        public static bool TryGetKey(Object? obj, out string key)
        {
            key = "";
            if (obj is AssetImporter importer)
                obj = AssetDatabase.LoadMainAssetAtPath(importer.assetPath);

            if (obj == null)
                return false;

            // Assets, and objects of a saved scene. Objects of an unsaved scene, and runtime-only objects, have no
            // stable identity (no asset GUID), so they get no comment.
            GlobalObjectId id = GlobalObjectId.GetGlobalObjectIdSlow(obj);
            if (id.identifierType == 0 || id.assetGUID == default)
                return false;

            key = id.ToString();
            return true;
        }

        /// <summary>The comment of the object, or an empty string.</summary>
        public static string Get(Object? obj)
        {
            return TryGetKey(obj, out string key) ? Get(key) : "";
        }

        /// <summary>The comment stored under the key, or an empty string.</summary>
        public static string Get(string key)
        {
            return instance.Map.TryGetValue(key, out string text) ? text : "";
        }

        /// <summary>Sets the comment of the object; an empty text removes it. Does nothing for an object that has no key.</summary>
        public static void Set(Object? obj, string text)
        {
            if (TryGetKey(obj, out string key))
                Set(key, text);
        }

        /// <summary>Sets the comment under the key; an empty text removes it. Saved shortly after the last change.</summary>
        public static void Set(string key, string text)
        {
            AssetComments self = instance;
            Dictionary<string, string> map = self.Map;

            if (string.IsNullOrEmpty(text))
            {
                if (!map.Remove(key))
                    return;
            }
            else
            {
                if (map.TryGetValue(key, out string old) && old == text)
                    return;
                map[key] = text;
            }

            self._textByGuid = null;
            self.ScheduleSave();
            Changed?.Invoke();
        }

        /// <summary>
        /// The comments of the asset with this GUID and of everything inside it, joined (null when there are none).
        /// For the Project window, where only the GUID of an item is known.
        /// </summary>
        public static string? GetTextForGuid(string guid)
        {
            AssetComments self = instance;
            if (self._textByGuid == null)
            {
                var byGuid = new Dictionary<string, string>();
                foreach (KeyValuePair<string, string> pair in self.Map)
                {
                    if (!GlobalObjectId.TryParse(pair.Key, out GlobalObjectId id))
                        continue;

                    string g = id.assetGUID.ToString();
                    byGuid[g] = byGuid.TryGetValue(g, out string existing) ? existing + "\n---\n" + pair.Value : pair.Value;
                }
                self._textByGuid = byGuid;
            }
            return self._textByGuid.TryGetValue(guid, out string text) ? text : null;
        }

        private void ScheduleSave()
        {
            _saveDue = EditorApplication.timeSinceStartup + SaveDelay;
            if (_savePending)
                return;

            _savePending = true;
            EditorApplication.update += SaveTick;
            EditorApplication.quitting += SaveNow;
            AssemblyReloadEvents.beforeAssemblyReload += SaveNow;
        }

        private void SaveTick()
        {
            if (EditorApplication.timeSinceStartup >= _saveDue)
                SaveNow();
        }

        private void SaveNow()
        {
            if (!_savePending)
                return;

            _savePending = false;
            EditorApplication.update -= SaveTick;
            EditorApplication.quitting -= SaveNow;
            AssemblyReloadEvents.beforeAssemblyReload -= SaveNow;
            Save(true);
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            if (_map == null)
                return; // nothing changed since the load; keep what was loaded

            _entries = new List<Entry>(_map.Count);
            foreach (KeyValuePair<string, string> pair in _map)
                _entries.Add(new Entry { key = pair.Key, text = pair.Value });

            // stable order keeps the file's diffs small
            _entries.Sort((a, b) => string.CompareOrdinal(a.key, b.key));
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            _map = null;
            _textByGuid = null;
        }
    }
}

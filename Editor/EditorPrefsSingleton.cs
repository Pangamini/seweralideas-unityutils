using System;
using SeweralIdeas.Utils;
using UnityEditor;

namespace SeweralIdeas.UnityUtils.Editor
{
    /// <summary>
    /// An abstract singleton class that's automatically loaded and saved in editor.
    /// Useful for local editor settings.
    /// </summary>
    
    [Serializable]
    public abstract class EditorPrefsSingleton<T> : Singleton<T> where T:EditorPrefsSingleton<T>, new()
    {
        private string m_key;
        private string GetPrefsKeyName() => typeof(T).Name;

        protected EditorPrefsSingleton()
        {
            AssemblyReloadEvents.beforeAssemblyReload += Save;
            EditorApplication.quitting += Save;
            Load();
        }
        
        public void Load()
        {
            m_key = GetPrefsKeyName();
            string json = EditorPrefs.GetString(m_key, null);

            if(string.IsNullOrWhiteSpace(json))
                return;
            
            EditorJsonUtility.FromJsonOverwrite(json, this);
        }

        public void Save()
        {
            string json = EditorJsonUtility.ToJson(this);
            EditorPrefs.SetString(m_key, json);
        }
    }
}
 
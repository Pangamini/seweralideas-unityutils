#nullable enable
using UnityEditor;
using UnityEngine;

namespace SeweralIdeas.UnityUtils.Editor
{
    /// <summary>
    /// Project-wide settings for optional SeweralIdeas.UnityUtils editor features.
    /// Uses ScriptableSingleton pattern for automatic serialization and persistence.
    /// </summary>
    [FilePath("ProjectSettings/UnityUtilsSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class UnityUtilsSettings : ScriptableSingleton<UnityUtilsSettings>
    {
        [SerializeField]
        [Tooltip("Adds a \"Premultiply Alpha\" toggle to the Texture Importer inspector, and premultiplies " +
                 "alpha into RGB on import for textures with it enabled.")]
        private bool _premultiplyAlphaEnabled; // disabled by default

        /// <summary>
        /// Adds a "Premultiply Alpha" toggle to the Texture Importer inspector, and premultiplies alpha
        /// into RGB on import for textures with it enabled.
        /// </summary>
        public bool PremultiplyAlphaEnabled
        {
            get => _premultiplyAlphaEnabled;
            set
            {
                _premultiplyAlphaEnabled = value;
                Save();
            }
        }

        public static bool IsPremultiplyAlphaEnabled => instance.PremultiplyAlphaEnabled;

        public void Save() => Save(true);
    }
}

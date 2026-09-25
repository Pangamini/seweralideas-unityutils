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

        [SerializeField]
        [Tooltip("Shows a per-component icon next to GameObjects in the Hierarchy window for components " +
                 "marked with [HierarchyIconAttribute]. Scans every script in the project once to build the " +
                 "icon table, so it's opt-in rather than always-on.")]
        private bool _hierarchyIconsEnabled; // disabled by default

        /// <summary>
        /// Shows a per-component icon next to GameObjects in the Hierarchy window for components marked
        /// with [HierarchyIconAttribute].
        /// </summary>
        public bool HierarchyIconsEnabled
        {
            get => _hierarchyIconsEnabled;
            set
            {
                _hierarchyIconsEnabled = value;
                Save();
            }
        }

        public static bool IsHierarchyIconsEnabled => instance.HierarchyIconsEnabled;

        [SerializeField]
        [Tooltip("Shows error/warning icons next to GameObjects in the Hierarchy window for components " +
                 "implementing IErrorCheck. Also toggleable from Window > Error Check Tool > Enable Hierarchy.")]
        private bool _errorCheckToolEnabled; // disabled by default

        /// <summary>
        /// Shows error/warning icons next to GameObjects in the Hierarchy window for components
        /// implementing IErrorCheck.
        /// </summary>
        public bool ErrorCheckToolEnabled
        {
            get => _errorCheckToolEnabled;
            set
            {
                _errorCheckToolEnabled = value;
                Save();
            }
        }

        public static bool IsErrorCheckToolEnabled => instance.ErrorCheckToolEnabled;

        [SerializeField]
        [Tooltip("Keeps every AutoAssetByNameLookup<T> asset's list in sync automatically on asset import. " +
                 "Enabled by default since this is pre-existing behavior projects may already depend on; " +
                 "disable only if you don't use AutoAssetByNameLookup, to skip its per-import project scan.")]
        private bool _autoAssetByNameLookupEnabled = true; // enabled by default: preserves existing behavior

        /// <summary>
        /// Keeps every AutoAssetByNameLookup&lt;T&gt; asset's list in sync automatically on asset import.
        /// </summary>
        public bool AutoAssetByNameLookupEnabled
        {
            get => _autoAssetByNameLookupEnabled;
            set
            {
                _autoAssetByNameLookupEnabled = value;
                Save();
            }
        }

        public static bool IsAutoAssetByNameLookupEnabled => instance.AutoAssetByNameLookupEnabled;

        public void Save() => Save(true);
    }
}

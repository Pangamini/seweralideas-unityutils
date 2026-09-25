#nullable enable
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SeweralIdeas.UnityUtils.Editor
{
    /// <summary>
    /// Provides a UI in Unity's Project Settings for configuring optional SeweralIdeas.UnityUtils
    /// editor features.
    /// </summary>
    public class UnityUtilsSettingsProvider : SettingsProvider
    {
        private const string SettingsPath = "Project/SeweralIdeas.UnityUtils";
        private SerializedObject? _serializedSettings;

        public UnityUtilsSettingsProvider(string path, SettingsScope scopes)
            : base(path, scopes)
        {
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            // ScriptableSingleton automatically creates the instance
            _serializedSettings = new SerializedObject(UnityUtilsSettings.instance);
        }

        public override void OnGUI(string searchContext)
        {
            if (_serializedSettings == null)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Texture Import", EditorStyles.boldLabel);

            _serializedSettings.Update();
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(
                _serializedSettings.FindProperty("_premultiplyAlphaEnabled"),
                new GUIContent("Enable Premultiply Alpha",
                    "Adds a \"Premultiply Alpha\" toggle to the Texture Importer inspector, and premultiplies " +
                    "alpha into RGB on import for flagged textures. Disabled by default."));

            if (EditorGUI.EndChangeCheck())
            {
                _serializedSettings.ApplyModifiedProperties();
                UnityUtilsSettings.instance.Save();
            }
        }

        [SettingsProvider]
        public static SettingsProvider CreateUnityUtilsSettingsProvider()
        {
            var provider = new UnityUtilsSettingsProvider(SettingsPath, SettingsScope.Project)
            {
                keywords = new[] { "premultiply", "alpha", "texture" }
            };
            return provider;
        }
    }
}

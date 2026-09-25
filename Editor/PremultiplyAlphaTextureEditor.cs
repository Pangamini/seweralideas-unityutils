#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SeweralIdeas.UnityUtils.Editor
{
    internal static class PremultiplyAlphaTextureFlag
    {
        private const string Marker = "premultiplyAlpha";
        private const char Separator = ';';

        public static bool Get(TextureImporter importer) => ParseTokens(importer.userData).Contains(Marker);

        public static void Set(TextureImporter importer, bool value)
        {
            var tokens = ParseTokens(importer.userData);
            if (value) tokens.Add(Marker);
            else tokens.Remove(Marker);
            importer.userData = string.Join(Separator, tokens);
        }

        private static HashSet<string> ParseTokens(string userData)
            => new(string.IsNullOrEmpty(userData) ? Array.Empty<string>() : userData.Split(Separator));
    }

    // Deliberately not a [CustomEditor(typeof(TextureImporter))] override: replacing the
    // active editor for TextureImporter hides chrome that UnityEditor.InspectorWindow only
    // draws around an editor it recognizes as an AssetImporterEditor (the multiple-importers
    // dropdown, the Apply/Revert bar) — even when that override just wraps and delegates to
    // the real internal TextureImporterInspector. Editor.finishedDefaultHeaderGUI is the
    // supported extension point for adding a bit of UI to an existing inspector without
    // replacing it, so the native importer inspector (and its chrome) stays fully intact.
    [InitializeOnLoad]
    internal static class PremultiplyAlphaTextureHeaderGUI
    {
        static PremultiplyAlphaTextureHeaderGUI()
        {
            UnityEditor.Editor.finishedDefaultHeaderGUI += OnHeaderGUI;
        }

        private static void OnHeaderGUI(UnityEditor.Editor editor)
        {
            if (!UnityUtilsSettings.IsPremultiplyAlphaEnabled)
                return;

            if (editor.target is not TextureImporter)
                return;

            var importers = editor.targets.Cast<TextureImporter>().ToArray();

            EditorGUI.showMixedValue = importers.Select(PremultiplyAlphaTextureFlag.Get).Distinct().Count() > 1;
            EditorGUI.BeginChangeCheck();
            bool value = PremultiplyAlphaTextureFlag.Get(importers[0]);
            value = EditorGUILayout.Toggle("Premultiply Alpha", value);
            EditorGUI.showMixedValue = false;

            if (EditorGUI.EndChangeCheck())
            {
                foreach (var importer in importers)
                {
                    PremultiplyAlphaTextureFlag.Set(importer, value);
                    importer.SaveAndReimport();
                }
            }
        }
    }

    public class PremultiplyAlphaTexturePostprocessor : AssetPostprocessor
    {
        private void OnPostprocessTexture(Texture2D texture)
        {
            if (!UnityUtilsSettings.IsPremultiplyAlphaEnabled)
                return;

            var importer = (TextureImporter)assetImporter;
            if (!PremultiplyAlphaTextureFlag.Get(importer))
                return;

            for (int mip = 0; mip < texture.mipmapCount; mip++)
            {
                var pixels = texture.GetPixels(mip);
                for (int i = 0; i < pixels.Length; i++)
                {
                    var c = pixels[i];
                    pixels[i] = new Color(c.r * c.a, c.g * c.a, c.b * c.a, c.a);
                }
                texture.SetPixels(pixels, mip);
            }
            texture.Apply(updateMipmaps: false);
        }
    }
}

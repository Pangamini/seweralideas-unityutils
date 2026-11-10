using System.Reflection;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace SeweralIdeas.UnityUtils.Editor
{
    /// <summary>
    /// The inspector of <see cref="ChannelMixImporter"/>: its settings, with a block for each output channel, Apply/Revert, and
    /// a preview pane with the mix the settings make now, before they are applied (it follows the edits), and a description of the imported texture in its header. The preview
    /// shows the color (opaque or with its alpha over a checkerboard) or one channel, one of the mip levels, once or tiled.
    /// </summary>
    [CustomEditor(typeof(ChannelMixImporter))]
    public class ChannelMixImporterEditor : ScriptedImporterEditor
    {
        private enum View { Color, R, G, B, A }

        private static readonly string[] OutputNames = { "R (red)", "G (green)", "B (blue)", "A (alpha)" };

        private SerializedProperty m_settings;
        private View m_view;
        private bool m_tiled;
        private bool m_transparent;
        private int m_mipLevel;

        private ChannelMixer.Result m_result;
        private ChannelMixSettings m_resultSettings;   // what m_result was made from, for the mip levels
        private bool m_hasResult;
        private int m_shownHash;            // of the settings the picture is for (0 = none yet)
        private bool m_updateQueued;
        private PreviewPicture m_preview;

        // The picture, rebuilt when the result or the view changes.
        private sealed class PreviewPicture
        {
            public Texture2D texture;
            public bool dirty = true;
        }

        protected override bool useAssetDrawPreview => false;

        public override void OnEnable()
        {
            base.OnEnable();
            m_settings = serializedObject.FindProperty(nameof(ChannelMixImporter.settings));
            m_preview = new PreviewPicture();
        }

        public override void OnDisable()
        {
            EditorApplication.update -= UpdatePreview;
            EditorApplication.update -= ComputeMip;
            m_mipQueued = false;
            m_updateQueued = false;
            if (m_preview != null && m_preview.texture != null)
                DestroyImmediate(m_preview.texture);
            m_preview = null;
            base.OnDisable();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_settings.FindPropertyRelative(nameof(ChannelMixSettings.channelCount)));
            var sizeMode = m_settings.FindPropertyRelative(nameof(ChannelMixSettings.sizeMode));
            EditorGUILayout.PropertyField(sizeMode);
            if (sizeMode.enumValueIndex == (int)ChannelMixSettings.SizeMode.Custom)
                EditorGUILayout.PropertyField(m_settings.FindPropertyRelative(nameof(ChannelMixSettings.customSize)));

            int channelCount = Mathf.Clamp(m_settings.FindPropertyRelative(nameof(ChannelMixSettings.channelCount)).intValue, 1, 4);
            EditorGUILayout.Space();
            DrawOutputs(channelCount);

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(channelCount < 3))
                EditorGUILayout.PropertyField(m_settings.FindPropertyRelative(nameof(ChannelMixSettings.sRGB)));
            var compression = m_settings.FindPropertyRelative(nameof(ChannelMixSettings.compression));
            EditorGUILayout.PropertyField(compression);
            if (compression.enumValueIndex != (int)ChannelMixSettings.Compression.None && channelCount >= 3)
            {
                EditorGUI.indentLevel++;
                var crunch = m_settings.FindPropertyRelative(nameof(ChannelMixSettings.crunch));
                EditorGUILayout.PropertyField(crunch);
                if (crunch.boolValue)
                    EditorGUILayout.PropertyField(m_settings.FindPropertyRelative(nameof(ChannelMixSettings.crunchQuality)));
                EditorGUI.indentLevel--;
            }
            var mipmaps = m_settings.FindPropertyRelative(nameof(ChannelMixSettings.mipmaps));
            EditorGUILayout.PropertyField(mipmaps);
            if (mipmaps.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_settings.FindPropertyRelative(nameof(ChannelMixSettings.mipSource)));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.PropertyField(m_settings.FindPropertyRelative(nameof(ChannelMixSettings.filterMode)));
            EditorGUILayout.PropertyField(m_settings.FindPropertyRelative(nameof(ChannelMixSettings.anisoLevel)));
            EditorGUILayout.PropertyField(m_settings.FindPropertyRelative(nameof(ChannelMixSettings.mipMapBias)));
            EditorGUILayout.PropertyField(m_settings.FindPropertyRelative(nameof(ChannelMixSettings.wrapMode)));

            serializedObject.ApplyModifiedProperties();

            FollowSettings();
            ApplyRevertGUI();
        }

        // The imported texture, as the texture importer describes its own in the preview: size, format, color space and the
        // memory it takes.
        private Texture2D m_imported;

        // What the texture importer shows (the storage the texture takes, mips included), which is internal; the profiler's
        // number is the fallback.
        private static readonly MethodInfo StorageMemorySize = typeof(AssetImporter).Assembly
            .GetType("UnityEditor.TextureUtil")
            ?.GetMethod("GetStorageMemorySizeLong", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(Texture) }, null);

        private string ImportedInfo()
        {
            // A reimport (Apply, a source that changed) replaces the object, and the old one reads as null.
            if (m_imported == null)
                m_imported = AssetDatabase.LoadAssetAtPath<Texture2D>(((AssetImporter)target).assetPath);
            if (m_imported == null)
                return "not imported";

            long size = StorageMemorySize != null
                ? (long)StorageMemorySize.Invoke(null, new object[] { m_imported })
                : UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(m_imported);
            string colorSpace = m_imported.isDataSRGB ? "sRGB" : "Linear";
            return $"{m_imported.width}x{m_imported.height}  {m_imported.format}  {colorSpace}  {EditorUtility.FormatBytes(size)}";
        }

        private void DrawOutputs(int channelCount)
        {
            var outputs = m_settings.FindPropertyRelative(nameof(ChannelMixSettings.outputs));
            // A file made before there were four outputs, or by hand: make them, so there is something to edit.
            if (outputs.arraySize < 4)
                outputs.arraySize = 4;

            for (int i = 0; i < 4; i++)
            {
                var output = outputs.GetArrayElementAtIndex(i);
                var texture = output.FindPropertyRelative(nameof(ChannelMixSettings.Output.texture));
                var channel = output.FindPropertyRelative(nameof(ChannelMixSettings.Output.channel));
                bool constant = texture.objectReferenceValue == null
                                || channel.enumValueIndex == (int)ChannelMixSettings.SourceChannel.Constant;

                using (new EditorGUI.DisabledScope(i >= channelCount))
                {
                    EditorGUILayout.LabelField(OutputNames[i] + (i >= channelCount ? " (not in the texture)" : ""), EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(texture);
                    EditorGUILayout.PropertyField(channel);
                    if (constant)
                    {
                        EditorGUILayout.PropertyField(output.FindPropertyRelative(nameof(ChannelMixSettings.Output.constant)));
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(output.FindPropertyRelative(nameof(ChannelMixSettings.Output.readMode)));
                        EditorGUILayout.PropertyField(output.FindPropertyRelative(nameof(ChannelMixSettings.Output.range)));
                        EditorGUILayout.PropertyField(output.FindPropertyRelative(nameof(ChannelMixSettings.Output.invert)));
                    }
                    EditorGUI.indentLevel--;
                }
            }
        }

        // Queues a new mix when the settings in the inspector (applied or not) are not what the picture shows. Mixing uses the
        // GPU, which does not belong in OnGUI, so it is done on the editor's next update.
        private void FollowSettings()
        {
            if (!(m_settings.boxedValue is ChannelMixSettings current))
                return;
            int hash = current.ContentHash();
            if (hash == m_shownHash && m_hasResult)
                return;
            m_shownHash = hash;
            if (m_updateQueued)
                return;
            m_updateQueued = true;
            EditorApplication.update += UpdatePreview;
        }

        private void UpdatePreview()
        {
            EditorApplication.update -= UpdatePreview;
            m_updateQueued = false;
            if (m_settings == null || target == null)
                return;

            try
            {
                serializedObject.Update();
                if (m_settings.boxedValue is ChannelMixSettings current)
                {
                    m_result = ChannelMixer.Mix(current);
                    m_resultSettings = current;
                    m_mipBytes = null;
                    m_hasResult = true;
                    m_preview.dirty = true;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
            Repaint();
        }

        public override bool HasPreviewGUI() => true;

        // The bytes of the mip levels of the result (index = level; 0 is not kept), made on demand and thrown away when the result
        // changes. A level made from the sources reads them on the GPU, so it is made on the editor's update, not in OnGUI.
        private byte[][] m_mipBytes;
        private bool m_mipQueued;

        private bool MipReady(int level)
            => level == 0 || (m_mipBytes != null && level < m_mipBytes.Length && m_mipBytes[level] != null);

        private void QueueMip()
        {
            if (m_mipQueued)
                return;
            m_mipQueued = true;
            EditorApplication.update += ComputeMip;
        }

        private void ComputeMip()
        {
            EditorApplication.update -= ComputeMip;
            m_mipQueued = false;
            if (!m_hasResult || m_preview == null)
                return;

            try
            {
                MakeMip(Mathf.Min(m_mipLevel, MaxMipLevel));
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
            m_preview.dirty = true;
            Repaint();
        }

        // The level, from the sources' own mips if the settings ask for that and they have them (as the importer would),
        // otherwise (all the levels at once) from the mix.
        private void MakeMip(int level)
        {
            if (level <= 0 || MipReady(level))
                return;
            int levels = MaxMipLevel + 1;
            if (m_mipBytes == null || m_mipBytes.Length != levels)
                m_mipBytes = new byte[levels][];

            var settings = m_resultSettings;
            if (settings != null && settings.mipSource == ChannelMixSettings.MipSource.FromSources && ChannelMixer.SourcesHaveMips(settings))
            {
                m_mipBytes[level] = ChannelMixer.MixLevel(settings, level).bytes;
                return;
            }

            var mips = ChannelMixer.GenerateMips(m_result.bytes, m_result.width, m_result.height, m_result.channelCount, m_result.sRGB);
            for (int i = 0; i < mips.Count && i + 1 < levels; i++)
                m_mipBytes[i + 1] = mips[i];
        }

        // The last level there is: the one that is 1 pixel on its longer side.
        private int MaxMipLevel
        {
            get
            {
                if (!m_hasResult)
                    return 0;
                int level = 0;
                for (int size = Mathf.Max(m_result.width, m_result.height); size > 1; size >>= 1)
                    level++;
                return level;
            }
        }

        public override void OnPreviewSettings()
        {
            // Buttons like those of a Texture2D's preview: the colors, or one channel; only the channels the texture has.
            int channels = m_hasResult ? m_result.channelCount : 4;
            if ((int)m_view > channels)
                m_view = View.Color;
            for (int i = 0; i <= channels; i++)
            {
                var view = (View)i;
                if (GUILayout.Toggle(m_view == view, view == View.Color ? "RGB" : view.ToString(), EditorStyles.toolbarButton) && m_view != view)
                {
                    m_view = view;
                    m_preview.dirty = true;
                }
            }

            // The color with its alpha, over a checkerboard (only a texture with an alpha channel has one).
            if (channels == 4)
            {
                bool transparent = GUILayout.Toggle(m_transparent, "Transparent", EditorStyles.toolbarButton);
                if (transparent != m_transparent)
                {
                    m_transparent = transparent;
                    m_preview.dirty = true;
                }
            }

            GUILayout.Space(6f);
            bool tiled = GUILayout.Toggle(m_tiled, "Tiled", EditorStyles.toolbarButton);
            if (tiled != m_tiled)
            {
                m_tiled = tiled;
                m_preview.dirty = true;
            }

            // Which mip level is shown, drawn at the size of the first (so that a small one shows its pixels). Like the Texture2D preview's slider, level 0 (the largest) is at the right.
            int max = MaxMipLevel;
            m_mipLevel = Mathf.Clamp(m_mipLevel, 0, max);
            GUILayout.Space(6f);
            GUILayout.Label("Mip", EditorStyles.miniLabel);
            int mip = Mathf.RoundToInt(GUILayout.HorizontalSlider(m_mipLevel, Mathf.Max(max, 1), 0, GUILayout.Width(60f)));
            GUILayout.Label(Mathf.Clamp(mip, 0, max).ToString(), EditorStyles.miniLabel, GUILayout.Width(16f));
            mip = Mathf.Clamp(mip, 0, max);
            if (mip != m_mipLevel)
            {
                m_mipLevel = mip;
                m_preview.dirty = true;
            }
        }

        public override string GetInfoString() => ImportedInfo();

        public override void OnPreviewGUI(Rect rect, GUIStyle background)
        {
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));
            if (!m_hasResult)
            {
                GUI.Label(rect, "Mixing…", EditorStyles.centeredGreyMiniLabel);
                return;
            }
            // A mip level that is not made yet is made on the next update; until then the picture stays as it was.
            bool ready = MipReady(Mathf.Min(m_mipLevel, MaxMipLevel));
            if (!ready)
                QueueMip();
            if ((m_preview.dirty && ready) || m_preview.texture == null)
                RebuildPreview();

            // The picture, in its own proportions, as large as fits.
            float aspect = (float)m_result.width / m_result.height;
            float width = Mathf.Min(rect.width, rect.height * aspect);
            float height = width / aspect;
            var area = new Rect(rect.x + (rect.width - width) * 0.5f, rect.y + (rect.height - height) * 0.5f, width, height);

            bool transparent = m_transparent && m_view == View.Color && m_result.channelCount == 4;
            if (transparent)
            {
                // The checkerboard, in cells of 8 pixels, for the alpha to show against.
                const float cell = 8f;
                GUI.DrawTextureWithTexCoords(area, Checker, new Rect(0f, 0f, area.width / (2f * cell), area.height / (2f * cell)), false);
            }

            if (m_tiled)
                GUI.DrawTextureWithTexCoords(area, m_preview.texture, new Rect(0f, 0f, 2f, 2f), transparent);
            else
                GUI.DrawTexture(area, m_preview.texture, ScaleMode.StretchToFill, transparent);
        }

        private static Texture2D s_checker;

        // Two by two pixels of two greys, repeated and not filtered: a checkerboard.
        private static Texture2D Checker
        {
            get
            {
                if (s_checker == null)
                {
                    s_checker = new Texture2D(2, 2, TextureFormat.RGBA32, false, true)
                    {
                        hideFlags = HideFlags.HideAndDontSave,
                        wrapMode = TextureWrapMode.Repeat,
                        filterMode = FilterMode.Point,
                    };
                    var light = new Color32(150, 150, 150, 255);
                    var dark = new Color32(105, 105, 105, 255);
                    s_checker.SetPixels32(new[] { light, dark, dark, light });
                    s_checker.Apply();
                }
                return s_checker;
            }
        }

        // The channel (or the colors) of the mip level as a picture: opaque unless it is the transparent view, as the alpha is
        // data and would be blended away. A channel the texture does not have is shown as black (the shader reads 0 there; 1
        // for alpha).
        private void RebuildPreview()
        {
            m_preview.dirty = false;
            int stride = m_result.channelCount;

            byte[] bytes = m_result.bytes;
            int width = m_result.width;
            int height = m_result.height;
            // The level asked for, or the full size while it is not made yet.
            int level = Mathf.Min(m_mipLevel, MaxMipLevel);
            if (!MipReady(level))
                level = 0;
            if (level > 0)
            {
                bytes = m_mipBytes[level];
                width = Mathf.Max(1, m_result.width >> level);
                height = Mathf.Max(1, m_result.height >> level);
            }

            var texture = m_preview.texture;
            // Color bytes of an sRGB output are drawn as such, or the editor shows them with the wrong gamma. Alpha is never encoded.
            bool srgb = m_result.sRGB && m_view != View.A;
            if (texture == null || texture.width != width || texture.height != height || texture.isDataSRGB != srgb)
            {
                if (texture != null)
                    DestroyImmediate(texture);
                texture = new Texture2D(width, height, TextureFormat.RGBA32, false, linear: !srgb)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    wrapMode = TextureWrapMode.Repeat,
                };
                m_preview.texture = texture;
            }
            // Smoothed as the texture is, but a mip level shows its pixels.
            texture.filterMode = level > 0 ? FilterMode.Point : FilterMode.Bilinear;

            bool transparent = m_transparent && m_view == View.Color;
            var pixels = new Color32[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                byte r = bytes[i * stride];
                byte g = stride > 1 ? bytes[i * stride + 1] : (byte)0;
                byte b = stride > 2 ? bytes[i * stride + 2] : (byte)0;
                byte a = stride > 3 ? bytes[i * stride + 3] : (byte)255;
                switch (m_view)
                {
                    case View.R: pixels[i] = new Color32(r, r, r, 255); break;
                    case View.G: pixels[i] = new Color32(g, g, g, 255); break;
                    case View.B: pixels[i] = new Color32(b, b, b, 255); break;
                    case View.A: pixels[i] = new Color32(a, a, a, 255); break;
                    default:     pixels[i] = new Color32(r, g, b, transparent ? a : (byte)255); break;
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
        }
    }
}

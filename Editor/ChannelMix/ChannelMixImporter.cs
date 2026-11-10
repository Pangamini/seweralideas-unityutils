using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace SeweralIdeas.UnityUtils.Editor
{
    /// <summary>
    /// Imports a .channelmix file as a Texture2D whose channels are taken from channels of other textures (see
    /// <see cref="ChannelMixSettings"/>). The file itself is only a marker: the settings are the importer's, saved in its .meta.
    /// The baked texture lives in the Library (and in builds that use it), not in the project, and is made again when the settings
    /// change (Apply in the inspector) or when a source changes: the importer depends on the import result of every source, so
    /// it follows their import settings too. Create one with Assets/Create/SeweralIdeas/Channel Mixed Texture; the texture is the
    /// asset's main object, to be assigned like any other.
    ///
    /// The sources are read on the GPU, so this imports in the main editor process (a scripted importer does, unless it opts out).
    /// </summary>
    [ScriptedImporter(1, "channelmix")]
    public class ChannelMixImporter : ScriptedImporter
    {
        public ChannelMixSettings settings = new ChannelMixSettings();

        public override void OnImportAsset(AssetImportContext ctx)
        {
            int count = settings.ChannelCount;
            for (int c = 0; c < count; c++)
            {
                var texture = settings.GetOutput(c).texture;
                if (texture == null)
                    continue;
                string path = AssetDatabase.GetAssetPath(texture);
                if (!string.IsNullOrEmpty(path))
                    ctx.DependsOnArtifact(path);
            }

            var result = ChannelMixer.Mix(settings);

            // Raw values: unless it is a color texture, which has the sRGB flag (only the formats of 3 and 4 channels have one).
            bool srgb = settings.sRGB && count >= 3;

            // A compressed texture is made from an RGBA one (the compressor takes only those): the channels it does not have are 0, alpha 1.
            bool compress = settings.compression != ChannelMixSettings.Compression.None;
            if (compress && (result.width % 4 != 0 || result.height % 4 != 0))
            {
                ctx.LogImportWarning($"A compressed texture needs a size that is a multiple of 4 ({result.width}x{result.height} is not): left uncompressed.");
                compress = false;
            }

            TextureFormat format = compress ? TextureFormat.RGBA32 : ChannelMixSettings.FormatFor(count);
            byte[] bytes = compress ? ExpandToRGBA(result.bytes, count) : result.bytes;

            var output = new Texture2D(result.width, result.height, format, settings.mipmaps, linear: !srgb)
            {
                name = Path.GetFileNameWithoutExtension(ctx.assetPath),
                wrapMode = settings.wrapMode,
                filterMode = settings.filterMode,
                anisoLevel = settings.anisoLevel,
                mipMapBias = settings.mipMapBias,
            };
            output.SetPixelData(bytes, 0);
            if (settings.mipmaps)
            {
                // The mips are made here, not by Unity's Apply: for an sRGB texture they have to be averaged in linear light.
                System.Collections.Generic.List<byte[]> mips;
                bool fromSources = settings.mipSource == ChannelMixSettings.MipSource.FromSources;
                if (fromSources && !ChannelMixer.SourcesHaveMips(settings))
                {
                    ctx.LogImportWarning("A source texture has no mip levels, so the mips cannot be taken from the sources: they are generated from the mix.");
                    fromSources = false;
                }

                if (fromSources)
                {
                    // Every level is the mix of the sources at its size, which are their own mips.
                    mips = new System.Collections.Generic.List<byte[]>();
                    int levels = ChannelMixer.LevelCount(result.width, result.height);
                    for (int level = 1; level < levels; level++)
                    {
                        byte[] levelBytes = ChannelMixer.MixLevel(settings, level).bytes;
                        mips.Add(compress ? ExpandToRGBA(levelBytes, count) : levelBytes);
                    }
                }
                else
                {
                    mips = ChannelMixer.GenerateMips(bytes, result.width, result.height, compress ? 4 : count, srgb);
                }

                for (int level = 0; level < mips.Count; level++)
                    output.SetPixelData(mips[level], level + 1);
            }
            output.Apply(updateMipmaps: false, makeNoLongerReadable: !compress);

            if (compress)
            {
                Compress(output, count);
                output.Apply(updateMipmaps: false, makeNoLongerReadable: true);
            }

            ctx.AddObjectToAsset("texture", output);
            ctx.SetMainObject(output);
        }

        // The whole mip chain is compressed, the one made on Apply.
        private void Compress(Texture2D texture, int channelCount)
        {
            bool high = settings.compression == ChannelMixSettings.Compression.High;
            if (settings.crunch && channelCount >= 3)
            {
                EditorUtility.CompressTexture(texture, channelCount == 3 ? TextureFormat.DXT1Crunched : TextureFormat.DXT5Crunched,
                                              settings.crunchQuality);
                return;
            }

            TextureFormat format;
            switch (channelCount)
            {
                case 1:  format = TextureFormat.BC4; break;
                case 2:  format = TextureFormat.BC5; break;
                case 3:  format = high ? TextureFormat.BC7 : TextureFormat.DXT1; break;
                default: format = high ? TextureFormat.BC7 : TextureFormat.DXT5; break;
            }
            var quality = settings.compression == ChannelMixSettings.Compression.Low ? TextureCompressionQuality.Fast
                        : high ? TextureCompressionQuality.Best : TextureCompressionQuality.Normal;
            EditorUtility.CompressTexture(texture, format, quality);
        }

        private static byte[] ExpandToRGBA(byte[] bytes, int channelCount)
        {
            int pixels = bytes.Length / channelCount;
            var rgba = new byte[pixels * 4];
            for (int p = 0; p < pixels; p++)
            {
                for (int c = 0; c < 4; c++)
                    rgba[p * 4 + c] = c < channelCount ? bytes[p * channelCount + c] : (byte)(c == 3 ? 255 : 0);
            }
            return rgba;
        }

        [MenuItem("Assets/Create/SeweralIdeas/Channel Mixed Texture")]
        private static void Create()
        {
            ProjectWindowUtil.CreateAssetWithTextContent("Channel Mix.channelmix",
                "A channel mixed texture. Its settings are in the importer, saved in the .meta file: select this asset to edit them.\n");
        }
    }
}

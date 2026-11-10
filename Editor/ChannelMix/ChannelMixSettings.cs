using System;
using UnityEngine;

namespace SeweralIdeas.UnityUtils.Editor
{
    /// <summary>
    /// The settings of a channel mixed texture (a .channelmix asset, see <see cref="ChannelMixImporter"/>): every channel of the
    /// output texture takes one channel of one source texture (or a constant), so that several masks can be packed into one texture
    /// without writing an image file by hand. The output is baked on import and updates when a source changes.
    /// </summary>
    [Serializable]
    public class ChannelMixSettings
    {
        public const int MaxSize = 4096;

        public enum SourceChannel
        {
            R,
            G,
            B,
            A,
            /// <summary> The brightness of the source (0.2126 R + 0.7152 G + 0.0722 B). </summary>
            Luminance,
            /// <summary> No source: the output channel is the constant. </summary>
            Constant,
        }

        public enum ReadMode
        {
            /// <summary> The values as they are stored in the source (what an image editor shows), however the importer treats it. </summary>
            AsStored,
            /// <summary> The values as a shader samples the source: decoded from sRGB if the source is an sRGB texture (in a Linear color space project). </summary>
            AsSampled,
        }

        public enum SizeMode
        {
            /// <summary> The size of the biggest source that is used. </summary>
            LargestSource,
            Custom,
        }

        [Serializable]
        public class Output
        {
            [Tooltip("The texture the channel is taken from. Empty = the constant.")]
            public Texture2D texture;
            public SourceChannel channel = SourceChannel.R;
            [Range(0f, 1f), Tooltip("The value of the channel when there is no source texture, or the source channel is Constant.")]
            public float constant;
            [Tooltip("Stored: the values as they are stored in the file. Sampled: as a shader reads them (decoded from sRGB if the source is an sRGB texture).")]
            public ReadMode readMode = ReadMode.AsStored;
            [Tooltip("The part of the source's range that becomes 0..1: values at or below x become 0, at or above y become 1 (the channel is stretched in between).")]
            public Vector2 range = new Vector2(0f, 1f);
            [Tooltip("1 - value, after the range.")]
            public bool invert;
        }

        public enum Compression
        {
            None,
            /// <summary> Fastest to import, the lowest quality (block formats: DXT). </summary>
            Low,
            /// <summary> The default of the texture importer (block formats: DXT). </summary>
            Normal,
            /// <summary> The best quality, and slow to import (BC7 for 3 and 4 channels). </summary>
            High,
        }

        public enum MipSource
        {
            /// <summary> The mips are made from the mix: each level is the average of 2x2 pixels of the one before (in linear light for an sRGB texture). </summary>
            Generate,
            /// <summary> Each level is the mix of the sources at that size, so of the mips the source's importer made (its filter, preserve coverage and so on). A source without mips makes it Generate instead. </summary>
            FromSources,
        }

        public SizeMode sizeMode = SizeMode.LargestSource;
        [Tooltip("The size of the output in pixels. The sources are resampled to it.")]
        public Vector2Int customSize = new Vector2Int(512, 512);
        [Range(1, 4), Tooltip("How many channels the texture has: 1 = R8, 2 = RG16, 3 = RGB24, 4 = RGBA32. Outputs beyond it are not made.")]
        public int channelCount = 4;
        [Tooltip("The output is a color texture: its values are sRGB encoded. Only for 3 and 4 channels (the other formats can't be). Leave off for masks and data.")]
        public bool sRGB = false;
        [Tooltip("Compress the baked texture: a quarter of the memory (an eighth with crunch on disk), and some loss of detail. Dimensions that are not a multiple of 4 are left uncompressed. 1 channel is BC4, 2 is BC5, 3 is DXT1 (BC7 at High), 4 is DXT5 (BC7 at High).")]
        public Compression compression = Compression.None;
        [Tooltip("Crunch compression on top of DXT: much smaller on disk (and in builds), slower to import, a little lower quality. Only for 3 and 4 channels.")]
        public bool crunch = false;
        [Range(0, 100), Tooltip("Quality of the crunch compression.")]
        public int crunchQuality = 50;
        public bool mipmaps = true;
        [Tooltip("Generate: the mips are made from the mix, by averaging (in linear light for an sRGB texture). From Sources: every level is the mix of the sources at that size, so it is made of the mips of the sources as their importers made them (Kaiser, preserve coverage...). If a source has no mips, Generate is used.")]
        public MipSource mipSource = MipSource.Generate;
        [Range(0, 16), Tooltip("Anisotropic filtering of the texture, 0 (off) to 16. Keeps it sharper at a slant.")]
        public int anisoLevel = 1;
        [Tooltip("Added to the mip level the GPU picks: negative is sharper (and shimmers), positive is blurrier.")]
        public float mipMapBias = 0f;
        public FilterMode filterMode = FilterMode.Bilinear;
        public TextureWrapMode wrapMode = TextureWrapMode.Repeat;
        public Output[] outputs = CreateOutputs();

        public int ChannelCount => Mathf.Clamp(channelCount, 1, 4);

        /// <summary> The output for a channel (0 = R ... 3 = A); one that was never serialized is a default. </summary>
        public Output GetOutput(int channel)
        {
            if (outputs == null || channel >= outputs.Length || outputs[channel] == null)
                return DefaultOutput(channel);
            return outputs[channel];
        }

        private static Output DefaultOutput(int channel) => new Output
        {
            channel = (SourceChannel)Mathf.Min(channel, 3),
            constant = channel == 3 ? 1f : 0f,
        };

        private static Output[] CreateOutputs()
        {
            var outputs = new Output[4];
            for (int i = 0; i < outputs.Length; i++)
                outputs[i] = DefaultOutput(i);
            return outputs;
        }

        /// <summary> The texture format for a channel count. The bytes of every format hold R, then G, B and A as far as it goes. </summary>
        public static TextureFormat FormatFor(int channelCount)
        {
            switch (channelCount)
            {
                case 1:  return TextureFormat.R8;
                case 2:  return TextureFormat.RG16;
                case 3:  return TextureFormat.RGB24;
                default: return TextureFormat.RGBA32;
            }
        }

        /// <summary> A hash of the settings (the sources by reference), to tell whether a result is out of date. </summary>
        public int ContentHash() => JsonUtility.ToJson(this).GetHashCode();
    }
}

using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace SeweralIdeas.UnityUtils.Editor
{
    /// <summary>
    /// Makes the pixels of a channel mixed texture from its <see cref="ChannelMixSettings"/>. The sources are read on the GPU
    /// (a blit into a render texture of the output's size, so any texture the editor can show works: compressed, not
    /// readable, any size, any importer), and mixed and filtered by Burst jobs. It has to run on the main thread.
    /// </summary>
    public static class ChannelMixer
    {
        public struct Result
        {
            /// <summary> The bytes of the texture: the channels one after the other for each pixel, rows from the bottom. </summary>
            public byte[] bytes;
            public int width;
            public int height;
            public int channelCount;
            /// <summary> The bytes are sRGB encoded colors (the settings asked for it, and the format can be). </summary>
            public bool sRGB;
        }

        /// <summary> The mix at full size (mip level 0). </summary>
        public static Result Mix(ChannelMixSettings settings)
        {
            GetSize(settings, out int width, out int height);
            return MixAt(settings, width, height);
        }

        /// <summary>
        /// The mix at a mip level (0 = full size): every source is resampled to the size of the level, which makes the GPU take
        /// its own mip level of it, as the texture importer made it (Kaiser, preserve coverage and the rest), so the mix is made
        /// of the mips of the sources. Only good if the sources have mips, see <see cref="SourcesHaveMips"/>.
        /// </summary>
        public static Result MixLevel(ChannelMixSettings settings, int level)
        {
            GetSize(settings, out int width, out int height);
            return MixAt(settings, Mathf.Max(1, width >> level), Mathf.Max(1, height >> level));
        }

        /// <summary> Whether every texture the mix uses has mip levels (one that has none cannot give the mips of a mix). </summary>
        public static bool SourcesHaveMips(ChannelMixSettings settings)
        {
            for (int c = 0; c < settings.ChannelCount; c++)
            {
                var output = settings.GetOutput(c);
                if (IsConstant(output))
                    continue;
                if (output.texture.mipmapCount <= 1)
                    return false;
            }
            return true;
        }

        /// <summary> How many mip levels a texture of the size has, the full size included. </summary>
        public static int LevelCount(int width, int height)
        {
            int levels = 1;
            for (int size = Mathf.Max(width, height); size > 1; size >>= 1)
                levels++;
            return levels;
        }

        private static bool IsConstant(ChannelMixSettings.Output output)
            => output.texture == null || output.channel == ChannelMixSettings.SourceChannel.Constant;

        private static Result MixAt(ChannelMixSettings settings, int width, int height)
        {
            int count = settings.ChannelCount;
            int pixels = width * height;
            var bytes = new byte[pixels * count];

            var output = new NativeArray<byte>(bytes.Length, Allocator.TempJob);
            var copies = new Dictionary<Texture2D, Texture2D>(); // each source as the CPU can read it, at this size
            try
            {
                for (int c = 0; c < count; c++)
                {
                    var setting = settings.GetOutput(c);
                    if (IsConstant(setting))
                    {
                        new ConstantJob { output = output, stride = count, channel = c, value = ToByte(setting.constant) }
                            .Schedule(pixels, 4096).Complete();
                        continue;
                    }

                    if (!copies.TryGetValue(setting.texture, out Texture2D copy))
                    {
                        copy = Read(setting.texture, width, height);
                        copies[setting.texture] = copy;
                    }

                    // A shader reads an sRGB texture decoded to linear (in a Linear color space project): the stored values are
                    // got by encoding the color again. The alpha is never encoded.
                    bool encode = setting.readMode == ChannelMixSettings.ReadMode.AsStored && setting.texture.isDataSRGB
                                  && QualitySettings.activeColorSpace == ColorSpace.Linear;
                    new ChannelJob
                    {
                        colors = copy.GetRawTextureData<float4>(),
                        output = output,
                        stride = count,
                        channel = c,
                        source = (int)setting.channel,
                        encode = encode,
                        invert = setting.invert,
                        low = setting.range.x,
                        scale = 1f / Mathf.Max(setting.range.y - setting.range.x, 1e-5f),
                    }.Schedule(pixels, 4096).Complete();
                }
                output.CopyTo(bytes);
            }
            finally
            {
                output.Dispose();
                foreach (var copy in copies.Values)
                    Object.DestroyImmediate(copy);
            }

            return new Result { bytes = bytes, width = width, height = height, channelCount = count, sRGB = settings.sRGB && count >= 3 };
        }

        [BurstCompile]
        private struct ConstantJob : IJobParallelFor
        {
            // Every pixel writes its own bytes (and the other channels have their own jobs, which run one after the other).
            [NativeDisableParallelForRestriction] public NativeArray<byte> output;
            public int stride;
            public int channel;
            public byte value;

            public void Execute(int i) => output[i * stride + channel] = value;
        }

        [BurstCompile]
        private struct ChannelJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float4> colors;
            [NativeDisableParallelForRestriction] public NativeArray<byte> output;
            public int stride;
            public int channel;
            public int source;      // a ChannelMixSettings.SourceChannel
            public bool encode;
            public bool invert;
            public float low;
            public float scale;

            public void Execute(int i)
            {
                float value = Pick(colors[i], source, encode);
                value = math.saturate((value - low) * scale);
                if (invert)
                    value = 1f - value;
                output[i * stride + channel] = ToByte(value);
            }
        }

        /// <summary>
        /// The mip levels below the given one (the first in the list is level 1), each the average of 2x2 pixels of the one before.
        /// The bytes are `stride` channels per pixel. For an sRGB texture the color channels (the first three) are averaged in
        /// linear light, as the texture importer does (and as a shader would blend them): averaging the encoded values makes
        /// bright detail too dark, so the mips come out with too much contrast. The alpha, and every channel of a data
        /// texture, are averaged as they are.
        /// </summary>
        public static List<byte[]> GenerateMips(byte[] level0, int width, int height, int stride, bool srgb)
        {
            var mips = new List<byte[]>();
            var decode = new NativeArray<float>(Decode, Allocator.TempJob);
            var encode = new NativeArray<byte>(Encode, Allocator.TempJob);
            var source = new NativeArray<byte>(level0, Allocator.TempJob);
            try
            {
                int sw = width;
                int sh = height;
                while (sw > 1 || sh > 1)
                {
                    int dw = Mathf.Max(1, sw >> 1);
                    int dh = Mathf.Max(1, sh >> 1);
                    var destination = new NativeArray<byte>(dw * dh * stride, Allocator.TempJob);
                    new MipJob
                    {
                        source = source, destination = destination, decode = decode, encode = encode,
                        sw = sw, sh = sh, dw = dw, stride = stride, srgb = srgb,
                    }.Schedule(dh, 8).Complete();

                    mips.Add(destination.ToArray());
                    source.Dispose();
                    source = destination;
                    sw = dw;
                    sh = dh;
                }
            }
            finally
            {
                decode.Dispose();
                encode.Dispose();
                source.Dispose();
            }
            return mips;
        }

        // One row of the next level for each index.
        [BurstCompile]
        private struct MipJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<byte> source;
            [NativeDisableParallelForRestriction] public NativeArray<byte> destination;
            [ReadOnly] public NativeArray<float> decode;   // byte to linear
            [ReadOnly] public NativeArray<byte> encode;    // linear (in EncodeSteps) to byte
            public int sw, sh, dw, stride;
            public bool srgb;

            public void Execute(int y)
            {
                int y0 = math.min(2 * y, sh - 1);
                int y1 = math.min(2 * y + 1, sh - 1);
                for (int x = 0; x < dw; x++)
                {
                    int x0 = math.min(2 * x, sw - 1);
                    int x1 = math.min(2 * x + 1, sw - 1);
                    int a = (y0 * sw + x0) * stride;
                    int b = (y0 * sw + x1) * stride;
                    int c = (y1 * sw + x0) * stride;
                    int d = (y1 * sw + x1) * stride;
                    int o = (y * dw + x) * stride;
                    for (int ch = 0; ch < stride; ch++)
                    {
                        if (srgb && ch < 3)
                        {
                            float sum = decode[source[a + ch]] + decode[source[b + ch]] + decode[source[c + ch]] + decode[source[d + ch]];
                            destination[o + ch] = encode[math.min((int)(sum * 0.25f * (EncodeSteps - 1) + 0.5f), EncodeSteps - 1)];
                        }
                        else
                        {
                            destination[o + ch] = (byte)((source[a + ch] + source[b + ch] + source[c + ch] + source[d + ch] + 2) >> 2);
                        }
                    }
                }
            }
        }

        // Tables for the sRGB curve on bytes: byte to linear, and linear (in steps) back to byte. Managed, copied for the jobs.
        private const int EncodeSteps = 4096;
        private static readonly float[] Decode = MakeDecodeTable();
        private static readonly byte[] Encode = MakeEncodeTable();

        private static float[] MakeDecodeTable()
        {
            var table = new float[256];
            for (int i = 0; i < table.Length; i++)
            {
                float v = i / 255f;
                table[i] = v <= 0.04045f ? v / 12.92f : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f);
            }
            return table;
        }

        private static byte[] MakeEncodeTable()
        {
            var table = new byte[EncodeSteps];
            for (int i = 0; i < table.Length; i++)
                table[i] = ToByte(LinearToSRGB(i / (float)(EncodeSteps - 1)));
            return table;
        }

        /// <summary> The size of the output. </summary>
        public static void GetSize(ChannelMixSettings settings, out int width, out int height)
        {
            if (settings.sizeMode == ChannelMixSettings.SizeMode.Custom)
            {
                width = Mathf.Clamp(settings.customSize.x, 1, ChannelMixSettings.MaxSize);
                height = Mathf.Clamp(settings.customSize.y, 1, ChannelMixSettings.MaxSize);
                return;
            }

            width = 0;
            height = 0;
            for (int c = 0; c < settings.ChannelCount; c++)
            {
                var output = settings.GetOutput(c);
                if (IsConstant(output))
                    continue;
                width = Mathf.Max(width, output.texture.width);
                height = Mathf.Max(height, output.texture.height);
            }
            // Nothing to take the size from: a token texture of constants.
            width = width == 0 ? 4 : Mathf.Min(width, ChannelMixSettings.MaxSize);
            height = height == 0 ? 4 : Mathf.Min(height, ChannelMixSettings.MaxSize);
        }

        // The texture as a shader samples it (so an sRGB texture is decoded in a Linear color space project), resampled to the
        // size (from its own mip level, when that is smaller). A float texture the jobs read: the caller destroys it.
        private static Texture2D Read(Texture2D source, int width, int height)
        {
            var target = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            var previous = RenderTexture.active;
            Texture2D readable = null;
            try
            {
                Graphics.Blit(source, target);
                RenderTexture.active = target;
                readable = new Texture2D(width, height, TextureFormat.RGBAFloat, false, true);
                readable.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                return readable;
            }
            catch
            {
                if (readable != null)
                    Object.DestroyImmediate(readable);
                throw;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
            }
        }

        private static float Pick(float4 color, int channel, bool encode)
        {
            if (channel == (int)ChannelMixSettings.SourceChannel.A)
                return color.w;

            float3 rgb = encode ? LinearToSRGB(color.xyz) : color.xyz;
            switch (channel)
            {
                case (int)ChannelMixSettings.SourceChannel.R: return rgb.x;
                case (int)ChannelMixSettings.SourceChannel.G: return rgb.y;
                case (int)ChannelMixSettings.SourceChannel.B: return rgb.z;
                default: return 0.2126f * rgb.x + 0.7152f * rgb.y + 0.0722f * rgb.z;
            }
        }

        // The exact sRGB curve (Mathf.LinearToGammaSpace is an approximation).
        private static float3 LinearToSRGB(float3 value)
        {
            value = math.saturate(value);
            return math.select(1.055f * math.pow(value, 1f / 2.4f) - 0.055f, 12.92f * value, value <= 0.0031308f);
        }

        private static float LinearToSRGB(float value)
        {
            value = math.saturate(value);
            return value <= 0.0031308f ? 12.92f * value : 1.055f * math.pow(value, 1f / 2.4f) - 0.055f;
        }

        private static byte ToByte(float value) => (byte)(int)(math.saturate(value) * 255f + 0.5f);
    }
}

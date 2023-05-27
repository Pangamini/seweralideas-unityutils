#if UNITY_COLLECTIONS && UNITY_MATHEMATICS

using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace SeweralIdeas.UnityUtils
{
    public struct NativeTextureRGBA32 : INativeDisposable
    {
        public readonly int2 Size;
        [ReadOnly] private NativeArray<Color32> m_array;
        private readonly bool m_ownArray;
        
        public NativeTextureRGBA32(Texture2D texture)
        {
            Size = new int2(texture.width, texture.height);
            m_array = texture.GetPixelData<Color32>(0);
            m_ownArray = false;
        }

        public Color32 GetPixel(int2 pixelCoords)
        {
            pixelCoords = math.clamp(pixelCoords, int2.zero, Size - 1);
            return m_array[pixelCoords.x + pixelCoords.y * Size.x];
        }

        public float4 GetPixelBilinear(float u, float v) => GetPixelBilinear(new float2(u, v));

        public float4 GetPixelBilinear(float2 uv)
        {
            float2 coords = uv * Size;
            coords -= 0.5f;
            float2 frac = math.frac(coords);
            int2 c0 = (int2)math.floor(coords);
            int2 c1 = c0 + 1;
            
            float4 s00 = (float4)(Vector4)(Color)GetPixel(c0);
            float4 s10 = (float4)(Vector4)(Color)GetPixel(new int2(c1.x, c0.y));
            float4 s01 = (float4)(Vector4)(Color)GetPixel(new int2(c0.x, c1.y));
            float4 s11 = (float4)(Vector4)(Color)GetPixel(new int2(c1.x, c1.y));

            float4 y0 = math.lerp(s00, s10, frac.x);
            float4 y1 = math.lerp(s01, s11, frac.x);
            float4 sample = math.lerp(y0, y1, frac.y);
            return sample;
        }
        
        public void Dispose() => Dispose(default);
        
        public JobHandle Dispose(JobHandle inputDeps)
        {
            if(m_ownArray)
                return m_array.Dispose(inputDeps);
            return inputDeps;
        }
    }
}


#endif
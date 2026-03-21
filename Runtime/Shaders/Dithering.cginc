#ifndef DITHERING_INCLUDED
#define DITHERING_INCLUDED

// ============================================================================
//  Dithering.cginc
//
//  Drop-in output dithering for Unity shaders.
//  Eliminates color banding by injecting unbiased noise scaled to the
//  render target's quantization step size, in the correct color space.
//
//  USAGE:
//
//  1. #include "Dithering.cginc"
//
//  2. From C# script, set per-material (or global) properties:
//
//       // Bits per channel of your render target (8, 10, 16 …)
//       material.SetFloat("_DitherBitDepth", 8);
//
//       // 1 if the RT is an sRGB format (e.g. R8G8B8A8_SRGB), 0 if linear/UNorm
//       material.SetFloat("_DitherSRGB", 1);
//
//     Or set them globally:
//       Shader.SetGlobalFloat("_DitherBitDepth", 8);
//       Shader.SetGlobalFloat("_DitherSRGB", 1);
//
//  3. In your fragment shader, right before returning:
//
//       return ApplyDither(color, input.positionCS.xy);
//
//     positionCS.xy is the SV_POSITION pixel coordinates.
//
//  OPTIONAL — temporal jitter (recommended when using TAA):
//
//       Shader.SetGlobalFloat("_DitherFrameIndex", Time.frameCount);
//
//     If _DitherFrameIndex is not set it defaults to 0 (static pattern),
//     which is fine without TAA.
//
// ============================================================================

// ---------------------------------------------------------------------------
//  Shader properties — declare these in your Properties block or set globally
// ---------------------------------------------------------------------------
//
//  Properties {
//      [HideInInspector] _DitherBitDepth ("Dither Bit Depth", Float) = 8
//      [HideInInspector] _DitherSRGB    ("Dither sRGB",      Float) = 1
//      [HideInInspector] _DitherFrameIndex ("Frame Index",   Float) = 0
//  }

uniform float _DitherBitDepth;
uniform float _DitherSRGB;
uniform float _DitherFrameIndex;


// ---------------------------------------------------------------------------
//  Interleaved Gradient Noise  (Jorge Jimenez, 2014)
//  Single ALU, no texture fetch, excellent spatial distribution.
// ---------------------------------------------------------------------------
float InterleavedGradientNoise(float2 screenPos)
{
    float3 magic = float3(0.06711056, 0.00583715, 52.9829189);
    return frac(magic.z * frac(dot(screenPos.xy, magic.xy)));
}


// ---------------------------------------------------------------------------
//  sRGB <-> Linear  (piecewise IEC 61966-2-1 exact curves)
// ---------------------------------------------------------------------------
float LinearToSRGB_Channel(float c)
{
    // Exact piecewise sRGB OETF
    return (c <= 0.0031308)
        ? c * 12.92
        : 1.055 * pow(abs(c), 1.0 / 2.4) - 0.055;
}

float SRGBToLinear_Channel(float c)
{
    // Exact piecewise sRGB EOTF
    return (c <= 0.04045)
        ? c / 12.92
        : pow(abs((c + 0.055) / 1.055), 2.4);
}

float3 LinearToSRGB(float3 c)
{
    return float3(
        LinearToSRGB_Channel(c.r),
        LinearToSRGB_Channel(c.g),
        LinearToSRGB_Channel(c.b));
}

float3 SRGBToLinear(float3 c)
{
    return float3(
        SRGBToLinear_Channel(c.r),
        SRGBToLinear_Channel(c.g),
        SRGBToLinear_Channel(c.b));
}


// ---------------------------------------------------------------------------
//  ApplyDither
//
//  colorLinear : your final fragment color in LINEAR space (rgb)
//  screenPos   : SV_POSITION .xy (pixel coordinates, not UVs)
//
//  Returns the color with dithering applied (still in linear space).
//  The GPU's format conversion to the RT happens after this, so the
//  noise is correctly placed to break up quantization.
// ---------------------------------------------------------------------------
float4 ApplyDither(float4 colorLinear, float2 screenPos)
{
    // Temporal offset — decorrelates the pattern across frames.
    // 5.588238 is chosen so consecutive frames produce maximally
    // different patterns (co-prime-ish with the noise constants).
    float2 ditherPos = screenPos + 5.588238 * _DitherFrameIndex;

    // Unbiased noise in [-0.5, 0.5)
    float noise = InterleavedGradientNoise(ditherPos) - 0.5;

    // Quantization step: 1 / (2^bits - 1)
    float levels = exp2(_DitherBitDepth) - 1.0;
    float step = 1.0 / levels;

    if (_DitherSRGB > 0.5)
    {
        // sRGB target: quantization occurs in sRGB space, so dither there.
        float3 srgb = LinearToSRGB(saturate(colorLinear.rgb));
        srgb += noise * step;
        colorLinear.rgb = SRGBToLinear(srgb);
    }
    else
    {
        // Linear / UNorm target: dither directly in linear space.
        colorLinear.rgb += noise * step;
    }

    return colorLinear;
}


// ---------------------------------------------------------------------------
//  Convenience overload for float3 (no alpha)
// ---------------------------------------------------------------------------
float3 ApplyDither(float3 colorLinear, float2 screenPos)
{
    float4 c = ApplyDither(float4(colorLinear, 1.0), screenPos);
    return c.rgb;
}


// ---------------------------------------------------------------------------
//  ApplyDither with explicit parameters (no uniforms needed)
//  Useful in compute shaders or when you don't want global state.
// ---------------------------------------------------------------------------
float4 ApplyDither(float4 colorLinear, float2 screenPos, float bitDepth, bool isSRGB, float frameIndex)
{
    float2 ditherPos = screenPos + 5.588238 * frameIndex;
    float noise = InterleavedGradientNoise(ditherPos) - 0.5;

    float levels = exp2(bitDepth) - 1.0;
    float step = 1.0 / levels;

    if (isSRGB)
    {
        float3 srgb = LinearToSRGB(saturate(colorLinear.rgb));
        srgb += noise * step;
        colorLinear.rgb = SRGBToLinear(srgb);
    }
    else
    {
        colorLinear.rgb += noise * step;
    }

    return colorLinear;
}


#endif // DITHERING_INCLUDED

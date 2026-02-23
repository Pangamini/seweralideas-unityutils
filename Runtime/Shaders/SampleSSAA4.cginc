#ifndef SSAA_4TAP_INCLUDED
#define SSAA_4TAP_INCLUDED

inline float4 SampleSSAA4(sampler2D smplr, float2 uv, float lodBias)
{
    // Screen-space UV gradients
    float2 du = ddx(uv);
    float2 dv = ddy(uv);

    // Rotated 2x2 sample pattern
    float2 a = du * 0.125 + dv * 0.375;
    float2 b = du * 0.375 - dv * 0.125;

    float4 sum = 0;
    sum += tex2Dbias(smplr, float4(uv + a, 0, lodBias));
    sum += tex2Dbias(smplr, float4(uv - a, 0, lodBias));
    sum += tex2Dbias(smplr, float4(uv + b, 0, lodBias));
    sum += tex2Dbias(smplr, float4(uv - b, 0, lodBias));

    return sum * 0.25;
}

#endif // SSAA_4TAP_INCLUDED
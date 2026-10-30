#ifndef PROJECT_LX_GROUND_DEPTH_INCLUDED
#define PROJECT_LX_GROUND_DEPTH_INCLUDED
TEXTURE2D(_GroundDepthMask);
SAMPLER(sampler_GroundDepthMask);

// Identical layout in lit, normal and forward passes.
CBUFFER_START(UnityPerMaterial)
    half4 _Color;
    float4x4 _WorldToGroundCell;
    float4 _GroundDepthRect;
    float4 _GroundDepthSettings;
    half4 _GroundDepthTint;
    float _GroundDepthEnabled;
CBUFFER_END

float2 GroundDepthUV(float3 worldPosition)
{
    float2 cell = mul(_WorldToGroundCell, float4(worldPosition, 1)).xy;
    return (cell - _GroundDepthRect.xy) * _GroundDepthRect.zw;
}

half3 GroundDepthMultiplier(float2 uv)
{
    float depth = SAMPLE_TEXTURE2D(_GroundDepthMask, sampler_GroundDepthMask, uv).r;
    float t = saturate((depth - _GroundDepthSettings.x) / max(_GroundDepthSettings.y, 0.001));
    t = t * t * (3.0 - 2.0 * t);
    t = pow(t, max(_GroundDepthSettings.w, 0.01));
    float amount = t * _GroundDepthSettings.z * _GroundDepthEnabled;
    return lerp(half3(1, 1, 1), _GroundDepthTint.rgb, amount);
}
#endif

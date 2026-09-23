Shader "ProjectLx/ScreenAwakeningEdge"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "Awakening Edge"
            ZWrite Off ZTest Always Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Fragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _LxScreenEdgeStrength;
            float _LxScreenEffectTime;

            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                float2 edgeDistance = abs(uv * 2.0 - 1.0);
                // Central 60% of each axis is completely undistorted.
                float edge = smoothstep(0.6, 1.0, max(edgeDistance.x, edgeDistance.y));
                float2 wave = float2(
                    sin(uv.y * 31.0 + _LxScreenEffectTime * 2.4),
                    cos(uv.x * 29.0 - _LxScreenEffectTime * 2.0));
                float2 offset = wave * (edge * edge * _LxScreenEdgeStrength);
                offset.x *= _ScaledScreenParams.y / max(_ScaledScreenParams.x, 1.0);
                float2 halfTexel = 0.5 / _ScaledScreenParams.xy;
                uv = clamp(uv + offset, halfTexel, 1.0 - halfTexel);
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
            }
            ENDHLSL
        }
    }
}

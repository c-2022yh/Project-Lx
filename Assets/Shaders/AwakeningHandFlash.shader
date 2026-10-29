Shader "ProjectLx/Awakening Hand Flash"
{
    Properties
    {
        [HDR] _FlashColor ("Flash Color", Color) = (0.2,0.5,1,1)
        _FlashIntensity ("Flash Intensity", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha One
        ColorMask RGB
        Cull Off
        ZWrite Off
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _FlashColor;
                float _FlashIntensity;
            CBUFFER_END
            struct Attributes { float3 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionCS=TransformObjectToHClip(input.positionOS);
                output.uv=input.uv;
                return output;
            }
            half4 Fragment(Varyings input):SV_Target
            {
                float2 p=(input.uv-0.5)*2.0;
                float core=exp2(-dot(p,p)*38.0);
                float horizontal=exp2(-abs(p.y)*48.0)*pow(saturate(1.0-abs(p.x)),3.0);
                float vertical=exp2(-abs(p.x)*48.0)*pow(saturate(1.0-abs(p.y)),3.0);
                float halo=exp2(-dot(p,p)*9.0)*0.12;
                return half4(_FlashColor.rgb*_FlashIntensity,saturate(core+horizontal+vertical+halo));
            }
            ENDHLSL
        }
    }
}

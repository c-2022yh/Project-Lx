Shader "ProjectLx/Moonlight Shard"
{
    Properties
    {
        [HDR] _Tint ("White Tint", Color) = (2,2,2,1)
        _Opacity ("Opacity", Float) = 1
        _RimIntensity ("White Edge Brightness", Float) = 1.6
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha One
        Cull Off
        ZWrite Off
        ColorMask RGB
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                float _Opacity, _RimIntensity;
            CBUFFER_END
            struct Attributes { float3 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            Varyings Vert(Attributes input)
            {
                Varyings output;output.positionCS=TransformObjectToHClip(input.positionOS);
                output.uv=input.uv;output.color=input.color;return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                float2 p=input.uv*2.0-1.0;
                float diamond=abs(p.x)+abs(p.y)*2.2;
                float body=1.0-smoothstep(0.75,0.95,diamond);
                float edge=smoothstep(0.5,0.76,diamond)*(1.0-smoothstep(0.76,1.05,diamond));
                float glow=pow(saturate(1.0-diamond/1.25),2.0)*0.15;
                float brightness=1.0+edge*_RimIntensity;
                return half4(_Tint.rgb*brightness*input.color.rgb,(body+glow)*_Opacity*_Tint.a*input.color.a);
            }
            ENDHLSL
        }
    }
}

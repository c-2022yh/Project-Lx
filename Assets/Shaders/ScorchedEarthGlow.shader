Shader "ProjectLx/Scorched Earth Red Glow"
{
    Properties
    {
        [HDR] _CoreColor ("Red Core", Color) = (3.6,0.035,0.07,1)
        [HDR] _HaloColor ("Crimson Halo", Color) = (1.8,0.015,0.04,1)
        _HaloStrength ("Halo Strength", Range(0,1)) = 0.3
        _CoreRatio ("Core Ratio", Float) = 0.5
        _CoreSoftness ("Core Softness", Float) = 1.6
        _Shape ("Core / Sparkle / Soft Mote", Float) = 0
        _Opacity ("Opacity", Float) = 1
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
                half4 _CoreColor, _HaloColor;
                float _HaloStrength, _CoreRatio, _CoreSoftness, _Shape, _Opacity;
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
                float radius=length(p);
                float center=exp(-3.0*radius*radius/max(0.0025,_CoreRatio*_CoreRatio*_CoreSoftness));
                float glow=exp(-3.0*radius*radius)*saturate(1.0-radius)*_HaloStrength;
                center*=1.0-smoothstep(0.7,1.0,radius);
                float mask;
                if(_Shape<0.5) mask=max(center,glow);
                else if(_Shape<1.5)
                {
                    float cross=min(abs(p.x),abs(p.y));
                    mask=pow(saturate(1.0-cross*10.0),2.0)*pow(saturate(1.0-max(abs(p.x),abs(p.y))),1.5);
                    mask=max(mask,glow);
                }
                else
                {
                    mask=exp(-4.0*radius*radius)*(1.0-smoothstep(0.55,1.0,radius));
                    center=exp(-3.0*radius*radius);
                }
                half3 tint=lerp(_HaloColor.rgb,_CoreColor.rgb,saturate(center*2.0));
                return half4(tint*input.color.rgb,mask*_Opacity*input.color.a*_CoreColor.a);
            }
            ENDHLSL
        }
    }
}

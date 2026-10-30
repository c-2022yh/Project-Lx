Shader "ProjectLx/Dash Moonlight"
{
    Properties
    {
        [PerRendererData] _MainTex ("Original Dash Sprite", 2D) = "white" {}
        _StartX ("World Start X", Float) = 0
        _Direction ("Direction", Float) = 1
        _Distance ("Distance", Float) = 7
        _RevealDistance ("Revealed Distance", Float) = 0
        _RevealSoftness ("Reveal Softness", Float) = 0.12
        _Age ("Age", Float) = 0
        _TravelTime ("Travel Time", Float) = 0.05
        _FlashTime ("Flash Time", Float) = 0.055
        _AfterglowTime ("Afterglow Time", Float) = 0.2
        _FlashIntensity ("Flash Intensity", Float) = 3
        _AfterglowIntensity ("Afterglow Intensity", Float) = 0.7
        _AlphaPower ("Core Alpha Power", Float) = 1.4
        _CoreWidthPixels ("White Cut Thickness", Float) = 8
        _EdgeGlow ("White Edge Glow", Float) = 0.65
        _EdgePixels ("Edge Width", Float) = 2
        _FinishedAt ("Finish Time", Float) = -1
        _EndFlashTime ("End Flash Time", Float) = 0.035
        _EndFlashIntensity ("End Flash Intensity", Float) = 4
        _EndFadeTime ("End Fade Time", Float) = 0.08
        _EndHoldTime ("End Hold Time", Float) = 0.04
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
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;
            CBUFFER_START(UnityPerMaterial)
                float _StartX, _Direction, _Distance, _RevealDistance, _RevealSoftness;
                float _Age, _TravelTime, _FlashTime, _AfterglowTime, _FlashIntensity, _AfterglowIntensity;
                float _AlphaPower, _CoreWidthPixels, _EdgeGlow, _EdgePixels, _FinishedAt, _EndFlashTime, _EndFlashIntensity, _EndFadeTime, _EndHoldTime;
            CBUFFER_END
            struct Attributes { float3 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float worldX:TEXCOORD1; half4 color:COLOR; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 world=TransformObjectToWorld(input.positionOS);
                output.positionCS=TransformWorldToHClip(world);
                output.worldX=world.x;
                output.uv=input.uv;
                output.color=input.color;
                return output;
            }
            float SampleInk(float2 uv)
            {
                half4 sample=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv);
                return sample.a*dot(sample.rgb,half3(0.2126,0.7152,0.0722));
            }
            half4 Frag(Varyings input):SV_Target
            {
                half4 original=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv);
                float along=(input.worldX-_StartX)*_Direction;
                float reveal=1.0-smoothstep(max(0.0,_RevealDistance-_RevealSoftness),max(0.001,_RevealDistance),along);
                reveal*=step(0.001,_RevealDistance)*step(0.0,along)*step(along,_Distance);
                float localAge=max(0.0,_Age-saturate(along/max(0.001,_Distance))*_TravelTime);
                float flash=pow(saturate(1.0-localAge/max(0.001,_FlashTime)),1.5);
                float fade=pow(saturate(1.0-max(0.0,localAge-_FlashTime)/max(0.001,_AfterglowTime)),1.5);
                float ink=dot(original.rgb,half3(0.2126,0.7152,0.0722));
                float intensity=lerp(_AfterglowIntensity,_FlashIntensity,flash);
                if(_FinishedAt>=0.0)
                {
                    float endAge=max(0.0,_Age-_FinishedAt);
                    float fadeAge=max(0.0,endAge-_EndHoldTime);
                    intensity=max(intensity,_EndFlashIntensity*saturate(1.0-fadeAge/max(0.001,_EndFlashTime)));
                    float endFade=pow(saturate(1.0-fadeAge/max(0.001,_EndFadeTime)),1.2);
                    fade=_EndFlashIntensity>0.0 ? endFade : fade*endFade;
                }
                float2 widthUV=_MainTex_TexelSize.xy*_CoreWidthPixels;
                float expanded=original.a*ink;
                // Contiguous vertical samples widen the near-horizontal cut without duplicating its tips.
                [unroll] for(int offset=-8;offset<=8;offset++)
                    expanded=max(expanded,SampleInk(input.uv+float2(0,offset/8.0)*widthUV));
                float core=pow(saturate(expanded),_AlphaPower);
                float2 edgeUV=_MainTex_TexelSize.xy*(_CoreWidthPixels+_EdgePixels);
                float neighbor=max(max(SampleInk(input.uv+float2(edgeUV.x,0)),SampleInk(input.uv-float2(edgeUV.x,0))),
                    max(SampleInk(input.uv+float2(0,edgeUV.y)),SampleInk(input.uv-float2(0,edgeUV.y))));
                float rim=max(0.0,pow(saturate(neighbor),_AlphaPower)-core);
                return half4(intensity.xxx,(core+rim*_EdgeGlow)*reveal*fade*input.color.a);
            }
            ENDHLSL
        }
    }
}

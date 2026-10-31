Shader "ProjectLx/Skill Starlight"
{
    Properties
    {
        [HDR] _Tint ("Tint", Color) = (0.65, 1.6, 2.1, 1)
        _Opacity ("Opacity", Range(0,1)) = 1
        _Shape ("Shape: Ribbon / Star / Sprite Edge / Pose", Float) = 0
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _EdgePixels ("Edge Width In Pixels", Float) = 3
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
                float _Opacity;
                float _Shape;
                float _EdgePixels;
            CBUFFER_END
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;
            struct Attributes { float3 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS=TransformObjectToHClip(input.positionOS);
                output.uv=input.uv;
                output.color=input.color;
                return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                float2 p=input.uv*2.0-1.0;
                float mask;
                float core;
                if (_Shape<0.5)
                {
                    mask=pow(saturate(1.0-abs(p.y)),1.6)*smoothstep(0.0,0.1,input.uv.x)*(1.0-smoothstep(0.9,1.0,input.uv.x));
                    core=pow(saturate(1.0-abs(p.y)*3.0),2.0);
                }
                else if (_Shape<1.5)
                {
                    float cross=min(abs(p.x),abs(p.y));
                    float extent=max(abs(p.x),abs(p.y));
                    mask=pow(saturate(1.0-cross*10.0),2.0)*pow(saturate(1.0-extent),1.3);
                    mask=max(mask,pow(saturate(1.0-length(p)),4.0)*0.45);
                    core=pow(saturate(1.0-length(p)*3.0),2.0);
                }
                else
                {
                    float2 stepUV=_MainTex_TexelSize.xy*_EdgePixels;
                    float alpha=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv).a;
                    float low=alpha;
                    float high=alpha;
                    [unroll] for(int x=-1;x<=1;x++)
                    [unroll] for(int y=-1;y<=1;y++)
                    {
                        float neighbor=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv+float2(x,y)*stepUV).a;
                        low=min(low,neighbor);
                        high=max(high,neighbor);
                    }
                    mask=saturate(alpha-low+(high-alpha)*0.5);
                    if (_Shape>2.5) mask=saturate(mask+alpha*0.16);
                    core=0.0;
                }
                half3 tint=lerp(_Tint.rgb,max(_Tint.rgb,half3(2.0,2.0,2.0)),core);
                return half4(tint*input.color.rgb,mask*_Opacity*_Tint.a*input.color.a);
            }
            ENDHLSL
        }
    }
}

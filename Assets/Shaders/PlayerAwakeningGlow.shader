Shader "ProjectLx/Player Awakening Glow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HDR] _GlowColor ("Glow Color", Color) = (0.2, 0.5, 1, 1)
        _GlowIntensity ("Glow Intensity", Float) = 6
        _OutlinePixels ("Inner Outline Pixels", Range(0.5, 8)) = 2
        _BodyGlow ("Body Glow", Range(0, 0.3)) = 0.025
        [HideInInspector] _SpriteUVRect ("Sprite UV Rect", Vector) = (0, 0, 1, 1)
        [HideInInspector] _Color ("Tint", Color) = (1, 1, 1, 1)
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="True" }
        Blend SrcAlpha One
        ColorMask RGB
        Cull Off
        ZWrite Off
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex GlowVertex
            #pragma fragment GlowFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;
            CBUFFER_START(UnityPerMaterial)
                half4 _GlowColor;
                float4 _SpriteUVRect;
                float _GlowIntensity;
                float _OutlinePixels;
                float _BodyGlow;
            CBUFFER_END

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half alpha : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings GlowVertex(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                SetUpSpriteInstanceProperties();
                Varyings output = (Varyings)0;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                output.positionCS = TransformObjectToHClip(positionOS);
                output.uv = input.uv;
                output.alpha = input.color.a * unity_SpriteColor.a;
                return output;
            }

            half SpriteAlpha(float2 uv)
            {
                float2 inside = step(_SpriteUVRect.xy, uv) * step(uv, _SpriteUVRect.zw);
                float2 safeUv = clamp(uv, _SpriteUVRect.xy, _SpriteUVRect.zw);
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, safeUv).a * inside.x * inside.y;
            }

            half4 GlowFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half alpha = SpriteAlpha(input.uv);
                float2 d = _MainTex_TexelSize.xy * _OutlinePixels;
                half surrounding = SpriteAlpha(input.uv + float2(d.x, 0));
                surrounding = min(surrounding, SpriteAlpha(input.uv - float2(d.x, 0)));
                surrounding = min(surrounding, SpriteAlpha(input.uv + float2(0, d.y)));
                surrounding = min(surrounding, SpriteAlpha(input.uv - float2(0, d.y)));
                surrounding = min(surrounding, SpriteAlpha(input.uv + d * 0.7071));
                surrounding = min(surrounding, SpriteAlpha(input.uv - d * 0.7071));
                surrounding = min(surrounding, SpriteAlpha(input.uv + float2(d.x, -d.y) * 0.7071));
                surrounding = min(surrounding, SpriteAlpha(input.uv + float2(-d.x, d.y) * 0.7071));
                // An inward rim survives tight sprite meshes; Bloom spreads it outside.
                half rim = saturate(alpha - surrounding);
                half emission = lerp(_BodyGlow, 1.0h, rim) * _GlowIntensity;
                return half4(_GlowColor.rgb * emission, alpha * input.alpha);
            }
            ENDHLSL
        }
    }
}

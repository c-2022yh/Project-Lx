Shader "ProjectLx/2D/AttackTrailAdditive"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _GlowWidth ("Edge Width (Texture Pixels)", Range(0.5, 8)) = 3
        _GlowIntensity ("Edge Intensity", Float) = 2.5
        [HideInInspector] _GlowTexelSize ("Glow Texel Size", Vector) = (0.001953125,0.001953125,512,512)
        [MaterialToggle] _ZWrite("ZWrite", Float) = 0

        // Legacy properties. They're here so that materials using this shader can gracefully fallback to the legacy sprite shader.
        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha One, Zero One
        Cull Off
        ZWrite [_ZWrite]

        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex UnlitVertex
            #pragma fragment UnlitFragment

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"
          
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _GlowWidth;
                float _GlowIntensity;
                float4 _GlowTexelSize;
            CBUFFER_END

            Varyings UnlitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings o = CommonUnlitVertex(input);
                o.color = input.color *_Color * unity_SpriteColor;
                return o;
            }

            // Sample the silhouette in eight directions to form a narrow outer halo.
            half ExpandedAlpha(float2 uv, float2 offset)
            {
                half alpha = 0;
                alpha = max(alpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(offset.x, 0)).a);
                alpha = max(alpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(offset.x, 0)).a);
                alpha = max(alpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0, offset.y)).a);
                alpha = max(alpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(0, offset.y)).a);
                float2 diagonal = offset * 0.70710678;
                alpha = max(alpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + diagonal).a);
                alpha = max(alpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - diagonal).a);
                alpha = max(alpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(diagonal.x, -diagonal.y)).a);
                alpha = max(alpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-diagonal.x, diagonal.y)).a);
                return alpha;
            }

            half4 UnlitFragment(Varyings input) : SV_Target
            {
                half center = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
                float2 offset = _GlowTexelSize.xy * _GlowWidth;
                half nearAlpha = ExpandedAlpha(input.uv, offset * 0.5);
                half farAlpha = ExpandedAlpha(input.uv, offset);
                half halo = saturate(nearAlpha * 0.65 + farAlpha * 0.35) * (1 - center);
                return half4(input.color.rgb * _GlowIntensity, halo * input.color.a);
            }
            ENDHLSL
        }
    }
}

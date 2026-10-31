Shader "ProjectLx/Awakening Slash Flame"
{
    // 각성 베기 잔상: 원래 베기 그림을 푸른 불꽃처럼 이글거리게 그린다.
    //   아지랑이처럼 일렁이는 모양 + 바깥으로 번지는 빛 + 잘게 끊어지며 타오르는 가장자리 + 가운데 흰빛
    // 색은 SpriteRenderer 색(AwakeningSlashFx의 Echo Color)을 쓴다. 베기 그림 애니메이션을 그대로 따라간다.
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Glow ("Glow Radius (UV)", Range(0, 0.05)) = 0.018
        _Distort ("Heat Distortion (UV)", Range(0, 0.05)) = 0.018
        _Speed ("Flicker Speed", Range(0, 4)) = 1.4
        _Intensity ("Brightness", Range(0, 3)) = 1.4
        _White ("White Core", Range(0, 1)) = 0.45
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
            #pragma vertex SlashVertex
            #pragma fragment SlashFragment
            #pragma multi_compile_instancing
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float _Glow;
                float _Distort;
                float _Speed;
                float _Intensity;
                float _White;
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
                half4 color : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings SlashVertex(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                SetUpSpriteInstanceProperties();
                Varyings output = (Varyings)0;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                output.positionCS = TransformObjectToHClip(positionOS);
                output.uv = input.uv;
                output.color = input.color * unity_SpriteColor;
                return output;
            }

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float Noise(float2 p)
            {
                float2 c = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(c), Hash(c + float2(1, 0)), f.x),
                            lerp(Hash(c + float2(0, 1)), Hash(c + float2(1, 1)), f.x), f.y);
            }

            half Alpha(float2 uv)
            {
                float2 inside = step(0.0, uv) * step(uv, 1.0);
                return SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, saturate(uv), 0).a * inside.x * inside.y;
            }

            half4 SlashFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float t = _Time.y * _Speed;
                float2 uv = input.uv;

                // 아지랑이: 그림 자체가 일렁인다
                float2 n = float2(Noise(uv * 18.0 + float2(0.0, -t * 3.0)),
                                  Noise(uv * 18.0 + float2(5.3, -t * 2.6))) - 0.5;
                float2 duv = uv + n * _Distort;
                half a0 = Alpha(duv);

                // 바깥으로 번지는 빛
                half glow = a0 * 0.4;
                [unroll] for (int i = 0; i < 8; i++)
                {
                    float ang = i * 0.7853982 + 0.3927;
                    float2 d = float2(cos(ang), sin(ang)) * _Glow;
                    glow += Alpha(duv + d) * 0.05 + Alpha(duv + d * 2.0) * 0.025;
                }

                // 잘게 끊어지며 위로 타오르는 가장자리
                float fl = Noise(uv * 26.0 + float2(0.0, -t * 4.0)) * 0.6
                         + Noise(uv * 55.0 + float2(3.0, -t * 6.0)) * 0.4;
                half flame = glow * smoothstep(0.25, 0.75, fl + a0 * 0.5);
                half flick = 0.8 + 0.4 * Noise(float2(t * 7.0, 1.7));   // 전체가 살짝 깜빡임

                half3 col = lerp(input.color.rgb, half3(1, 1, 1), saturate(a0 * _White)) * _Intensity * flick;
                half alpha = saturate(flame * 1.8 + a0 * 0.45) * input.color.a;
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}

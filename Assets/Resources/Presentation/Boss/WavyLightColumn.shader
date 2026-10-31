Shader "ProjectLx/Wavy Light Column"
{
    // 일렁이는 빛기둥 (보스방 문 자리에서 새어 나오는 빛).
    //   가장자리가 물결치듯 흔들리고, 안쪽에 빛 자락이 위로 흐르며, 작은 빛가루가 떠오른다.
    //   uv.y 0 = 바닥 (가장 밝음) → 1 = 위 (사라짐). 색·진하기는 SpriteRenderer 색.
    // 흰 네모 스프라이트(GolemBossVisual.Square)에 쓴다.
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Speed ("Flow Speed", Range(0, 4)) = 1
        _Wave ("Edge Wave", Range(0, 0.4)) = 0.18
        _Width ("Core Width", Range(0.05, 0.6)) = 0.28
        [HideInInspector] _Color ("Tint", Color) = (1, 1, 1, 1)
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1, 1, 1, 1)
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
            #pragma vertex LightVertex
            #pragma fragment LightFragment
            #pragma multi_compile_instancing
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Speed;
                float _Wave;
                float _Width;
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

            Varyings LightVertex(Attributes input)
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

            float Fbm(float2 p)
            {
                float s = 0.0, a = 0.5;
                [unroll] for (int i = 0; i < 4; i++) { s += a * Noise(p); p = p * 2.03 + 17.7; a *= 0.5; }
                return s / 0.9375;
            }

            half4 LightFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.uv;
                float t = _Time.y * _Speed;

                // 가장자리가 물결치듯: 높이마다 가운데가 좌우로 흔들림
                float warp = (Fbm(float2(uv.y * 3.0 - t * 0.6, 1.3)) - 0.5) * _Wave * 2.0;
                float x = uv.x - 0.5 - warp * (0.4 + uv.y);
                float width = _Width * (0.8 + 0.5 * Fbm(float2(uv.y * 2.0 - t * 0.4, 7.1)));
                float column = exp(-(x * x) / (width * width));

                // 위로 흐르는 빛 자락
                float wisp = Fbm(float2(uv.x * 5.0 + warp * 3.0, uv.y * 2.5 - t * 1.1));
                wisp = smoothstep(0.35, 0.8, wisp);

                // 위로 갈수록 옅어짐, 바닥에 고인 빛
                float fade = pow(saturate(1.0 - uv.y), 1.4);
                float floorGlow = exp(-uv.y * 14.0) * exp(-(x * x) / (width * width * 4.0));
                float flick = 0.85 + 0.15 * Noise(float2(t * 3.0, 4.2));

                // 떠오르는 빛가루
                float2 sp = float2(uv.x * 10.0, uv.y * 14.0 - t * 2.2);
                float2 cell = floor(sp);
                float2 f = frac(sp) - 0.5;
                float rnd = Hash(cell);
                float spark = step(0.9, rnd) * exp2(-70.0 * dot(f, f)) * column * (1.0 - uv.y);

                float density = (column * (0.45 + 0.75 * wisp) * fade + floorGlow * 0.8) * flick + spark * 1.5;
                // 사각형 가장자리에서 0으로
                float edge = smoothstep(0.0, 0.12, uv.x) * smoothstep(0.0, 0.12, 1.0 - uv.x) * smoothstep(0.0, 0.08, 1.0 - uv.y);
                half3 col = lerp(input.color.rgb, half3(1, 1, 1), saturate(floorGlow * 0.5 + spark));
                return half4(col, saturate(density * edge) * input.color.a);
            }
            ENDHLSL
        }
    }
}

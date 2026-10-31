Shader "ProjectLx/Awakening Silhouette Aura"
{
    // 캐릭터 실루엣을 따라 빛나는 각성 아우라.
    //   가까운 빛: 몸 테두리에 바짝 붙은 밝은 빛 (가운데는 흰빛)
    //   먼 빛:     흐늘거리며 넓게 퍼지는 푸른 빛
    //   피어오름:  실루엣이 위로 번지며 불꽃/연기처럼 흩어지는 자락
    //   반짝이:    위로 떠오르는 작은 별빛
    // 캐릭터 그림 바로 뒤에 넓은 사각형 하나로 그린다. AwakeningSilhouetteAura.cs가 값을 넣어 준다.
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HDR] _AuraColor ("Aura Color", Color) = (0.3, 0.62, 1, 1)
        _Intensity ("Brightness", Float) = 2.2
        _Opacity ("Opacity", Range(0, 1)) = 0.85
        _NearRadius ("Near Glow Radius", Float) = 0.15
        _FarRadius ("Far Glow Radius", Float) = 0.45
        _Rise ("Rise Height", Float) = 1.3
        _Sparkle ("Sparkle", Range(0, 1)) = 0.6
        _White ("White Core", Range(0, 1)) = 0.35
        [HideInInspector] _SpriteUVRect ("Sprite UV Rect", Vector) = (0, 0, 1, 1)
        [HideInInspector] _SpriteRectInQuad ("Sprite Rect In Quad", Vector) = (0, 0, 1, 1)
        [HideInInspector] _QuadSize ("Quad Size", Vector) = (1, 1, 0, 0)
        [HideInInspector] _Flip ("Flip X", Float) = 0
        [HideInInspector] _AuraTime ("Animation Clock", Float) = 0
        [HideInInspector] _Flash ("Flash", Float) = 0
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
            #pragma vertex AuraVertex
            #pragma fragment AuraFragment
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _AuraColor;
                float _Intensity;
                float _Opacity;
                float _NearRadius;
                float _FarRadius;
                float _Rise;
                float _Sparkle;
                float _White;
                float4 _SpriteUVRect;
                float4 _SpriteRectInQuad;   // 사각형 안에서 캐릭터 그림 자리 (왼쪽 아래 x, y, 폭, 높이 / 월드 칸)
                float4 _QuadSize;           // 사각형 크기 (월드 칸)
                float _Flip;
                float _AuraTime;
                float _Flash;
            CBUFFER_END

            struct Attributes { float3 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings AuraVertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
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
                float2 cell = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(cell), Hash(cell + float2(1, 0)), f.x),
                    lerp(Hash(cell + float2(0, 1)), Hash(cell + float2(1, 1)), f.x), f.y);
            }

            float FlowNoise(float2 p)
            {
                return Noise(p) * 0.65 + Noise(p * 2.03 + 17.7) * 0.35;
            }

            // 캐릭터 그림의 불투명도 (q = 사각형 왼쪽 아래 기준 월드 칸)
            float Mask(float2 q)
            {
                float2 s = (q - _SpriteRectInQuad.xy) / _SpriteRectInQuad.zw;
                s.x = lerp(s.x, 1.0 - s.x, _Flip);
                float inside = step(0.0, s.x) * step(s.x, 1.0) * step(0.0, s.y) * step(s.y, 1.0);
                float2 uv = lerp(_SpriteUVRect.xy, _SpriteUVRect.zw, saturate(s));
                return SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, uv, 0).a * inside;
            }

            // 흐리게 번진 실루엣 (반지름 r 월드 칸)
            float Soft(float2 q, float r)
            {
                float sum = Mask(q) * 0.5;
                [unroll] for (int i = 0; i < 8; i++)
                {
                    float a = i * 0.7853982 + 0.3927;
                    float2 d = float2(cos(a), sin(a));
                    sum += Mask(q + d * r) + Mask(q + d * (r * 0.5)) * 0.75;
                }
                return sum / 14.5;
            }

            half4 AuraFragment(Varyings input) : SV_Target
            {
                float2 q = input.uv * _QuadSize.xy;
                float t = _AuraTime;

                // 몸에 바짝 붙은 빛
                float nearGlow = Soft(q, _NearRadius);

                // 넓게 일렁이는 빛
                float2 warp = float2(FlowNoise(q * 1.7 + float2(0.0, -t * 1.1)) - 0.5,
                                     FlowNoise(q * 1.3 + float2(5.2, -t * 0.8)) - 0.5) * 0.1;
                float farGlow = Soft(q + warp, _FarRadius);

                // 위로 피어오르는 자락: 아래쪽 실루엣을 끌어올려 번지게 한다
                float wisp = 0.0;
                [unroll] for (int k = 1; k <= 4; k++)
                {
                    float h = k / 4.0;
                    float sway = (Noise(float2(q.y * 1.3 - t * 0.9, k * 3.1)) - 0.5) * 0.32 * h;
                    wisp += Soft(q - float2(sway, h * _Rise), _FarRadius * (0.6 + h)) * (1.0 - h * 0.7);
                }
                wisp *= 0.5;
                float flame = FlowNoise(float2(q.x * 2.2, q.y * 1.4 - t * 1.6));
                float flameFine = Noise(float2(q.x * 5.0 + 7.0, q.y * 3.0 - t * 2.4));
                wisp *= smoothstep(0.34, 0.74, flame * 0.7 + flameFine * 0.3) * 2.6;

                // 떠오르는 반짝이
                float2 sp = float2(q.x * 7.0, q.y * 7.0 - t * 1.5);
                float2 cell = floor(sp);
                float2 f = frac(sp) - 0.5;
                float rnd = Hash(cell);
                float spark = step(0.93, rnd) * exp2(-80.0 * dot(f, f))
                    * (0.5 + 0.5 * sin(t * 6.0 + rnd * 40.0));
                spark *= saturate(farGlow * 3.0 + wisp) * _Sparkle;

                float density = nearGlow * 1.2 + farGlow * 0.45 + wisp * 0.55;
                // 이글거림: 빛 세기가 자리마다 빠르게 일렁인다
                float flick = 0.8 + 0.4 * Noise(float2(q.x * 3.5, q.y * 3.0 - t * 3.2));
                density *= flick;
                density *= 1.0 + _Flash * 0.6;

                // 사각형 가장자리에서는 0으로
                float edge = smoothstep(0.0, 0.1, input.uv.x) * (1.0 - smoothstep(0.9, 1.0, input.uv.x))
                    * smoothstep(0.0, 0.04, input.uv.y) * (1.0 - smoothstep(0.65, 1.0, input.uv.y));

                // 몸 테두리에만 살짝 흰빛, 나머지는 푸른빛
                float core = smoothstep(0.3, 0.6, nearGlow);
                half3 col = lerp(_AuraColor.rgb, half3(1.0, 1.0, 1.0), core * _White) * _Intensity;
                col += _AuraColor.rgb * spark * 2.0;   // 반짝이도 같은 파랑
                float alpha = saturate((density + spark * 2.0) * edge) * _Opacity;
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}

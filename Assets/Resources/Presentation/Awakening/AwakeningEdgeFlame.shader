Shader "ProjectLx/UI Awakening Edge Flame"
{
    // 각성 중 화면 가장자리에서 일렁이는 푸른 기운.
    // 부드러운 연기 덩어리가 가장자리를 따라 천천히 흐르고, 흐릿한 빛망울이 떠오른다 (일자 띠 아님, 흰빛 없음).
    // 화면 전체를 덮는 UI Image에 쓴다. AwakeningEdgeFlame.cs가 기존 가장자리 이미지의 머티리얼을 이걸로 바꾼다.
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _FlameColor ("Flame Color", Color) = (0.2, 0.5, 1, 1)
        _Thickness ("Thickness (screen height)", Range(0.02, 0.4)) = 0.075
        _Wave ("Wave", Range(0, 0.2)) = 0.08
        _Intensity ("Brightness", Range(0, 2)) = 1
        _Opacity ("Opacity", Range(0, 1)) = 0.4
        _Speed ("Flow Speed", Range(0, 4)) = 1
        _White ("Bokeh Lightness", Range(0, 1)) = 0.6

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };

            fixed4 _Color;
            fixed4 _FlameColor;
            float _Thickness, _Wave, _Intensity, _Opacity, _Speed, _White;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color;
                o.uv = v.uv;
                return o;
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
                for (int i = 0; i < 4; i++) { s += a * Noise(p); p = p * 2.03 + 17.7; a *= 0.5; }
                return s / 0.9375;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float aspect = _ScreenParams.x / max(_ScreenParams.y, 1.0);
                float2 p = float2(i.uv.x * aspect, i.uv.y);
                float t = _Time.y * _Speed;

                // 천천히 크게 휘는 흐름 (연기가 피어오르듯)
                float2 w = float2(Fbm(p * 1.2 + float2(t * 0.12, -t * 0.18)),
                                  Fbm(p * 1.2 + float2(-t * 0.15, t * 0.10) + 9.2)) - 0.5;
                float2 q = p + w * _Wave;

                // 가장 가까운 가장자리까지 거리 (화면 높이 = 1)
                float d = min(min(q.x, aspect - q.x), min(q.y, 1.0 - q.y));

                // 부드러운 연기 덩어리: 진한 곳과 옅은 곳이 섞여 천천히 흐른다
                float smoke = Fbm(q * 2.2 + w * 1.5 + float2(t * 0.05, -t * 0.12));
                float smoke2 = Fbm(q * 4.5 - w + float2(-t * 0.08, -t * 0.20));
                float reach = _Thickness * (0.6 + 0.9 * smoke);
                float u = saturate(d / max(reach, 0.001));
                float body = (1.0 - u) * (1.0 - u);
                float density = body * (0.45 + 0.75 * smoothstep(0.25, 0.8, smoke * 0.6 + smoke2 * 0.4));

                // 흐릿하게 빛나는 빛망울 (천천히 떠오르며 깜빡임)
                float2 bp = float2(p.x * 9.0, p.y * 9.0 - t * 0.35);
                float2 cell = floor(bp);
                float rnd = Hash(cell);
                float2 c = float2(Hash(cell + 3.1), Hash(cell + 7.7)) * 0.5 + 0.25;
                float2 f = frac(bp) - c;
                float sr = Hash(cell + 1.3);
                float size = lerp(10.0, 220.0, sr * sr * (3.0 - 2.0 * sr));   // 큰 망울부터 아주 작은 점까지 크기가 제각각
                float bokeh = step(0.78, rnd) * lerp(0.55, 1.0, sr) * exp2(-size * dot(f, f))
                    * (0.55 + 0.45 * sin(t * 1.7 + rnd * 40.0))
                    * (1.0 - smoothstep(0.0, _Thickness * 2.5, d));

                float3 col = _FlameColor.rgb * _Intensity * (0.8 + 0.4 * smoke2);
                col = lerp(col, float3(0.65, 0.82, 1.0), saturate(bokeh) * _White);
                float alpha = saturate(density + bokeh * 0.7) * _Opacity * i.color.a;
                return fixed4(col * i.color.rgb, alpha);
            }
            ENDCG
        }
    }
}

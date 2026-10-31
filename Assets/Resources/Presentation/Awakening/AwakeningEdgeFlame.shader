Shader "ProjectLx/UI Awakening Edge Flame"
{
    // 각성 중 화면 가장자리에서 일렁이는 푸른 불꽃.
    // 가장자리를 따라 길이가 제각각인 파란 불꽃이 물결치듯 일렁이며 위로 흐른다 (일자 띠가 아님, 흰빛 없음).
    // 화면 전체를 덮는 UI Image에 쓴다. AwakeningEdgeFlame.cs가 기존 가장자리 이미지의 머티리얼을 이걸로 바꾼다.
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _FlameColor ("Flame Color", Color) = (0.2, 0.5, 1, 1)
        _Thickness ("Thickness (screen height)", Range(0.02, 0.4)) = 0.09
        _Wave ("Wave", Range(0, 0.2)) = 0.06
        _Intensity ("Brightness", Range(0, 2)) = 1
        _Opacity ("Opacity", Range(0, 1)) = 0.85
        _Speed ("Flow Speed", Range(0, 4)) = 1
        _White ("Spark Lightness", Range(0, 1)) = 0.5

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

                // 일렁임: 화면 좌표를 천천히 크게 휘게 해서 불꽃 경계가 물결치듯 움직인다
                float2 w = float2(Fbm(p * 1.6 + float2(t * 0.35, -t * 0.5)),
                                  Fbm(p * 1.6 + float2(-t * 0.4, t * 0.3) + 9.2)) - 0.5;
                float2 q = p + w * _Wave;

                // 가장 가까운 가장자리까지 거리 (화면 높이 = 1)
                float d = min(min(q.x, aspect - q.x), min(q.y, 1.0 - q.y));

                // 불꽃 길이는 자리마다 다르고 위로 흐른다. 아래쪽 가장자리가 조금 더 두껍다
                float n = Fbm(float2(q.x * 2.5, q.y * 2.0 - t * 0.6) + w * 2.0);
                float reach = _Thickness * lerp(1.15, 0.85, i.uv.y) * (0.55 + 0.8 * n);
                float u = saturate(d / max(reach, 0.001));
                float body = pow(1.0 - u, 1.6);
                float tongues = smoothstep(0.3, 0.7, Fbm(float2(q.x * 6.0, q.y * 4.5 - t * 1.1)) + (1.0 - u) * 0.6);
                float flame = body * lerp(0.55, 1.0, tongues);

                // 떠오르는 작은 불티 (연한 파랑)
                float2 sp = float2(p.x * 26.0, p.y * 26.0 - t * 3.0);
                float2 cell = floor(sp);
                float2 f = frac(sp) - 0.5;
                float rnd = Hash(cell);
                float spark = step(0.97, rnd) * exp2(-60.0 * dot(f, f))
                    * (1.0 - smoothstep(0.0, _Thickness * 1.8, d)) * 0.6;

                // 흰빛 없이 파란색만. 불꽃 혀가 짙은 곳만 조금 더 밝게
                float3 col = _FlameColor.rgb * _Intensity * (0.85 + 0.25 * tongues);
                col = lerp(col, float3(0.6, 0.82, 1.0), saturate(spark * 1.5) * _White);
                float alpha = saturate(flame + spark) * _Opacity * i.color.a;
                return fixed4(col * i.color.rgb, alpha);
            }
            ENDCG
        }
    }
}

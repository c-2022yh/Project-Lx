Shader "ProjectLx/UI Awakening Edge Flame"
{
    // 각성 중 화면 가장자리에서 일렁이는 푸른 불꽃.
    // 가장자리를 따라 길이가 제각각인 불꽃 혀가 안쪽으로 날름거리며 위로 흐른다 (일자 띠가 아님).
    // 화면 전체를 덮는 UI Image에 쓴다. AwakeningEdgeFlame.cs가 기존 가장자리 이미지의 머티리얼을 이걸로 바꾼다.
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _FlameColor ("Flame Color", Color) = (0.3, 0.62, 1, 1)
        _Thickness ("Thickness (screen height)", Range(0.02, 0.4)) = 0.13
        _Intensity ("Brightness", Range(0, 3)) = 1.3
        _Opacity ("Opacity", Range(0, 1)) = 0.85
        _Speed ("Flow Speed", Range(0, 4)) = 1
        _White ("White Core", Range(0, 1)) = 0.45

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
        Blend SrcAlpha One
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
            float _Thickness, _Intensity, _Opacity, _Speed, _White;

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

                // 가장 가까운 가장자리까지 거리 (화면 높이 = 1)
                float d = min(min(p.x, aspect - p.x), min(p.y, 1.0 - p.y));

                // 불꽃은 위로 흐르고, 아래쪽 가장자리일수록 두껍다
                float2 flow = float2(p.x * 3.0, p.y * 2.2 - t * 0.9);
                float n = Fbm(flow);
                float nFine = Fbm(float2(p.x * 9.0 + 3.1, p.y * 6.0 - t * 1.7));
                float reach = _Thickness * lerp(1.3, 0.75, i.uv.y) * (0.3 + 1.1 * n);

                // 혀 모양: 가장자리에서 멀어질수록 잘게 끊어진다
                float u = saturate(d / max(reach, 0.001));
                float body = 1.0 - smoothstep(0.0, 1.0, u);
                float tongues = smoothstep(0.35, 0.75, nFine + (1.0 - u) * 0.55);
                float flame = body * tongues;
                flame = max(flame, (1.0 - smoothstep(0.0, _Thickness * 0.25, d)) * 0.6);   // 맨 가장자리는 늘 은은하게

                // 가장자리 가까이 흰 심지
                float core = (1.0 - smoothstep(0.0, reach * 0.4, d)) * tongues;

                // 떠오르는 불티
                float2 sp = float2(p.x * 26.0, p.y * 26.0 - t * 4.0);
                float2 cell = floor(sp);
                float2 f = frac(sp) - 0.5;
                float rnd = Hash(cell);
                float spark = step(0.965, rnd) * exp2(-60.0 * dot(f, f))
                    * (1.0 - smoothstep(0.0, _Thickness * 2.0, d));

                float3 col = lerp(_FlameColor.rgb, float3(1, 1, 1), saturate(core * _White)) * _Intensity;
                col += spark * 1.5;
                float alpha = saturate(flame + spark) * _Opacity * i.color.a;
                return fixed4(col * i.color.rgb, alpha);
            }
            ENDCG
        }
    }
}

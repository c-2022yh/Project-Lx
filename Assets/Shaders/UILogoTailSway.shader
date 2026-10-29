Shader "UI/LogoTailSway"
{
    // 타이틀 로고의 여우 꼬리만 살랑살랑 흔드는 UI 셰이더 (10.29).
    //
    // 로고 그림 한 장을 그대로 쓰고, 꼬리 부분만 뿌리(_Root)를 중심으로 조금씩 돌려서 다시 그린다.
    // 뿌리에서 멀수록 더 크게, 조금 늦게 움직여서 채찍처럼 휘는 느낌이 난다.
    //   - 로고: 여우 모양 그림(_TailMask)으로 꼬리 픽셀만 골라 움직인다 (글자는 그대로).
    //   - 로고 뒤 빛: 오른쪽(_TailStart보다 오른쪽)을 통째로 같이 움직인다 (_UseMask = 0).
    // 좌표는 모두 "로고 그림 기준 UV"로 계산한다. _MapST로 이 그래픽의 UV → 로고 UV를 바꾼다.
    // _SwayTime은 TitleLogoLight가 실제 시간으로 넣어 준다.

    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _TailMask ("Fox Mask (A)", 2D) = "white" {}
        _MapST ("UV → Logo UV (xy scale, zw offset)", Vector) = (1, 1, 0, 0)
        _Root ("Tail Root (logo UV)", Vector) = (0.74, 0.11, 0, 0)
        _Aspect ("Logo Width / Height", Float) = 2.545
        _TailStart ("Tail Start X (logo UV)", Float) = 0.70
        _Reach ("Tail Length (logo heights)", Float) = 0.95
        _Amp ("Sway (radians at tip)", Float) = 0.07
        _Speed ("Speed", Float) = 1.4
        _Wave ("Delay along tail", Float) = 2.0
        _UseMask ("Use Fox Mask", Float) = 1
        _SwayTime ("Time", Float) = 0

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

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
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _TailMask;
            fixed4 _Color;
            float4 _ClipRect;
            float4 _MapST;
            float4 _Root;
            float _Aspect, _TailStart, _Reach, _Amp, _Speed, _Wave, _UseMask, _SwayTime;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPos = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            float Inside(float2 uv)
            {
                return step(0.0, uv.x) * step(uv.x, 1.0) * step(0.0, uv.y) * step(uv.y, 1.0);
            }

            // 이 로고 UV 자리가 꼬리인 정도 (0~1)
            float TailWeight(float2 L)
            {
                float zone = smoothstep(_TailStart - 0.03, _TailStart + 0.03, L.x);
                if (_UseMask > 0.5)
                {
                    float m = tex2D(_TailMask, saturate(L)).a * Inside(L);
                    return zone * m;
                }
                return zone;
            }

            // 화면의 L 자리에 보여야 하는 꼬리가 원래 그림의 어디에 있는지 (반대로 돌려서 찾는다)
            float2 SwaySource(float2 L)
            {
                float2 p = (L - _Root.xy) * float2(_Aspect, 1.0);
                float d = saturate(length(p) / max(_Reach, 0.001));
                float spread = atan2(p.y, p.x) * 0.8;   // 꼬리 갈래마다 박자가 조금씩 다르게
                float a = _Amp * d * sin(_SwayTime * _Speed - d * _Wave + spread);
                float s = sin(-a), c = cos(-a);
                p = float2(c * p.x - s * p.y, s * p.x + c * p.y);
                return _Root.xy + p / float2(_Aspect, 1.0);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 L = i.texcoord * _MapST.xy + _MapST.zw;
                float2 src = SwaySource(L);
                float2 srcUV = (src - _MapST.zw) / _MapST.xy;

                // 제자리 그림(꼬리가 아닌 부분) + 움직인 꼬리
                fixed4 still = tex2D(_MainTex, i.texcoord);
                still.a *= 1.0 - TailWeight(L);

                fixed4 moved = tex2D(_MainTex, srcUV);
                moved.a *= TailWeight(src) * Inside(srcUV);

                fixed4 color;
                color.a = moved.a + still.a * (1.0 - moved.a);
                color.rgb = (moved.rgb * moved.a + still.rgb * still.a * (1.0 - moved.a)) / max(color.a, 0.0001);
                color *= i.color;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(i.worldPos.xy, _ClipRect);
                #endif

                return color;
            }
            ENDCG
        }
    }
}

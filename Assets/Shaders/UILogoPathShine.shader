Shader "UI/LogoPathShine"
{
    // 타이틀 로고의 여우 모양을 따라 빛이 흘러가게 하는 UI 셰이더.
    //
    // 그림(Title_Logo_C_FoxPath)의 R 값이 "여우 몸을 따라 얼마나 왔는지(0~1)"이고,
    // A 값이 여우 모양이다. 머리 0 → 앞다리 → 아래로 휘는 선 → 꼬리 1 순서로 커진다.
    // _Progress 근처(±_Width)인 곳만 밝게 그린다. TitleLogoLight가 _Progress를 0→1로 움직인다.
    // 그림은 sRGB를 끄고 들여와야 R 값이 그대로 읽힌다.
    // 10.29: 꼬리가 흔들리면(UI/LogoTailSway) 빛도 같이 흔들리도록 같은 계산을 넣었다. _Amp = 0이면 예전과 같다.

    Properties
    {
        [PerRendererData] _MainTex ("Path Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _Progress ("Progress", Float) = -1
        _Width ("Band Width", Range(0.01, 0.3)) = 0.07
        _Intensity ("Intensity", Range(0, 2)) = 1

        _MapST ("UV → Logo UV", Vector) = (1, 1, 0, 0)
        _Root ("Tail Root (logo UV)", Vector) = (0.74, 0.11, 0, 0)
        _Aspect ("Logo Width / Height", Float) = 2.545
        _TailStart ("Tail Start X", Float) = 0.70
        _Reach ("Tail Length", Float) = 0.95
        _Amp ("Sway (0 = off)", Float) = 0
        _Speed ("Speed", Float) = 1.4
        _Wave ("Delay along tail", Float) = 2.0
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
        Blend SrcAlpha One   // 더하기: 로고 위에 빛이 얹힌다
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
            fixed4 _Color;
            float4 _ClipRect;
            float _Progress;
            float _Width;
            float _Intensity;
            float4 _MapST;
            float4 _Root;
            float _Aspect, _TailStart, _Reach, _Amp, _Speed, _Wave, _SwayTime;

            // UI/LogoTailSway와 같은 계산: 화면의 L 자리에 보여야 할 꼬리가 원래 그림의 어디인지
            float2 SwaySource(float2 L)
            {
                float2 p = (L - _Root.xy) * float2(_Aspect, 1.0);
                float d = saturate(length(p) / max(_Reach, 0.001));
                float spread = atan2(p.y, p.x) * 0.8;
                float a = _Amp * d * sin(_SwayTime * _Speed - d * _Wave + spread);
                float s = sin(-a), c = cos(-a);
                p = float2(c * p.x - s * p.y, s * p.x + c * p.y);
                return _Root.xy + p / float2(_Aspect, 1.0);
            }

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

            fixed4 frag(v2f i) : SV_Target
            {
                float2 L = i.texcoord * _MapST.xy + _MapST.zw;
                float2 srcUV = (SwaySource(L) - _MapST.zw) / _MapST.xy;
                float zone = smoothstep(_TailStart - 0.03, _TailStart + 0.03, L.x);
                fixed4 path = lerp(tex2D(_MainTex, i.texcoord), tex2D(_MainTex, srcUV), zone);

                // 빛 띠: 가운데가 가장 밝고 양옆으로 부드럽게 사라진다
                float d = abs(path.r - _Progress) / max(_Width, 0.0001);
                float band = saturate(1.0 - d);
                band = band * band * (3.0 - 2.0 * band);

                fixed4 color = i.color;
                color.a *= band * path.a * _Intensity;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(i.worldPos.xy, _ClipRect);
                #endif

                return color;
            }
            ENDCG
        }
    }
}

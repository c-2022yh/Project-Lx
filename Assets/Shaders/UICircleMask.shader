Shader "UI/CircleMask"
{
    // 유물 아이콘을 원으로 잘라내는 UI 셰이더.
    //
    // 유니티 기본 Mask 컴포넌트는 스텐실 버퍼라 픽셀 단위로 딱 자른다.
    // 그래서 원 가장자리가 계단처럼 보인다. 여기서는 경계에서 알파를 부드럽게
    // 떨어뜨려 매끈하게 만든다.
    //
    // 원의 중심과 반지름은 스프라이트 UV가 아니라 TEXCOORD1로 받는다.
    // 아이콘 스프라이트가 텍스처 전체를 덮지 않는 경우가 있어서(가장자리 여백을
    // 잘라낸 채로 임포트되면 UV가 0~1이 아니다) UV로 계산하면 원이 치우친다.
    // 그 좌표는 CircleMaskedImage가 칸 기준 0~1로 채워 넣는다.

    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)

        // 아이콘 네모가 칸보다 얼마나 큰지에 맞춘 값.
        // 아이콘을 1.45배로 키워 넣으므로 칸의 원은 0.5 / 1.45 = 0.345다.
        _Radius ("Circle Radius", Range(0.05, 0.5)) = 0.345
        _Softness ("Edge Softness", Range(0.0005, 0.05)) = 0.004

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
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 maskuv   : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 maskuv   : TEXCOORD1;
                float4 worldPos : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float _Radius;
            float _Softness;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.worldPos = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.maskuv = v.maskuv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                half4 color = (tex2D(_MainTex, i.texcoord) + _TextureSampleAdd) * i.color;

                // 칸 한가운데에서 얼마나 떨어졌는지. 경계에서만 부드럽게 지운다.
                float dist = distance(i.maskuv, float2(0.5, 0.5));
                color.a *= 1.0 - smoothstep(_Radius - _Softness, _Radius + _Softness, dist);

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(i.worldPos.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}

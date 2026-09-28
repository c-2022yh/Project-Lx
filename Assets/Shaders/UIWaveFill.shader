Shader "UI/WaveFill"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)

        _FillAmount ("Fill Amount", Range(0, 1)) = 0
        _FillBottom ("Fill Bottom", Range(0, 1)) = 0.15
        _FillTop ("Fill Top", Range(0, 1)) = 0.85
        _CircleAreaCorrection ("Circle Area Correction", Range(0, 1)) = 0

        _WaveAmplitude ("Wave Amplitude", Range(0, 0.1)) = 0.015
        _WaveFrequency ("Wave Frequency", Range(0.1, 10)) = 2
        _WaveSpeed ("Wave Speed", Range(-5, 5)) = 0.6
        _WaveSoftness ("Wave Softness", Range(0.0001, 0.05)) = 0.003

        _WaveHighlightColor ("Wave Highlight Color", Color) = (0.85, 0.95, 1, 0)
        _WaveHighlightWidth ("Wave Highlight Width", Range(0, 0.05)) = 0.006

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [HideInInspector] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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
            Name "WaveFill"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            float _FillAmount;
            float _FillBottom;
            float _FillTop;
            float _CircleAreaCorrection;

            float _WaveAmplitude;
            float _WaveFrequency;
            float _WaveSpeed;
            float _WaveSoftness;

            fixed4 _WaveHighlightColor;
            float _WaveHighlightWidth;

            v2f vert(appdata_t input)
            {
                v2f output;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.worldPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = TRANSFORM_TEX(input.texcoord, _MainTex);
                output.color = input.color * _Color;

                return output;
            }

            //원에서 보이는 면적이 기력 비율과 비슷해지도록 높이를 역산
            float GetCircleAreaHeight(float areaAmount)
            {
                areaAmount = saturate(areaAmount);

                if (areaAmount <= 0.0001) return 0;
                if (areaAmount >= 0.9999) return 1;

                float height = areaAmount;

                //Newton 반복으로 원형 구간의 면적 비율에 해당하는 높이를 구함
                [unroll]
                for (int index = 0; index < 4; index++)
                {
                    float circleY = 1 - 2 * height;
                    float root = sqrt(max(1 - circleY * circleY, 0.00001));

                    float currentArea = (
                        acos(clamp(circleY, -1, 1))
                        - circleY * root
                    ) / UNITY_PI;

                    float derivative = 4 * root / UNITY_PI;

                    height = saturate(
                        height
                        - (currentArea - areaAmount) / max(derivative, 0.0001)
                    );
                }

                return height;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                const float TWO_PI = 6.2831853;

                fixed4 color = (tex2D(_MainTex, input.texcoord) + _TextureSampleAdd)
                    * input.color;

                float fillAmount = saturate(_FillAmount);

                //기본값 0이면 높이를 등간격으로 올리고,
                //1이면 원 안에서 보이는 면적이 기력 비율과 비슷하게 보정됨
                float circleAreaHeight = GetCircleAreaHeight(fillAmount);
                float correctedFillAmount = lerp(
                    fillAmount,
                    circleAreaHeight,
                    saturate(_CircleAreaCorrection)
                );

                //PNG 전체가 아니라 실제 원형 보주가 있는 세로 범위만 사용
                float fillBottom = min(_FillBottom, _FillTop);
                float fillTop = max(_FillBottom, _FillTop);
                float fillRange = max(fillTop - fillBottom, 0.0001);
                float fillHeight = lerp(
                    fillBottom,
                    fillTop,
                    correctedFillAmount
                );

                //0%와 100% 근처에서는 파도 높이를 줄여 빈 픽셀이나 잘림이 생기지 않게 함
                float endpointFade = smoothstep(0, 0.05, fillAmount)
                    * smoothstep(0, 0.05, 1 - fillAmount);

                float primaryWave = sin(
                    (input.texcoord.x * _WaveFrequency
                    + _Time.y * _WaveSpeed) * TWO_PI
                );

                float secondaryWave = sin(
                    (input.texcoord.x * _WaveFrequency * 1.73
                    - _Time.y * _WaveSpeed * 0.63
                    + 0.37) * TWO_PI
                );

                float combinedWave = (primaryWave + secondaryWave * 0.35) / 1.35;

                float waveHeight = saturate(
                    fillHeight
                    + combinedWave
                    * _WaveAmplitude
                    * fillRange
                    * endpointFade
                );

                float softness = max(_WaveSoftness, 0.0001);

                //물결 아래에서는 100% 이미지를 표시하고 위에서는 투명하게 만듦
                float fillMask = 1 - smoothstep(
                    waveHeight - softness,
                    waveHeight + softness,
                    input.texcoord.y
                );

                //정확히 0%일 때 완전 투명, 100%일 때 완전 표시
                fillMask = fillAmount <= 0.0001 ? 0 : fillMask;
                fillMask = fillAmount >= 0.9999 ? 1 : fillMask;

                //필요할 때만 Material의 Highlight Color 알파를 올려 얇은 반사선을 추가
                float highlightMask = 1 - smoothstep(
                    _WaveHighlightWidth,
                    _WaveHighlightWidth + softness,
                    abs(input.texcoord.y - waveHeight)
                );

                highlightMask *= endpointFade * fillMask;

                color.rgb = lerp(
                    color.rgb,
                    _WaveHighlightColor.rgb,
                    highlightMask * _WaveHighlightColor.a
                );

                color.a *= fillMask;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
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

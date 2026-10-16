Shader "UI/OrbCrescentFill"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _FillAmount ("Energy Progress", Range(0, 1)) = 0
        _FillRect ("Image Local Rect", Vector) = (-0.5, -0.5, 1, 1)
        _CircleRadius ("Circle Radius", Range(0.01, 0.5)) = 0.38
        _CrescentWidth ("Maximum Crescent Width", Range(0.01, 0.95)) = 0.3
        _EdgeSoftness ("Edge Softness", Range(0.0001, 0.02)) = 0.002
        _EdgeColor ("Inner Rim Highlight", Color) = (0.9, 0.8, 1, 0.35)
        _EdgeWidth ("Highlight Width", Range(0, 0.05)) = 0.006
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
                float2 fillUV : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float4 _FillRect;
            float _FillAmount;
            float _CircleRadius;
            float _CrescentWidth;
            float _EdgeSoftness;
            fixed4 _EdgeColor;
            float _EdgeWidth;

            v2f vert(appdata_t input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.worldPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = TRANSFORM_TEX(input.texcoord, _MainTex);
                // Shape coordinates are independent of sprite atlas texture UVs.
                output.fillUV = (input.vertex.xy - _FillRect.xy) / max(_FillRect.zw, float2(0.0001, 0.0001));
                output.color = input.color * _Color;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 color = (tex2D(_MainTex, input.texcoord) + _TextureSampleAdd) * input.color;
                float amount = saturate(_FillAmount);
                float radius = clamp(_CircleRadius, 0.01, 0.5);
                float softness = max(_EdgeSoftness, 0.0001);
                float2 circlePosition = input.fillUV - float2(0.5, 0.5);
                float outerDistance = length(circlePosition);
                // Move the subtracting circle rightward: reveal a crescent from the left.
                float offset = 2 * radius * saturate(_CrescentWidth) * amount;
                float innerDistance = length(circlePosition - float2(offset, 0));
                float outerMask = 1 - smoothstep(radius - softness, radius + softness, outerDistance);
                float innerMask = 1 - smoothstep(radius - softness, radius + softness, innerDistance);
                float crescent = outerMask * (1 - innerMask);
                crescent = amount <= 0.0001 ? 0 : crescent;
                float rim = 1 - smoothstep(_EdgeWidth, _EdgeWidth + softness, abs(innerDistance - radius));
                color.rgb = lerp(color.rgb, _EdgeColor.rgb, rim * _EdgeColor.a);
                color.a *= crescent;
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

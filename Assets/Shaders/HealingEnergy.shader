Shader "ProjectLx/Healing Energy"
{
    Properties
    {
        [HDR] _Tint ("Tint", Color) = (0.3, 2, 0.65, 1)
        _Opacity ("Opacity", Range(0, 1)) = 1
        _Shape ("Shape: Ribbon / Light / Leaf / Plus / Core / Sparkle", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha One
        Cull Off
        ZWrite Off
        ColorMask RGB
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                float _Opacity;
                float _Shape;
            CBUFFER_END
            struct Attributes { float3 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.uv * 2.0 - 1.0;
                half mask;
                if (_Shape < 0.5)
                    mask = pow(saturate(1.0 - abs(p.y)), 1.4) * smoothstep(0.0, 0.08, input.uv.x) * (1.0 - smoothstep(0.92, 1.0, input.uv.x));
                else if (_Shape < 1.5)
                    mask = pow(saturate(1.0 - length(p)), 1.8);
                else if (_Shape < 2.5)
                {
                    float2 leaf = float2(p.x + p.y * 0.35, p.y);
                    float width = 0.45 * saturate(1.0 - leaf.y * leaf.y);
                    mask = (1.0 - smoothstep(-0.03, 0.03, abs(leaf.x) - width)) * (1.0 - smoothstep(0.85, 1.0, abs(leaf.y)));
                }
                else if (_Shape < 3.5)
                {
                    float d = min(max(abs(p.x) - 0.23, abs(p.y) - 0.78), max(abs(p.x) - 0.78, abs(p.y) - 0.23));
                    mask = 1.0 - smoothstep(-0.035, 0.035, d);
                }
                else if (_Shape < 4.5)
                    mask = pow(saturate(1.0 - length(p)), 2.2);
                else
                {
                    float cross = min(abs(p.x), abs(p.y));
                    float extent = max(abs(p.x), abs(p.y));
                    mask = pow(saturate(1.0 - cross * 9.0), 2.0) * pow(saturate(1.0 - extent), 1.5);
                }
                half3 tint = _Tint.rgb * input.color.rgb;
                if (_Shape > 3.5)
                    tint = lerp(tint, max(tint, half3(1.6, 1.8, 1.7)), pow(saturate(1.0 - length(p) * 2.0), 2.0));
                return half4(tint, mask * _Tint.a * input.color.a * _Opacity);
            }
            ENDHLSL
        }
    }
}

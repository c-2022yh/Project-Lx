Shader "ProjectLx/ShrineInnerLight"
{
    Properties
    {
        [HDR] _GlowColor ("Glow Color", Color) = (1.8, 1.15, 0.35, 1)
        _Brightness ("Brightness", Float) = 1
        _Visibility ("Visibility", Float) = 0
        _Sparkles ("Sparkles", Vector) = (1, 1, 0.65, 0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
                float4 _GlowColor;
                float4 _Sparkles;
                float _Brightness;
                float _Visibility;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }
            float Hash(float n) { return frac(sin(n * 127.1) * 43758.5453); }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.uv * 2 - 1;
                // Soft center with a weaker halo touching the columns and lower sill.
                float core = exp(-dot(p * float2(2.5, 1.7), p * float2(2.5, 1.7))) * 0.55;
                float halo = exp(-dot(p * 1.55, p * 1.55)) * 0.12;
                float boundary = (1 - smoothstep(0.73, 1, abs(p.x))) * (1 - smoothstep(0.73, 1, abs(p.y)));
                float sparks = 0;
                [unroll] for (int i = 0; i < 7; i++)
                {
                    float seed = i + 1;
                    float cycle = _Sparkles.w * _Sparkles.z * (0.5 + Hash(seed)) + Hash(seed + 12) * 7;
                    float generation = floor(cycle);
                    float age = frac(cycle);
                    float2 sparklePosition = float2((Hash(seed + generation * 17) - 0.5) * 0.95,
                        (Hash(seed + generation * 23 + 41) - 0.5) * 0.95 + age * 0.12);
                    float2 d = abs(p - sparklePosition) / max(0.3, _Sparkles.y);
                    float star = exp(-dot(d, d) * 2400)
                        + exp(-d.x * 180 - d.y * 32) * 0.3
                        + exp(-d.y * 180 - d.x * 32) * 0.3;
                    sparks += star * pow(sin(age * PI), 6);
                }
                float glow = (core + halo) * _Brightness;
                float3 color = _GlowColor.rgb * glow + lerp(_GlowColor.rgb, float3(2, 1.9, 1.4), 0.6) * sparks * _Sparkles.x;
                return half4(color, boundary * _Visibility);
            }
            ENDHLSL
        }
    }
}

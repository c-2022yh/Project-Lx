Shader "ProjectLx/Player Awakening Aura"
{
    Properties
    {
        [HDR] _AuraColor ("Aura Color", Color) = (0.15, 0.45, 1, 1)
        _AuraIntensity ("Brightness", Float) = 2.5
        _AuraOpacity ("Opacity", Range(0, 1)) = 0.4
        _RiseSpeed ("Rise Speed", Float) = 0.65
        _NoiseScale ("Noise Scale", Range(1, 12)) = 5
        _Turbulence ("Turbulence", Range(0, 1)) = 0.65
        [HideInInspector] _AuraTime ("Animation Clock", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha One
        ColorMask RGB
        Cull Off
        ZWrite Off
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            HLSLPROGRAM
            #pragma vertex AuraVertex
            #pragma fragment AuraFragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _AuraColor;
                float _AuraIntensity;
                float _AuraOpacity;
                float _RiseSpeed;
                float _NoiseScale;
                float _Turbulence;
                float _AuraTime;
            CBUFFER_END

            struct Attributes { float3 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings AuraVertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                return output;
            }

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float Noise(float2 p)
            {
                float2 cell = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(cell), Hash(cell + float2(1, 0)), f.x),
                    lerp(Hash(cell + float2(0, 1)), Hash(cell + float2(1, 1)), f.x), f.y);
            }

            float FlowNoise(float2 p)
            {
                return Noise(p) * 0.65 + Noise(p * 2.03 + 17.7) * 0.35;
            }

            half4 AuraFragment(Varyings input) : SV_Target
            {
                float x = (input.uv.x - 0.5) * 2.0;
                float y = input.uv.y;
                float t = _AuraTime * _RiseSpeed;
                // Subtracting time from Y moves noise features upward.
                float2 flowUV = float2(x * _NoiseScale, y * _NoiseScale - t);
                float flow = FlowNoise(flowUV);
                float warpedX = x + (flow - 0.5) * _Turbulence * 0.45
                    + sin(y * 8.0 - t * 1.7) * _Turbulence * 0.08;
                float width = lerp(0.98, 0.42, y * y);
                float sides = 1.0 - smoothstep(width * 0.5, width, abs(warpedX));
                float vertical = smoothstep(0.0, 0.13, y) * (1.0 - smoothstep(0.5, 1.0, y));
                // Two loose streams break into wisps, with less light behind the face.
                float lane = abs(abs(warpedX) - lerp(0.45, 0.2, y));
                float stream = exp2(-85.0 * lane * lane);
                float wisps = smoothstep(0.4, 0.72, flow);
                float hollow = lerp(0.25, 1.0, smoothstep(0.1, 0.65, abs(x)));
                float density = (0.06 + wisps * 0.55 + stream * wisps * 0.75)
                    * sides * vertical * hollow;
                // Fade the quad borders exactly to zero even when noise distorts them.
                float edge = smoothstep(0.0, 0.07, input.uv.x)
                    * (1.0 - smoothstep(0.93, 1.0, input.uv.x));
                return half4(_AuraColor.rgb * _AuraIntensity, saturate(density * edge) * _AuraOpacity);
            }
            ENDHLSL
        }
    }
}

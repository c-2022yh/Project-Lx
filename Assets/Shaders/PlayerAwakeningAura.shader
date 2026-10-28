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
        _FanAngle ("Fan Angle (Degrees)", Range(20, 140)) = 80
        _StreamCount ("Stream Count", Range(3, 16)) = 7
        [HideInInspector] _AuraAspect ("Canvas Aspect", Float) = 1.285714
        [HideInInspector] _AuraTime ("Animation Clock", Float) = 0
        [HideInInspector] _ChargeGlow ("Charge Glow", Float) = 0
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
                float _FanAngle;
                float _StreamCount;
                float _AuraAspect;
                float _ChargeGlow;
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
                // Polar flow starts at the quad's bottom-center and expands upward.
                float2 p = float2((input.uv.x - 0.5) * _AuraAspect, input.uv.y);
                float radius = length(p);
                float halfAngle = radians(clamp(_FanAngle, 20.0, 140.0) * 0.5);
                float angle = atan2(p.x, max(p.y, 0.0001)) / halfAngle;
                float lanes = (angle * 0.5 + 0.5) * _StreamCount;
                float t = _AuraTime * _RiseSpeed;
                // Radial advection sends the center upward and the sides diagonally outward.
                float flow = FlowNoise(float2(lanes * 0.73, radius * _NoiseScale - t));
                float bend = (Noise(float2(lanes * 0.5 + 3.0, radius * 4.0 - t)) - 0.5)
                    * _Turbulence * 0.8;
                float stream = pow(saturate(0.5 + 0.5 * sin((lanes + bend) * 6.2831853)), 3.0);
                // Blend the streams together at the origin instead of drawing thin spokes at the feet.
                stream = lerp(0.25 + flow * 0.25, stream, smoothstep(0.06, 0.26, radius));
                float wisps = smoothstep(0.3, 0.7, flow);
                float sides = 1.0 - smoothstep(0.68, 1.0, abs(angle));
                // Different lengths and broken streams avoid a hard fan outline or uniform rays.
                float reach = 0.72 + 0.25 * Noise(float2(lanes * 0.65, t * 0.15 + 11.0));
                float radialFade = smoothstep(0.0, 0.04, radius)
                    * (1.0 - smoothstep(reach * 0.55, reach, radius));
                float density = (0.07 + flow * 0.12 + stream * wisps * 0.95)
                    * sides * radialFade;
                float2 chargePoint = float2(p.x, p.y - 0.07);
                float chargeHalo = exp2(-90.0 * dot(chargePoint, chargePoint))
                    * smoothstep(0.0, 0.03, p.y) * _ChargeGlow;
                density = max(density, chargeHalo);
                // Fade the quad borders exactly to zero even when noise distorts them.
                float edge = smoothstep(0.0, 0.07, input.uv.x)
                    * (1.0 - smoothstep(0.93, 1.0, input.uv.x))
                    * (1.0 - smoothstep(0.94, 1.0, input.uv.y));
                return half4(_AuraColor.rgb * _AuraIntensity, saturate(density * edge) * _AuraOpacity);
            }
            ENDHLSL
        }
    }
}

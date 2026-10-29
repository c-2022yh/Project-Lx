// 풀·덤불이 바람에 살랑이고, 플레이어가 지나가면 옆으로 밀렸다가 바스락 흔들린다.
// 풀 타일맵(Tilemap Renderer)이나 덤불 SpriteRenderer의 Material에 M_FoliageSway를 넣으면 된다.
// 스프라이트 아래쪽(UV y=0)은 고정, 위로 갈수록 많이 흔들린다.
// 플레이어 위치는 FoliageInteraction이 _FoliagePlayer로 넣어준다.
// 조명(Light2D)은 받지 않는다(Unlit). 지금 씬에는 Light2D가 없어서 똑같이 보인다.
Shader "ProjectLx/2D/FoliageSway"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}

        [Header(Wind)]
        _WindStrength ("Wind Strength", Range(0, 0.3)) = 0.05
        _WindSpeed ("Wind Speed", Range(0, 6)) = 1.4
        _WindWave ("Wind Wave Length", Range(0.05, 2)) = 0.4

        [Header(Player)]
        _PushRadius ("Push Radius", Range(0.1, 3)) = 0.9
        _PushStrength ("Push Strength", Range(0, 1)) = 0.22
        _RustleStrength ("Rustle Strength", Range(0, 0.5)) = 0.07

        [Header(Sprite Area)]
        _UVBottom ("UV Bottom (fixed)", Range(0, 1)) = 0
        _UVTop ("UV Top (max sway)", Range(0, 1)) = 1

        [MaterialToggle] _ZWrite("ZWrite", Float) = 0

        // Legacy properties. They're here so that materials using this shader can gracefully fallback to the legacy sprite shader.
        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "CanUseSpriteAtlas" = "True" }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite [_ZWrite]

        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex UnlitVertex
            #pragma fragment UnlitFragment

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"

            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _WindStrength;
                float _WindSpeed;
                float _WindWave;
                float _PushRadius;
                float _PushStrength;
                float _RustleStrength;
                float _UVBottom;
                float _UVTop;
            CBUFFER_END

            // xy: 플레이어 위치, z: 속도(0~1), w: 플레이어 있음(1) / 없음(0)
            float4 _FoliagePlayer;

            float3 ApplySway(float3 positionOS, float2 uv)
            {
                float3 positionWS = TransformObjectToWorld(positionOS);

                float h = saturate((uv.y - _UVBottom) / max(_UVTop - _UVBottom, 0.0001));
                float bend = h * h;

                // 바람: 위치마다 위상이 달라서 물결처럼 지나간다.
                float t = _Time.y * _WindSpeed;
                float phase = positionWS.x / _WindWave;
                float wind = sin(t + phase) * 0.65 + sin(t * 2.3 + phase * 1.7) * 0.35;
                float offset = wind * _WindStrength;

                // 플레이어: 가까울수록 반대쪽으로 눕고, 움직이는 중이면 바스락.
                float2 d = positionWS.xy - _FoliagePlayer.xy;
                float nearX = saturate(1.0 - abs(d.x) / _PushRadius);
                float nearY = saturate(1.0 - abs(d.y) / (_PushRadius * 1.6));
                float near = nearX * nearY * _FoliagePlayer.w;
                float side = d.x >= 0.0 ? 1.0 : -1.0;
                offset += side * near * near * _PushStrength;
                offset += sin(_Time.y * 22.0 + positionWS.x * 5.0) * near * _FoliagePlayer.z * _RustleStrength;

                float3 offsetWS = float3(offset * bend, 0.0, 0.0);
                positionOS += mul((float3x3)GetWorldToObjectMatrix(), offsetWS);
                return positionOS;
            }

            Varyings UnlitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                input.positionOS = ApplySway(input.positionOS, input.uv);

                Varyings o = CommonUnlitVertex(input);
                o.color = input.color * _Color * unity_SpriteColor;
                return o;
            }

            half4 UnlitFragment(Varyings input) : SV_Target
            {
                return CommonUnlitFragment(input, input.color);
            }
            ENDHLSL
        }
    }

}

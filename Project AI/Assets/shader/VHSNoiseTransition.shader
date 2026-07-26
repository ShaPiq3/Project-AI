Shader "Custom/VHSNoiseTransition"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _NoiseIntensity ("Noise Intensity", Range(0,1)) = 0
        _GlitchAmount ("Glitch Amount", Range(0,1)) = 0
        _CollapseAmount ("CRT Collapse Amount", Range(0,1)) = 0
        _MaxNoiseCoverage ("Max Noise Coverage (화면을 완전히 덮지 않도록 상한)", Range(0,1)) = 0.55
        _ScanlineIntensity ("Scanline Intensity", Range(0,1)) = 0.6

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
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanvasRenderer"="True"
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

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color  : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;

            float _NoiseIntensity;
            float _GlitchAmount;
            float _CollapseAmount;
            float _MaxNoiseCoverage;
            float _ScanlineIntensity;

            v2f vert (appdata_t v)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            fixed4 frag (v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                float t = _Time.y;
                fixed texA = tex2D(_MainTex, uv).a; // Mask/RectMask2D 지원용

                // ---- 글리치 블록 지터 ----
                float block = floor(uv.y * 40.0);
                float jitter = (hash(float2(block, floor(t * 18.0))) - 0.5) * _GlitchAmount * 0.06;
                float2 nUV = uv + float2(jitter, 0);

                // ---- TV 스노우 느낌의 스파크형 노이즈 (검은 배경에 흰 점이 반짝임) ----
                float rawNoise = hash(nUV * float2(800.0, 800.0) + t * 90.0);
                float spark = pow(rawNoise, 3.0);

                // ---- 스캔라인 밴딩 (가로줄 명암) ----
                float scanRow = floor(uv.y * 220.0);
                float scanDark = lerp(1.0, hash(float2(scanRow, floor(t * 10.0))) * 0.6 + 0.4, _ScanlineIntensity);

                float noiseVal = saturate(spark * scanDark);
                float noiseAlpha = noiseVal * _NoiseIntensity * _MaxNoiseCoverage;
                fixed3 noiseColor = fixed3(noiseVal, noiseVal, noiseVal);

                // ---- CRT 전원 차단 연출: 위아래에서 검은 커튼이 좁혀지며 중앙으로 수렴 ----
                float distFromCenter = abs(uv.y - 0.5);
                float visibleHalfHeight = 0.5 * (1.0 - _CollapseAmount);
                float collapseMask = smoothstep(visibleHalfHeight, visibleHalfHeight + 0.02, distFromCenter);

                // 수렴 경계에 밝은 섬광 라인
                float edgeDist = abs(distFromCenter - visibleHalfHeight);
                float edgeGlow = (1.0 - smoothstep(0.0, 0.008, edgeDist));
                edgeGlow *= step(0.02, _CollapseAmount) * step(_CollapseAmount, 0.9);
                edgeGlow = saturate(edgeGlow);

                // 중요: 커튼이 다 좁혀져도 화면 정중앙(distFromCenter=0)은 수학적으로 절대
                // 검게 안 되는 구조라서, 붕괴 마지막 구간(0.85~1.0)에서는 화면 전체를
                // 강제로 완전 암전시켜 "가운데까지 확실히 닫히도록" 만듭니다.
                float finalBlackout = smoothstep(0.85, 1.0, _CollapseAmount);
                collapseMask = max(collapseMask, finalBlackout);

                fixed3 col = lerp(noiseColor, fixed3(0, 0, 0), collapseMask);
                col = saturate(col + edgeGlow * fixed3(0.75, 0.95, 0.95));

                float a = max(noiseAlpha, collapseMask);
                a = saturate(a) * texA;

                return fixed4(col, a);
            }
            ENDCG
        }
    }
}

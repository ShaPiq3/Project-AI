Shader "Custom/ScreenShatterGlitch"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _SnapshotTex ("Screen Snapshot (런타임에 C#에서 세팅됨)", 2D) = "black" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _TearAmount ("Tear Amount (0=정상, 1=완전히 조각남)", Range(0,1)) = 0
        _BlockSize ("Block Size (화면 비율 기준, 작을수록 조각이 많아짐)", Range(0.005, 0.2)) = 0.045
        _MaxBlockOffset ("Max Block Displacement", Range(0, 0.3)) = 0.07
        _ChromaticAberration ("Chromatic Aberration Amount", Range(0, 0.05)) = 0.012
        _StreakAmount ("Vertical Streak Amount", Range(0,1)) = 0.55

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

        // GrabPass 제거됨 (URP/SRP 비호환 문제). 대신 _SnapshotTex를 C#에서 매 전환 시작 시 세팅합니다.
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
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            sampler2D _SnapshotTex;
            fixed4 _Color;

            float _TearAmount;
            float _BlockSize;
            float _MaxBlockOffset;
            float _ChromaticAberration;
            float _StreakAmount;

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

            float2 hash2(float2 p)
            {
                return frac(sin(float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)))) * 43758.5453);
            }

            fixed4 frag (v2f IN) : SV_Target
            {
                fixed texA = tex2D(_MainTex, IN.texcoord).a; // Mask/RectMask2D 지원용

                if (_TearAmount <= 0.0001)
                {
                    return fixed4(0, 0, 0, 0);
                }

                // 캔버스 전체를 덮는 패널이므로 texcoord(0~1)가 곧 화면 좌표와 같습니다.
                float2 screenUV = IN.texcoord;
                float t = _Time.y;
                float frameSeed = floor(t * 30.0); // 프레임마다 갱신되어 지지직거림

                // ---- 화면을 사각 블록으로 나눠 각 블록을 랜덤하게 어긋나게 밀어냄 ----
                float2 blockUV = floor(screenUV / _BlockSize);
                float2 blockRand = hash2(blockUV + frameSeed * 7.0);
                float2 blockOffset = (blockRand - 0.5) * 2.0 * _MaxBlockOffset * _TearAmount;

                float2 sampleUV = screenUV + blockOffset;

                // ---- 색수차 (RGB 채널을 살짝 다른 위치에서 샘플링) ----
                float caAmt = _ChromaticAberration * _TearAmount;
                fixed r = tex2D(_SnapshotTex, sampleUV + float2(caAmt, 0)).r;
                fixed g = tex2D(_SnapshotTex, sampleUV).g;
                fixed b = tex2D(_SnapshotTex, sampleUV - float2(caAmt, 0)).b;
                fixed3 col = fixed3(r, g, b);

                // ---- 일부 블록은 세로로 길게 스미어(줄무늬 번짐) 처리 ----
                float streakTrigger = step(1.0 - _StreakAmount * 0.6, hash(blockUV + frameSeed * 3.0 + 17.0));
                float2 streakUV = float2(sampleUV.x, frac(sampleUV.y * 4.0 + frameSeed * 0.05));
                fixed3 streakColor = tex2D(_SnapshotTex, streakUV).rgb;
                col = lerp(col, streakColor, streakTrigger * _TearAmount);

                // ---- 블록 경계선을 살짝 밝게 해서 "조각남"을 강조 ----
                float2 blockFrac = frac(screenUV / _BlockSize);
                float edgeDist = min(min(blockFrac.x, 1.0 - blockFrac.x), min(blockFrac.y, 1.0 - blockFrac.y));
                float edgeLine = (1.0 - smoothstep(0.0, 0.06, edgeDist)) * _TearAmount * 0.35;
                col = saturate(col + edgeLine);

                // ---- 막바지엔 완전히 검게 덮어서 확실하게 다음 씬으로 넘어가도록 함 ----
                float finalCut = smoothstep(0.88, 1.0, _TearAmount);
                col = lerp(col, fixed3(0, 0, 0), finalCut);

                float a = saturate(_TearAmount) * texA;

                return fixed4(col, a);
            }
            ENDCG
        }
    }
}

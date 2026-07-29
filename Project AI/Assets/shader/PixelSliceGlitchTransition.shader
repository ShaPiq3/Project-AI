Shader "Custom/PixelSliceGlitchTransition"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _TearAmount ("Tear Amount (0=정상, 1=완전히 조각남)", Range(0,1)) = 0
        _SliceCount ("Slice Count", Float) = 28
        _SliceAngleDeg ("Slice Angle (0=가로, 90=세로)", Range(0,90)) = 18
        _MaxOffset ("Max Slice Offset", Range(0, 0.5)) = 0.18
        _GlitchColorAmount ("Color Corruption Amount", Range(0,1)) = 0.5

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

            float _TearAmount;
            float _SliceCount;
            float _SliceAngleDeg;
            float _MaxOffset;
            float _GlitchColorAmount;

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

                if (_TearAmount <= 0.0001)
                {
                    return fixed4(0, 0, 0, 0);
                }

                // ---- 사선/가로 방향으로 화면을 조각내는 좌표계 ----
                float ang = radians(_SliceAngleDeg);
                float2 dir = float2(cos(ang), sin(ang));
                float2 perp = float2(-dir.y, dir.x);

                float sliceCoord = dot(uv - 0.5, dir);
                float sliceIndex = floor(sliceCoord * _SliceCount);
                float sliceFrac = frac(sliceCoord * _SliceCount);

                // 슬라이스마다 다른 난수 (프레임마다 갱신되어 지지직거리는 느낌)
                float frameSeed = floor(t * 40.0);
                float sliceRandA = hash(float2(sliceIndex, frameSeed));
                float sliceRandB = hash(float2(sliceIndex + 91.0, frameSeed * 1.7));
                float sliceRevealAt = hash(float2(sliceIndex, 5.0)); // 슬라이스별로 조금씩 다른 타이밍에 갈라짐

                // ---- 슬라이스별 어긋남(displacement) ----
                float offsetAmt = (sliceRandA - 0.5) * 2.0 * _MaxOffset * _TearAmount;
                float2 offsetUV = uv + perp * offsetAmt;

                // ---- 어긋난 좌표에서 블록형 모자이크 노이즈 샘플링 (디지털 신호 깨짐 느낌) ----
                float2 blockUV = floor(offsetUV * 50.0) / 50.0;
                float blockNoise = hash(blockUV * 133.7 + frameSeed);
                float brightness = step(0.5, blockNoise);
                fixed3 sliceColor = fixed3(brightness, brightness, brightness);

                // ---- 일부 슬라이스에 색번짐(RGB corruption) ----
                float colorTrigger = step(1.0 - _GlitchColorAmount * 0.4, sliceRandB);
                fixed3 corruptColor = fixed3(
                    hash(float2(sliceIndex, 1.0)),
                    hash(float2(sliceIndex, 2.0)),
                    hash(float2(sliceIndex, 3.0))
                );
                sliceColor = lerp(sliceColor, corruptColor, colorTrigger);

                // ---- 슬라이스 경계에 밝은 파열 라인 ----
                float edgeLine = 1.0 - smoothstep(0.0, 0.05, min(sliceFrac, 1.0 - sliceFrac));
                sliceColor = saturate(sliceColor + edgeLine * _TearAmount * fixed3(1, 1, 1));

                // ---- 각 슬라이스가 진행률(_TearAmount)에 따라 순차적으로 "찢어짐"에 합류 ----
                float sliceAlpha = smoothstep(sliceRevealAt * 0.5, sliceRevealAt * 0.5 + 0.1, _TearAmount);

                // ---- 막바지에는 화면 전체를 강제로 완전히 덮어서 확실하게 전환되도록 함 ----
                float finalCut = smoothstep(0.9, 1.0, _TearAmount);
                sliceColor = lerp(sliceColor, fixed3(0, 0, 0), finalCut);
                float a = max(sliceAlpha, finalCut);

                a = saturate(a) * texA;

                return fixed4(sliceColor, a);
            }
            ENDCG
        }
    }
}

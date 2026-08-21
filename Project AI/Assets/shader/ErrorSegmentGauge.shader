Shader "Custom/ErrorSegmentGauge"
{
    // 💡 오류 파라미터 UI용. Image 오브젝트 1개 안에서 _SegmentCount 칸을 프로시저럴로 그린다.
    // _LitCount는 정수가 아니라 계속 트윈되는 값이라, 경계에 걸친 칸은 그 안에서 픽셀 단위로
    // 부드럽게 채워진다(부분 채움). 거기에 경계 발광 + 좌->우로 흐르는 스캔라인을 얹는다.
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _SegmentCount ("Segment Count", Float) = 100
        _LitCount ("Lit Count (C#가 매 프레임 갱신)", Float) = 0
        _LitColor ("Lit Color (구간별로 C#가 갱신)", Color) = (0, 0.74, 0.83, 1)
        _UnlitColor ("Unlit Color", Color) = (0.25, 0.25, 0.25, 1)

        _GapRatio ("Gap Ratio (칸 사이 간격)", Range(0, 0.5)) = 0.18
        _EdgeSoftness ("Edge Softness (경계 칸 부드러움)", Range(0.001, 0.5)) = 0.12
        _HardEdge ("Hard Edge (0=부드럽게, 1=딱 끊기게)", Range(0, 1)) = 0

        _GlowWidth ("Glow Width (세그먼트 단위)", Float) = 2.5
        _GlowIntensity ("Glow Intensity", Range(0, 3)) = 0.9

        _ScanlineSpacing ("Scanline Spacing (px)", Float) = 26
        _ScanlineWidth ("Scanline Width (px)", Float) = 5
        _ScanlineSpeed ("Scanline Speed (px/sec)", Float) = 40
        _ScanlineOpacity ("Scanline Opacity", Range(0, 1)) = 0.35

        _RectSize ("Rect Size In Local Units (자동 세팅됨)", Vector) = (600, 20, 0, 0)

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
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;

            float _SegmentCount;
            float _LitCount;
            fixed4 _LitColor;
            fixed4 _UnlitColor;

            float _GapRatio;
            float _EdgeSoftness;
            float _HardEdge;

            float _GlowWidth;
            float _GlowIntensity;

            float _ScanlineSpacing;
            float _ScanlineWidth;
            float _ScanlineSpeed;
            float _ScanlineOpacity;

            float4 _RectSize;

            v2f vert (appdata_t v)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag (v2f IN) : SV_Target
            {
                float2 sizePx = max(_RectSize.xy, float2(1.0, 1.0));
                float2 pxPos = IN.texcoord * sizePx; // 왼쪽 아래 원점 기준 픽셀 좌표

                float segmentCount = max(_SegmentCount, 1.0);
                float segmentWidthPx = sizePx.x / segmentCount;

                float rawIndexF = pxPos.x / segmentWidthPx;
                float segIndex = floor(rawIndexF);
                float localX = frac(rawIndexF); // 0~1, 칸(간격 포함) 안에서의 위치

                // ---- 칸 사이 간격: 간격 구간은 투명 처리 ----
                float halfGap = saturate(_GapRatio) * 0.5;
                float gapMask = step(halfGap, localX) * step(localX, 1.0 - halfGap);

                // 간격을 뺀, 칸의 "실제 채워지는 영역" 기준 0~1 위치
                float contentWidth = max(1.0 - saturate(_GapRatio), 0.0001);
                float contentX = saturate((localX - halfGap) / contentWidth);

                // 💡 [버그 수정] 막대 전체 기준의 연속 위치로 계산해야 한다. 예전엔 칸별로
                // saturate(_LitCount - segIndex)한 litAmount를 기준으로 smoothstep을 걸었는데,
                // 이러면 이미 꽉 찬 칸(litAmount가 saturate로 정확히 1.0이 됨)도 그 칸의 오른쪽
                // 끝(contentX=1.0)이 우연히 경계값(1.0)과 겹쳐서 매번 살짝 어둡게 물들었다.
                // 그래서 켜진 칸 전체에 걸쳐 얼룩덜룩한 무늬가 생기고, 흔들리는 애니메이션과
                // 맞물려 "다른 색 네모가 움직이는" 것처럼 보였다.
                float continuousPos = segIndex + contentX;

                float softness = max(_EdgeSoftness, 0.001);
                float softFillMask = 1.0 - smoothstep(_LitCount - softness, _LitCount + softness, continuousPos);
                float hardFillMask = step(continuousPos, _LitCount);
                float fillMask = lerp(softFillMask, hardFillMask, saturate(_HardEdge));

                fixed3 cellColor = lerp(_UnlitColor.rgb, _LitColor.rgb, fillMask);

                // ---- 켜진/꺼진 경계 부근 발광 (세그먼트 단위 연속 거리 기준) ----
                float distFromEdge = abs(_LitCount - continuousPos);
                float glow = _GlowIntensity * saturate(1.0 - distFromEdge / max(_GlowWidth, 0.0001));
                cellColor += _LitColor.rgb * glow * fillMask;

                // ---- 좌->우로 흐르는 스캔라인 (켜진 칸 위에만, 은은하게 밝기를 더함) ----
                // 💡 fillMask를 안 곱하면 꺼진(회색) 구간까지 파랗게 물들어서 "파란 막대가
                // 오른쪽으로 흘러가는" 것처럼 보였음 - 켜진 칸에만 얹히게 fillMask를 곱한다.
                float scanPos = pxPos.x - _Time.y * _ScanlineSpeed;
                float scanWave = abs(frac(scanPos / _ScanlineSpacing + 0.5) - 0.5) * _ScanlineSpacing;
                float scanline = 1.0 - smoothstep(0.0, max(_ScanlineWidth, 0.001), scanWave);
                cellColor += scanline * _ScanlineOpacity * fillMask * _LitColor.rgb;

                fixed texA = tex2D(_MainTex, IN.texcoord).a; // RectMask2D 등 마스킹 지원용
                float alpha = gapMask * texA * IN.color.a;

                return fixed4(saturate(cellColor), alpha);
            }
            ENDCG
        }
    }
}

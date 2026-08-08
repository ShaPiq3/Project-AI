Shader "Custom/ClueScanFrame"
{
    // 💡 단서 수집 모드 호버 오버레이용 UI 쉐이더.
    // 대상 RectTransform 크기(_RectSize, C#에서 매번 세팅)를 기준으로
    // 테두리 라인 + 바깥 글로우 + 네 모서리 브라켓을 픽셀 단위로 그려서,
    // 문장/이미지 크기가 달라져도 테두리 두께와 모서리 브라켓 길이가 항상 일정하게 보입니다.
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _FillColor ("Fill Color", Color) = (0.35, 0.95, 0.85, 0.18)
        _BorderColor ("Border/Corner Color", Color) = (0.3, 0.95, 0.9, 1)
        _GlowColor ("Glow Color", Color) = (0.25, 0.9, 0.85, 1)

        _BorderThickness ("Border Thickness (px)", Float) = 1.25
        _GlowSize ("Glow Size (px)", Float) = 8
        _GlowIntensity ("Glow Intensity", Range(0, 3)) = 1.3

        _CornerLength ("Corner Bracket Length (px)", Float) = 14
        _CornerThickness ("Corner Bracket Thickness (px)", Float) = 3
        _CornerBrightness ("Corner Brightness Multiplier", Range(1, 3)) = 1.8

        _PulseSpeed ("Glow Pulse Speed", Float) = 2.2
        _PulseAmount ("Glow Pulse Amount (0=off)", Range(0, 1)) = 0.15

        _RectSize ("Rect Size In Local Units (자동 세팅됨)", Vector) = (100, 40, 0, 0)

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

            fixed4 _FillColor;
            fixed4 _BorderColor;
            fixed4 _GlowColor;

            float _BorderThickness;
            float _GlowSize;
            float _GlowIntensity;

            float _CornerLength;
            float _CornerThickness;
            float _CornerBrightness;

            float _PulseSpeed;
            float _PulseAmount;

            float4 _RectSize;

            v2f vert (appdata_t v)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            // 표준 "over" 알파 합성 (top을 bottom 위에 얹음)
            fixed4 over(fixed4 top, fixed4 bottom)
            {
                float a = top.a + bottom.a * (1.0 - top.a);
                fixed3 rgb = (top.rgb * top.a + bottom.rgb * bottom.a * (1.0 - top.a)) / max(a, 0.0001);
                return fixed4(rgb, a);
            }

            // 모서리 하나에 대한 ㄴ자 브라켓 마스크 (c = 그 모서리 기준 픽셀 거리, armLength/armThickness는 이미 안전 범위로 clamp된 값)
            float cornerBracket(float2 c, float armLength, float armThickness)
            {
                float horiz = step(c.x, armLength) * (1.0 - smoothstep(armThickness - 1.0, armThickness + 1.0, c.y));
                float vert  = step(c.y, armLength) * (1.0 - smoothstep(armThickness - 1.0, armThickness + 1.0, c.x));
                return saturate(horiz + vert);
            }

            fixed4 frag (v2f IN) : SV_Target
            {
                float2 sizePx = max(_RectSize.xy, float2(1.0, 1.0));
                float2 pxPos = IN.texcoord * sizePx; // 왼쪽 아래 원점 기준 픽셀 좌표

                // 💡 [안전장치] _RectSize가 실제보다 훨씬 작게 들어오는 경우(초기화 타이밍 문제 등)에도
                // 테두리/모서리가 화면 전체를 뒤덮어버리지 않도록, 대상 크기의 절반을 넘지 않게 clamp합니다.
                float shortSide = min(sizePx.x, sizePx.y);
                float safeBorderThickness = clamp(_BorderThickness, 0.0, shortSide * 0.25);
                float safeGlowSize = clamp(_GlowSize, 0.0, shortSide * 0.25);
                float safeCornerLength = clamp(_CornerLength, 0.0, max(shortSide * 0.5 - safeBorderThickness, 0.0));
                float safeCornerThickness = clamp(_CornerThickness, 0.0, safeBorderThickness * 3.0 + 1.0);

                float distLeft   = pxPos.x;
                float distRight  = sizePx.x - pxPos.x;
                float distBottom = pxPos.y;
                float distTop    = sizePx.y - pxPos.y;
                float distToEdge = min(min(distLeft, distRight), min(distBottom, distTop));

                // ---- 얇은 테두리 라인 ----
                float borderMask = 1.0 - smoothstep(safeBorderThickness - 1.0, safeBorderThickness + 1.0, distToEdge);

                // ---- 테두리 바깥으로 은은하게 번지는 글로우 (살짝 펄스) ----
                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseAmount;
                float glowMask = saturate(1.0 - smoothstep(0.0, safeGlowSize, distToEdge - safeBorderThickness));
                glowMask *= _GlowIntensity * pulse;

                // ---- 네 모서리의 밝은 ㄴ자 브라켓 ----
                float cornerMask = saturate(
                    cornerBracket(pxPos, safeCornerLength, safeCornerThickness) +
                    cornerBracket(float2(sizePx.x - pxPos.x, pxPos.y), safeCornerLength, safeCornerThickness) +
                    cornerBracket(float2(pxPos.x, sizePx.y - pxPos.y), safeCornerLength, safeCornerThickness) +
                    cornerBracket(float2(sizePx.x - pxPos.x, sizePx.y - pxPos.y), safeCornerLength, safeCornerThickness)
                );

                // ---- 레이어 합성: 글로우(가장 뒤) -> 채우기 -> 테두리 -> 모서리 브라켓(가장 앞) ----
                fixed4 result = fixed4(0, 0, 0, 0);
                result = over(fixed4(_GlowColor.rgb, _GlowColor.a * glowMask), result);
                result = over(fixed4(_FillColor.rgb, _FillColor.a), result);
                result = over(fixed4(_BorderColor.rgb, _BorderColor.a * borderMask), result);
                result = over(fixed4(_BorderColor.rgb * _CornerBrightness, _BorderColor.a * cornerMask), result);

                fixed texA = tex2D(_MainTex, IN.texcoord).a; // RectMask2D 등 마스킹 지원용
                result *= texA * IN.color.a;

                return result;
            }
            ENDCG
        }
    }
}

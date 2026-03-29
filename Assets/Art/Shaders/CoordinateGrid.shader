Shader "StarFunc/CoordinateGrid"
{
    Properties
    {
        _GridStep     ("Grid Step", Float) = 1.0
        _GridColor    ("Grid Color", Color) = (0.12, 0.12, 0.18, 0.4)

        _AxisColorX          ("Axis X Color", Color) = (1.00, 0.40, 0.40, 1.0)
        _AxisColorY          ("Axis Y Color", Color) = (0.40, 1.00, 0.55, 1.0)
        _AxisColorMultiplier ("Axis Color Multiplier", Float) = 1.0
        _AxisThickness       ("Axis Thickness", Float) = 0.06

        _GridThickness ("Grid Thickness", Float) = 0.03
        _OriginBlend   ("Origin Blend Radius", Float) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "CoordinateGrid"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float  _GridStep;
                half4  _GridColor;
                half4  _AxisColorX;
                half4  _AxisColorY;
                float  _AxisColorMultiplier;
                float  _AxisThickness;
                float  _GridThickness;
                float  _OriginBlend;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 worldPos   : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(worldPos);
                output.worldPos = worldPos.xy;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 wp = input.worldPos;

                // --- Grid lines ---
                // Distance to nearest grid line in world units
                float2 gridDist = abs(frac(wp / _GridStep + 0.5) - 0.5) * _GridStep;

                // Screen-space derivatives for anti-aliasing
                float2 fw = fwidth(wp);

                // Anti-aliased grid: smoothstep from line edge to line edge + 1 pixel
                float gridX = 1.0 - smoothstep(_GridThickness * 0.5, _GridThickness * 0.5 + fw.x, gridDist.x);
                float gridY = 1.0 - smoothstep(_GridThickness * 0.5, _GridThickness * 0.5 + fw.y, gridDist.y);
                float grid = max(gridX, gridY);

                // --- Axis lines (separate colors) ---
                // axisX = the horizontal line at y=0 (the X axis)
                float axisX = 1.0 - smoothstep(_AxisThickness * 0.5, _AxisThickness * 0.5 + fw.y, abs(wp.y));
                // axisY = the vertical line at x=0 (the Y axis)
                float axisY = 1.0 - smoothstep(_AxisThickness * 0.5, _AxisThickness * 0.5 + fw.x, abs(wp.x));

                // Blend axis colors toward white near the origin
                float originX = 1.0 - saturate(abs(wp.x) / max(_OriginBlend, 0.001));
                float originY = 1.0 - saturate(abs(wp.y) / max(_OriginBlend, 0.001));
                half4 axisColX = lerp(_AxisColorX * _AxisColorMultiplier, half4(1, 1, 1, 1), originX);
                half4 axisColY = lerp(_AxisColorY * _AxisColorMultiplier, half4(1, 1, 1, 1), originY);

                // --- Compose ---
                half4 color = half4(0, 0, 0, 0);
                color = lerp(color, _GridColor, grid * _GridColor.a);
                color = lerp(color, axisColX, axisX * axisColX.a);
                color = lerp(color, axisColY, axisY * axisColY.a);

                return color;
            }
            ENDHLSL
        }
    }

    FallBack Off
}

Shader "StarFunc/SpriteAdditive"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,0.4)
        _BlurStrength ("Blur Strength (texels)", Range(0,16)) = 4
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Pass
        {
            // URP 2D Renderer (Renderer2D.asset) only invokes passes tagged
            // Universal2D for sprite materials. Without this tag the renderer
            // silently falls back to its built-in sprite path and the
            // additive blend defined above is ignored, so the material reads
            // as a plain alpha-blended sprite. Keep UniversalForward for the
            // forward renderer too, in case the project ever switches.
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 posOS  : POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 posCS  : SV_POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;
            half4 _Color;
            float _BlurStrength;

            Varyings vert(Attributes i)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.posCS = TransformObjectToHClip(i.posOS.xyz);
                o.uv    = i.uv;
                o.color = i.color * _Color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // 9-tap Gaussian blur. Offset is measured in source texels so
                // the blur is resolution-independent. Set _BlurStrength = 0 in
                // the material to skip the blur and pay only one tap.
                float2 offset = _MainTex_TexelSize.xy * _BlurStrength;
                float2 uv = i.uv;

                half4 col = 0;
                col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-1,-1) * offset) * 0.0625;
                col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2( 0,-1) * offset) * 0.125;
                col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2( 1,-1) * offset) * 0.0625;
                col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-1, 0) * offset) * 0.125;
                col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv                          ) * 0.25;
                col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2( 1, 0) * offset) * 0.125;
                col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-1, 1) * offset) * 0.0625;
                col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2( 0, 1) * offset) * 0.125;
                col += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2( 1, 1) * offset) * 0.0625;

                return col * i.color;
            }
            ENDHLSL
        }
    }
}

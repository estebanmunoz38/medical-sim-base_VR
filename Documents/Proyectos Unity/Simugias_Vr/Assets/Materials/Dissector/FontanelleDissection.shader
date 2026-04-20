Shader "URP/FontanelleDissection"
{
    Properties
    {
        _BaseMap ("Base Texture", 2D) = "white" {}
        _MaskMap ("Dissection Mask", 2D) = "black" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalRenderPipeline" "Queue"="AlphaTest" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _BaseMap;
            sampler2D _MaskMap;
            float4 _BaseMap_ST;
            float4 _Color;
            float _Cutoff;

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                half4 baseCol = tex2D(_BaseMap, i.uv) * _Color;
                half mask = tex2D(_MaskMap, i.uv).r;

                // Cortamos donde el tejido fue removido
                clip(1.0 - mask - _Cutoff);

                return baseCol;
            }
            ENDHLSL
        }
    }
}

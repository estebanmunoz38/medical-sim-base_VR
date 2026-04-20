Shader "Hidden/FontanelleBrush"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        ZWrite Off
        ZTest Always
        Cull Off
        Blend One One   // IMPORTANTE (acumulativo)

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            float4 _BrushUV;
            float _BrushRadius;
            float _BrushStrength;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.vertex.xy;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float d = distance(i.uv, _BrushUV.xy);
                float mask = saturate(1.0 - d / _BrushRadius);
                return mask * _BrushStrength;
            }
            ENDCG
        }
    }
}

Shader "Camera+/Bordered"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _FillColor ("Fill Color", Color) = (1,0,0,1)
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineFactor ("Outline Factor", Range(0,0.2)) = 0.075
        _Quality ("Quality", Range(4,32)) = 32
        _ShrinkFactor ("ShrinkFactor", Range(1,10)) = 4
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _FillColor;
            float4 _OutlineColor;
            float _OutlineFactor;
            int _Quality;
            float _ShrinkFactor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                float2 uvCenter = float2(0.5, 0.5);
                o.uv = lerp(uvCenter, v.uv, (1 + _OutlineFactor * _ShrinkFactor));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 mainCol = float4(0,0,0,0);
                float2 baseUV = i.uv;
                if (baseUV.x >= 0 && baseUV.x <= 1 && baseUV.y >= 0 && baseUV.y <= 1)
                    mainCol = tex2D(_MainTex, baseUV);

                if (_FillColor.a > 0)
                   mainCol.rgb = _FillColor.rgb;
                if (_OutlineFactor == 0)
						  return mainCol;

                // Outline creation by sampling around the current pixel in a circle
                float outlineAlpha = 0;
                float factor = 2 * 3.14159265359 / _Quality;
                for(int j = 0; j < _Quality; j++)
                {
                    float angle = j * factor;
                    float2 loc = baseUV + _OutlineFactor * float2(cos(angle), sin(angle));
                    if (loc.x >= 0 && loc.x <= 1 && loc.y >= 0 && loc.y <= 1)
                        outlineAlpha = max(outlineAlpha, tex2D(_MainTex, loc).a);
                }

                // Apply the maximum alpha from the sampled texture points for the outline
                fixed4 outlineCol = _OutlineColor;
                outlineCol.a *= outlineAlpha;

                // Combine the outline and the fill, ensuring fill overlays outline
                return (fixed4)lerp(outlineCol, mainCol, mainCol.a);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
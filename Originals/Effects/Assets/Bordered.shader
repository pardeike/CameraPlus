Shader "Camera+/Bordered"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MainUVScale ("Main UV Scale", Vector) = (1,1,0,0)
        _OutlineTex ("Outline Texture", 2D) = "white" {}
        _OutlineUVScale ("Outline UV Scale", Vector) = (1,1,0,0)
        _FillColor ("Fill Color", Color) = (1,0,0,1)
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineFactor ("Outline Factor", Range(0,0.2)) = 0.075
        _ShrinkFactor ("ShrinkFactor", Range(1,10)) = 4
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }

        Blend One OneMinusSrcAlpha
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
            float2 _MainUVScale;
            sampler2D _OutlineTex;
            float2 _OutlineUVScale;
            float4 _FillColor;
            float4 _OutlineColor;
            float _OutlineFactor;
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
                float2 mainUV = (baseUV - 0.5) * _MainUVScale + 0.5;
                if (mainUV.x >= 0 && mainUV.x <= 1 && mainUV.y >= 0 && mainUV.y <= 1)
                    mainCol = tex2D(_MainTex, mainUV);

                mainCol.rgb = lerp(mainCol.rgb, _FillColor.rgb, _FillColor.a);
                if (_OutlineFactor == 0)
                    return fixed4(mainCol.rgb * mainCol.a, mainCol.a);

                float outlineAlpha = 0;
                float2 outlineUV = (baseUV - 0.5) * _OutlineUVScale + 0.5;
                if (outlineUV.x >= 0 && outlineUV.x <= 1 && outlineUV.y >= 0 && outlineUV.y <= 1)
                    outlineAlpha = tex2D(_OutlineTex, outlineUV).a;

                float fillAlpha = mainCol.a;
                float visibleOutlineAlpha = outlineAlpha * _OutlineColor.a * (1 - fillAlpha);
                float combinedAlpha = fillAlpha + visibleOutlineAlpha;
                fixed3 combinedColor = mainCol.rgb * fillAlpha + _OutlineColor.rgb * visibleOutlineAlpha;
                return fixed4(combinedColor, combinedAlpha);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}

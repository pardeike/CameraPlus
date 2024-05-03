Shader "Camera+/HueBand"
{
    Properties
    {
        _Hue ("Hue", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

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

            float _Hue;

            fixed3 HSVtoRGB(fixed3 hsv)
            {
                fixed4 K = fixed4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                fixed3 p = abs(frac(hsv.xxx + K.xyz) * 6.0 - K.www);
                return hsv.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), hsv.y);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float y = 1 - i.uv.y;
                fixed3 rgb;
                fixed d = abs(_Hue - y);
                if (d <= 0.0025)
                    rgb = fixed3(1, 1, 1);
                else {
                    if (d >= 0.006)
                        rgb = HSVtoRGB(fixed3(y, 1, 1));
                    else
                        rgb = fixed3(0, 0, 0);
                }
                return fixed4(rgb, 1.0);
            }
            ENDCG
        }
    }
     FallBack "Diffuse"
}
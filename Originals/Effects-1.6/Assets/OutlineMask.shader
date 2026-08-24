Shader "Hidden/Camera+/OutlineMask"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "black" {}
        _Step ("Jump Step", Float) = 1
        _OutlineFactor ("Outline Factor", Float) = 0.075
        _SourceUVScale ("Source UV Scale", Vector) = (1,1,0,0)
    }
    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float2 _SourceUVScale;

            float4 frag(v2f_img i) : SV_Target
            {
                float2 sourceUV = (i.uv - 0.5) * _SourceUVScale + 0.5;
                if (sourceUV.x < 0 || sourceUV.x > 1 || sourceUV.y < 0 || sourceUV.y > 1)
                    return 0;
                float alpha = tex2D(_MainTex, sourceUV).a;
                return alpha >= 0.5 ? float4(i.uv, 0, 1) : 0;
            }
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Step;
            float2 _SourceUVScale;

            float4 frag(v2f_img i) : SV_Target
            {
                float4 best = 0;
                float bestDistance = 1e20;

                [unroll]
                for (int y = -1; y <= 1; y++)
                {
                    [unroll]
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 sampleUV = i.uv + float2(x, y) * _MainTex_TexelSize.xy * _Step;
                        float4 candidate = tex2D(_MainTex, sampleUV);
                        float2 delta = (candidate.xy - i.uv) * _SourceUVScale;
                        float candidateDistance = dot(delta, delta);
                        if (candidate.a > 0 && candidateDistance < bestDistance)
                        {
                            best = candidate;
                            bestDistance = candidateDistance;
                        }
                    }
                }
                return best;
            }
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _OutlineFactor;
            float2 _SourceUVScale;

            float4 frag(v2f_img i) : SV_Target
            {
                float4 nearest = tex2D(_MainTex, i.uv);
                if (nearest.a == 0)
                    return 0;

                float distanceToShape = length((nearest.xy - i.uv) * _SourceUVScale);
                float2 sourceTexelSize = _MainTex_TexelSize.xy * _SourceUVScale;
                float antialiasWidth = max(sourceTexelSize.x, sourceTexelSize.y);
                float alpha = saturate(0.5 + (_OutlineFactor - distanceToShape) / antialiasWidth);
                return float4(1, 1, 1, alpha);
            }
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float2 _SourceUVScale;

            float4 frag(v2f_img i) : SV_Target
            {
                float2 sourceUV = (i.uv - 0.5) * _SourceUVScale + 0.5;
                if (sourceUV.x < 0 || sourceUV.x > 1 || sourceUV.y < 0 || sourceUV.y > 1)
                    return 0;
                return tex2D(_MainTex, sourceUV);
            }
            ENDCG
        }
    }
}

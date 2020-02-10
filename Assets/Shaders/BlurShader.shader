Shader "Unlit/BlurShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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
            #pragma multi_compile _ BLUR_SQUARES BLUR_NOTHING

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

            sampler _MainTex;
            float2 BlurPixelSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
#ifdef BLUR_NOTHING
                return float4(1, 0, 0, 0);
#endif

                // sample the texture
                float col = 0;
                [unroll] for (int x = -1; x <= 1; x++)
                {
                    [unroll] for (int y = -1; y <= 1; y++)
                    {
                        float2 index = i.uv;
                        index += BlurPixelSize * float2(x, y);
                        float value1 = tex2D(_MainTex, index).r;
#ifdef BLUR_SQUARES
                        value1 *= value1;
#endif
                        col += value1;
                    }
                }
                col /= 9;
                return float4(col, 0, 0, 0);
            }
            ENDCG
        }
    }
}

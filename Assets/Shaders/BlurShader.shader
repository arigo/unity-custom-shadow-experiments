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
            #pragma multi_compile _ BLUR_NOTHING

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
                return float4(1, 1, 0, 0);
#endif

                // sample the texture
                float2 col = float2(0, 0);
                [unroll] for (int x = -1; x <= 1; x++)
                {
                    [unroll] for (int y = -1; y <= 1; y++)
                    {
                        float2 index = i.uv;
                        index += BlurPixelSize * float2(x, y);
                        float2 value_enc = tex2D(_MainTex, index);
                        float value1 = value_enc.r + value_enc.g;

                        col += float2(value1, value1 * value1);
                    }
                }
                col /= 9;

                float2 limited_precision = f16tof32(f32tof16(col.rg));
                float2 correction = col.rg - limited_precision;

                return float4(limited_precision.rg, correction.rg);
            }
            ENDCG
        }
    }
}

﻿Shader "Unlit/BlurShader"
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
            #pragma multi_compile _ BLUR_LINEAR_PART BLUR_NOTHING

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

            float pick(float2 index, int x, int y)
            {
                index += BlurPixelSize * float2(x, y);
                float col = tex2D(_MainTex, index);
#ifndef BLUR_LINEAR_PART
                col *= col;
#endif
                return col;
            }

            float4 frag (v2f i) : SV_Target
            {
#ifdef BLUR_NOTHING
                return float4(1, 1, 0, 0);
#endif

#ifdef GUASSIAN_KERNEL
//                // sample the texture and apply a Gaussian Kernel blur
//                float col = 0;
//                col += pick(i.uv, -2, -2) * 1 / 256.0;
//                col += pick(i.uv, -2, -1) * 4 / 256.0;
//                col += pick(i.uv, -2, +0) * 6 / 256.0;
//                col += pick(i.uv, -2, +1) * 4 / 256.0;
//                col += pick(i.uv, -2, +2) * 1 / 256.0;
//
//                col += pick(i.uv, -1, -2) * 4 / 256.0;
//                col += pick(i.uv, -1, -1) * 16 / 256.0;
//                col += pick(i.uv, -1, +0) * 24 / 256.0;
//                col += pick(i.uv, -1, +1) * 16 / 256.0;
//                col += pick(i.uv, -1, +2) * 4 / 256.0;
//
//                col += pick(i.uv, +0, -2) * 6 / 256.0;
//                col += pick(i.uv, +0, -1) * 24 / 256.0;
//                col += pick(i.uv, +0, +0) * 36 / 256.0;
//                col += pick(i.uv, +0, +1) * 24 / 256.0;
//                col += pick(i.uv, +0, +2) * 6 / 256.0;
//
//                col += pick(i.uv, +1, -2) * 4 / 256.0;
//                col += pick(i.uv, +1, -1) * 16 / 256.0;
//                col += pick(i.uv, +1, +0) * 24 / 256.0;
//                col += pick(i.uv, +1, +1) * 16 / 256.0;
//                col += pick(i.uv, +1, +2) * 4 / 256.0;
//
//                col += pick(i.uv, +2, -2) * 1 / 256.0;
//                col += pick(i.uv, +2, -1) * 4 / 256.0;
//                col += pick(i.uv, +2, +0) * 6 / 256.0;
//                col += pick(i.uv, +2, +1) * 4 / 256.0;
//                col += pick(i.uv, +2, +2) * 1 / 256.0;

#else
                // sample the texture and apply a simple Box Blur
                float col = 0;
                [unroll] for (int x = -1; x <= 1; x++)
                {
                    [unroll] for (int y = -1; y <= 1; y++)
                    {
                        float2 index = i.uv;
                        index += BlurPixelSize * float2(x, y);
                        float value1 = tex2D(_MainTex, index).r;
#ifdef BLUR_LINEAR_PART
                        col += value1;
#else
                        col += value1 * value1;
#endif
                    }
                }
                col /= 9;
#endif

                return float4(col, 0, 0, 1);
            }
            ENDCG
        }
    }
}

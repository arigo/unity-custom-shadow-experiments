﻿Shader "Hidden/ShadowVSM/Depth" {
    Properties
    {
        _Color("Main Color", Color) = (1,1,1,1)
        _MainTex("Texture", 2D) = "white" {}
    }

    SubShader {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM
            #include "UnityCG.cginc"
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ VSM_DRAW_TRANSPARENT_SHADOWS

#ifdef VSM_DRAW_TRANSPARENT_SHADOWS
            #include "Dither Functions.cginc"
#endif

            sampler2D _MainTex;
            float4 _Color;
            float SShadowCascade;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 col = _Color;
                #if defined(VSM_DRAW_TRANSPARENT_SHADOWS)
                ditherClip(i.vertex, col.a);
                #else
                if (col.a < 0.5) discard;
                #endif

                float depth = i.vertex.z;
#ifdef UNITY_REVERSED_Z
                depth = 0.5 - depth;
#else
                depth = depth - 0.5;
#endif
                depth *= 128;

                float dx = ddx(depth);
                float dy = ddy(depth);
                float bias = 0.25 * (dx * dx + dy * dy);

                return float4(depth, depth * depth + bias, 1, 0);
            }
            ENDCG
        }
    }

    Fallback "VertexLit"
}
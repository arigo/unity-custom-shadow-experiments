Shader "Hidden/CustomShadows/Depth" {
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
            #include "../Addons/Dither Functions.cginc"
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ DRAW_TRANSPARENT_SHADOWS

            sampler2D _MainTex;
            float4 _Color;

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
                #if defined(DRAW_TRANSPARENT_SHADOWS)
                ditherClip(i.vertex, col.a);
                #else
                if (col.a < 0.5) discard;
                #endif

                float depth = i.vertex.z;
#ifdef SHADER_API_D3D11
                depth = 1 - depth;
#endif

                float limited_precision_depth = f16tof32(f32tof16(depth));
                float correction = depth - limited_precision_depth;
                return float4(limited_precision_depth, correction, 0, 0);
            }
            ENDCG
        }
    }

    Fallback "VertexLit"
}
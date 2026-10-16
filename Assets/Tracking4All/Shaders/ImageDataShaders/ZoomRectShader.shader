Shader "Tracking4All/ZoomRectShader"
{
    Properties
    {
        _MainTex ("Source Texture", 2D) = "white" {}
        _UVRect ("UV Rect (x, y, width, height)", Vector) = (0, 0, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            sampler2D _MainTex;
            float4 _UVRect; // x, y, w, h

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (float4 vertex : POSITION, float2 uv : TEXCOORD0)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(vertex);
                o.uv = uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                uv = _UVRect.xy + uv * _UVRect.zw;
                return tex2D(_MainTex, uv);
            }
            ENDCG
        }
    }
}

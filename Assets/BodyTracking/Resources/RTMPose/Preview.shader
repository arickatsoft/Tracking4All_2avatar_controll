Shader "FitFight/RTMPosePreview"
{
    Properties { _MainTex ("Camera", 2D) = "black" {} }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            int _Rotation, _VerticalFlip;
            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 p = float2(i.uv.x, 1-i.uv.y);
                float2 raw = p;
                if (_Rotation == 90) raw = float2(p.y, 1-p.x);
                else if (_Rotation == 180) raw = 1-p;
                else if (_Rotation == 270) raw = float2(1-p.y, p.x);
                raw.y = 1-raw.y;
                if (_VerticalFlip != 0) raw.y = 1-raw.y;
                return tex2D(_MainTex, raw);
            }
            ENDCG
        }
    }
}

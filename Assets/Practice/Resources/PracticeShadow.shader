Shader "FitFight/PracticeShadow"
{
    Properties { _Strength ("Shadow strength", Range(0,1)) = 0.5 }
    SubShader
    {
        Tags { "Queue"="Transparent-10" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            struct v2f { float4 pos:SV_POSITION; SHADOW_COORDS(1) };
            float _Strength;
            v2f vert(appdata_base v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); TRANSFER_SHADOW(o); return o; }
            fixed4 frag(v2f i):SV_Target { return fixed4(0,0,0,(1-SHADOW_ATTENUATION(i))*_Strength); }
            ENDCG
        }
    }
    Fallback Off
}

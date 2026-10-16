Shader "Tracking4All/SegmentationRainbowCutoutUI"
{
 Properties
{
    _MainTex ("Main Texture", 2D) = "" {}
    _Threshold ("Threshold Value", Float) = 0.5
    _TimeMultiplier ("Color Shift Speed", Float) = 1.0
    _ResolutionX ("Camera Width", int) = 1920
    _ResolutionY ("Camera Height", int) = 1080
}
SubShader
{
    Tags { "RenderType" = "TransparentCutout" "Queue" = "Overlay" }
    LOD 100

    Pass
    {
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #include "UnityCG.cginc"

        struct appdata_t
        {
            float4 vertex : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct v2f
        {
            float2 uv : TEXCOORD0;
            float4 pos : SV_POSITION;
        };

        StructuredBuffer<float> _SegmentationBuffer;

        float _Threshold;
        float _TimeMultiplier;
        int _ResolutionX;
        int _ResolutionY;

        // Function to convert HSV to RGB
        float3 HSVtoRGB(float h, float s, float v)
        {
            float3 rgb = clamp(abs(frac(h + float3(0.0, 2.0/3.0, 1.0/3.0)) * 6.0 - 3.0) - 1.0, 0.0, 1.0);
            return v * lerp(float3(1.0, 1.0, 1.0), rgb, s);
        }

        v2f vert (appdata_t v)
        {
            v2f o;
            o.pos = UnityObjectToClipPos(v.vertex);
            o.uv = v.uv;
            return o;
        }

        fixed4 frag (v2f i) : SV_Target
        {
            int index = int(i.uv.y * _ResolutionY) * _ResolutionX + int(i.uv.x * _ResolutionX);
            float bufferValue = _SegmentationBuffer[index];

            if (bufferValue < _Threshold)
            {
                discard;
            }

            // Compute rainbow color based on time
            float hue = frac(_Time.y * _TimeMultiplier * 0.1); // Slow color cycle
            float3 rainbowColor = HSVtoRGB(hue, 1.0, 1.0);

            return float4(rainbowColor, 1.0);
        }
        ENDCG
    }
}
Fallback "UI/Default"

}

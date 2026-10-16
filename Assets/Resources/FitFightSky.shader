Shader "FitFight/CloudSky" {
 SubShader {
 Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
 Cull Off ZWrite Off
 Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma target 3.0
 #include "UnityCG.cginc"
 struct appdata {float4 vertex:POSITION;};
 struct v2f {float4 pos:SV_POSITION;float3 ray:TEXCOORD0;};
 v2f vert(appdata v) {v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.ray=v.vertex.xyz;return o;}
 float hash(float2 p){float3 q=frac(float3(p.x,p.y,p.x)*.1031);q+=dot(q,q.yzx+33.33);return frac((q.x+q.y)*q.z);}
 float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
 fixed4 frag(v2f i):SV_Target {
 float3 d=normalize(i.ray);float h=saturate(d.y);
 float3 sky=lerp(float3(.60,.78,.90),float3(.18,.50,.85),sqrt(h));
 float2 p=d.xz/(max(d.y,.08)+.3)*6.4;
 p+=float2(noise(p*.7),noise(p*.7+17))*.8;
 float n=noise(p)*.48+noise(p*2.1)*.26+noise(p*4.2)*.15+noise(p*8.4)*.075+noise(p*16.8)*.035;
 float cloud=smoothstep(.58,.75,n)*smoothstep(.02,.16,h);
 return fixed4(lerp(sky,float3(.97,.97,.94),cloud*.92),1);
 }
 ENDCG
 }
 }
}

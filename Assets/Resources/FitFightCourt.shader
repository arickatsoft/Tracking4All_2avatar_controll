Shader "FitFight/Court" {
 Properties { _Color("Concrete", Color) = (0.63,0.59,0.49,1) _Unit("Tile size", Float) = 1 }
 SubShader {
 Tags { "RenderType"="Opaque" }
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows
 #pragma target 3.0
 fixed4 _Color; float _Unit;
 struct Input { float3 worldPos; };
 float hash(float2 p) {float3 q=frac(float3(p.x,p.y,p.x)*.1031);q+=dot(q,q.yzx+33.33);return frac((q.x+q.y)*q.z);}
 float noise(float2 p) {float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
 void surf(Input IN, inout SurfaceOutputStandard o) {
 float2 p=IN.worldPos.xz/max(_Unit,.01);
 float2 f=frac(p); float edge=min(min(f.x,1-f.x),min(f.y,1-f.y));
 float aa=max(length(fwidth(p))*.5,.001);
 float joint=1-smoothstep(.003-aa,.007+aa,edge);
 float grain=hash(floor(p*240));
 float tile=hash(floor(p));
 float weathering=noise(p*3)*.55+noise(p*11)*.3+noise(p*37)*.15;
 float grainFade=1-saturate(length(fwidth(p))*180);
 fixed3 concrete=_Color.rgb*(.65+.24*tile+.45*weathering+(grain-.5)*.18*grainFade);
 // Court lines, painted flush onto the surface, cannot z-fight with the floor.
 float stripe=1-smoothstep(.025,.04,abs(abs(p.x)-2.5));
 o.Albedo=lerp(lerp(concrete,concrete*.40,joint),fixed3(.65,.63,.56),stripe*.85);
 // Fine stone relief, faded with pixel footprint to avoid distant shimmer.
 float relief=noise(p*28);
 float dx=noise(p*28+float2(.12,0))-relief;
 float dy=noise(p*28+float2(0,.12))-relief;
 float reliefFade=1-saturate(length(fwidth(p))*28);
 o.Normal=normalize(float3(-dx*1.2*reliefFade,-dy*1.2*reliefFade,1));
 o.Metallic=0; o.Smoothness=.035; o.Occlusion=1-joint*.2;
 }
 ENDCG
 }
 Fallback "Diffuse"
}

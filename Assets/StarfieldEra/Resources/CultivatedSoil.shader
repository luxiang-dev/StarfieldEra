Shader "StarfieldEra/Cultivated Soil"
{
 Properties {_Atlas("Painted soil atlas",2D)="white"{} [MainColor]_BaseColor("Soil tint",Color)=(1,1,1,1)}
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent-10"}
  Pass
  {
   Tags {"LightMode"="UniversalForwardOnly"}
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off
   Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile_fragment _ _SHADOWS_SOFT
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   TEXTURE2D(_Atlas);SAMPLER(sampler_Atlas);
   CBUFFER_START(UnityPerMaterial)
   float4 _Atlas_ST;half4 _BaseColor;
   CBUFFER_END
   struct A{float4 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;half4 state:COLOR;};
   struct V{float4 p:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float2 uv:TEXCOORD2;half4 state:TEXCOORD3;};
   V vert(A i){V o;o.world=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.world);o.normal=TransformObjectToWorldNormal(i.n);o.uv=i.uv;o.state=i.state;return o;}
   float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float noise(float2 p){float2 a=floor(p),b=frac(p);b=b*b*(3-2*b);return lerp(lerp(hash(a),hash(a+float2(1,0)),b.x),lerp(hash(a+float2(0,1)),hash(a+1),b.x),b.y);}
   half4 frag(V i):SV_Target
   {
    float2 uv=float2(i.uv.x/4.48,i.uv.y/4);
    half3 detail=SAMPLE_TEXTURE2D_GRAD(_Atlas,sampler_Atlas,float2(.5,0)+(.008+frac(uv)*.484),ddx(uv)*.484,ddy(uv)*.484).rgb;
    float crest=pow(.5+.5*cos(i.uv.x*9.817477),1.8);
    float blotch=noise(i.world.xz*.65+14);
    half3 soil=lerp(SRGBToLinear(half3(.47,.32,.20)),detail,.62);
    soil*=lerp(.72,1.12,crest)*(.94+blotch*.12)*(.94+i.state.b*.12);
    soil*=lerp(half3(1,1,1),half3(.64,.69,.66),i.state.r)*_BaseColor.rgb;
    // Clay highlights and darker pockets remain soft instead of forming solid repeated stripes.
    soil=lerp(soil,SRGBToLinear(half3(.58,.40,.25)),crest*noise(i.world.xz*1.2)*.13);
    float grain=noise(i.world.xz*17);
    float alpha=saturate(i.state.a+(grain-.5)*.13)*smoothstep(0,.10,i.state.a);
    Light sun=GetMainLight(TransformWorldToShadowCoord(i.world));half3 n=normalize(i.normal);
    half shade=(.80+.20*saturate(dot(n,sun.direction)))*lerp(.76,1,sun.shadowAttenuation);
    return half4(soil*shade,alpha);
   }
   ENDHLSL
  }
 }
}

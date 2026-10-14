Shader "StarfieldEra/Cultivated Soil"
{
 Properties {_Atlas("Painted soil atlas",2D)="white"{} [MainColor]_BaseColor("Wetness tint",Color)=(1,1,1,1)}
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
   struct A{float4 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;};
   struct V{float4 p:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float2 uv:TEXCOORD2;};
   V vert(A i){V o;o.world=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.world);o.normal=TransformObjectToWorldNormal(i.n);o.uv=i.uv;return o;}
   half4 frag(V i):SV_Target
   {
    float2 p=(i.uv-.5)*2.3;float edge=-10;
    for(int j=0;j<6;j++){float angle=j*1.04719755;edge=max(edge,dot(p,float2(cos(angle),sin(angle))));}
    float grain=frac(sin(dot(floor(i.world.xz*35),float2(47.1,91.7)))*4137.2);
    float alpha=saturate((1.08-edge+(grain-.5)*.04)/.08);
    half3 detail=SAMPLE_TEXTURE2D(_Atlas,sampler_Atlas,float2(.5,0)+(.008+frac(i.uv)*.484)).rgb;
    half3 soil=detail*_BaseColor.rgb;
    // Extra dark grooves follow the actual sculpted ridges.
    soil*=.82+.18*pow(.5+.5*cos(p.x*18.8496),2);
    Light sun=GetMainLight(TransformWorldToShadowCoord(i.world));half3 n=normalize(i.normal);
    return half4(soil*(SampleSH(n)+sun.color*(.3+.7*saturate(dot(n,sun.direction)))*sun.shadowAttenuation),alpha);
   }
   ENDHLSL
  }
 }
}

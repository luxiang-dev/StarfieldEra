Shader "StarfieldEra/Painted Water"
{
 Properties {_Shallow("Shallow turquoise",Color)=(.39,.71,.69,1) _Deep("Deep blue green",Color)=(.19,.52,.57,1)}
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
  Pass
  {
   Tags {"LightMode"="UniversalForwardOnly"}
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off
   HLSLPROGRAM
   #pragma target 4.5
   #pragma vertex vert
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
   #pragma fragment frag
   #define _SURFACE_TYPE_TRANSPARENT 1
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
   CBUFFER_START(UnityPerMaterial)
   half4 _Shallow,_Deep;
   CBUFFER_END
   struct A {float4 p:POSITION;float4 color:COLOR;};
   struct V {float4 p:SV_POSITION;float3 world:TEXCOORD0;float depth:TEXCOORD1;float4 screen:TEXCOORD2;};
   V vert(A i){V o;o.world=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.world);o.screen=ComputeScreenPos(o.p);o.depth=i.color.r;return o;}
   float2 random2(float2 p){return frac(sin(float2(dot(p,float2(127.1,311.7)),dot(p,float2(269.5,183.3))))*43758.5453);}
   float voronoi(float2 p)
   {
    float2 cell=floor(p),f=frac(p);float nearest=8,second=8;
    for(int y=-1;y<=1;y++)for(int x=-1;x<=1;x++)
    {float2 offset=float2(x,y);float2 q=offset+random2(cell+offset)-f;float d=dot(q,q);if(d<nearest){second=nearest;nearest=d;}else second=min(second,d);}
    return sqrt(second)-sqrt(nearest);
   }
   half4 frag(V i):SV_Target
   {
    float raw=SampleSceneDepth(i.screen.xy/i.screen.w);
    float scene=unity_OrthoParams.w>.5?LinearDepthToEyeDepth(raw):LinearEyeDepth(raw,_ZBufferParams);
    float own=-TransformWorldToView(i.world).z;float depth=max(0,scene-own);
    float band=voronoi(i.world.xz*1.25+float2(_Time.y*.035,_Time.y*.022));
    float shoreline=max(1-smoothstep(.04,.22,depth),(1-smoothstep(.48,.7,i.depth))*.65);
    float foam=shoreline*smoothstep(.08,.14,band+sin(_Time.y*1.4+i.world.x*.6)*.05);
    float glint=(1-smoothstep(.015,.048,band))*(.3+.3*sin(_Time.y+i.world.z*2));
    half3 water=lerp(_Shallow.rgb,_Deep.rgb,saturate(i.depth*1.5));
    water=lerp(water,half3(.86,.94,.82),saturate(foam*.85+glint*.12));
    Light sun=GetMainLight(TransformWorldToShadowCoord(i.world),i.world,half4(1,1,1,1));water*=lerp(.66,1,sun.shadowAttenuation);
    return half4(water,lerp(.48,.86,saturate(max(depth,i.depth*.3)*3)));
   }
   ENDHLSL
  }
 }
}

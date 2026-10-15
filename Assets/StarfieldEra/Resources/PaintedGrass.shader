Shader "StarfieldEra/Painted Grass"
{
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
  Pass
  {
   Tags {"LightMode"="UniversalForwardOnly"}
   Cull Off
   HLSLPROGRAM
   #pragma target 4.5
   #pragma multi_compile _ _ADDITIONAL_LIGHTS
   #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   #include "StorybookLighting.hlsl"
   struct A{float4 p:POSITION;half4 color:COLOR;};
   struct V{float4 p:SV_POSITION;float3 world:TEXCOORD0;half4 color:COLOR;};
   V vert(A i){V o;o.world=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.world);o.color=i.color;return o;}
   half4 frag(V i):SV_Target{return half4(StorybookShade(i.color.rgb,i.world,half3(0,1,0),i.p,0),1); }
   ENDHLSL
  }
 }
}

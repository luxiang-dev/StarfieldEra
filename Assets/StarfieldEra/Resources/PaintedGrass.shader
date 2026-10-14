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
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile_fragment _ _SHADOWS_SOFT
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   struct A{float4 p:POSITION;half4 color:COLOR;};
   struct V{float4 p:SV_POSITION;float3 world:TEXCOORD0;half4 color:COLOR;};
   V vert(A i){V o;o.world=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.world);o.color=i.color;return o;}
   half4 frag(V i):SV_Target{Light sun=GetMainLight(TransformWorldToShadowCoord(i.world));return half4(i.color.rgb*lerp(.75,1,sun.shadowAttenuation),1);}
   ENDHLSL
  }
 }
}

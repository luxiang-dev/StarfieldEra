Shader "StarfieldEra/Opaque Surface Passes"
{
 SubShader
 {
 Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
  Pass
  {
   Name "ShadowCaster" Tags {"LightMode"="ShadowCaster"}
   ZWrite On Cull Off ColorMask 0
   HLSLPROGRAM
   #pragma vertex SurfaceShadowVertex
   #pragma fragment SurfaceDepthFragment
   #pragma multi_compile_instancing
   #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
   #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


   #include "StorybookSurfacePass.hlsl"
   ENDHLSL
  }
  Pass
  {
   Name "DepthOnly" Tags {"LightMode"="DepthOnly"}
   ZWrite On Cull Off ColorMask R
   HLSLPROGRAM
   #pragma vertex SurfaceVertex
   #pragma fragment SurfaceDepthFragment
   #pragma multi_compile_instancing
   #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
   #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


   #include "StorybookSurfacePass.hlsl"
   ENDHLSL
  }
  Pass
  {
   Name "DepthNormals" Tags {"LightMode"="DepthNormalsOnly"}
   ZWrite On Cull Off
   HLSLPROGRAM
   #pragma vertex SurfaceVertex
   #pragma fragment SurfaceNormalFragment
   #pragma multi_compile_instancing
   #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
   #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


   #include "StorybookSurfacePass.hlsl"
   ENDHLSL
  }
 }
}

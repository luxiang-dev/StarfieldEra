Shader "StarfieldEra/Storybook Models"
{
 Properties {_BaseMap("Painted color",2D)="white"{} [MainColor]_BaseColor("Color",Color)=(1,1,1,1) _Cutoff("Cutout",Range(0,1))=.35 _EmissionMap("Emission mask",2D)="white"{} [HDR]_EmissionColor("Emission",Color)=(0,0,0,0)}
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
  Pass
  {
   Tags {"LightMode"="UniversalForwardOnly"}
   Cull Off
   HLSLPROGRAM
   #pragma target 4.5
   #pragma vertex vert
   #pragma multi_compile _ _ADDITIONAL_LIGHTS
   #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
   #pragma fragment frag
   #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   #include "StorybookLighting.hlsl"
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);TEXTURE2D(_EmissionMap);SAMPLER(sampler_EmissionMap);
   CBUFFER_START(UnityPerMaterial)
   float4 _BaseMap_ST;half4 _BaseColor;float _Cutoff;half4 _EmissionColor;
   CBUFFER_END
   struct A{float4 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;};
   struct V{float4 p:SV_POSITION;float3 world:TEXCOORD0;float3 n:TEXCOORD1;float2 uv:TEXCOORD2;};
   V vert(A i){V o;o.world=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.world);o.n=TransformObjectToWorldNormal(i.n);o.uv=TRANSFORM_TEX(i.uv,_BaseMap);return o;}
   half4 frag(V i):SV_Target
   {
    half4 paint=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv)*_BaseColor;clip(paint.a-_Cutoff);
    half3 emission=_EmissionColor.rgb*SAMPLE_TEXTURE2D(_EmissionMap,sampler_EmissionMap,i.uv).rgb;
    return half4(StorybookShade(paint.rgb,i.world,i.n,i.p,emission),1);
   }
   ENDHLSL
  }
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
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);TEXTURE2D(_EmissionMap);SAMPLER(sampler_EmissionMap);
   CBUFFER_START(UnityPerMaterial)
   float4 _BaseMap_ST;half4 _BaseColor;float _Cutoff;half4 _EmissionColor;
   CBUFFER_END
   #define STORYBOOK_MODEL_ALPHA 1
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
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);TEXTURE2D(_EmissionMap);SAMPLER(sampler_EmissionMap);
   CBUFFER_START(UnityPerMaterial)
   float4 _BaseMap_ST;half4 _BaseColor;float _Cutoff;half4 _EmissionColor;
   CBUFFER_END
   #define STORYBOOK_MODEL_ALPHA 1
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
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);TEXTURE2D(_EmissionMap);SAMPLER(sampler_EmissionMap);
   CBUFFER_START(UnityPerMaterial)
   float4 _BaseMap_ST;half4 _BaseColor;float _Cutoff;half4 _EmissionColor;
   CBUFFER_END
   #define STORYBOOK_MODEL_ALPHA 1
   #include "StorybookSurfacePass.hlsl"
   ENDHLSL
  }

 }
}

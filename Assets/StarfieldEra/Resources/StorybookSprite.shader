Shader "StarfieldEra/Storybook Sprite"
{
 Properties {[PerRendererData]_MainTex("Painted sprite",2D)="white"{} _Color("Tint",Color)=(1,1,1,1)}
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest" "CanUseSpriteAtlas"="True"}
  Pass
  {
   Tags {"LightMode"="UniversalForwardOnly"}
   Cull Off
   ZWrite On
   HLSLPROGRAM
   #pragma target 4.5
   #pragma multi_compile _ _ADDITIONAL_LIGHTS
   #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
   #pragma vertex vert
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
   #pragma fragment frag
   #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   #include "StorybookLighting.hlsl"
   TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
   CBUFFER_START(UnityPerMaterial)
   half4 _Color;
   CBUFFER_END
   struct A{float4 p:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
   struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;float3 world:TEXCOORD1;};
   V vert(A i){V o;o.world=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.world);o.uv=i.uv;o.color=i.color*_Color;return o;}
   half4 frag(V i):SV_Target{half4 paint=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv)*i.color;clip(paint.a-.12);return half4(StorybookShade(paint.rgb,i.world,half3(0,1,0),i.p,0),1);}
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
   TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);CBUFFER_START(UnityPerMaterial) half4 _Color; CBUFFER_END
   #define STORYBOOK_SPRITE_ALPHA 1
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
   TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);CBUFFER_START(UnityPerMaterial) half4 _Color; CBUFFER_END
   #define STORYBOOK_SPRITE_ALPHA 1
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
   TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);CBUFFER_START(UnityPerMaterial) half4 _Color; CBUFFER_END
   #define STORYBOOK_SPRITE_ALPHA 1
   #include "StorybookSurfacePass.hlsl"
   ENDHLSL
  }
 }
}

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
float3 _LightDirection;float3 _LightPosition;
struct SurfaceA {float4 position:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;half4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
struct SurfaceV {float4 position:SV_POSITION;half3 normal:TEXCOORD0;float2 uv:TEXCOORD1;half alpha:TEXCOORD2;UNITY_VERTEX_OUTPUT_STEREO};
SurfaceV SurfaceVertex(SurfaceA input)
{
 SurfaceV o=(SurfaceV)0;UNITY_SETUP_INSTANCE_ID(input);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
 o.position=TransformObjectToHClip(input.position.xyz);o.normal=TransformObjectToWorldNormal(input.normal);o.uv=input.uv;o.alpha=input.color.a;return o;
}
SurfaceV SurfaceShadowVertex(SurfaceA input)
{
 SurfaceV o=SurfaceVertex(input);float3 world=TransformObjectToWorld(input.position.xyz);
 #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
 float3 direction=normalize(_LightPosition-world);
 #else
 float3 direction=_LightDirection;
 #endif
 o.position=ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(world,o.normal,direction)));return o;
}
void SurfaceClip(SurfaceV i)
{
 #if defined(STORYBOOK_MODEL_ALPHA)
 clip(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv*_BaseMap_ST.xy+_BaseMap_ST.zw).a*_BaseColor.a-_Cutoff);
 #elif defined(STORYBOOK_SPRITE_ALPHA)
 clip(SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).a*_Color.a*i.alpha-.12);
 #endif
}
half4 SurfaceDepthFragment(SurfaceV i):SV_Target {SurfaceClip(i);return 0;}
half4 SurfaceNormalFragment(SurfaceV i):SV_Target
{
 SurfaceClip(i);half3 normal=normalize(i.normal);
 #if defined(_GBUFFER_NORMALS_OCT)
 float2 oct=PackNormalOctQuadEncode(normal);return half4(PackFloat2To888(saturate(oct*.5+.5)),0);
 #else
 return half4(normal,0);
 #endif
}

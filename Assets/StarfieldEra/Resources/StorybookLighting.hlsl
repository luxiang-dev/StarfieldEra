#ifndef STARFIELD_STORYBOOK_LIGHTING
#define STARFIELD_STORYBOOK_LIGHTING
// URP's native lighting path supplies cascaded shadows, clustered point lights and SSAO.
half3 StorybookShade(half3 paint,float3 world,half3 normal,float4 screenPosition,half3 emission)
{
 InputData data=(InputData)0;
 data.positionWS=world;data.normalWS=normalize(normal);data.viewDirectionWS=SafeNormalize(GetWorldSpaceViewDir(world));
 data.shadowCoord=TransformWorldToShadowCoord(world);data.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(screenPosition);
 data.bakedGI=SampleSH(data.normalWS)*.55h+half3(.13,.15,.12);data.shadowMask=half4(1,1,1,1);
 SurfaceData surface=(SurfaceData)0;surface.albedo=paint;surface.alpha=1;surface.occlusion=1;surface.emission=emission;
 half4 lit=UniversalFragmentBlinnPhong(data,surface);
 return paint*.15h+lit.rgb*.75h;
}
#endif

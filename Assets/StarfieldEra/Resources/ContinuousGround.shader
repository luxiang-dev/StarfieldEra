Shader "StarfieldEra/Continuous Ground"
{
 Properties
 {
  _Atlas("Painted grass / dirt / sand / soil",2D)="white"{}
  _GrassVariants("Grass type atlas",2D)="white"{}
  _TileScale("Meters per painted repeat",Float)=7
  _BlendWidth("Base height blend width",Range(.02,.4))=.12
  [Header(Noise Driven Height Blend)]
  _BlendSharpness("Blend sharpness",Range(1,32))=10
  _BlendNoiseStrength("Boundary noise strength",Range(0,.6))=.26
  _BlendMidScale("Medium noise scale (per metre)",Range(.1,8))=2.2
  _BlendFineScale("Fine root noise scale (per metre)",Range(2,30))=13
  _TextureHeightStrength("Painted texture height influence",Range(0,.5))=.13
  [Header(Turf Rim)]
  _TurfEdgeWidth("Turf rim width (metres)",Range(.01,.5))=.16
  _TurfShadowColor("Outside contact shadow tint",Color)=(.35,.29,.18,1)
  _TurfShadowStrength("Outside contact shadow strength",Range(0,1))=.32
  _TurfHighlightColor("Inside warm grass rim",Color)=(.73,.80,.36,1)
  _TurfHighlightStrength("Inside rim highlight strength",Range(0,.6))=.14
  [Header(Painted Grass Strokes)]
  _GrassStrokeScale("Grass clumps per metre",Range(.3,5))=1.65
  _GrassStrokeAspect("Narrow blade aspect (X / Z)",Range(1,4))=2.1
  _GrassStrokeDensity("Grass stroke density",Range(0,1))=.56
  _GrassStrokeStrength("Grass stroke highlight strength",Range(0,1))=.22
  _GrassStrokeColor("Warm blade paint",Color)=(.70,.78,.36,1)
  _GrassRootColor("Grass root shadow tint",Color)=(.38,.46,.25,1)
  _GrassRootStrength("Grass root shadow strength",Range(0,1))=.28
  _GrassWindSpeed("Grass wind speed",Range(0,3))=.65
  _GrassSwayStrength("Grass tip sway",Range(0,.25))=.065
  [Header(Macro Colour And Cloud Shadows)]
  _MacroScale("Macro colour scale (per metre)",Range(.01,.3))=.075
  _MacroStrength("Macro colour strength",Range(0,1))=.24
  _MacroWarmColor("Warm yellow green patches",Color)=(.68,.74,.38,1)
  _MacroCoolColor("Cool emerald patches",Color)=(.32,.61,.48,1)
  _CloudScale("Cloud scale (per metre)",Range(.005,.2))=.035
  _CloudSpeed("Cloud speed (metres per second)",Range(0,2))=.18
  _CloudDirection("Cloud direction (world X Z)",Vector)=(1,.4,0,0)
  _CloudShadowStrength("Cloud shadow strength",Range(0,.6))=.18
  _CloudShadowColor("Cool cloud shadow tint",Color)=(.72,.80,.90,1)
 }
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
  HLSLINCLUDE
  #include "ContinuousGroundInput.hlsl"
  ENDHLSL
  Pass
  {
   Name "ForwardLit" Tags {"LightMode"="UniversalForwardOnly"}
   HLSLPROGRAM
   #pragma target 4.5
   #pragma vertex GroundVertex
   #pragma fragment GroundFragment
   #pragma multi_compile_instancing
   #pragma multi_compile _ _ADDITIONAL_LIGHTS
   #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
   #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   #include "StorybookLighting.hlsl"
   #include "ContinuousGroundArt.hlsl"
   struct GroundAttributes
   {
    float4 positionOS:POSITION;float3 normalOS:NORMAL;float4 color:COLOR;
    float2 biome:TEXCOORD1;float4 varieties:TEXCOORD2;UNITY_VERTEX_INPUT_INSTANCE_ID
   };
   struct GroundVaryings
   {
    float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;
    float4 weights:TEXCOORD2;float2 biome:TEXCOORD3;float4 varieties:TEXCOORD4;
    UNITY_VERTEX_OUTPUT_STEREO
   };
   GroundVaryings GroundVertex(GroundAttributes input)
   {
    GroundVaryings o;UNITY_SETUP_INSTANCE_ID(input);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.positionWS=TransformObjectToWorld(input.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);
    o.normalWS=TransformObjectToWorldNormal(input.normalOS);o.weights=input.color;o.biome=input.biome;o.varieties=input.varieties;return o;
   }
   half4 GroundFragment(GroundVaryings input):SV_Target
   {
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float2 worldXZ=input.positionWS.xz;float2 uv=worldXZ/max(_TileScale,.01);
    // Preserve COLOR grass/dirt/sand/snow and the existing UV1/UV2 biome/grass-variety inputs.
    float4 varieties=max(input.varieties,0);varieties/=max(dot(varieties,1),.0001);
    half3 grass=GroundGrassAtlas(worldXZ/5,float2(0,.5))*varieties.x
               +GroundGrassAtlas(worldXZ/5.8,float2(.5,.5))*varieties.y
               +GroundGrassAtlas(worldXZ/7,float2(0,0))*varieties.z
               +GroundGrassAtlas(worldXZ/8.5,float2(.5,0))*varieties.w;
    half3 dirt=GroundAtlas(uv,float2(.5,.5));half3 sand=GroundAtlas(uv,float2(0,0));
    half3 snow=SRGBToLinear(half3(.80,.86,.87));
    dirt=lerp(dirt,SRGBToLinear(half3(.54,.59,.51)),saturate(input.biome.x)*.8);
    grass*=1+saturate(-input.biome.x)*half3(-.25,-.16,.08);
    // Read texture heights before animation/colour washes so the terrain boundaries stay still.
    float4 heights=float4(dot(grass,half3(.3,.59,.11)),dot(dirt,half3(.3,.59,.11)),dot(sand,half3(.3,.59,.11)),.5);
    float field,presence;
    float4 layers=GroundHeightWeights(input.weights,heights,worldXZ,field,presence);
    grass=GroundMacroVariation(grass,worldXZ);
    grass=GroundGrassStrokes(grass,worldXZ);
    half3 paint=grass*layers.x+dirt*layers.y+sand*layers.z+snow*layers.w;
    float turfShadow,turfHighlight;
    GroundTurfMasks(field,presence,worldXZ,layers,turfShadow,turfHighlight);
    paint*=lerp(half3(1,1,1),_TurfShadowColor.rgb,turfShadow*_TurfShadowStrength);
    paint=lerp(paint,_TurfHighlightColor.rgb,turfHighlight*_TurfHighlightStrength);
    // Preserve existing cold lakebeds/marshes and exposed rock on steep slopes.
    paint*=lerp(1,half3(.66,.80,.78),input.biome.y*.55);
    half3 normal=normalize(input.normalWS);
    float cliff=1-smoothstep(.72,.94,normal.y);
    float strata=.88+.12*smoothstep(.35,.65,frac(input.positionWS.y*3.2+GroundNoise(worldXZ*.7)*.25));
    half3 rock=SRGBToLinear(half3(.49,.48,.39))*strata;
    paint=lerp(paint,rock,cliff);
    half3 lit=StorybookShade(paint,input.positionWS,normal,input.positionCS,0);
    return half4(GroundCloudShadows(lit,worldXZ),1);
   }
   ENDHLSL
  }
  // Own passes replace UsePass so UnityPerMaterial has identical layout everywhere.
  Pass
  {
   Name "ShadowCaster" Tags {"LightMode"="ShadowCaster"}
   ZWrite On Cull Off ColorMask 0
   HLSLPROGRAM
   #pragma target 4.5
   #pragma vertex SurfaceShadowVertex
   #pragma fragment SurfaceDepthFragment
   #pragma multi_compile_instancing
   #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
   #include "StorybookSurfacePass.hlsl"
   ENDHLSL
  }
  Pass
  {
   Name "DepthOnly" Tags {"LightMode"="DepthOnly"}
   ZWrite On Cull Off ColorMask R
   HLSLPROGRAM
   #pragma target 4.5
   #pragma vertex SurfaceVertex
   #pragma fragment SurfaceDepthFragment
   #pragma multi_compile_instancing
   #include "StorybookSurfacePass.hlsl"
   ENDHLSL
  }
  Pass
  {
   Name "DepthNormals" Tags {"LightMode"="DepthNormalsOnly"}
   ZWrite On Cull Off
   HLSLPROGRAM
   #pragma target 4.5
   #pragma vertex SurfaceVertex
   #pragma fragment SurfaceNormalFragment
   #pragma multi_compile_instancing
   #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
   #include "StorybookSurfacePass.hlsl"
   ENDHLSL
  }
 }
}

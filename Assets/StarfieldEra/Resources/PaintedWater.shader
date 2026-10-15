Shader "StarfieldEra/Painted Water"
{
 Properties
 {
  _ShallowColor("Shallow water (alpha = opacity)",Color)=(.29,.67,.54,.32)
  _DeepColor("Deep water (alpha = opacity)",Color)=(.065,.32,.40,.88)
  _DepthMaxDistance("Depth gradient distance (metres)",Range(.1,5))=.65
  _EdgeFade("Transparent shoreline depth (metres)",Range(.01,.5))=.14
  _FoamColor("Foam",Color)=(.91,.97,.85,.85)
  _FoamWidth("Foam reach (metres)",Range(.05,1.5))=.44
  _FoamDistortion("Foam distortion (metres)",Range(0,.3))=.075
  _FoamSpeed("Shoreward foam speed",Range(0,1))=.13
  _CausticsScale("Broad caustics scale",Range(.1,3))=.65
  _CausticsStrength("Soft caustics intensity",Range(0,1))=.18
 }
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
   #pragma fragment frag
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
   #define _SURFACE_TYPE_TRANSPARENT 1
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
   CBUFFER_START(UnityPerMaterial)
   half4 _ShallowColor, _DeepColor, _FoamColor;
   float _DepthMaxDistance, _EdgeFade, _FoamWidth, _FoamDistortion, _FoamSpeed;
   float _CausticsScale, _CausticsStrength;
   CBUFFER_END
   struct A {float4 p:POSITION;float4 color:COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID};
   struct V {float4 p:SV_POSITION;float3 world:TEXCOORD0;float shore:TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO};
   V vert(A i){V o;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.shore=i.color.g;o.world=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.world);return o;}
   float2 random2(float2 p){return frac(sin(float2(dot(p,float2(127.1,311.7)),dot(p,float2(269.5,183.3))))*43758.5453);}
   // Smooth interpolated noise in world space: no texture import or UV seams.
   float noise(float2 p)
   {
    float2 c=floor(p),f=frac(p);f=f*f*(3-2*f);
    return lerp(lerp(random2(c).x,random2(c+float2(1,0)).x,f.x),lerp(random2(c+float2(0,1)).x,random2(c+1).x,f.x),f.y);
   }
   float voronoi(float2 p)
   {
    float2 cell=floor(p),f=frac(p);float nearest=8,second=8;
    for(int y=-1;y<=1;y++)for(int x=-1;x<=1;x++)
    {float2 offset=float2(x,y);float2 q=offset+random2(cell+offset)-f;float d=dot(q,q);if(d<nearest){second=nearest;nearest=d;}else second=min(second,d);}
    return sqrt(second)-sqrt(nearest);
   }
   half4 frag(V i):SV_Target
   {
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
    float2 screenUV=GetNormalizedScreenSpaceUV(i.p);
    float raw=SampleSceneDepth(screenUV);
    // Inverse VP reconstruction handles orthographic/perspective, reversed Z and GL's -1..1 clip depth.
    #if !UNITY_REVERSED_Z
     raw=lerp(UNITY_NEAR_CLIP_VALUE,1,raw);
    #endif
    float3 bottom=ComputeWorldSpacePosition(screenUV,raw,UNITY_MATRIX_I_VP);
    float depthDiff=max(0,i.world.y-bottom.y);
    // Extra contour feathering hides the clipped mesh rim even where the lakebed is already deep.
    float edgeAlpha=smoothstep(0,max(.001,_EdgeFade),depthDiff)*smoothstep(0,max(.02,_EdgeFade),i.shore);
    float gradient=smoothstep(0,max(.001,_DepthMaxDistance),depthDiff);
    half4 water=lerp(_ShallowColor,_DeepColor,gradient);
    float2 uv=i.world.xz;float t=_Time.y;
    float drift=noise(uv*1.15+float2(t*.045,-t*.032));
    float detail=noise(uv*3.4+float2(-t*.022,t*.036));
    float distortedDepth=depthDiff+((drift-.5)*1.5+(detail-.5)*.5)*_FoamDistortion;
    // Two interrupted, soft crests travel toward depth zero and disappear at the bank.
    float phase=distortedDepth/max(.025,_FoamWidth)*12.56637+t*_FoamSpeed*6.283185;
    float crest=smoothstep(.58,.93,sin(phase+drift*1.8));
    float breakup=smoothstep(.24,.66,drift*.65+detail*.35);
    float shore=1-smoothstep(_FoamWidth*.38,_FoamWidth,depthDiff);
    float foam=crest*breakup*shore*edgeAlpha;
    float a=voronoi(uv*_CausticsScale+float2(t*.024,t*.017));
    float b=voronoi(uv*(_CausticsScale*1.43)+float2(-t*.019,t*.028)+17.3);
    float softA=1-smoothstep(.025,.22,a),softB=1-smoothstep(.035,.24,b);
    float caustics=softA*softB*_CausticsStrength*smoothstep(_EdgeFade,_FoamWidth*1.5,depthDiff)*(1-foam);
    water.rgb=lerp(water.rgb,half3(.69,.88,.72),caustics);
    water.rgb=lerp(water.rgb,_FoamColor.rgb,foam*_FoamColor.a);
    Light sun=GetMainLight(TransformWorldToShadowCoord(i.world),i.world,half4(1,1,1,1));
    water.rgb*=lerp(.72,1,sun.shadowAttenuation);
    water.a=lerp(water.a,max(water.a,_FoamColor.a),foam)*edgeAlpha;
    return water;
   }
   ENDHLSL
  }
 }
}

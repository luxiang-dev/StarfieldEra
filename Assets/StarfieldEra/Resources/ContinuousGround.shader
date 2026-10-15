Shader "StarfieldEra/Continuous Ground"
{
 Properties { _Atlas("Painted grass / dirt / sand / soil",2D)="white"{} _GrassVariants("Grass type atlas",2D)="white"{} _TileScale("Meters per repeat",Float)=7 _BlendWidth("Height blend width",Range(.02,.4))=.12 }
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
  Pass
  {
   Tags {"LightMode"="UniversalForwardOnly"}
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
   TEXTURE2D(_Atlas);SAMPLER(sampler_Atlas);
   TEXTURE2D(_GrassVariants);SAMPLER(sampler_GrassVariants);
   CBUFFER_START(UnityPerMaterial)
   float4 _Atlas_ST;float _TileScale,_BlendWidth;
   CBUFFER_END
   struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;float4 color:COLOR;float2 biome:TEXCOORD1;float4 varieties:TEXCOORD2;};
   struct V {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;half3 normal:TEXCOORD1;float4 weights:TEXCOORD2;float2 biome:TEXCOORD3;float4 varieties:TEXCOORD4;};
   V vert(A i){V o;o.world=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);o.normal=TransformObjectToWorldNormal(i.normalOS);o.weights=i.color;o.biome=i.biome;o.varieties=i.varieties;return o;}
   float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float noise(float2 p){float2 a=floor(p),b=frac(p);b=b*b*(3-2*b);return lerp(lerp(hash(a),hash(a+float2(1,0)),b.x),lerp(hash(a+float2(0,1)),hash(a+1),b.x),b.y);}
   half3 tile(float2 uv,float2 quadrant){return SAMPLE_TEXTURE2D_GRAD(_Atlas,sampler_Atlas,quadrant+(.006+frac(uv)*.488),ddx(uv)*.488,ddy(uv)*.488).rgb;}
   half3 grassTile(float2 uv,float2 quadrant)
   {return SAMPLE_TEXTURE2D_GRAD(_GrassVariants,sampler_GrassVariants,quadrant+(.006+frac(uv)*.488),ddx(uv)*.488,ddy(uv)*.488).rgb;}
   float stroke(float2 p,float2 a,float2 b)
   {float2 d=b-a;float t=saturate(dot(p-a,d)/dot(d,d));return length(p-a-d*t);}
   half4 frag(V i):SV_Target
   {
    float2 uv=i.world.xz/_TileScale;
    float4 varieties=max(0,i.varieties);varieties/=max(dot(varieties,1),.0001);
    half3 dryGrass=grassTile(i.world.xz/5,float2(0,.5));
    half3 shortGrass=grassTile(i.world.xz/5.8,float2(.5,.5));
    half3 denseGrass=grassTile(i.world.xz/7,float2(0,0));
    half3 tallGrass=grassTile(i.world.xz/8.5,float2(.5,0));
    half3 grass=dryGrass*varieties.x+shortGrass*varieties.y+denseGrass*varieties.z+tallGrass*varieties.w;
    half3 dirt=tile(uv,float2(.5,.5));
    half3 sand=tile(uv,float2(0,0));
    half3 snow=SRGBToLinear(half3(.80,.86,.87));
    // Nested grass patches give large areas distinct painted masses rather than one flat fill.
    float patch=noise(i.world.xz*.23+float2(13,7))+.24*noise(i.world.xz*.075);
    float meadowPatch=smoothstep(.52,.61,patch);
    grass=lerp(grass,SRGBToLinear(half3(.55,.66,.34)),meadowPatch*.16);
    float shadePatch=smoothstep(.3,.4,noise(i.world.xz*.15+31));
    grass=lerp(grass,SRGBToLinear(half3(.38,.52,.28)),(1-shadePatch)*.18);
    // Quiet painted washes, leaving readable silhouettes for foliage and buildings.

    dirt=lerp(dirt,SRGBToLinear(half3(.54,.59,.51)),saturate(i.biome.x)*.8);
    grass*=1+saturate(-i.biome.x)*half3(-.25,-.16,.08);
    // Luminance is the painted texture's height proxy; high frequency noise bites into edges.
    float4 height=float4(dot(grass,half3(.3,.59,.11)),dot(dirt,half3(.3,.59,.11)),dot(sand,half3(.3,.59,.11)),.7);
    height+=float4(noise(i.world.xz*9),noise(i.world.zx*13+8),noise(i.world.xz*11+24),0)*.23;
    float4 scores=i.weights+height*.28;
    float highest=max(max(scores.x,scores.y),max(scores.z,scores.w));
    float4 w=max(0,scores-highest+_BlendWidth)*i.weights;w/=max(dot(w,1),.0001);
    half3 paint=grass*w.x+dirt*w.y+sand*w.z+snow*w.w;
    // A narrow dark grassy lip and a warm bare-earth fringe separate adjacent terrain masses.
    float landEdge=w.x-w.y-w.z;
    float lip=(1-smoothstep(.02,.16,abs(landEdge)))*saturate(i.weights.x*5);
    paint*=1-lip*.22;
    float fringe=(1-smoothstep(.15,.4,abs(landEdge)))*step(landEdge,0)*saturate(i.weights.x*4);
    paint=lerp(paint,SRGBToLinear(half3(.66,.56,.36)),fringe*.18);
    float2 cell=floor(i.world.xz*.9),p=frac(i.world.xz*.9)-.5;
    float blades=min(stroke(p,float2(0,-.12),float2(-.11,.06)),min(stroke(p,float2(0,-.12),float2(.015,.14)),stroke(p,float2(0,-.12),float2(.12,.03))));
    float density=lerp(.78,.5,saturate(lip+abs(meadowPatch-.5)*.4));
    float ink=(1-smoothstep(.008,.024,blades))*step(density,hash(cell))*w.x;
    paint=lerp(paint,grass*.7,ink*.20);
    paint*=lerp(1,half3(.66,.80,.78),i.biome.y*.55);
    half3 n=normalize(i.normal);
    // Steep slopes expose layered ochre-gray rock beneath the grassy terrace.
    float cliff=1-smoothstep(.72,.94,n.y);
    float strata=.88+.12*smoothstep(.35,.65,frac(i.world.y*3.2+noise(i.world.xz*.7)*.25));
    half3 rock=SRGBToLinear(half3(.49,.48,.39))*strata;
    paint=lerp(paint,rock,cliff);
    return half4(StorybookShade(paint,i.world,n,i.positionCS,0),1);
   }
   ENDHLSL
  }
  UsePass "StarfieldEra/Opaque Surface Passes/DepthOnly"
  UsePass "StarfieldEra/Opaque Surface Passes/ShadowCaster"
  UsePass "StarfieldEra/Opaque Surface Passes/DepthNormals"
 }
}

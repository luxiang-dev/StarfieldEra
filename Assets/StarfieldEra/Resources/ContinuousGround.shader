Shader "StarfieldEra/Continuous Ground"
{
 Properties { _Atlas("Painted grass / dirt / sand / soil",2D)="white"{} _TileScale("Meters per repeat",Float)=5 _BlendWidth("Height blend width",Range(.02,.4))=.12 }
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
  Pass
  {
   Tags {"LightMode"="UniversalForwardOnly"}
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile_fragment _ _SHADOWS_SOFT
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   TEXTURE2D(_Atlas);SAMPLER(sampler_Atlas);
   CBUFFER_START(UnityPerMaterial)
   float4 _Atlas_ST;float _TileScale,_BlendWidth;
   CBUFFER_END
   struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;float4 color:COLOR;float2 biome:TEXCOORD1;};
   struct V {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;half3 normal:TEXCOORD1;float4 weights:TEXCOORD2;float2 biome:TEXCOORD3;};
   V vert(A i){V o;o.world=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);o.normal=TransformObjectToWorldNormal(i.normalOS);o.weights=i.color;o.biome=i.biome;return o;}
   float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float noise(float2 p){float2 a=floor(p),b=frac(p);b=b*b*(3-2*b);return lerp(lerp(hash(a),hash(a+float2(1,0)),b.x),lerp(hash(a+float2(0,1)),hash(a+1),b.x),b.y);}
   half3 tile(float2 uv,float2 quadrant){return SAMPLE_TEXTURE2D_GRAD(_Atlas,sampler_Atlas,quadrant+(.006+frac(uv)*.488),ddx(uv)*.488,ddy(uv)*.488).rgb;}
   float stroke(float2 p,float2 a,float2 b)
   {float2 d=b-a;float t=saturate(dot(p-a,d)/dot(d,d));return length(p-a-d*t);}
   half4 frag(V i):SV_Target
   {
    float2 uv=i.world.xz/_TileScale;
    half3 grass=SRGBToLinear(half3(.46,.59,.30))*(.94+noise(i.world.xz*.32)*.12);
    half3 dirt=SRGBToLinear(half3(.73,.61,.42))*(.95+noise(i.world.xz*.45+8)*.1);
    half3 sand=SRGBToLinear(half3(.83,.76,.55))*(.96+noise(i.world.xz*.4+24)*.08);
    half3 snow=SRGBToLinear(half3(.80,.86,.87));
    // Quiet painted washes, leaving readable silhouettes for foliage and buildings.
    grass*=.96+dot(tile(uv,float2(0,.5)),half3(.3,.59,.11))*.08;
    dirt=lerp(dirt,SRGBToLinear(half3(.54,.59,.51)),saturate(i.biome.x)*.8);
    grass*=1+saturate(-i.biome.x)*half3(-.25,-.16,.08);
    // Luminance is the painted texture's height proxy; high frequency noise bites into edges.
    float4 height=float4(dot(grass,half3(.3,.59,.11)),dot(dirt,half3(.3,.59,.11)),dot(sand,half3(.3,.59,.11)),.7);
    height+=float4(noise(i.world.xz*9),noise(i.world.zx*13+8),noise(i.world.xz*11+24),0)*.23;
    float4 scores=i.weights+height*.28;
    float highest=max(max(scores.x,scores.y),max(scores.z,scores.w));
    float4 w=max(0,scores-highest+_BlendWidth)*i.weights;w/=max(dot(w,1),.0001);
    half3 paint=grass*w.x+dirt*w.y+sand*w.z+snow*w.w;
    float2 cell=floor(i.world.xz*.9),p=frac(i.world.xz*.9)-.5;
    float blades=min(stroke(p,float2(0,-.12),float2(-.11,.06)),min(stroke(p,float2(0,-.12),float2(.015,.14)),stroke(p,float2(0,-.12),float2(.12,.03))));
    float ink=(1-smoothstep(.008,.024,blades))*step(.78,hash(cell))*w.x;
    paint=lerp(paint,grass*.7,ink*.65);
    paint*=lerp(1,half3(.66,.80,.78),i.biome.y*.55);
    half3 n=normalize(i.normal);Light sun=GetMainLight(TransformWorldToShadowCoord(i.world));
    // Steep slopes expose layered ochre-gray rock beneath the grassy terrace.
    float cliff=1-smoothstep(.72,.94,n.y);
    float strata=.88+.12*smoothstep(.35,.65,frac(i.world.y*3.2+noise(i.world.xz*.7)*.25));
    half3 rock=SRGBToLinear(half3(.49,.48,.39))*strata;
    paint=lerp(paint,rock,cliff);
    half shade=(.82+.18*saturate(dot(n,sun.direction)))*lerp(.73,1,sun.shadowAttenuation);
    half3 light=half3(shade,shade,shade);
    return half4(paint*light,1);
   }
   ENDHLSL
  }
  UsePass "Universal Render Pipeline/Lit/DepthOnly"
  UsePass "Universal Render Pipeline/Lit/ShadowCaster"
 }
}

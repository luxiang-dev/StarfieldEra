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
   half4 frag(V i):SV_Target
   {
    float2 uv=i.world.xz/_TileScale;
    half3 grass=tile(uv,float2(0,.5)),dirt=tile(uv,float2(.5,.5)),sand=tile(uv,float2(0,0));
    grass=lerp(half3(.36,.49,.23),grass,.55);
    half3 snow=lerp(half3(.77,.83,.84),half3(.96,.97,.89),noise(i.world.xz*3));
    dirt=lerp(dirt,dot(dirt,half3(.299,.587,.114))*half3(.95,1.02,.99),saturate(i.biome.x)*.8);
    grass*=1+saturate(-i.biome.x)*half3(-.55,-.24,-.05);
    // Luminance is the painted texture's height proxy; high frequency noise bites into edges.
    float4 height=float4(dot(grass,half3(.3,.59,.11)),dot(dirt,half3(.3,.59,.11)),dot(sand,half3(.3,.59,.11)),.7);
    height+=float4(noise(i.world.xz*9),noise(i.world.zx*13+8),noise(i.world.xz*11+24),0)*.23;
    float4 scores=i.weights+height*.28;
    float highest=max(max(scores.x,scores.y),max(scores.z,scores.w));
    float4 w=max(0,scores-highest+_BlendWidth)*i.weights;w/=max(dot(w,1),.0001);
    half3 paint=grass*w.x+dirt*w.y+sand*w.z+snow*w.w;
    paint*=lerp(1,half3(.56,.76,.71),i.biome.y*.8);
    Light sun=GetMainLight(TransformWorldToShadowCoord(i.world));half3 n=normalize(i.normal);
    half3 light=SampleSH(n)+sun.color*(.2+.8*saturate(dot(n,sun.direction)))*sun.shadowAttenuation;
    return half4(paint*light,1);
   }
   ENDHLSL
  }
  UsePass "Universal Render Pipeline/Lit/DepthOnly"
 }
}

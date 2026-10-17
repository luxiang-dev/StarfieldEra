Shader "StarfieldEra/Drainage Flow"
{
 Properties {_Flow("Active flow",Range(0,1))=0}
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+6"}
  Pass
  {
   Tags {"LightMode"="UniversalForwardOnly"}
   Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial) float _Flow; CBUFFER_END
   struct A {float4 p:POSITION;float2 uv:TEXCOORD0;};struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;};
   V vert(A i){V o;o.world=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.world);o.uv=i.uv;return o;}
   half4 frag(V i):SV_Target
   {
    clip(_Flow-.01);float cross=sin(i.uv.x*3.14159);float phase=i.uv.y*16-_Time.y*3;
    float streak=pow(saturate(sin(phase+sin(i.world.x*13+i.world.z*9)*.2)),12)*pow(cross,2);
    float glint=pow(saturate(sin(i.world.x*21+i.world.z*27-_Time.y*4)),18)*.2;
    return half4(lerp(half3(.13,.38,.31),half3(.61,.79,.67),streak*.25+glint),(.66+streak*.25)*smoothstep(0,.15,cross)*_Flow);
   }
   ENDHLSL
  }
 }
}
